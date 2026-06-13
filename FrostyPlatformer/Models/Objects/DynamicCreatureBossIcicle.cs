#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    /// <summary>Istappens faser.</summary>
    public enum BossIciclePhase { Warn, Fall, Shatter }

    /// <summary>
    /// Istappsregn — akt 3:s arena-hazard (vintern pressar in). Faller från taket.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Skriptad fas-maskin ovanpå Creature (telegraf → fall → krasch).
    ///
    /// MOTIVERING:
    /// Akt 3 ska tvinga rörelse — man får inte campa i öppna ytor. Istappar telegraferas med en
    /// markör på marken (rättvist), faller sedan i sin kolumn och krossas mot första ytan (golv
    /// ELLER plattform). Vid nedslaget smulas den till skärvor som skingras och försvinner (ingen
    /// kvarliggande plotter). Oförstörbar (kan inte stampas); skadar vid kontakt likt en is-spik.
    /// Den gamla DynamicCreatureEnemyIcicle är hårdkodad mot mapnine-geometrin, därför en egen typ.
    ///
    /// ANVÄNDNING:
    /// Spawnas av GameplayState.ManageHazards under Giant-akten. Skadar hjälten via den vanliga
    /// kollisionslogiken (behandlas som is-spik).
    /// </remarks>
    public class DynamicCreatureBossIcicle : Creature
    {
        private const float WarnDur    = 0.7f;   // telegraf innan fallet
        private const float ShatterDur = 0.4f;   // hur länge smul-partiklarna skingras
        private const float FallSpeed  = 12f;    // tiles/s nedåt

        // Skärvornas spridningsriktningar (tiles) — smulet kastas utåt/uppåt vid nedslag.
        private static readonly (float dx, float dy)[] Shards =
            { (-1f, -0.4f), (1f, -0.4f), (-0.6f, 0.3f), (0.6f, 0.3f) };

        /// <summary>Istappar kan inte förstöras av hoppspark.</summary>
        public override bool IsIndestructible => true;

        public BossIciclePhase Phase { get; private set; } = BossIciclePhase.Warn;

        /// <summary>Marknivån i kolumnen — var varningsmarkören ritas och en fallback för nedslag.</summary>
        public float GroundMarkerY { get; set; }

        private float _timer;
        private float _anim;

        public DynamicCreatureBossIcicle() : base("giant_hazard", SpriteId.GiantHazard)
        {
            Friendly = false;
            Health = 1000;
            MaxHealth = 1000;
            SolidVsDynamic = false;   // vilande vid taket under telegrafen
            SolidVsMap = true;
            DamageGiven = 4;
        }

        public void Configure(float columnX, float groundMarkerY)
        {
            px = columnX; py = 0f;
            GroundMarkerY = groundMarkerY;
            Phase = BossIciclePhase.Warn;
            _timer = WarnDur;
        }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Redundant) return;   // låt borttagnings-räknaren ticka upp i fred (annars nollställs den)

            _anim += fElapsedTime;

            switch (Phase)
            {
                case BossIciclePhase.Warn:
                    SolidVsDynamic = false;
                    py = 0f; vx = 0; vy = 0;                 // hänger vid taket medan markören blinkar
                    _timer -= fElapsedTime;
                    if (_timer <= 0f) Phase = BossIciclePhase.Fall;
                    break;

                case BossIciclePhase.Fall:
                    SolidVsDynamic = true;                   // farlig på vägen ned (skadar vid kontakt)
                    vx = 0; vy = FallSpeed;
                    // Krossas mot första ytan (Grounded) — eller huvudgolvet som fallback.
                    if (Grounded || py >= GroundMarkerY)
                    {
                        Phase = BossIciclePhase.Shatter;
                        _timer = ShatterDur;
                        SolidVsDynamic = false;
                        vx = 0; vy = 0;
                    }
                    break;

                case BossIciclePhase.Shatter:
                    vx = 0; vy = 0;
                    _timer -= fElapsedTime;
                    if (_timer <= 0f) { Redundant = true; RemoveCount = 1; }  // sätts EN gång
                    break;
            }
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            switch (Phase)
            {
                case BossIciclePhase.Warn:
                    // Blinkande varningsmarkör på marken + skymtande istapp vid taket.
                    if (((int)(_anim * 6f) & 1) == 0)
                    {
                        gfx.DrawPartialSprite(SpriteId, ToPixel(px, ox), ToPixel(GroundMarkerY, oy), 2 * 16, 0, 16, 16);
                        gfx.DrawPartialSprite(SpriteId, ToPixel(px, ox), ToPixel(py, oy), 0, 0, 16, 16);
                    }
                    break;

                case BossIciclePhase.Fall:
                    gfx.DrawPartialSprite(SpriteId, ToPixel(px, ox), ToPixel(py, oy), 0, 0, 16, 16);
                    break;

                case BossIciclePhase.Shatter:
                    // Smulet skingras utåt och tunnas ut → "evaporerar".
                    float p = 1f - Math.Clamp(_timer / ShatterDur, 0f, 1f);   // 0 → 1
                    int count = Math.Max(1, (int)Math.Round((1f - p) * Shards.Length));
                    float spread = 0.2f + p * 1.3f;
                    for (int i = 0; i < count; i++)
                    {
                        float sxp = px + Shards[i].dx * spread;
                        float syp = py + Shards[i].dy * spread;
                        gfx.DrawPartialSprite(SpriteId, ToPixel(sxp, ox), ToPixel(syp, oy), 1 * 16, 0, 16, 16);
                    }
                    break;
            }
        }
    }
}
