#nullable enable

namespace FrostyPlatformer.Global.GlobalNamespace
{
    /// <summary>
    /// Spelets fyra årstider — styr val av parallax-bakgrundslager per karta.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Value Object (enum)
    ///
    /// MOTIVERING:
    /// Varje karta tillhör en årstid. Årstiden avgör vilka parallax-lager som aktiveras
    /// i GameplayState.Draw(). Kopplingen mellan karta och årstid är statisk (definieras
    /// i MapName-konventionen) och kan slås upp utan att ladda kartdata.
    ///
    /// ANVÄNDNING:
    /// SeasonHelper.FromMapName(mapName) returnerar korrekt årstid i GameplayState.Enter().
    /// ParallaxSystem.SetSeason() väljer aktiva lager baserat på returnerat värde.
    /// </remarks>
    public enum Season
    {
        /// <summary>Banor 1–2 (mapone, maptwo): gröna landskap och blommor.</summary>
        Spring,

        /// <summary>Banor 3–4 (mapthree, mapfour): lummig och varm sommarnatur.</summary>
        Summer,

        /// <summary>Banor 5–6 (mapfive, mapsix): orange och röda höstfärger.</summary>
        Fall,

        /// <summary>Banor 7–9 (mapseven, mapeight, mapnine): snö, is och vintermörker.</summary>
        Winter,
    }

    /// <summary>
    /// Mappar kartnamn till årstid för parallax-bakgrundsval.
    /// </summary>
    public static class SeasonHelper
    {
        /// <summary>
        /// Returnerar årstiden för angiven karta, eller null för kartor utan parallax
        /// (t.ex. världskartan och okända kartnamn).
        /// </summary>
        /// <param name="mapName">Kartnamnet från <see cref="MapName"/>-konstanterna.</param>
        /// <returns>Årstid, eller null om kartan saknar parallax-bakgrund.</returns>
        public static Season? FromMapName(string? mapName) => mapName switch
        {
            MapName.MapOne   or MapName.MapTwo                              => Season.Spring,
            MapName.MapThree or MapName.MapFour                             => Season.Summer,
            MapName.MapFive  or MapName.MapSix                              => Season.Fall,
            MapName.MapSeven or MapName.MapEight or MapName.MapNine         => Season.Winter,
            _                                                                => null,
        };
    }
}
