namespace FrostyPlatformer.Global
{
    /// <summary>
    /// Samlade spelkonstanter för Penguin After All.
    /// Ersätter magic numbers utspridda i kodbasen.
    /// </summary>
    public static class GameConstants
    {
        // ─────────────────────────────────────────────
        // Skärm och rendering
        // ─────────────────────────────────────────────
        public const int ScreenWidth       = 256;
        public const int ScreenHeight      = 224;
        public const int PixelWidth        = 4;
        public const int PixelHeight       = 4;
        public const int FrameRate         = -1;   // -1 = okappat

        public const int TileSize          = 16;   // Tile-storlek i pixlar
        public const int TileSheetColumns  = 5;    // Antal kolumner i tile-spritesheet


        // ─────────────────────────────────────────────
        // Fysik – gravitation och rörelse
        // ─────────────────────────────────────────────

        // Tre gravitationsstyrkor beroende på spelläge:
        public const float GravityPowerJump         = 17.0f;   // Hjälten i BPower-läge med hoppminne
        public const float GravityNormal            = 20.0f;   // Normal gravitation
        public const float GravityHeavy             = 21.0f;   // Tyngre gravitation (ej BPower)

        // Fall-hastighetsgränser:
        public const float FallSpeedThreshold       = 20.0f;   // Gräns för "faller snabbt"-tillstånd
        public const float FallSpeedMax             = 25.0f;   // Absolut fallhastighetstak (triggers detHarBallatUr)

        public const float JumpVelocity             = -9.3f;
        public const float JumpDamageRebound        = -8.5f;   // Studs uppåt vid hoppspark på fiende
        public const float EnemyJumpVelocity        = -7.7f;

        public const float MoveAccelerationGround   = 25.0f;
        public const float MoveAccelerationAir      = 15.0f;

        public const float MaxSpeedNormal           = 6.0f;
        public const float MaxSpeedPower            = 10.0f;

        public const float CollisionBorderPrecision = 0.000000005f;


        // ─────────────────────────────────────────────
        // Hopp – buffer och coyote time
        // ─────────────────────────────────────────────
        public const int JumpBufferFrames  = 5;    // Antal frames hoppet "minns" knapptryckning


        // ─────────────────────────────────────────────
        // Animationstider (sekunder)
        // ─────────────────────────────────────────────
        public const float AnimFrameSlow1  = 0.3f;
        public const float AnimFrameSlow2  = 0.6f;
        public const float AnimFrameSlow3  = 0.9f;

        public const float AnimFrameFast1  = 0.1f;
        public const float AnimFrameFast2  = 0.2f;
        public const float AnimFrameFast3  = 0.4f;

        public const float TextTypeSpeed   = 0.2f; // Tid per bokstav vid textanimation


        // ─────────────────────────────────────────────
        // Spellogik och regler
        // ─────────────────────────────────────────────
        public const int  SplashScreenDuration = 60;     // Frames splash-skärmen visas
        public const int  IdleTimeout          = 200;    // Frames innan idle-animering
        public const int  ObjectCleanupCount   = 4;      // RemoveCount-tröskel för objektborttagning
        public const int  PerfectEndingHealth  = 100;    // Hälsotröskel för perfekt slut


        // ─────────────────────────────────────────────
        // Världskarta – stagepositioner (x-koordinater)
        // ─────────────────────────────────────────────
        public const float WorldMapStage1X  = 3.0f;
        public const float WorldMapStage2X  = 6.0f;
        public const float WorldMapStage3X  = 9.0f;
        public const float WorldMapStage4X  = 12.0f;
        public const float WorldMapStage5X  = 15.0f;
        public const float WorldMapStage6X  = 18.0f;
        public const float WorldMapStage7X  = 21.0f;
        public const float WorldMapStage8X  = 24.0f;
        public const float WorldMapStageTolerance = 0.1f; // Tolerans för positionsjämförelse


        // ─────────────────────────────────────────────
        // Hastighetsbegränsning (velocity clamp)
        // ─────────────────────────────────────────────
        public const float MaxVelocityX         = 10.0f;  // Max horisontell hastighet
        public const float MaxVelocityXCrash    = 11.0f;  // Crashgräns horisontellt (återställer till 0)
        public const float MaxVelocityYUp       = 10.0f;  // Max uppåthastighet
        public const float MaxVelocityYUpCrash  = 11.0f;  // Crashgräns uppåt (återställer till 0)


        // ─────────────────────────────────────────────
        // Luftmotstånd / bromsning (drag)
        // ─────────────────────────────────────────────
        public const float DragNormal           = 3.0f;   // Bromskoefficient normalt läge
        public const float DragBPower           = 2.0f;   // Bromskoefficient BPower-läge

        /// <summary>Tröskel under vilken vx sätts till exakt 0 (normal mark, normalt läge).</summary>
        public const float SlipperinessNormal         = 3.0f;
        /// <summary>Tröskel under vilken vx sätts till exakt 0 (normal mark, BPower).</summary>
        public const float SlipperinessNormalBPower   = 0.09f;
        /// <summary>Tröskel under vilken vx sätts till exakt 0 (isig mark, normalt läge).</summary>
        public const float SlipperinessIce            = 0.1f;
        /// <summary>Tröskel under vilken vx sätts till exakt 0 (isig mark, BPower).</summary>
        public const float SlipperinessIceBPower      = 0.08f;


        // ─────────────────────────────────────────────
        // Stridsvärden
        // ─────────────────────────────────────────────
        public const float KnockbackMultiplierX = 3.0f;
        public const float KnockbackMultiplierY = 2.0f;
    }
}
