#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    /// <summary>Jätt-armens faser.</summary>
    public enum GiantArmPhase { Rest, Telegraph, Dropping, Stuck, Recoiling }

    /// <summary>
    /// Jättens arm — en av två som hänger från axlarna; akt 3:s attack och måltavla.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Skriptad fas-maskin ovanpå Creature (rörelsen styrs av faser, inte av fysiken).
    ///
    /// MOTIVERING:
    /// Jätten ska läsas som en kropp med TVÅ armar som STRÄCKER sig efter spelaren — inte lösryckta
    /// nävar i fasta lanes. Vid varje slag låses ett mål på spelarens kolumn (telegraferat); näven
    /// interpoleras längs en linje från axeln till målet, och armen ritas som en segmenterad is-arm
    /// längs samma linje → den både följer dig och sitter ihop med kroppen, i valfri vinkel. Den
    /// planterar på ytan UNDER spelaren (golv ELLER plattformstopp), så plattformar inte är slag-säkra.
    /// §5-mekaniken (slå-i-marken→fastnar→exponerar stampbar svagpunkt) körs i Stuck-fönstret.
    /// Positionen skriptas i Behaviour (körs sist) så fysiken inte stör koreografin. Stamp dränerar
    /// BossPhaseController (auktoritativ hälsa, SRP).
    ///
    /// ANVÄNDNING:
    /// Två instanser spawnas av GameplayState.ManageGiant och turas om att slå (TriggerSlam).
    /// Stampas via JumpDamage endast i Stuck-fasen.
    /// </remarks>
    public class DynamicCreatureGiantArm : Creature
    {
        private const float TelegraphDur = 0.8f;   // varning + sikta innan slaget
        private const float DropTime     = 0.22f;  // tid att sträcka ut till målet (snabbt slag)
        private const float StuckDur     = 0.9f;   // hur länge svagpunkten är exponerad
        private const float RecoilTime   = 0.4f;   // tid att dra tillbaka

        /// <summary>Axelns ankarpunkt (tiles) — armen sträcks härifrån.</summary>
        public float ShoulderX { get; set; }
        public float ShoulderY { get; set; }

        /// <summary>Vänster/höger arm — så GameplayState kan växla sida.</summary>
        public bool IsLeft { get; set; }

        /// <summary>Huvudet (kuliss) — armen styr dess uttryck under slaget.</summary>
        public DynamicCreatureGiant? Giant { get; set; }

        /// <summary>Arenan — för att hitta ytan (golv/plattform) under spelaren vid sikte.</summary>
        public IMapData? Arena { get; set; }

        /// <summary>Sant när armen är mitt i ett slag (inte i vila).</summary>
        public bool IsSlamming => Phase != GiantArmPhase.Rest;

        public GiantArmPhase Phase { get; private set; } = GiantArmPhase.Rest;

        private float _timer;
        private float _anim;
        private float _t;              // 0 = vid axeln, 1 = vid målet
        private float _targetX, _targetY;
        private int   _surfaceY;       // ytan näven planterar på (för markören)
        private bool  _locked;

        public DynamicCreatureGiantArm() : base("giant_arm", SpriteId.GiantArm)
        {
            Friendly = false;
            Health = 1000;       // BossPhaseController är auktoritativ; armen "dör" inte av stamp.
            MaxHealth = 1000;
            SolidVsDynamic = false;
            SolidVsMap = false;
            DamageGiven = 6;     // slaget gör ont vid kontakt
            IsAttackable = false;
        }

        public void Configure(float shoulderX, float shoulderY, bool isLeft,
                              DynamicCreatureGiant giant, IMapData arena)
        {
            ShoulderX = shoulderX; ShoulderY = shoulderY;
            IsLeft = isLeft; Giant = giant; Arena = arena;
            _targetX = shoulderX; _targetY = shoulderY;
            _t = 0f; Phase = GiantArmPhase.Rest;
            ApplyPos();
        }

        /// <summary>Startar ett slag (om armen vilar).</summary>
        public void TriggerSlam()
        {
            if (Phase != GiantArmPhase.Rest) return;
            Phase = GiantArmPhase.Telegraph;
            _timer = TelegraphDur;
            _locked = false;
        }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            _anim += fElapsedTime;
            vx = 0; vy = 0;

            switch (Phase)
            {
                case GiantArmPhase.Rest:
                    SolidVsDynamic = false; IsAttackable = false;
                    _t = 0f;
                    break;

                case GiantArmPhase.Telegraph:
                    SolidVsDynamic = false; IsAttackable = false;
                    if (!_locked && player != null && Arena != null) { LockTarget(player); _locked = true; }
                    _t = 0f;
                    if (Giant != null) Giant.Pose = GiantPose.Telegraph;
                    _timer -= fElapsedTime;
                    if (_timer <= 0f) Phase = GiantArmPhase.Dropping;
                    break;

                case GiantArmPhase.Dropping:
                    SolidVsDynamic = true; IsAttackable = false;   // tung och farlig på vägen ut
                    _t += fElapsedTime / DropTime;
                    if (_t >= 1f)
                    {
                        _t = 1f;
                        Phase = GiantArmPhase.Stuck;
                        _timer = StuckDur;
                        if (Giant != null) Giant.Pose = GiantPose.Roar;
                    }
                    break;

                case GiantArmPhase.Stuck:
                    SolidVsDynamic = true; IsAttackable = true;     // svagpunkten exponerad → stampbar
                    _t = 1f;
                    _timer -= fElapsedTime;
                    if (Giant != null && _timer < StuckDur - 0.4f) Giant.Pose = GiantPose.Idle;
                    if (_timer <= 0f) Phase = GiantArmPhase.Recoiling;
                    break;

                case GiantArmPhase.Recoiling:
                    SolidVsDynamic = false; IsAttackable = false;
                    _t -= fElapsedTime / RecoilTime;
                    if (_t <= 0f) { _t = 0f; Phase = GiantArmPhase.Rest; }
                    break;
            }

            ApplyPos();
        }

        // Låser målet till spelarens kolumn och ytan (golv/plattform) under hen.
        private void LockTarget(DynamicGameObject player)
        {
            int w = Arena!.Width, h = Arena.Height;
            int tx = Math.Clamp((int)Math.Round(player.px), 1, w - 2);
            int sy = (int)player.py + 1;
            while (sy < h && !Arena.GetSolid(tx, sy)) sy++;
            _surfaceY = sy;
            _targetX  = tx;
            _targetY  = sy - 2f;   // svagpunkten 2 tiles över ytan → stampbar (se jump-constraint)
        }

        // Interpolerar näven längs linjen axel→mål och sätter objektets position dit.
        private void ApplyPos()
        {
            px = ShoulderX + (_targetX - ShoulderX) * _t;
            py = ShoulderY + (_targetY - ShoulderY) * _t;
        }

        /// <summary>Lyckad stamp på svagpunkten: rycker tillbaka + huvudet grimaserar.</summary>
        public void OnStomped()
        {
            Phase = GiantArmPhase.Recoiling;
            Giant?.ReactHit();
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            // Telegraf-markör på ytan där slaget kommer landa (blinkar) — ger tid att dodga.
            if (Phase == GiantArmPhase.Telegraph && ((int)(_anim * 6f) & 1) == 0)
                gfx.DrawPartialSprite(SpriteId.GiantWeakPoint,
                    ToPixel(_targetX, ox), ToPixel(_surfaceY - 1, oy), 16, 0, 16, 16);

            // Synlig segmenterad is-arm längs linjen axel→näve (valfri vinkel).
            float dx = px - ShoulderX, dy = py - ShoulderY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            int segs = Math.Max(1, (int)(dist / 0.8f));
            for (int i = 1; i < segs; i++)
            {
                float t = i / (float)segs;
                gfx.DrawPartialSprite(SpriteId,
                    ToPixel(ShoulderX + dx * t, ox), ToPixel(ShoulderY + dy * t, oy), 0, 0, 16, 16);
            }

            // Axel-kapsel (sitter ihop med huvudet) + näven. Vänster arm ritas spegelvänd
            // så formen pekar åt rätt håll (mot huvudet) i stället för åt fel håll.
            void Blit(int dx, int dy, int sxp, int syp, int w, int h)
            {
                if (IsLeft) gfx.DrawPartialSpriteFlippedX(SpriteId, dx, dy, sxp, syp, w, h);
                else        gfx.DrawPartialSprite(SpriteId, dx, dy, sxp, syp, w, h);
            }
            Blit(ToPixel(ShoulderX, ox), ToPixel(ShoulderY, oy), 16, 0, 16, 16);
            int frame = Phase == GiantArmPhase.Stuck ? 1 : 0;
            Blit(ToPixel(px, ox) - 8, ToPixel(py, oy), frame * 32, 16, 32, 32);

            // Svagpunkts-ikon på knogen — state speglar fasen (göms i vila).
            if (Phase != GiantArmPhase.Rest)
            {
                int wp = Phase switch
                {
                    GiantArmPhase.Telegraph => 1,
                    GiantArmPhase.Dropping  => 1,
                    GiantArmPhase.Stuck     => (((int)(_anim * 8f)) & 1) == 0 ? 2 : 3,
                    _                       => 4,
                };
                gfx.DrawPartialSprite(SpriteId.GiantWeakPoint,
                    ToPixel(px, ox), ToPixel(py, oy), wp * 16, 0, 16, 16);
            }
        }
    }
}
