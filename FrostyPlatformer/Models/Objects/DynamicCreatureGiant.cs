#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    /// <summary>Jättens ansiktsuttryck — väljer vilket huvud-frame i giant_body-arket som ritas.</summary>
    public enum GiantPose { Idle, Telegraph, Roar, Hit, Cracked }

    /// <summary>
    /// Jätten i kölden — slutbossens akt 3-antagonist (tvivlet som kolossal maskin-pingvin).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Komponerad Creature — kroppen ritas av flera fristående delar ur ETT ark
    /// (torso · huvud-uttryck · kärna · fötter), inte som en enda frame.
    ///
    /// MOTIVERING:
    /// Riktiga jätten (v14) är för stor och för mångdelad för en enda 80×64-frame. Delarna
    /// ska kunna röra sig oberoende (huvudet bobbar, kärnan pulsar, uttrycket byts) — därför
    /// komponeras de vid renderingstillfället från namngivna celler + offsets (mot bossens
    /// övre-vänstra ankare). Näven/armarna är egna objekt (<see cref="DynamicCreatureGiantArm"/>)
    /// eftersom de stretchar efter spelaren. Stridens hälsa/akt bor i BossPhaseController (SRP).
    ///
    /// ANVÄNDNING:
    /// Spawnas/städas av GameplayState så länge BossPhaseController.CurrentAct == Giant.
    /// Slam-mekaniken sätter <see cref="Pose"/> (Telegraf vid uppladdning, Roar vid slag,
    /// Hit vid stamp, Cracked vid falsk seger). Kroppen är icke-solid; näven blir måltavlan.
    /// Programmerad rörelse (huvud/kropp-bob, blink, kärn-puls, fot-skifte) sker här i Behaviour/DrawSelf.
    /// </remarks>
    public class DynamicCreatureGiant : Creature
    {
        // En del ur giant_body-arket: käll-cell (Sx,Sy,W,H) + rit-offset (Dx,Dy) i pixlar
        // relativt bossens övre-vänstra ankare (px,py). Se sheet_spec.json.
        private readonly record struct Part(int Sx, int Sy, int W, int H, int Dx, int Dy);

        private static readonly Part Torso        = new(  2,  2, 69, 63, 46, 52);
        private static readonly Part Core0        = new( 88, 67, 19, 18, 71, 73);
        private static readonly Part Core1        = new(109, 67, 21, 19, 70, 73);
        private static readonly Part Core2        = new(132, 67, 23, 20, 69, 72);
        private static readonly Part Core3        = new(157, 67, 21, 19, 70, 73);
        // Kärn-puls: ping-pong 0→3→0 för en andande glöd.
        private static readonly Part[] CorePulse  = { Core0, Core1, Core2, Core3, Core2, Core1 };
        private static readonly Part Foot         = new(180, 67, 19, 10, 57, 111);
        private static readonly Part FootRight     = new(180, 67, 19, 10, 84, 111);   // speglad, isär
        private static readonly Part HeadIdle      = new( 73,  2, 41, 48, 60, 15);
        private static readonly Part HeadBlink     = new(116,  2, 41, 48, 60, 15);   // ambient (motion-pass)
        private static readonly Part HeadTelegraph = new(159,  2, 41, 48, 60, 15);
        private static readonly Part HeadRoar      = new(202,  2, 41, 48, 60, 15);
        private static readonly Part HeadHit       = new(  2, 67, 41, 48, 60, 15);
        private static readonly Part HeadCracked   = new( 45, 67, 41, 48, 60, 15);

        private const float CollapseDur = 0.9f;   // skälv → sjunker → borta (bryggan till akt 4)
        private const float BobAmp      = 1.5f;   // px — huvudets bob
        private const float BodyBobAmp  = 1f;     // px — kroppens (torso+kärna) subtila andning (samma fas)
        private const float BobSpeed    = 1.6f;
        private const float PulseSpeed  = 4f;     // kärn-puls: frames/sek över ping-pong-sekvensen
        private const float FootShuffleDur  = 0.45f;  // hur länge en fot-lyftning tar
        private const float FootLiftAmp     = 2f;     // px en fot lyfts vid viktförskjutning
        private const float FootIntervalMin = 2.5f, FootIntervalMax = 6f;  // paus mellan fot-skiften

        private readonly Random _rng = new Random();
        private float _anchorX, _anchorY;
        private bool  _anchored;
        private float _animTime;
        private float _hitLeft;        // kort grimas efter en träff (överstyr Pose)
        private float _blinkLeft;      // pågående blink (idle)
        private float _blinkCooldown = 3f;
        private float _footTimer = 2f; // nedräkning till nästa fot-skifte
        private float _footShuffleLeft;// pågående fot-lyftning
        private int   _footWhich;      // 0 = vänster fot, 1 = höger
        private bool  _collapsing;
        private float _collapseTimer;

        /// <summary>Sant medan akt 3 (Giant) pågår. Sätts av GameplayState.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Aktuellt ansiktsuttryck. Sätts av slam-mekaniken; default idle.</summary>
        public GiantPose Pose { get; set; } = GiantPose.Idle;

        /// <summary>Utlöser en kort grimas (Hit-uttryck) — anropas när svagpunkten stampas.</summary>
        public void ReactHit() => _hitLeft = 0.35f;

        /// <summary>Sant medan jätten håller på att rasa (bryggan till akt 4).</summary>
        public bool IsCollapsing => _collapsing;

        /// <summary>Startar kollaps-sekvensen: skälv → sjunker i bitar → borta. Anropas när jätten besegrats.</summary>
        public void BeginCollapse()
        {
            if (_collapsing) return;
            _collapsing = true;
            _collapseTimer = CollapseDur;
            Pose = GiantPose.Cracked;
        }

        public DynamicCreatureGiant() : base("giant_body", SpriteId.GiantBody)
        {
            Friendly = false;
            Health = 1000;       // BossPhaseController är auktoritativ för stridshälsan.
            MaxHealth = 1000;
            SolidVsDynamic = false;  // kroppen är kuliss — näven/svagpunkten blir måltavlan
            SolidVsMap = false;
            DamageGiven = 0;         // kroppen skadar inte; hazards/näv-slam gör det
            IsAttackable = false;
        }

        // Vilande närvaro: nita fast positionen (Behaviour körs sist i Update, efter att fysiken
        // flyttat objektet, så att sätta px/py här låser jätten oavsett gravitation).
        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Redundant) return;   // låt borttagnings-räknaren ticka i fred efter kollaps
            if (!_anchored) { _anchorX = px; _anchorY = py; _anchored = true; }
            px = _anchorX; py = _anchorY;
            vx = 0; vy = 0;

            if (_collapsing)
            {
                _collapseTimer -= fElapsedTime;
                if (_collapseTimer <= 0f) { Redundant = true; RemoveCount = 1; }
                return;
            }

            if (_hitLeft > 0f) _hitLeft -= fElapsedTime;

            _animTime += fElapsedTime;

            // Blink: en kort blink i idle med slumpad paus emellan.
            if (_blinkLeft > 0f) _blinkLeft -= fElapsedTime;
            else
            {
                _blinkCooldown -= fElapsedTime;
                if (_blinkCooldown <= 0f)
                {
                    _blinkLeft     = 0.12f;
                    _blinkCooldown = 2.5f + (float)_rng.NextDouble() * 3f;
                }
            }

            // Fot-skifte: då och då lyfter jätten en fot lite (viktförskjutning).
            if (_footShuffleLeft > 0f) _footShuffleLeft -= fElapsedTime;
            else
            {
                _footTimer -= fElapsedTime;
                if (_footTimer <= 0f)
                {
                    _footShuffleLeft = FootShuffleDur;
                    _footWhich       = _rng.Next(2);
                    _footTimer       = FootIntervalMin + (float)_rng.NextDouble() * (FootIntervalMax - FootIntervalMin);
                }
            }
        }

        // Ritar en del vid bossens ankare + delens offset. flip = spegla i x-led (höger fot).
        private void DrawPart(IRenderContext gfx, float ox, float oy, in Part p, bool flip = false, int extraY = 0)
        {
            int x = ToPixel(px, ox) + p.Dx;
            int y = ToPixel(py, oy) + p.Dy + extraY;
            if (flip) gfx.DrawPartialSpriteFlippedX(SpriteId, x, y, p.Sx, p.Sy, p.W, p.H);
            else      gfx.DrawPartialSprite(SpriteId, x, y, p.Sx, p.Sy, p.W, p.H);
        }

        // Väljer huvud-uttryck: träff-grimas överstyr allt, sedan blink i idle, annars Pose.
        private Part HeadForPose()
        {
            if (_hitLeft > 0f) return HeadHit;
            if (_blinkLeft > 0f && Pose == GiantPose.Idle) return HeadBlink;
            return Pose switch
            {
                GiantPose.Telegraph => HeadTelegraph,
                GiantPose.Roar      => HeadRoar,
                GiantPose.Hit       => HeadHit,
                GiantPose.Cracked   => HeadCracked,
                _                   => HeadIdle,
            };
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            if (_collapsing) { DrawCollapse(gfx, ox, oy); return; }

            int coreFrame = (int)(_animTime * PulseSpeed) % CorePulse.Length;
            int bodyBob   = (int)Math.Round(Math.Sin(_animTime * BobSpeed) * BodyBobAmp);
            int headBob   = (int)Math.Round(Math.Sin(_animTime * BobSpeed) * BobAmp);
            int footLift  = _footShuffleLeft > 0f
                ? (int)Math.Round(Math.Sin((1f - _footShuffleLeft / FootShuffleDur) * Math.PI) * FootLiftAmp)
                : 0;

            // Bakifrån och fram: torso+kärna andas ihop, en fot lyfts ibland, huvudet bobbar en aning mer.
            DrawPart(gfx, ox, oy, Torso, extraY: bodyBob);
            DrawPart(gfx, ox, oy, Foot,      extraY: _footWhich == 0 ? -footLift : 0);
            DrawPart(gfx, ox, oy, FootRight, flip: true, extraY: _footWhich == 1 ? -footLift : 0);
            DrawPart(gfx, ox, oy, CorePulse[coreFrame], extraY: bodyBob);
            DrawPart(gfx, ox, oy, HeadForPose(), extraY: headBob);
        }

        // Kollaps: kort skälv, sedan brister torso + huvud i 16×16-bitar som slungas utåt och
        // faller (tunnas ut → "evaporerar"); fötterna trillar och kärnan flammar ut. Den vita
        // blixten sköts av GameplayState.
        private void DrawCollapse(IRenderContext gfx, float ox, float oy)
        {
            float p  = 1f - Math.Clamp(_collapseTimer / CollapseDur, 0f, 1f);   // 0 → 1
            int   bx = ToPixel(px, ox), by = ToPixel(py, oy);

            void Blit(in Part pt, int x, int y, bool flip = false)
            {
                if (flip) gfx.DrawPartialSpriteFlippedX(SpriteId, x, y, pt.Sx, pt.Sy, pt.W, pt.H);
                else      gfx.DrawPartialSprite(SpriteId, x, y, pt.Sx, pt.Sy, pt.W, pt.H);
            }

            if (p < 0.28f)   // skälv innan bristningen
            {
                int jx = (int)Math.Round(Math.Sin(p * 90f) * 3f);
                Blit(Torso,       bx + Torso.Dx + jx,       by + Torso.Dy);
                Blit(Foot,        bx + Foot.Dx + jx,        by + Foot.Dy);
                Blit(FootRight,   bx + FootRight.Dx + jx,   by + FootRight.Dy, flip: true);
                Blit(Core0,       bx + Core0.Dx + jx,       by + Core0.Dy);
                Blit(HeadCracked, bx + HeadCracked.Dx + jx, by + HeadCracked.Dy);
                return;
            }

            float q = (p - 0.28f) / 0.72f;   // 0 → 1 under bristningen

            // Kärn-flare som slocknar först.
            if (q < 0.55f)
            {
                var c = CorePulse[(int)(q * 10f) % CorePulse.Length];
                Blit(c, bx + c.Dx, by + c.Dy);
            }

            // Fötterna trillar utåt-ned.
            int ff = (int)Math.Round(q * q * 26f);
            Blit(Foot,      bx + Foot.Dx - (int)Math.Round(q * 6f),      by + Foot.Dy + ff);
            Blit(FootRight, bx + FootRight.Dx + (int)Math.Round(q * 6f), by + FootRight.Dy + ff, flip: true);

            // Torso + huvud smulas i 16×16-bitar.
            Shatter(gfx, bx, by, Torso, q);
            Shatter(gfx, bx, by, HeadCracked, q);
        }

        // Smular en del i 16×16-bitar som slungas utåt från delens mitt + faller (gravitation),
        // och tunnas ut med q → färre bitar ju längre in i kollapsen.
        private void Shatter(IRenderContext gfx, int bx, int by, in Part part, float q)
        {
            int cols = (part.W + 15) / 16, rows = (part.H + 15) / 16;
            float cx = part.W / 2f, cy = part.H / 2f;
            for (int gy = 0; gy < rows; gy++)
                for (int gx = 0; gx < cols; gx++)
                {
                    if (((gx * 7 + gy * 13) % 11) < (int)(q * 11f)) continue;   // tunna ut
                    int w = Math.Min(16, part.W - gx * 16);
                    int h = Math.Min(16, part.H - gy * 16);
                    float ux = gx * 16 + 8 - cx, uy = gy * 16 + 8 - cy;         // riktning ut från mitten
                    int dx = (int)Math.Round(ux * q * 1.4f + Math.Sin((gx + gy) * 3.1f + q * 18f) * 2f * q);
                    int dy = (int)Math.Round(uy * q * 0.6f + q * q * 22f);      // fall
                    gfx.DrawPartialSprite(SpriteId,
                        bx + part.Dx + gx * 16 + dx,
                        by + part.Dy + gy * 16 + dy,
                        part.Sx + gx * 16, part.Sy + gy * 16, w, h);
                }
        }
    }
}
