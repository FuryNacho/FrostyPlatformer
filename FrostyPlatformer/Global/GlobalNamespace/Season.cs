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

        /// <summary>Användarbyggda banor (slot1–slot7): mörk nattbakgrund med snöstämning.</summary>
        Custom,

        /// <summary>Slutboss-arenan (mapten): mörk natthimmel med snöklädda fjäll-siluetter.</summary>
        Boss,
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
            MapName.MapTen                                                   => Season.Boss,
            _ when mapName != null && mapName.StartsWith("slot")            => Season.Custom,
            _                                                                => null,
        };

        /// <summary>
        /// Returnerar årstiden för en kartas valda tileset (.tsx-filnamn).
        /// Används för användarbanor där parallaxen ska matcha vald tilesheet,
        /// inte kartnamnet. Okänd källa faller tillbaka på <see cref="Season.Custom"/>.
        /// </summary>
        public static Season FromTilesetSource(string? tilesetSource)
        {
            string key = (tilesetSource ?? "").Trim().ToLowerInvariant().Replace(".tsx", "");
            return key switch
            {
                "tilesheetspring" => Season.Spring,
                "tilesheetsummer" => Season.Summer,
                "tilesheetfall"   => Season.Fall,
                "tilesheetwinter" => Season.Winter,
                _                 => Season.Custom,
            };
        }
    }
}
