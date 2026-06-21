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
        private const float TraverseSpeedMul   = 1.7f;   // fart-boost när hjälten är långt bort (ren transport → snabbare)
        private const float RetreatSpeed       = 2.6f;   // reträtt-tempo (tydlig disengage)
        private const float AngrySpeedMul      = 1.5f;
        private const float MoveBurst          = 1.3f;   // robotisk rytm: längd på en rörelse-skur
        private const float PauseDur           = 0.5f;   // robotisk rytm: längd på mekanisk "tänka"-paus
        private const float BPowerJumpVelocity   = -10.0f; // ascend-språngets utgångsfart — TAKET enligt fysikens
                                                           // MaxVelocityYUp (−11 = crashgräns → vy nollställs).
                                                           // Högre hopp fås INTE av mer fart utan av mjukare stig-
                                                           // gravitation, se AscendGravityRelief nedan.
        private const float AscendAirSpeed       = 3.5f;   // horisontell fart när hon styr IN på plattformen (efter att ha klarat kanten)
        private const float GapJumpAirSpeed      = 5.5f;   // ballistiskt gap-hopp: hög sidofart som BEHÅLLS i luften (bågar
                                                           // över ett gap upp till en högre platå när rakt-upp inte når). < MaxVelocityX(10).
        private const float AscendLeapCooldown   = 0.8f;   // minsta tid mellan klätterhopp → planerade hopp, inte kängurustuds
        private const float AscendGravityRelief  = 4.0f;   // motverkar en del av gravitationen (20) under stigningen → effektiv ~16
                                                           // → topp ~3.1 tiles (klarar 2-tiles-platå med marginal). < gravitationen.
        private const int   AscendJumpRange      = 2;      // hoppar upp mot plattformen när kanten är så här nära. 2, inte 1: kroppen är
                                                           // 1 tile BRED, så vid bara 1 tiles avstånd hamnar hennes högra/vänstra halva
                                                           // UNDER mål-platåns kant → bonkar undersidan och fastnar. Vid 2 är hela kroppen
                                                           // fri. Modellen: hoppa RAKT upp förbi kanten, styr in FÖRST när kanten är klarad.
        private const int   ClimbScanRange       = 12;     // hur långt mot hjälten hon letar en klättringsbar plattform
        private const float ClimbReach           = 8f;     // är hjälten LÄNGRE bort än så här (i sidled) jagar hon horisontellt mot henne
                                                           // (ner till golvet, traversera) i stället för att klättra lokalt — målet är
                                                           // hjälten, inte närmaste platå. Inom räckhåll → klättra upp mot henne.
        private const float GlitchStartDist    = 2.5f;   // akt 4: börjar glitcha på detta avstånd (samma nivå)
        private const float GlitchFullDist     = 1.0f;   // ...full glitch så här nära
        private const float DodgeRange         = 3.5f;   // akt 4: rymmer om hjälten är ovanför inom detta avstånd
        private const float DodgeSpeed         = 3.5f;   // akt 4: undanmanöver-fart (snabbare än gång)
        private const float ExitDuration       = 0.8f;   // akt 1→2: glitch-upplösningens längd innan hon göms
        private const float DustFallDuration   = 1.4f;   // akt 1→2: hur länge dammet faller efter upplösningen (dramatisk paus)
        private const int   DustCount          = 14;     // antal damm-flagor som lossnar när hon löses upp
        private const float DustGravity        = 4.0f;   // damm-fall: acceleration nedåt (tiles/s²)

        private readonly Random _rng = new Random();

        private float _invulnTimer;
        private float _retreatTimer;
        private float _retreatDir = 1f;
        private float _leapTimer;
        private float _rhythmTimer;
        private bool  _moving = true;
        private float _ascendDir = 1f;     // riktning hon styr IN på plattformen (kan vara BORT från hjälten)
        private float _descendDir = 1f;    // riktning hon kliver av en kant — behålls i luften så hon fullföljer fallet
        private int   _climbLaunch  = int.MinValue;   // latchad startkolumn (MinValue = ingen)
        private float _climbLaunchDir = 1f;           // hopp-riktning för latchad start
        private int   _climbUp2     = int.MinValue;   // vilken plattformsnivå latchen gäller (annars ogiltig)
        private bool  _ascending;          // mitt i ett klätterhopp: rakt upp tills kanten är klarad, sen styr in
        private int   _ascendTargetRow;    // plattformsraden (up2) hon klättrar mot — "kanten klarad" = py ≤ denna − 1
        private bool  _gapJumping;         // mitt i ett ballistiskt gap-hopp: behåll sidofart hela bågen
        private int   _gapDir = 1;         // riktning för gap-hoppet (mot den högre platån över gapet)
        private float _glitch;             // glitch-intensitet 0..1 (akt 1-exit + akt 4 nära hjälten)
        private float _exitTimer = -1f;    // akt 1→2: glitch-upplösning pågår när ≥ 0
        private bool  _hidden;             // efter exit: kroppen behålls för akt 4 men ritas inte
        private float _dustTimer;          // akt 1→2: damm faller medan > 0 (efter upplösningen)
        private readonly float[] _dustX  = new float[DustCount];
        private readonly float[] _dustY  = new float[DustCount];
        private readonly float[] _dustVx = new float[DustCount];
        private readonly float[] _dustVy = new float[DustCount];

        /// <summary>Sant medan akt 1 (Mirror) pågår. Sätts av GameplayState.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Argt läge (låg boss-hälsa): snabbare, tätare/slumpigare språng. Sätts av GameplayState.</summary>
        public bool Angry { get; set; }

        /// <summary>Arenan (sätts av GameplayState) — låter henne känna av plattformskanter att hoppa upp på.</summary>
        public IMapData? Arena { get; set; }

        /// <summary>Akt 4: hon står stilla och passiv (slåss inte) men är stampbar — stampar du
        /// fortsätter loopen i stället för att vinna. Sätts av GameplayState.</summary>
        public bool Accepting { get; set; }

        /// <summary>Sant medan akt 1→2-exiten pågår: glitch-upplösningen OCH dammets fall efteråt.
        /// Svärmen väntar in hela förloppet (tom arena → dammet lägger sig) innan den droppar in.</summary>
        public bool ExitInProgress => _exitTimer >= 0f || _dustTimer > 0f;

        /// <summary>
        /// Startar akt 1→2-exiten: hon glitchar ur arenan där hon står och göms sedan. Kroppen
        /// behålls (osynlig) eftersom akt 4 återanvänder samma objekt (<see cref="Accepting"/>).
        /// Idempotent — en pågående eller redan avslutad exit rörs inte.
        /// </summary>
        public void BeginExit()
        {
            if (_hidden || _exitTimer >= 0f) return;
            _exitTimer = ExitDuration;
        }

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

            // Dammet faller (efter upplösningen) — körs varje frame oavsett övrigt tillstånd, så
            // det fortsätter falla även när kroppen är dold och inaktiv.
            if (_dustTimer > 0f)
            {
                _dustTimer -= fElapsedTime;
                UpdateDust(fElapsedTime);
            }

            // Akt 1→2: glitch-upplösning. När akt 1 är vunnen sönderfaller hon DÄR hon står och
            // försvinner ur arenan — glitch-intensiteten trappas upp mot slutet, sen göms kroppen
            // (behålls för akt 4) och dammet lossnar. Svärmen väntar in hela förloppet via ExitInProgress.
            if (_exitTimer >= 0f)
            {
                _exitTimer -= fElapsedTime;
                vx = 0f; vy = 0f;
                IsAttackable = false;
                SolidVsDynamic = false;
                _glitch = Math.Clamp(1f - _exitTimer / ExitDuration, 0f, 1f);
                if (_exitTimer <= 0f) { _hidden = true; _exitTimer = -1f; _glitch = 0f; SpawnDust(); }
                return;
            }

            // Akt 4 — Spegeln: hennes rörelse är din, SPEGELVÄND. Går du mot henne går hon mot dig;
            // går du bort går hon bort; hoppar du hoppar hon. Ni möts i mittpunkten. Ingen
            // kontakt-kollision (skada av/ingen block — akt 4 hanteras manuellt i GameplayState).
            // Hon glitchar mer ju närmare du kommer.
            if (Accepting)
            {
                _hidden = false;   // akt 4 återanvänder kroppen (StageMirror flyttar hit henne) → syns igen
                SolidVsDynamic = false;
                IsAttackable = false;
                if (player != null)
                {
                    float dxAbs = Math.Abs(player.px - px);
                    float dyAbs = Math.Abs(player.py - py);
                    bool heroAbove = player.py < py - 1.2f;

                    if (heroAbove && dxAbs < DodgeRange)
                    {
                        // Hjälten är ovanför (plattform/hopp) → hon RYMMER i sidled bort från att
                        // vara under, så en stamp blir mycket svår. (Frångår spegeln medvetet här.)
                        float away = px >= player.px ? 1f : -1f;
                        vx = away * DodgeSpeed;
                        _glitch = 0f;
                    }
                    else
                    {
                        vx = -player.vx;                                                   // spegelvänd sidled
                        if (Grounded && player.vy < -0.1f) vy = GameConstants.JumpVelocity; // speglar hopp

                        // Glitchar bara på SAMMA NIVÅ och nära — hoppar du eller är på annan nivå
                        // upphör den. Trappas upp ju närmare (2 block → 1 block).
                        bool sameLevel = dyAbs < 0.6f;
                        _glitch = (sameLevel && dxAbs <= GlitchStartDist)
                            ? Math.Clamp((GlitchStartDist - dxAbs) / (GlitchStartDist - GlitchFullDist), 0f, 1f)
                            : 0f;
                    }
                }
                else { vx = 0; _glitch = 0f; }
                return;
            }

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
            bool  farFromHero = Math.Abs(dx) > ClimbReach;   // hjälten långt bort i sidled → ren transport
            // Fart: argt läge snabbare; PLUS en boost när hon mest transporterar sig långt (knappar in avståndet).
            float speed = ChaseSpeed * (Angry ? AngrySpeedMul : 1f) * (farFromHero ? TraverseSpeedMul : 1f);

            // Hjälten UNDER bossen (även bara strax under) → ta dig NER. Ingen dödzon: så fort hjälten
            // är lägre än bossen ska hon ner, aldrig hoppa upp (det var "studsar på platån ovanför").
            bool playerBelow = player.py > py + 0.6f;

            // Hjälten inom direkt stamp-räckvidd (nära i sidled, AT eller över bossens nivå). Då
            // prioriteras attack framför klättring — men aldrig när hjälten är under (då gäller descend).
            bool canStomp = Math.Abs(dx) < 3f && player.py > py - 2f && !playerBelow;

            // Robotisk rytm: längre rörelse-skurar med "tänka"-pauser (ej vibrerande).
            _rhythmTimer -= fElapsedTime;
            if (_rhythmTimer <= 0f)
            {
                _moving = !_moving;
                _rhythmTimer = _moving ? MoveBurst : PauseDur;
            }

            // ── Vertikal avsikt: klättra (hjälten ovanför), descend (under) eller jaga (i nivå) ──
            // Klättermodell: "hoppa RAKT upp förbi kanten, styr in FÖRST när kanten är klarad". Då kör
            // hon aldrig in i plattformens sida och behöver aldrig backa för startsträcka (det var det
            // sköra som gav både kängurustuds och fastnandet). Hon tar en nivå i taget; väl uppe räknas
            // nästa nivå om → tvånivåers-klättring sker automatiskt.
            float moveDir = dir;
            bool  wantClimbJump = false;   // står vid en FRI startkolumn intill plattformen → hoppa upp
            bool  wantGapJump = false;     // står vid plattformskanten mot en högre platå tvärs ett gap → båg-hopp
            float climbDir = dir;          // riktning IN mot plattformen (oftast mot hjälten)

            // MÅLET ÄR HJÄLTEN, inte närmaste platå. Är hon långt bort i sidled jagar bossen
            // HORISONTELLT mot henne först (kliver av platåer ner till golvet och traverserar);
            // klättring/descend lokalt sker bara när hon är inom räckhåll. Annars klättrade hon
            // närmaste platå och studsade upp/ner i stället för att närma sig hjälten.
            if (farFromHero)
            {
                moveDir = dir;   // jaga mot hjälten; fear of heights är av → hon faller av platåer naturligt
                ClearClimbLatch();
            }
            else if (playerAbove && !canStomp && Arena != null)
            {
                int up2 = (int)py + 1 - 2;   // plattformsraden 2 tiles upp
                // Latcha en startkolumn för den här nivån: räkna om bara när nivån ändras eller den
                // latchade kolumnen inte längre har fri stigning. Då fullföljer hon en omväg (t.ex.
                // förbi hjälten till en fri sida) utan att flippa fram och tillbaka när hjälte-riktningen
                // växlar. En ny start accepteras BARA om den för henne mot hjälten (annars klättrade hon
                // närmaste platå åt fel håll i stället för att närma sig hjälten).
                if (_climbUp2 != up2 || _climbLaunch == int.MinValue || !RiseClear(Arena, _climbLaunch, up2))
                {
                    var (ok, launchCol, cdir) = FindClimbLaunch(Arena, up2, dir);
                    if (ok && LaunchTowardHero(launchCol, cdir, dx))
                    { _climbLaunch = launchCol; _climbLaunchDir = cdir; _climbUp2 = up2; }
                    else
                        ClearClimbLatch();
                }

                if (_climbLaunch != int.MinValue)
                {
                    climbDir = _climbLaunchDir;
                    // Ligger startkolumnen TVÄRS ÖVER ett gap (inte på bossens nuvarande yta — saknar stöd
                    // på hennes fot-rad)? Då kan hon inte gå dit. I stället ett GAP-HOPP: gå till sin EGEN
                    // plattformskant mot målet och hoppa ballistiskt över gapet upp på den högre platån.
                    if (!Arena.GetSolid(_climbLaunch, (int)py + 1))
                    {
                        int gapDir = _climbLaunchDir >= 0f ? 1 : -1;
                        int footRow = (int)py + 1;
                        if (!Arena.GetSolid((int)px + gapDir, footRow))   // nästa kolumn mot målet är ett stup → vid kanten
                            { moveDir = 0f; wantGapJump = true; _gapDir = gapDir; }
                        else
                            moveDir = gapDir;                              // gå fram till kanten
                    }
                    else
                    {
                        // Rakt-upp-klättring (intilliggande platå): hoppa när hon (a) är VID startkolumnen och
                        // (b) hela kroppen [px, px+1) har fri stigning (annars bonkar ena halvan undersidan).
                        float gap = _climbLaunch - px;
                        bool bodyClear = RiseClear(Arena, (int)px, up2) && RiseClear(Arena, (int)(px + 0.999f), up2);
                        if (Math.Abs(gap) < 0.6f && bodyClear)
                            { moveDir = 0f; wantClimbJump = true; }
                        else
                            moveDir = gap > 0f ? 1f : -1f;
                    }
                }
                else
                    moveDir = dir;   // ingen väg upp MOT hjälten i sikte → närma dig henne på nuvarande nivå
            }
            else if (playerBelow && Grounded && Arena != null)
            {
                ClearClimbLatch();
                // DESCEND: hjälten är under → kliv av plattformen och fall ner. Gå till NÄRMASTE kant
                // och commit:a dit — INTE mot hjältens exakta kolumn. (Tidigare följde hon hjälte-
                // riktningen, men när hjälten var rakt under flippade den varje frame hon korsade
                // hjältens kolumn → hon vägde fram och tillbaka på stället och kom aldrig ner.)
                // Fear of heights är av när hjälten inte är ovanför, så hon faller när hon når kanten.
                // Heltäckande golv (ingen kant) → jaga mot hjälten i stället.
                int footRow = (int)py + 1;
                int dl = SupportEdgeDist(Arena, footRow, -1);
                int dr = SupportEdgeDist(Arena, footRow, 1);
                if (dl == 0 && dr == 0) moveDir = dir;       // ingen kant → jaga
                else if (dl == 0)       moveDir = 1f;        // bara kant åt höger
                else if (dr == 0)       moveDir = -1f;       // bara kant åt vänster
                else                    moveDir = (dl <= dr) ? -1f : 1f;   // närmaste kanten, commit
            }
            else
                ClearClimbLatch();   // samma nivå (jaga + stamp) → ingen latch

            // Kom ihåg markriktningen hon rör sig åt — den behålls i luften (commit-drop) så hon
            // fullföljer ett avkliv åt det hållet i stället för att jakt-driva tillbaka upp på platån.
            if (Grounded && moveDir != 0f) _descendDir = moveDir;

            // Långt bort → rör sig kontinuerligt (ingen "tänka"-paus) så transporten går snabbt.
            vx = (farFromHero || _moving) ? moveDir * speed : 0f;

            // Commit-drop: när hon precis klivit av en kant (hjälten under, i luften, inte i ett klätter-
            // hopp) fortsätter hon i FALL-riktningen i stället för att hjälte-jakten (vx mot hjälten) drar
            // tillbaka henne in över platån. Annars re-grundas hon på platåns kant och VINGLAR där ett par
            // sekunder innan hon kommer loss (grundningen räcker så länge ena kroppshalvan är över kanten).
            if (playerBelow && !Grounded && !_ascending && !_gapJumping)
                vx = _descendDir * speed;

            // Avsiktliga hopp, cooldown-gatade (planerade — aldrig varje frame):
            //  • Gap-hopp: vid kanten mot en högre platå tvärs ett gap → ballistisk båge (sidofart behålls).
            //  • Klätterhopp: vid en intilliggande platå → RAKT upp (vx=0), insteg i ascend-styrningen.
            //  • Attack-hopp: mot en hjälte i nivå.
            if (Grounded && _leapTimer <= 0f && (wantGapJump || wantClimbJump || canStomp))
            {
                if (wantGapJump)
                {
                    vy = BPowerJumpVelocity;
                    vx = _gapDir * GapJumpAirSpeed;       // ballistiskt: hög sidofart över gapet, behålls i luften
                    _gapJumping = true;
                    _ascending = false;
                    _leapTimer = AscendLeapCooldown;
                }
                else if (wantClimbJump)
                {
                    vy = BPowerJumpVelocity;
                    vx = 0f;                              // rakt upp först — ingen sidledsfart in i plattformens sida
                    _ascending = true;
                    _gapJumping = false;
                    _ascendTargetRow = (int)py + 1 - 2;   // plattformsraden hon siktar på
                    _ascendDir = climbDir;
                    _leapTimer = AscendLeapCooldown;
                }
                else
                {
                    vy = GameConstants.JumpVelocity;
                    vx = dir * speed;
                    _ascending = false;
                    _gapJumping = false;
                    float baseInterval = Angry ? LeapIntervalAngry : LeapIntervalNormal;
                    _leapTimer = Angry ? baseInterval * (0.6f + (float)_rng.NextDouble() * 0.8f) : baseInterval;
                }
            }

            // Gap-hopps-styrning: ballistisk båge — behåll hög sidofart HELA vägen (till skillnad från
            // rakt-upp-klättringen) så hon bågar över gapet och dalar ner på den högre platån. Mjukare
            // stig-gravitation för höjden, men släpps om ett tak sitter strax ovanför huvudet.
            if (_gapJumping)
            {
                if (Grounded && vy >= 0f)
                    _gapJumping = false;                                   // landat
                else
                {
                    vx = _gapDir * GapJumpAirSpeed;
                    int gHeadRow = (int)py;
                    bool gCeilingClose = Arena != null && gHeadRow - 2 >= 0 &&
                        (Arena.GetSolid((int)px, gHeadRow - 1) || Arena.GetSolid((int)(px + 0.9f), gHeadRow - 1) ||
                         Arena.GetSolid((int)px, gHeadRow - 2) || Arena.GetSolid((int)(px + 0.9f), gHeadRow - 2));
                    if (vy < 0f && !gCeilingClose)
                        vy -= AscendGravityRelief * fElapsedTime;
                }
            }
            // Ascend-styrning: "hoppa rakt upp, styr in sen". Medan hon stiger och ännu inte klarat
            // plattformskanten håller hon sig i sin kolumn (vx=0) så hon inte kör in i sidan. När fötterna
            // är en bit ovanför plattformens ovansida styr hon in i sidled och DALAR ner på plattformen.
            else if (_ascending)
            {
                if (Grounded && vy >= 0f)
                    _ascending = false;                                   // landat (på plattformen eller marken)
                else
                {
                    bool clearedEdge = py <= _ascendTargetRow - 1.2f;     // fötterna ~0.2 tile ovanför plattformsytan
                    vx = clearedEdge ? _ascendDir * AscendAirSpeed : 0f;

                    // Mjukare stig-gravitation → högre, säker båge (utgångsfarten kan ej höjas; clampen
                    // kraschar allt snabbare än −11 till 0). Släpps om ett tak sitter strax ovanför huvudet.
                    int headRow = (int)py;
                    bool ceilingClose = Arena != null && headRow - 2 >= 0 &&
                        (Arena.GetSolid((int)px, headRow - 1) || Arena.GetSolid((int)(px + 0.9f), headRow - 1) ||
                         Arena.GetSolid((int)px, headRow - 2) || Arena.GetSolid((int)(px + 0.9f), headRow - 2));
                    if (vy < 0f && !ceilingClose)
                        vy -= AscendGravityRelief * fElapsedTime;
                }
            }
        }

        /// <summary>
        /// Riktning (-1/+1) till närmaste kolumn där taket (raden <paramref name="up2"/>) tar slut,
        /// dvs. plattformens kant — så hon kan ta sig ut från under en plattform och hoppa upp.
        /// 0 om ingen kant inom räckhåll.
        /// </summary>
        /// <summary>
        /// Hittar en FRI startkolumn att hoppa upp på en plattform (rad <paramref name="up2"/>) ifrån.
        /// "Fri" = hela stigningen ovanför kolumnen (mål-raden + de två raderna ovanför) är tom, så hon
        /// inte bonkar en ÖVERLIGGANDE plattform. Eftersom nivåerna ligger kant-i-kant kan en plattforms
        /// närmaste kant ligga under nästa nivås platå — då provas plattformens ANDRA kant (den fria
        /// sidan). FÖRTUR åt <paramref name="prefer"/> (hjälte-riktningen).
        /// Returnerar (hittad, startkolumn, hopp-riktning mot plattformen) eller (false, 0, 0).
        /// </summary>
        private (bool ok, int launchCol, float climbDir) FindClimbLaunch(IMapData map, int up2, float prefer)
        {
            if (up2 < 0) return (false, 0, 0f);
            int hx = (int)px;
            int p = prefer >= 0f ? 1 : -1;
            // En giltig startkolumn C har FRI stigning (RiseClear) OCH en plattform att styra in på
            // inom 2 tiles. Skanna utåt, föredragen (hjälte-)riktning först per avstånd — så hon väljer
            // ett startläge mot hjälten men hittar det konsekvent oavsett vilken sida om henne hon står
            // (annars flippade sökriktningen och hon oscillerade). farFromHero-gaten ovan ser till att
            // hon inte klättrar lokalt alls när hjälten är långt bort.
            for (int d = 0; d <= ClimbScanRange; d++)
            {
                for (int k = 0; k < 2; k++)
                {
                    if (d == 0 && k == 1) continue;
                    int c = hx + (k == 0 ? p : -p) * d;
                    if (c < 0 || c >= map.Width) continue;
                    if (!RiseClear(map, c, up2)) continue;
                    for (int j = 1; j <= 2; j++)
                    {
                        if (map.GetSolid(c + j, up2)) return (true, c, 1f);
                        if (map.GetSolid(c - j, up2)) return (true, c, -1f);
                    }
                }
            }
            return (false, 0, 0f);
        }

        /// <summary>Sant om en straight-up-stigning från kolumn <paramref name="col"/> är fri hela vägen
        /// förbi mål-plattformen (raderna up2, up2−1, up2−2) — ingen överliggande platå i vägen.</summary>
        private static bool RiseClear(IMapData map, int col, int up2)
            => !map.GetSolid(col, up2)
            && !map.GetSolid(col, up2 - 1)
            && (up2 - 2 < 0 || !map.GetSolid(col, up2 - 2));

        /// <summary>
        /// Sant om det är vettigt att klättra på den här starten med tanke på hjälten: den för henne MOT
        /// hjälten. Antingen genom att hon går mot hjälten för att nå startkolumnen, eller — om hon redan
        /// står där — att plattformen hon hoppar mot ligger åt hjälte-hållet. (Hjälten ~rakt ovanför →
        /// alltid ok att klättra upp.) Det hindrar att hon klättrar närmaste platå åt FEL håll.
        /// </summary>
        private bool LaunchTowardHero(int launchCol, float climbDir, float dx)
        {
            if (Math.Abs(dx) < 1.5f) return true;
            float toLaunch = launchCol - px;
            if (Math.Abs(toLaunch) > 0.6f)
                return Math.Sign(toLaunch) == Math.Sign(dx);
            return Math.Sign(climbDir) == Math.Sign(dx);
        }

        /// <summary>Nollställer den latchade klätter-starten (när hon inte längre klättrar mot en plattform).</summary>
        private void ClearClimbLatch()
        {
            _climbLaunch = int.MinValue;
            _climbUp2 = int.MinValue;
        }

        /// <summary>
        /// Avstånd i tiles åt riktning <paramref name="d"/> (±1) till kanten på plattformen hon STÅR på
        /// (första kolumn där raden <paramref name="footRow"/> slutar vara solid) — för descend. 0 om
        /// ingen kant inom räckhåll åt det hållet (heltäckande golv).
        /// </summary>
        private int SupportEdgeDist(IMapData map, int footRow, int d)
        {
            int hx = (int)px;
            for (int i = 1; i <= ClimbScanRange; i++)
                if (!map.GetSolid(hx + d * i, footRow)) return i;
            return 0;
        }

        // ── Damm-effekt (akt 1→2-upplösning) ───────────────────────────────────────
        // När hon löst upp sig lossnar en handfull grå flagor som faller och bleknar — den
        // "dramatiska pausen" innan svärmen kommer. Allt i tile-koordinater (px/py-rymd);
        // hastigheter i tiles/sekund. Rent visuellt, ingen kollision.
        private void SpawnDust()
        {
            for (int i = 0; i < DustCount; i++)
            {
                _dustX[i]  = px + (float)_rng.NextDouble();              // sprid över kroppsbredden [px, px+1)
                _dustY[i]  = py + (float)_rng.NextDouble();
                _dustVx[i] = (float)(_rng.NextDouble() * 2 - 1) * 0.5f;  // liten sidodrift
                _dustVy[i] = (float)_rng.NextDouble() * 0.6f;           // börjar falla nedåt
            }
            _dustTimer = DustFallDuration;
        }

        private void UpdateDust(float dt)
        {
            for (int i = 0; i < DustCount; i++)
            {
                _dustVy[i] += DustGravity * dt;   // gravitation → dammet accelererar nedåt
                _dustX[i]  += _dustVx[i] * dt;
                _dustY[i]  += _dustVy[i] * dt;
            }
        }

        private void DrawDust(IRenderContext gfx, float ox, float oy)
        {
            if (_dustTimer <= 0f) return;
            float fade = Math.Clamp(_dustTimer / DustFallDuration, 0f, 1f);   // bleknar mot slutet
            byte g = (byte)((int)(150 * fade) + 40);
            var col = new RenderColor(g, g, g);
            for (int i = 0; i < DustCount; i++)
                gfx.FillRect(ToPixel(_dustX[i], ox), ToPixel(_dustY[i], oy), 1, 1, col);
        }

        // Anropas varje frame under horisontell rörelse (turnPatrol=true endast vid riktig
        // Avsiktligt en no-op. Här fanns tidigare TVÅ mekanismer som båda visade sig kontraproduktiva:
        //  (a) "klättra-hopp" vid varje väggträff → kängurustudsandet (reflex-hopp som körde över
        //      Behaviours planerade klättring), och
        //  (b) "fear of heights" som nålade fast henne vid en plattformskant när hjälten var ovanför →
        //      hon FRÖS på en mellanplattform i stället för att kliva av och positionera om mot nästa
        //      nivå (det var "tappar konceptet med tre nivåer / slutar jaga").
        // All vertikal navigering — klättra rakt upp, descenda till närmaste kant, traversera mellan
        // plattformar genom att kliva av och om-klättra — sköts nu i Behaviour. En väggträff stoppar
        // henne bara (vx nollas redan av kollisionssystemet) så Behaviour kan ompositionera nästa frame.
        public override void OnWallCollision(ref float newX, bool turnPatrol, bool movingLeft, IMapData map, float fBorder)
        {
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
            vy = -4f;   // rekyl-studs UPP — separerar henne vertikalt från hjälten direkt (annars blir hon
                        // stående ovanpå och re-skadar när i-frames går ut innan hon glider av i sidled)
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            // Dammet ritas även när kroppen är dold (det faller kvar efter att hon försvunnit).
            DrawDust(gfx, ox, oy);

            // Dold efter akt 1→2-exiten: kroppen lever kvar för akt 4 men ritas inte under tiden.
            if (_hidden) return;

            // Blinka under healing-time som träff-feedback (hoppa över ritning varannan "tick").
            if (_invulnTimer > 0f && ((int)(_invulnTimer * 20f) & 1) == 0)
                return;

            int screenX = ToPixel(px, ox);
            int screenY = ToPixel(py, oy);

            // Vänd ansiktet efter färdriktningen. TurnedTo sätts av basklassens Update utifrån vx
            // (Right = 0 default, Left = 1) → sprite:n speglas horisontellt när hon går vänster.
            bool faceLeft = TurnedTo == Enum.PlayerOrientation.Left;

            // Akt 4: pixel-glitch — sprite:n ritas i horisontella band där en del förskjuts i
            // sidled (tear), plus främmande cyan/magenta-pixlar som "bryter sönder" henne. Allt
            // trappas upp med _glitch (samma nivå + närhet). Tydligt att något är annorlunda.
            if (_glitch > 0f)
            {
                for (int sy = 0; sy < 16; sy += 2)
                {
                    int off = 0;
                    if (_rng.NextDouble() < _glitch * 0.7f)
                        off = (int)((_rng.NextDouble() * 2 - 1) * (1f + _glitch * 4f));
                    if (faceLeft)
                        gfx.DrawPartialSpriteFlippedX(SpriteId, screenX + off, screenY + sy, 0, sy, 16, 2);
                    else
                        gfx.DrawPartialSprite(SpriteId, screenX + off, screenY + sy, 0, sy, 16, 2);
                }

                int shards = (int)(_glitch * 5f);
                for (int i = 0; i < shards; i++)
                {
                    int bx = screenX + _rng.Next(0, 14);
                    int by = screenY + _rng.Next(0, 14);
                    var col = _rng.Next(2) == 0 ? new RenderColor(40, 230, 230) : new RenderColor(230, 40, 230);
                    gfx.FillRect(bx, by, 2 + _rng.Next(0, 2), 2, col);
                }
                return;
            }

            if (faceLeft)
                gfx.DrawPartialSpriteFlippedX(SpriteId, screenX, screenY, 0, 0, 16, 16);
            else
                gfx.DrawPartialSprite(SpriteId, screenX, screenY, 0, 0, 16, 16);
        }
    }
}
