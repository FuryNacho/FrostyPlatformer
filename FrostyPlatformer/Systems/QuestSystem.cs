#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Commands;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models.Objects;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Implementerar IQuestSystem med GameContext som lagringsbackend.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Stateful Service (med Blackboard som lagringsbackend)
    ///
    /// MOTIVERING:
    /// Quest-listan bor på GameContext.ActiveQuests så att alla spellägen kan
    /// läsa den utan direkt beroende på QuestSystem. QuestSystem äger enbart
    /// mutationerna (Add, PopulateForMap) och exponerar ActiveQuests som
    /// IReadOnlyList för att förhindra oavsiktliga sidoeffekter utifrån.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() innan det första ChangeMap-anropet:
    ///   _questSystem = new QuestSystem(_context);
    /// Injiceras i GameServices.Quests för framtida state-åtkomst.
    /// </remarks>
    public class QuestSystem : IQuestSystem
    {
        private readonly GameContext _context;

        /// <summary>
        /// Skapar en QuestSystem som läser och skriver till context.ActiveQuests.
        /// </summary>
        public QuestSystem(GameContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Quest> ActiveQuests => _context.ActiveQuests;

        /// <inheritdoc/>
        public void Add(Quest quest)
        {
            // Prepend: nyast quest hamnar först — matchad med original Program.AddQuest-logik
            var updated = new List<Quest> { quest };
            foreach (var q in _context.ActiveQuests)
                updated.Add(q);
            _context.ActiveQuests = updated;
        }

        /// <inheritdoc/>
        public void PopulateForMap(List<DynamicGameObject> objects, string mapName)
        {
            foreach (var q in _context.ActiveQuests)
                q.PopulateDynamics(objects, mapName);
        }
    }
}
