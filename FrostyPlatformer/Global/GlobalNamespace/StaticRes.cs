#nullable enable
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
        public const string TileSheetCustom   = "tilesheet_transparent_Custom";

        // Boss-arenans tileset. Inte säsongsberoende och påverkas inte av
        // UseTransparentTileSheets — slutboss-banan (mapten) har ett eget kallt ark.
        public const string TileSheetBoss     = "tilesheet_boss";

        /// <summary>
        /// Mappar en kartas TilesetSource (.tsx-filnamn) till rätt transparenta
        /// tilesheet-sprite. Okänd eller saknad källa faller tillbaka på custom-tileseeten.
        /// </summary>
        public static string TileSheetForTileset(string? tilesetSource)
        {
            string key = (tilesetSource ?? "").Trim().ToLowerInvariant().Replace(".tsx", "");
            return key switch
            {
                "tilesheetspring" => TileSheetSpring,
                "tilesheetsummer" => TileSheetSummer,
                "tilesheetfall"   => TileSheetFall,
                "tilesheetwinter" => TileSheetWinter,
                "tilesheetcustom" => TileSheetCustom,
                "tilesheetboss"   => TileSheetBoss,
                "tilesheetwm"     => TileSheetWorldMap,
                _                 => TileSheetCustom,
            };
        }

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

        // Slutboss-sprites (placeholder-art; byts mot riktig pixelart i samma rutnät).
        public const string MirrorScarlet = "mirror_scarlet";
        public const string SwarmCopy     = "swarm_copy";
        // Slutbossens akt 3 (jätten) byggs av flera ark — ett per kroppsdel/roll, så att de
        // senare kan bytas mot riktig pixelart oberoende av varandra.
        public const string GiantBody      = "giant_body";       // torso + huvud-uttryck + kärna + fötter
        public const string GiantArm       = "giant_arm";        // arm/näve slam-frames
        public const string GiantWeakPoint = "giant_weakpoint";  // svagpunktens 5 states
        public const string GiantHazard    = "giant_hazard";     // istapp/krasch/varningsmarkör

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

        // Slutboss-SFX (syntetiserade av Tools/generate_sfx.py — se det scriptet).
        public const string SlamImpact      = "slam_impact.wav";   // jättens näve i marken (akt 3)
        public const string SlamHammer      = "slam_hammer.wav";   // tyngre variant: hammarslaget (camp-brytaren)
        public const string PoffGlitch      = "poff_glitch.wav";   // digital poff/glitch (akt 4-final m.m.)
        public const string ActSting1       = "act_sting1.wav";    // akt-övergång 1→2 (spegel → svärm)
        public const string ActSting2       = "act_sting2.wav";    // akt-övergång 2→3 (svärm → jätte)
        public const string ActSting3       = "act_sting3.wav";    // akt-övergång 3→4 (jätte → acceptans)
        // Slutboss-SFX skiva 2.
        public const string IceWhoosh       = "ice_whoosh.wav";      // istapp faller (akt 3)
        public const string IceShatter      = "ice_shatter.wav";     // istapp krossas mot marken
        public const string PoffSmall       = "poff_small.wav";      // svärm-kopia poffar (akt 2)
        public const string GlitchDissolve  = "glitch_dissolve.wav"; // spegel-Scarlets glitch-exit (akt 1→2)
        public const string GiantCollapse   = "giant_collapse.wav";  // jätten rasar (akt 3→4)
        public const string GiantArrive     = "giant_arrive.wav";    // jätten materialiseras/portal (akt 2→3)

        public const string BGSoundWorld    = "uno.wav";
        public const string BGSoundGame     = "theone.wav";
        public const string BGSoundFinalStage = "bossong.wav";
        public const string BGSoundAcceptance = "acceptance.wav"; // akt 4 (Acceptans): lugnt ambient C-dur-tema — parallelldur till bossong
        public const string BGSoundEnd      = "theend.wav";
        public const string BGNearPerfectEnd = "finalend.wav";
        public const string BGPerfectEnd    = "Caveman.wav";

        // Level editor-musik. Spelas bara i editorn, med lägre volym än spelmusiken,
        // och sekvenseras av EditorMusicSequencer (main, main, middle, loop).
        public const string EditorMusicMain   = "arctic_workshop.wav";
        public const string EditorMusicMiddle = "arctic_workshop_middle_act.wav";
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
        public const string MapTen   = "mapten";
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
