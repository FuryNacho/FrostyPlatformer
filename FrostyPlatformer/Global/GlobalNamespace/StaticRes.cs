namespace FrostyPlatformer.Global.GlobalNamespace
{
    /// <summary>
    /// Nycklar för sprites laddade via Aggregate.GetSpritePath().
    /// Varje konstant matchar exakt det friendly-name som registrerades i
    /// Aggregate.LoadSprites() — en stavning, en definition, noll magic strings.
    /// Obs: splash-sprites hanteras av SplashScreenRef (Start/End).
    /// </summary>
    public static class SpriteRef
    {
        // Tilesheet per säsong
        public const string TileSheetSpring   = "tilesheetspring";
        public const string TileSheetSummer   = "tilesheetsummer";
        public const string TileSheetFall     = "tilesheetfall";
        public const string TileSheetWinter   = "tilesheetwinter";
        public const string TileSheetWorldMap = "tilesheetwm";

        // Karaktärer och UI
        public const string Font  = "font";
        public const string Hero  = "hero";
        public const string Items = "items";

        // Fiender
        public const string EnemyIcicle  = "enemyzero";
        public const string EnemyPenguin = "enemyone";
        public const string EnemyWalrus  = "enemytwo";
        public const string EnemyFrost   = "enemythree";
        public const string EnemyBoss    = "enemyboss";
        public const string EnemyWind    = "enemywind";

        // Slutscener
        public const string EndArt = "endart";
    }

    /// <summary>Nycklar för items laddade via Aggregate.GetItem().</summary>
    public static class ItemRef
    {
        public const string Energi = "energi";
    }

    public static class SoundRef
    {
        public const string Jump            = "PAA hopp.wav";
        public const string Land            = "PAA landa.wav";
        public const string Damage          = "PAA hoppa pa krak.wav";
        public const string DamageHero      = "PAA traffljud.wav";
        public const string PickUp          = "PAA PLOCKA UPP.wav";

        public const string BGSoundWorld    = "uno.wav";
        public const string BGSoundGame     = "theone.wav";
        public const string BGSoundFinalStage = "bossong.wav";
        public const string BGSoundEnd      = "theend.wav";
        public const string BGNearPerfectEnd = "finalend.wav";
        public const string BGPerfectEnd    = "Caveman.wav";
    }

    public static class MapName
    {
        public const string WorldMap = "worldmap";
        public const string MapOne   = "mapone";
        public const string MapTwo   = "maptwo";
        public const string MapThree = "mapthree";
        public const string MapFour  = "mapfour";
        public const string MapFive  = "mapfive";
        public const string MapSix   = "mapsix";
        public const string MapSeven = "mapseven";
        public const string MapEight = "mapeight";
        public const string MapNine  = "mapnine";
    }

    public static class SplashScreenRef
    {
        public const string Start = "splashstart";
        public const string End   = "splashend";
    }

    //  string tiledMapPath = Path.Combine(ReadWrite.GetRoot, "Resources", "Assets", "MapData", "Tiled");
    public static class MapPath
    {
        public const string Resources = "Resources";
        public const string Assets = "Assets";
        public const string MapData = "MapData";
        public const string Tiled = "Tiled";

    }

}
