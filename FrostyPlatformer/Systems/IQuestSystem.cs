#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Commands;
using FrostyPlatformer.Models.Objects;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Hanterar spelets aktiva quests: lägger till nya och populerar kartladdningar.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion
    ///
    /// MOTIVERING:
    /// Quest-listan hanterades direkt av Program via ListQuests-wrappern på
    /// GameContext.ActiveQuests. Ny quest-logik (prepend-lista) och
    /// PopulateDynamics-loopen vid kartbyte låg som inline-kod i Program.cs.
    /// IQuestSystem samlar dessa ansvarsområden bakom ett testbart interface.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new QuestSystem(context) och injiceras
    /// via GameServices.Quests. Program.AddQuest() delegerar till Add().
    /// Program.ChangeMap() delegerar till PopulateForMap() istället för
    /// att iterera ActiveQuests inline.
    /// </remarks>
    public interface IQuestSystem
    {
        /// <summary>Aktiva quests i prioritetsordning (nyast först).</summary>
        IReadOnlyList<Quest> ActiveQuests { get; }

        /// <summary>
        /// Lägger till ett quest högst upp i listan. Nyast quest prioriteras
        /// när aktiva quests processas.
        /// </summary>
        void Add(Quest quest);

        /// <summary>
        /// Anropar PopulateDynamics på alla aktiva quests för angiven karta.
        /// Kallas av Program.ChangeMap() efter att kartan är laddad.
        /// </summary>
        void PopulateForMap(List<DynamicGameObject> objects, string mapName);
    }
}
