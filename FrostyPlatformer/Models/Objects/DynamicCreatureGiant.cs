#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    /// <summary>Jättens ansiktsuttryck — väljer vilken frame i giant_head-arket som ritas.</summary>
    public enum GiantPose { Idle, Telegraph, Roar, Hit, Cracked }

    /// <summary>
    /// Jätten i kölden — slutbossens akt 3-antagonist (tvivlet som kolossal storm).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Frame-/pose-styrd Creature (huvudet ritas som hela frames, inte tile-komposit).
    ///
    /// MOTIVERING:
    /// §5 låste svagpunkten som en 16×16-enhet, men HUVUDET behöver inte vara tile-komposit —
    /// det är kuliss/hot. Som hela uttrycks-frames (giant_head-arket) får det rik mimik med
    /// minimal kod: idle/telegraf/vrål/träff/sprucken + blink. Rörelse läggs ovanpå som
    /// PROGRAMMERADE effekter (bob, blink) — inte handanimerade frames. Stridens hälsa/akt
    /// bor i den testbara BossPhaseController (SRP). Arm/näve + svagpunkt är egna ark (5b).
    ///
    /// ANVÄNDNING:
    /// Spawnas/städas av GameplayState så länge BossPhaseController.CurrentAct == Giant.
    /// Slam-mekaniken (5b) sätter <see cref="Pose"/> (Telegraf vid uppladdning, Hit vid stamp,
    /// Cracked vid falsk seger). Kroppen är icke-solid; näven/svagpunkten blir måltavlan.
    /// </remarks>
    public class DynamicCreatureGiant : Creature
    {
        private const int FrameW = 80, FrameH = 64;   // ett huvud-frame i giant_head-arket
        // Frame-index (kolumn) i arket — i samma ordning som GiantPose, plus blink sist.
        private const int FrameBlink = 5;
        private const float BobAmplitude = 3f;        // px upp/ned — "looming"-känsla
        private const float BobSpeed     = 1.6f;

        private readonly Random _rng = new Random();
        private float _anchorX, _anchorY;
        private bool  _anchored;
        private float _animTime;
        private float _blinkCooldown = 3f;
        private float _blinkLeft;
        private float _hitLeft;        // kort grimas efter en träff (överstyr Pose)
        private bool  _collapsing;
        private float _collapseTimer;

        private const float CollapseDur = 0.9f;   // skälv → smulas → borta (bryggan till akt 4)

        /// <summary>Sant medan akt 3 (Giant) pågår. Sätts av GameplayState.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Aktuellt ansiktsuttryck. Sätts av slam-mekaniken (5b); default idle.</summary>
        public GiantPose Pose { get; set; } = GiantPose.Idle;

        /// <summary>Utlöser en kort grimas (Hit-frame) — anropas när svagpunkten stampas.</summary>
        public void ReactHit() => _hitLeft = 0.35f;

        /// <summary>Sant medan jätten håller på att rasa (bryggan till akt 4).</summary>
        public bool IsCollapsing => _collapsing;

        /// <summary>Startar kollaps-sekvensen: skälv → smulas i bitar → borta. Anropas när jätten besegrats.</summary>
        public void BeginCollapse()
        {
            if (_collapsing) return;
            _collapsing = true;
            _collapseTimer = CollapseDur;
            Pose = GiantPose.Cracked;
        }

        public DynamicCreatureGiant() : base("giant_head", SpriteId.GiantHead)
        {
            Friendly = false;
            Health = 1000;       // BossPhaseController är auktoritativ för stridshälsan.
            MaxHealth = 1000;
            SolidVsDynamic = false;  // kroppen är kuliss — näven/svagpunkten (5b) blir måltavlan
            SolidVsMap = false;
            DamageGiven = 0;         // kroppen skadar inte; hazards/näv-slam kommer i 5b–5c
            IsAttackable = false;
        }

        // Vilande närvaro: nita fast positionen (Behaviour körs sist i Update, efter att fysiken
        // flyttat objektet, så att sätta px/py här låser jätten oavsett gravitation). Programmerat
        // liv (bob/blink) räknas här men ändrar inte förankringen — bara hur huvudet ritas.
        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Redundant) return;   // låt borttagnings-räknaren ticka i fred efter kollaps
            if (!_anchored) { _anchorX = px; _anchorY = py; _anchored = true; }
            px = _anchorX; py = _anchorY;
            vx = 0; vy = 0;

            _animTime += fElapsedTime;

            if (_collapsing)
            {
                _collapseTimer -= fElapsedTime;
                if (_collapseTimer <= 0f) { Redundant = true; RemoveCount = 1; }
                return;   // ingen bob/blink medan den rasar
            }

            if (_hitLeft > 0f) _hitLeft -= fElapsedTime;

            if (_blinkLeft > 0f)
                _blinkLeft -= fElapsedTime;
            else
            {
                _blinkCooldown -= fElapsedTime;
                if (_blinkCooldown <= 0f)
                {
                    _blinkLeft     = 0.12f;
                    _blinkCooldown = 2.5f + (float)_rng.NextDouble() * 3f;
                }
            }
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            if (_collapsing) { DrawCollapse(gfx, ox, oy); return; }

            int frame = (int)Pose;
            // En träff-grimas överstyr allt; annars blinkar huvudet bara i idle.
            if (_hitLeft > 0f)
                frame = (int)GiantPose.Hit;
            else if (_blinkLeft > 0f && Pose == GiantPose.Idle)
                frame = FrameBlink;

            int bob = (int)Math.Round(Math.Sin(_animTime * BobSpeed) * BobAmplitude);
            int screenX = ToPixel(px, ox);
            int screenY = ToPixel(py, oy) + bob;
            gfx.DrawPartialSprite(SpriteId, screenX, screenY, frame * FrameW, 0, FrameW, FrameH);
        }

        // Kollaps: först ett skälv (sprucket ansikte), sen smulas huvudet i 16×16-bitar som
        // skingras utåt och faller — och tunnas ut tills det är borta. Återbrukar Cracked-framen.
        private void DrawCollapse(IRenderContext gfx, float ox, float oy)
        {
            float p = 1f - Math.Clamp(_collapseTimer / CollapseDur, 0f, 1f);   // 0 → 1
            int crackedX = (int)GiantPose.Cracked * FrameW;

            if (p < 0.35f)
            {
                // Skälv: hela det spruckna huvudet skakar på plats.
                int jx = (int)Math.Round(Math.Sin(p * 80f) * 3f);
                gfx.DrawPartialSprite(SpriteId, ToPixel(px, ox) + jx, ToPixel(py, oy),
                    crackedX, 0, FrameW, FrameH);
                return;
            }

            // Smul: varje ruta flyger utåt från mitten + faller; allt färre rutor → "evaporerar".
            float q = (p - 0.35f) / 0.65f;                 // 0 → 1
            int cols = FrameW / 16, rows = FrameH / 16;    // 5 × 4 rutor
            for (int gy = 0; gy < rows; gy++)
                for (int gx = 0; gx < cols; gx++)
                {
                    if (((gx * rows + gy) % 5) < (int)(q * 5f)) continue;  // tunna ut
                    float cx = px + gx + (gx - 2f) * q * 1.6f;
                    float cy = py + gy + (gy - 1.5f) * q * 0.6f + q * q * 4f;          // fall
                    gfx.DrawPartialSprite(SpriteId, ToPixel(cx, ox), ToPixel(cy, oy),
                        crackedX + gx * 16, gy * 16, 16, 16);
                }
        }
    }
}
