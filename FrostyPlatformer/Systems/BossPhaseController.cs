#nullable enable
using System;

namespace FrostyPlatformer.Systems
{
    /// <summary>Slutbossens fyra akter plus det avslutade tillståndet.</summary>
    public enum BossAct
    {
        /// <summary>Akt 1 — Spegeln: plattforms-duell mot spegel-Scarlet.</summary>
        Mirror,
        /// <summary>Akt 2 — De många: svärm av kopior + istappsregn.</summary>
        Swarm,
        /// <summary>Akt 3 — Jätten i kölden: arena-hazard-boss med svagpunkt.</summary>
        Giant,
        /// <summary>Akt 4 — Acceptans: inverterad final, vinst genom att gå mot henne.</summary>
        Acceptance,
        /// <summary>Striden är över (acceptansen fullbordad) → hemkomst.</summary>
        Resolved,
    }

    /// <summary>Stridens utfall sett från spelaren.</summary>
    public enum BossOutcome
    {
        /// <summary>Striden pågår.</summary>
        Ongoing,
        /// <summary>Spelaren fullbordade akt 4 — acceptansen.</summary>
        PlayerWon,
        /// <summary>Scarlets värme slocknade innan striden var klar.</summary>
        PlayerLost,
    }

    /// <summary>
    /// Driver slutbossens akt-progression, boss-hälsa per akt, fake-out-övergångar
    /// och Scarlets värmemätare (den energi-länkade dräneringen).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Tillståndsmaskin (ren domänlogik, motor-agnostisk).
    ///
    /// MOTIVERING:
    /// FINAL_BOSS_PLAN kräver att fas-logiken går att testa utan hårdvara (likt EditorMath).
    /// Striden är "koreografi ovanpå motorn", så akt-ordning, fake-outs och dränering hör
    /// hemma i en fristående, deterministisk klass — inte i den motorbundna Creature-koden
    /// (SRP/DIP). Rendering, input och spawn läser tillståndet härifrån men matar bara in
    /// råa händelser (träff, tick, gå-mot-henne).
    ///
    /// ANVÄNDNING:
    /// Skapas när boss-arenan (mapten) laddas. GameplayState anropar Tick() varje frame,
    /// TakeHit() när bossen stampas, och ApproachToward() i akt 4. HUD läser BossHealth/
    /// Warmth för barerna och ConsumeFakeOut() för "baren reser sig igen"-beatet.
    /// </remarks>
    public sealed class BossPhaseController
    {
        // Boss-HP för de tre skade-akterna (akt 4 är inte skadebaserad).
        private readonly int _mirrorHealth;
        private readonly int _swarmHealth;
        private readonly int _giantHealth;

        // Akt 4: hur långt spelaren gått mot henne (0..1) och regenereringstakt.
        private float _approach;
        private readonly float _warmthRegenPerSecond;

        /// <summary>Aktuell akt. Börjar i <see cref="BossAct.Mirror"/>.</summary>
        public BossAct CurrentAct { get; private set; }

        /// <summary>Bossens nuvarande hälsa i aktuell skade-akt (0 i akt 4 / Resolved).</summary>
        public int BossHealth { get; private set; }

        /// <summary>Bossens maxhälsa i aktuell skade-akt — för HUD-barens skala.</summary>
        public int BossMaxHealth { get; private set; }

        /// <summary>Scarlets värme/laddning (0..<see cref="MaxWarmth"/>). Når den 0 förlorar spelaren.</summary>
        public float Warmth { get; private set; }

        /// <summary>Maxvärde för värmemätaren.</summary>
        public float MaxWarmth { get; }

        /// <summary>
        /// Värmedränering per sekund under skade-akterna. Sätts av spelaren utifrån
        /// insamlad energi via <see cref="ComputeWarmthDrain"/> (mer energi → långsammare).
        /// </summary>
        public float WarmthDrainPerSecond { get; set; }

        /// <summary>Spelarens framsteg mot henne i akt 4 (0..1). Endast meningsfullt i akt 4.</summary>
        public float ApproachProgress => _approach;

        /// <summary>Stridens utfall. Tick/TakeHit/ApproachToward blir no-ops när den inte är Ongoing.</summary>
        public BossOutcome Outcome { get; private set; }

        /// <summary>Sant när en fake-out-övergång precis skett ("baren reser sig igen").</summary>
        public bool FakeOutPending { get; private set; }

        /// <summary>
        /// Skapar en boss-fas-controller.
        /// </summary>
        /// <param name="mirrorHealth">Boss-HP i akt 1.</param>
        /// <param name="swarmHealth">Boss-HP i akt 2.</param>
        /// <param name="giantHealth">Boss-HP i akt 3.</param>
        /// <param name="maxWarmth">Värmemätarens maxvärde (även startvärde).</param>
        /// <param name="warmthDrainPerSecond">Initial dräneringstakt (sätts normalt via <see cref="ComputeWarmthDrain"/>).</param>
        /// <param name="warmthRegenPerSecond">Hur snabbt värmen återvänder i akt 4 (acceptans värmer).</param>
        /// <param name="startAct">
        /// Akten striden börjar i. Normalt <see cref="BossAct.Mirror"/> (hela striden); andra
        /// värden låter dev-läget hoppa in mitt i (DevConfig.BossStartAct). Akt-baren laddas
        /// med startaktens hälsa; skadefria akter (Acceptance/Resolved) börjar utan bar.
        /// </param>
        public BossPhaseController(
            int mirrorHealth = 30,
            int swarmHealth = 24,
            int giantHealth = 40,
            float maxWarmth = 100f,
            float warmthDrainPerSecond = 2f,
            float warmthRegenPerSecond = 6f,
            BossAct startAct = BossAct.Mirror)
        {
            _mirrorHealth = mirrorHealth;
            _swarmHealth = swarmHealth;
            _giantHealth = giantHealth;
            MaxWarmth = maxWarmth;
            WarmthDrainPerSecond = warmthDrainPerSecond;
            _warmthRegenPerSecond = warmthRegenPerSecond;

            CurrentAct = startAct;
            (BossMaxHealth, BossHealth) = startAct switch
            {
                BossAct.Mirror => (_mirrorHealth, _mirrorHealth),
                BossAct.Swarm  => (_swarmHealth, _swarmHealth),
                BossAct.Giant  => (_giantHealth, _giantHealth),
                _              => (0, 0),   // Acceptance/Resolved: ingen skade-bar
            };
            Warmth = maxWarmth;
            Outcome = startAct == BossAct.Resolved ? BossOutcome.PlayerWon : BossOutcome.Ongoing;
        }

