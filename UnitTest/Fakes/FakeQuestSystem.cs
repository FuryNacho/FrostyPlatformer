#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Commands;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IQuestSystem. Spårar Add- och PopulateForMap-anrop
    /// utan att röra GameContext.
    /// </summary>
    public class FakeQuestSystem : IQuestSystem
    {
        private readonly List<Quest> _quests = new List<Quest>();

        public IReadOnlyList<Quest> ActiveQuests => _quests;

        /// <summary>Antal gånger PopulateForMap() anropades.</summary>
        public int PopulateCallCount { get; private set; }

        /// <summary>Senaste kartnamn som skickades till PopulateForMap().</summary>
        public string? LastMapName { get; private set; }

        public void Add(Quest quest) => _quests.Insert(0, quest);

        public void PopulateForMap(List<DynamicGameObject> objects, string mapName)
        {
            PopulateCallCount++;
            LastMapName = mapName;
        }
    }
}
