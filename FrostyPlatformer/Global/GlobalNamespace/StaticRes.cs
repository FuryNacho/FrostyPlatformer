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
        // Sätt till true för att ladda transparenta tilesheets (parallax-experiment).
        // Byt tillbaka till false för att använda ordinarie tilesheets.
        // Alla SpriteRef.TileSheet*-referenser uppdateras automatiskt — ingen annan kod behöver ändras.
        private const bool UseTransparentTileSheets = true;

        // Tilesheet per säsong — static readonly för att tillåta villkorlig initiering från flaggan ovan.
        public static readonly string TileSheetSpring  = UseTransparentTileSheets ? "tilesheet_transparent_spring"  : "tilesheetspring";
        public static readonly string TileSheetSummer  = UseTransparentTileSheets ? "tilesheet_transparent_summer"  : "tilesheetsummer";
        public static readonly string TileSheetFall    = UseTransparentTileSheets ? "tilesheet_transparent_fall"    : "tilesheetfall";
        public static readonly string TileSheetWinter  = UseTransparentTileSheets ? "tilesheet_transparent_winter"  : "tilesheetwinter";
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

    /// <summary>Mapp-segment för resurssökvägar — används med Path.Combine.</summary>
    public static class MapPath
    {
        public const string Resources = "Resources";
        public const string Assets    = "Assets";
        public const string MapData   = "MapData";
        public const string Tiled     = "Tiled";
        public const string UserMaps  = "UserMaps";
        public const string Sprites   = "Sprites";
        public const string Settings  = "Settings";
        public const string Sound     = "Sound";
    }

    /// <summary>Fil-extensions som återkommer på flera ställen.</summary>
    public static class FileExt
    {
        public const string Png  = ".png";
        public const string Json = ".json";
    }

    /// <summary>Fil-namn (utan extension) för persistent data i Settings-mappen.</summary>
    public static class DataFile
    {
        public const string Settings  = @"\settings";
        public const string HighScore = @"\highscore";
    }

}
