#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    /// <summary>
    /// Spegel-Scarlet — slutbossens akt 1-antagonist (den kalla maskinen under skalet).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Konkret Creature-subklass med ett litet beteende-tillstånd (engage/reträtt).
    ///
    /// MOTIVERING:
    /// Stridens akt-progression/hälsa bor i den testbara BossPhaseController (SRP). Här bor
    /// akt 1-duellens rörelse: ett robotiskt, avsiktligt mönster som JAGAR hjältens nivå.
    /// Hjälten ovanför → rör dig mot hen och b-power-språng uppåt (står du rakt under sveper du
    /// i committade skurar för att nå en plattformskant); fear of heights så hon inte trillar av
    /// vunnen höjd. Hjälten på samma/lägre nivå → jaga på marken, stampa när nära, trilla ner
    /// från plattformar för att följa efter. Efter att ha tagit/gett skada drar hon sig undan
    /// (bryter loopar). Argt läge (låg boss-hälsa) → snabbare/tätare/slumpigare.
    ///
    /// ANVÄNDNING:
    /// Spawnas av MapTen.PopulateDynamics. GameplayState sätter Active/Angry, anropar OnStomped()
    /// vid lyckad stamp och OnDealtDamage() när hon träffar hjälten.
    /// </remarks>
    public class DynamicCreatureMirrorScarlet : Creature
    {
        private const float InvulnDuration     = 0.8f;   // "healing-time": kan ej skadas direkt efter en träff
        private const float RetreatDuration    = 0.9f;   // aktivt dra sig undan efter att ha tagit/gett skada
        private const float LeapIntervalNormal = 2.2f;   // tid mellan språng (deterministiskt)
        private const float LeapIntervalAngry  = 1.1f;   // argt läge: tätare språng
        private const float ChaseSpeed         = 2.0f;   // approach-tempo (långsammare än spelaren)
        private const float RetreatSpeed       = 2.6f;   // reträtt-tempo (tydlig disengage)
        private const float AngrySpeedMul      = 1.5f;
        private const float MoveBurst          = 1.3f;   // robotisk rytm: längd på en rörelse-skur
        private const float PauseDur           = 0.5f;   // robotisk rytm: längd på mekanisk "tänka"-paus
        private const float BPowerJumpVelocity = -10.0f; // "b-power"-språng (~2.5 tiles) — max tillåtet av
                                                         // fysikens MaxVelocityYUp; mer kraschar (vy→0) → inget hopp
        private const float AscendAirSpeed     = 3.0f;   // horisontell fart i luften under ascend (når kanten i tid)
        private const int   AscendStandoff     = 2;      // hoppar mot plattformen från ≤ så här många tiles bort

        private readonly Random _rng = new Random();

        private float _invulnTimer;
        private float _retreatTimer;
        private float _retreatDir = 1f;
        private float _leapTimer;
        private float _rhythmTimer;
        private bool  _moving = true;
        private bool  _heroAbove;          // hjälten på högre nivå → aktiverar fear of heights

        /// <summary>Sant medan akt 1 (Mirror) pågår. Sätts av GameplayState.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Argt läge (låg boss-hälsa): snabbare, tätare/slumpigare språng. Sätts av GameplayState.</summary>
        public bool Angry { get; set; }

        /// <summary>Arenan (sätts av GameplayState) — låter henne känna av plattformskanter att hoppa upp på.</summary>
        public IMapData? Arena { get; set; }

        public DynamicCreatureMirrorScarlet() : base("mirror_scarlet", SpriteId.EnemyMirrorScarlet)
        {
            Friendly = false;
            Health = 1000;       // BossPhaseController är auktoritativ för stridshälsan; håll kroppen vid liv.
            MaxHealth = 1000;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 4;     // matchar vanliga fiender → samma energi-kaskad vid kontakt (kapas av hälsa)
            IsAttackable = true;
        }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (_invulnTimer > 0f) _invulnTimer -= fElapsedTime;
            if (_leapTimer > 0f)   _leapTimer   -= fElapsedTime;

            if (!Active || player == null)
            {
                vx = 0;
                IsAttackable = false;
                return;
            }

            // Alltid stampbar utom under "healing-time" direkt efter en träff.
            IsAttackable = _invulnTimer <= 0f;

            // Reträtt: efter att ha tagit ELLER gett skada drar hon sig undan en stund
            // (bryter loopen där man fastnar i upprepad skada, åt båda håll).
            if (_retreatTimer > 0f)
            {
                _retreatTimer -= fElapsedTime;
                vx = _retreatDir * RetreatSpeed;
                return;
            }

            float dx  = player.px - px;
            float dir = dx >= 0f ? 1f : -1f;
            bool  playerAbove = player.py < py - 1.5f;
            _heroAbove = playerAbove;   // styr fear of heights i OnWallCollision
            float speed = Angry ? ChaseSpeed * AngrySpeedMul : ChaseSpeed;

            // Robotisk rytm: längre rörelse-skurar med "tänka"-pauser (ej vibrerande).
            _rhythmTimer -= fElapsedTime;
            if (_rhythmTimer <= 0f)
            {
                _moving = !_moving;
                _rhythmTimer = _moving ? MoveBurst : PauseDur;
            }

            // Jaga hjältens NIVÅ. Ovanför → gå MOT hjälten; står hon rakt under en plattform
            // (bonkar undersidan om hon hoppar) sveper hon i committade skurar ut till en kant.
            // I exakt det ögonblick en plattformskant är ett steg framför henne (mot hjälten)
            // tvingar hon fram ett b-power-hopp UPP på plattformen. Samma/lägre → jaga på marken.
            float moveDir = dir;
            bool  leapUp = false;     // får b-power-hoppa mot hjälten (bågen landar på plattformen i vägen)
            bool  forceNow = false;   // vid en plattformskant → hoppa direkt (utan att vänta på cooldown)
            if (playerAbove && Arena != null)
            {
                int up2 = (int)py + 1 - 2;   // plattformsnivå 2 tiles upp
                // Kroppen är en hel tile bred [px, px+1) — kolla BÅDA kolumnerna. Räcker det att
                // en pixel är kvar under plattformen så bonkar hon vid hopp → räknas som blockerad.
                int cL = (int)px;
                int cR = (int)(px + 0.9f);
                bool blockedAbove = up2 >= 0 && (Arena.GetSolid(cL, up2) || Arena.GetSolid(cR, up2));
                if (blockedAbove)
                {
                    // Rakt under en plattform → hoppa INTE (bonk); gå till närmaste kant ut.
                    float edge = NearestEdgeDir(Arena, up2);
                    moveDir = edge != 0f ? edge : -dir;
                }
                else if (up2 >= 0)
                {
                    leapUp = true;
                    // Avstånd (i tiles, mot hjälten) till plattformskanten hon vill upp på.
                    int dist = 99;
                    for (int d = 1; d <= 5; d++)
                        if (Arena.GetSolid((int)px + (int)dir * d, up2)) { dist = d; break; }

                    if (dist <= AscendStandoff)
                    {
                        // 1-2 tiles från kanten → bra startsträcka: håll & hoppa upp (snyggt + når fram).
                        moveDir = 0f;
                        forceNow = true;
                    }
                    else
                        moveDir = dir;   // ingen plattform i hoppavstånd → närma dig hjälten
                }
            }

            vx = _moving ? moveDir * speed : 0f;

            // Avsiktliga språng (aldrig random): b-power mot hjälten ovanför (för att nå en plattform),
            // eller vanligt hopp för att landa på en närliggande hjälte i nivå.
            bool canStomp = Math.Abs(dx) < 3f && player.py > py - 2f;
            if (Grounded && (forceNow || (_leapTimer <= 0f && (leapUp || canStomp))))
            {
                if (leapUp || forceNow)
                {
                    vy = BPowerJumpVelocity;
                    vx = dir * AscendAirSpeed;   // mot hjälten/plattformen (snabb nog att nå kanten i tid)
                }
                else
                {
                    vy = GameConstants.JumpVelocity;
                    vx = dir * speed;
                }
                float baseInterval = Angry ? LeapIntervalAngry : LeapIntervalNormal;
                // Argt läge: jitter på timingen (mer oförutsägbart); normalt: fast rytm.
                _leapTimer = Angry ? baseInterval * (0.6f + (float)_rng.NextDouble() * 0.8f) : baseInterval;
            }

            // Luftstyrning mot hjälten under ascend-språnget: utan detta äter luftmotståndet upp
            // horisontalfarten → hon kommer bara rakt upp och landar kort. Sustained fart bär henne
            // i sidled fram till och upp på plattformen medan hon är hög nog.
            if (playerAbove && !Grounded)
                vx = dir * AscendAirSpeed;
        }

        /// <summary>
        /// Riktning (-1/+1) till närmaste kolumn där taket (raden <paramref name="up2"/>) tar slut,
        /// dvs. plattformens kant — så hon kan ta sig ut från under en plattform och hoppa upp.
        /// 0 om ingen kant inom räckhåll.
        /// </summary>
        private float NearestEdgeDir(IMapData map, int up2)
        {
            int hx = (int)px;
            for (int d = 1; d <= 8; d++)
            {
                if (!map.GetSolid(hx - d, up2)) return -1f;
                if (!map.GetSolid(hx + d, up2)) return 1f;
            }
            return 0f;
        }

        // Anropas varje frame under horisontell rörelse (turnPatrol=true endast vid riktig
        // väggträff). Vägg → byt svep-riktning + klättra-hopp. Annars: villkorad fear of heights.
        public override void OnWallCollision(ref float newX, bool turnPatrol, bool movingLeft, IMapData map, float fBorder)
        {
            if (!Active || _retreatTimer > 0f) return;

            if (turnPatrol)   // faktisk vägg → klättra-hopp
            {
                if (Grounded && _leapTimer <= 0f)
                {
                    vy = GameConstants.JumpVelocity;
                    _leapTimer = Angry ? LeapIntervalAngry : LeapIntervalNormal;
                }
                return;
            }

            // Fear of heights — endast när hjälten är OVANFÖR: kliv inte av en plattformskant
            // (då tappar hon höjden hon klättrat till). Hjälten på samma/lägre nivå → ok att falla.
            if (_heroAbove && Grounded)
            {
                int footRow = (int)py + 1;
                float aheadX = movingLeft ? newX : newX + (1f - fBorder);
                if (!map.GetSolid((int)aheadX, footRow))
                {
                    newX = px;   // stanna kvar på plattformen (språnget tar henne vidare uppåt)
                    vx = 0;
                }
            }
        }

        /// <summary>Lyckad stamp: healing-time (osårbar) + reträtt bort från hjälten.</summary>
        public void OnStomped(float assailantX)
        {
            _invulnTimer  = InvulnDuration;
            _retreatTimer = RetreatDuration;
            _retreatDir   = px >= assailantX ? 1f : -1f;
            IsAttackable  = false;
            vy = -3f;   // liten studs
        }

        /// <summary>Efter att ha gett skada: prioritera reträtt så spelaren inte loop-stunnas.</summary>
        public void OnDealtDamage(float victimX)
        {
            _retreatTimer = RetreatDuration;
            _retreatDir   = px >= victimX ? 1f : -1f;
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            // Blinka under healing-time som träff-feedback (hoppa över ritning varannan "tick").
            if (_invulnTimer > 0f && ((int)(_invulnTimer * 20f) & 1) == 0)
                return;

            int screenX = ToPixel(px, ox);
            int screenY = ToPixel(py, oy);
            gfx.DrawPartialSprite(SpriteId, screenX, screenY, 0, 0, 16, 16);
        }
    }
}