        /// <summary>Sant om aktuell akt är en skade-akt (stamp gör skada på bossen).</summary>
        public bool IsDamageAct =>
            CurrentAct is BossAct.Mirror or BossAct.Swarm or BossAct.Giant;

        /// <summary>
        /// Tar emot ett stamp-träff. Endast verksam i skade-akter. När bossens hälsa når 0
        /// utlöses en fake-out och striden går vidare till nästa akt.
        /// </summary>
        /// <param name="damage">Skada (≥0). Negativa värden ignoreras.</param>
        public void TakeHit(int damage)
        {
            if (Outcome != BossOutcome.Ongoing || !IsDamageAct || damage <= 0)
                return;

            BossHealth -= damage;
            if (BossHealth > 0)
                return;

            BossHealth = 0;
            AdvanceFromDamageAct();
        }

        /// <summary>
        /// Stegar tiden framåt: dränerar värmen i skade-akterna, eller återför den i akt 4.
        /// Om värmen når 0 under en skade-akt förlorar spelaren.
        /// </summary>
        /// <param name="elapsedSeconds">Förfluten tid sedan föregående tick.</param>
        public void Tick(float elapsedSeconds)
        {
            if (Outcome != BossOutcome.Ongoing || elapsedSeconds <= 0f)
                return;

            if (CurrentAct == BossAct.Acceptance)
            {
                // Acceptans värmer — dräneringen vänder.
                Warmth = Math.Min(MaxWarmth, Warmth + _warmthRegenPerSecond * elapsedSeconds);
                return;
            }

            if (!IsDamageAct)
                return;

            Warmth -= WarmthDrainPerSecond * elapsedSeconds;
            if (Warmth <= 0f)
            {
                Warmth = 0f;
                Outcome = BossOutcome.PlayerLost;
            }
        }

        /// <summary>
        /// Akt 4-input: spelaren går mot henne istället för att slåss. Framsteget ackumuleras;
        /// när det når 1.0 smälter de samman och striden är vunnen.
        /// </summary>
        /// <param name="amount">Hur mycket närmande detta tick bidrar med (≥0).</param>
        public void ApproachToward(float amount)
        {
            if (Outcome != BossOutcome.Ongoing || CurrentAct != BossAct.Acceptance || amount <= 0f)
                return;

            _approach = Math.Min(1f, _approach + amount);
            if (_approach >= 1f)
            {
                CurrentAct = BossAct.Resolved;
                Outcome = BossOutcome.PlayerWon;
            }
        }

        /// <summary>
        /// Läser och nollställer fake-out-flaggan. HUD anropar denna för att spela
        /// "baren reser sig igen"-effekten exakt en gång per övergång.
        /// </summary>
        public bool ConsumeFakeOut()
        {
            if (!FakeOutPending)
                return false;
            FakeOutPending = false;
            return true;
        }

        /// <summary>
        /// Beräknar värmedränering per sekund utifrån insamlad energi. Mer energi →
        /// långsammare dränering. Helig regel (FINAL_BOSS_PLAN §4): även 0 energi ska gå
        /// att klara — insamling köper marginal, aldrig en mur. Därför är dränering vid 0
        /// energi taket och vid full energi golvet.
        /// </summary>
        /// <param name="collectedEnergy">Hur mycket energi spelaren samlat (0..maxEnergy).</param>
        /// <param name="maxEnergy">Maximal energi i spelet (>0).</param>
        /// <param name="drainAtZeroEnergy">Dränering vid 0 energi (den tuffaste, men klarbara).</param>
        /// <param name="drainAtFullEnergy">Dränering vid full energi (den bekvämaste).</param>
        public static float ComputeWarmthDrain(
            int collectedEnergy, int maxEnergy,
            float drainAtZeroEnergy, float drainAtFullEnergy)
        {
            if (maxEnergy <= 0)
                return drainAtZeroEnergy;

            float t = Math.Clamp(collectedEnergy / (float)maxEnergy, 0f, 1f);
            return drainAtZeroEnergy + (drainAtFullEnergy - drainAtZeroEnergy) * t;
        }

        // Går vidare från en avklarad skade-akt till nästa akt och utlöser fake-out-beatet.
        private void AdvanceFromDamageAct()
        {
            FakeOutPending = true;
            switch (CurrentAct)
            {
                case BossAct.Mirror:
                    CurrentAct = BossAct.Swarm;
                    BossMaxHealth = _swarmHealth;
                    BossHealth = _swarmHealth;
                    break;
                case BossAct.Swarm:
                    CurrentAct = BossAct.Giant;
                    BossMaxHealth = _giantHealth;
                    BossHealth = _giantHealth;
                    break;
                case BossAct.Giant:
                    // Den stora vändningen: jätten faller men sätter ihop sig till henne igen.
                    CurrentAct = BossAct.Acceptance;
                    BossMaxHealth = 0;
                    BossHealth = 0;
                    break;
            }
        }
    }
}
