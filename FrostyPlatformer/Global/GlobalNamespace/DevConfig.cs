#nullable enable
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
    }
}
