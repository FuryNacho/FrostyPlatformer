#nullable enable
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.Global.GlobalNamespace
{
    /// <summary>
    /// Centrala utvecklingsflaggor — ett enda ställe att slå av och på dev-genvägar.
    /// Allt här är medvetna hack för att underlätta utveckling; ingen spellogik ska
    /// byggas på dessa flaggor, de bara påverkar vilka värden som matas in i ytterkanten.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Static configuration (global dev gate)
    ///
    /// MOTIVERING:
    /// Tidigare låg dev-flaggorna utspridda (t.ex. EditorState.DevMode). Att samla dem
    /// i en static class gör dem enkla att hitta och flippa under utveckling utan att
    /// leta i koden. Static (inte singleton) räcker: rena flaggor utan livscykel eller
    /// tillstånd. Mutabel static bool slipper också CS0162 för gatad kod.
    ///
    /// ANVÄNDNING:
    /// Sätts nära entrypoint (Program.cs) under utveckling. Default false = release-läge.
    /// Läses endast i ytterkanten (call-sites), aldrig inuti testbar logik.
    /// </remarks>
    public static class DevConfig
    {
        /// <summary>
        /// True aktiverar världskarte-editering i level editorn: StopPoint-läge,
        /// världskartans egen tilesheet och worldmap-raden i kartväljaren.
        /// Dolt för vanliga spelare när false.
        /// </summary>
        public static bool WorldMapEditor = false;

        /// <summary>
        /// True låser upp alla banor på världskartan så man kan gå direkt till slutbossen.
        /// Spelaren kan röra sig fritt förbi annars låsta noder.
        /// </summary>
        public static bool UnlockAllStages = false;

        /// <summary>
        /// True gör att ett nytt spel startar med full hälsa (HeroEnergi = MaxHealth, visas
        /// som "%" i HUD:en) istället för standardstartens 7. Sparar in att tanka upp hälsa
        /// innan man speltestar slutbossen. Sätts i Reset().
        /// </summary>
        public static bool FullEnergy = false;

        /// <summary>
        /// Vilken akt slutbossen ska STARTA i — så man kan speltesta en senare akt direkt
        /// utan att först klara de tidigare. <see cref="BossAct.Mirror"/> (=normalt) kör hela
        /// striden från början. Övriga värden hoppar in mitt i: Swarm (akt 2), Giant (akt 3),
        /// Acceptance (akt 4). Läses när BossPhaseController skapas i GameplayState.Enter.
        /// </summary>
        public static BossAct BossStartAct = BossAct.Mirror;
    }
}
