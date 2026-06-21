#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
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

        // Hammarslag — overhead-variant när hjälten campar på en plattform. Näven LADDAS UPP redan under
        // telegrafen (reser sig och hänger utsträckt högt över målkolumnen = själva varningen) och huggs
        // sedan snabbt RAKT NER. Trigg/val sköts av GameplayState; i övrigt stampbart som ett vanligt slag.
        private const float HammerRaise      = 4f;    // hur högt över målet näven lyfts (apex)
        private const float HammerWindupFrac = 0.6f;  // andel av telegraf-tiden som lyftet tar (resten = håll utsträckt)
        private const float HammerChopTime   = 0.2f;  // snabbt nedhugg från apexen (näven är redan uppladdad)

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

        /// <summary>Kolumnen näven slår ner i (det låsta målet) — så GameplayState kan släppa extra is där.</summary>
        public float ImpactX => _targetX;

        /// <summary>
        /// Läser och NOLLSTÄLLER "näven slog precis i marken"-signalen (Dropping→Stuck). GameplayState
        /// anropar denna för att trigga en extra is-skur per nedslag — exakt en gång per slag.
        /// </summary>
        public bool ConsumeSlamLanded()
        {
            if (!_landedSignal) return false;
            _landedSignal = false;
            return true;
        }

        private float _timer;
        private float _anim;
        private float _t;              // 0 = vid axeln, 1 = vid målet
        private float _targetX, _targetY;
        private int   _surfaceY;       // ytan näven planterar på (för markören)
        private bool  _locked;
        private bool  _landedSignal;   // engångsflagga: näven slog precis i marken (konsumeras av GameplayState)
        private bool  _hammer;         // detta slag är ett overhead-hammarslag (ej stampbart)

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

        /// <summary>Startar ett slag (om armen vilar). <paramref name="hammer"/> = overhead-hammarslag
        /// (näven huggs ner uppifrån, ej stampbart) i stället för det vanliga raka slaget.</summary>
        public void TriggerSlam(bool hammer = false)
        {
            if (Phase != GiantArmPhase.Rest) return;
            _hammer = hammer;
            Phase = GiantArmPhase.Telegraph;
            _timer = TelegraphDur;
            _locked = false;
        }

        /// <summary>
        /// Drar tillbaka näven OMEDELBART (anropas när näven träffat hjälten). Hindrar att hjälten
        /// fastnar/mosas under näven och begränsar ett slag till EN träff. No-op om armen redan
        /// vilar/retirerar. Sätter inte igång retur från Telegraph (slaget har inte landat än).
        /// </summary>
        public void RecoilNow()
        {
            if (Phase == GiantArmPhase.Dropping || Phase == GiantArmPhase.Stuck)
            {
                Phase = GiantArmPhase.Recoiling;
                IsAttackable = false;
            }
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
                    // Hammarslaget telegraferas tydligare: jätten morrar redan under uppladdningen (sur min)
                    // i stället för den vanliga telegraf-posen → spelaren ser att det är ett hammarslag på väg.
                    if (Giant != null) Giant.Pose = _hammer ? GiantPose.Roar : GiantPose.Telegraph;
                    _timer -= fElapsedTime;
                    if (_timer <= 0f) Phase = GiantArmPhase.Dropping;
                    break;

                case GiantArmPhase.Dropping:
                    SolidVsDynamic = true; IsAttackable = false;   // tung och farlig på vägen ut
                    _t += fElapsedTime / (_hammer ? HammerChopTime : DropTime);
                    if (_t >= 1f)
                    {
                        // Båda slagen fastnar (stampbar svagpunkt) och triggar is-skuren. Hammaren skiljer
                        // sig bara i leveransen (overhead-båge) + extra varning, inte i belöningen.
                        _t = 1f;
                        Phase = GiantArmPhase.Stuck;
                        _timer = StuckDur;
                        _landedSignal = true;   // näven slog i marken → trigga extra is-skur (konsumeras en gång)
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
            // Näven landar 1 tile över ytan = i hjältens nivå, så den når marken visuellt (inget glapp).
            // Tryggt mot "stå still → auto-stamp": retire-on-träff gör att näven träffar + studsar tillbaka
            // innan den hinner plantera sig på en stillastående hjälte (och JumpDamage är gatad till Stuck).
            _targetY  = sy - 1f;
        }

        // Sätter nävens position. Rakt slag: interpolera längs linjen axel→mål. Hammarslag UNDER
        // nedhugget: lyft till en apex ovanför målkolumnen (första HammerApexFrac) och hugg sedan
        // RAKT NER på målet (resten). Retur (Recoiling) drar tillbaka längs den raka linjen.
        private void ApplyPos()
        {
            if (_hammer && (Phase == GiantArmPhase.Telegraph || Phase == GiantArmPhase.Dropping))
            {
                float apexY = Math.Max(1f, Math.Min(ShoulderY, _targetY) - HammerRaise);
                if (Phase == GiantArmPhase.Telegraph)
                {
                    // LADDA UPP: lyft näven till apexen tidigt och håll den utsträckt högt över kolumnen
                    // (= varningen) tills hugget. Lyftet sker över första HammerWindupFrac av telegraf-tiden.
                    float r = Math.Clamp((1f - _timer / TelegraphDur) / HammerWindupFrac, 0f, 1f);
                    px = ShoulderX + (_targetX - ShoulderX) * r;
                    py = ShoulderY + (apexY - ShoulderY) * r;
                }
                else   // Dropping → hugg rakt ner från apexen
                {
                    px = _targetX;
                    py = apexY + (_targetY - apexY) * _t;
                }
                return;
            }

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
            // Telegraf-markör på ytan där slaget kommer landa (blinkar) — ger tid att dodga. För
            // hammarslaget är dessutom den uppladdade, utsträckta näven högt över kolumnen den tydliga
            // varningen (se ApplyPos), så ingen extra apex-ikon behövs.
            if (Phase == GiantArmPhase.Telegraph && !DevConfig.HideSlamMarker && ((int)(_anim * 6f) & 1) == 0)
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

            // Svagpunkts-ikon på knogen — state speglar fasen (göms i vila). Gäller båda slagen
            // (hammaren är stampbar igen).
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
