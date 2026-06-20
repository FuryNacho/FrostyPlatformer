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
    }

    /// <summary>
    /// Driver slutbossens akt-progression, boss-hälsa per akt och fake-out-övergångar.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Tillståndsmaskin (ren domänlogik, motor-agnostisk).
    ///
    /// MOTIVERING:
    /// FINAL_BOSS_PLAN kräver att fas-logiken går att testa utan hårdvara (likt EditorMath).
    /// Striden är "koreografi ovanpå motorn", så akt-ordning och fake-outs hör hemma i en
    /// fristående, deterministisk klass — inte i den motorbundna Creature-koden (SRP/DIP).
    /// Rendering, input och spawn läser tillståndet härifrån men matar bara in råa händelser
    /// (träff, gå-mot-henne).
    ///
    /// Värmemätaren (en tids-press kopplad till hjältens energi) togs bort 2026-06-20: under
    /// speltest visade den sig inte tillföra något till bossupplevelsen — svårighetsgraden
    /// bärs av striden själv, inte av en klocka. Förlust sker numera bara via hjältens hälsa.
    ///
    /// ANVÄNDNING:
    /// Skapas när boss-arenan (mapten) laddas. GameplayState anropar TakeHit() när bossen
    /// stampas och ApproachToward() i akt 4. HUD läser BossHealth för baren och
    /// ConsumeFakeOut() för "baren reser sig igen"-beatet.
    /// </remarks>
    public sealed class BossPhaseController
    {
        // Boss-HP för de tre skade-akterna (akt 4 är inte skadebaserad).
        private readonly int _mirrorHealth;
        private readonly int _swarmHealth;
        private readonly int _giantHealth;

        // Akt 4: hur långt spelaren gått mot henne (0..1).
        private float _approach;

        /// <summary>Aktuell akt. Börjar i <see cref="BossAct.Mirror"/>.</summary>
        public BossAct CurrentAct { get; private set; }

        /// <summary>Bossens nuvarande hälsa i aktuell skade-akt (0 i akt 4 / Resolved).</summary>
        public int BossHealth { get; private set; }

        /// <summary>Bossens maxhälsa i aktuell skade-akt — för HUD-barens skala.</summary>
        public int BossMaxHealth { get; private set; }

        /// <summary>Spelarens framsteg mot henne i akt 4 (0..1). Endast meningsfullt i akt 4.</summary>
        public float ApproachProgress => _approach;

        /// <summary>Stridens utfall. TakeHit/ApproachToward blir no-ops när den inte är Ongoing.</summary>
        public BossOutcome Outcome { get; private set; }

        /// <summary>Sant när en fake-out-övergång precis skett ("baren reser sig igen").</summary>
        public bool FakeOutPending { get; private set; }

        /// <summary>
        /// Skapar en boss-fas-controller.
        /// </summary>
        /// <param name="mirrorHealth">Boss-HP i akt 1.</param>
        /// <param name="swarmHealth">Boss-HP i akt 2.</param>
        /// <param name="giantHealth">Boss-HP i akt 3.</param>
        /// <param name="startAct">
        /// Akten striden börjar i. Normalt <see cref="BossAct.Mirror"/> (hela striden); andra
        /// värden låter dev-läget hoppa in mitt i (DevConfig.BossStartAct). Akt-baren laddas
        /// med startaktens hälsa; skadefria akter (Acceptance/Resolved) börjar utan bar.
        /// </param>
        public BossPhaseController(
            int mirrorHealth = 30,
            int swarmHealth = 24,
            int giantHealth = 40,
            BossAct startAct = BossAct.Mirror)
        {
            _mirrorHealth = mirrorHealth;
            _swarmHealth = swarmHealth;
            _giantHealth = giantHealth;

            CurrentAct = startAct;
            (BossMaxHealth, BossHealth) = startAct switch
            {
                BossAct.Mirror => (_mirrorHealth, _mirrorHealth),
                BossAct.Swarm  => (_swarmHealth, _swarmHealth),
                BossAct.Giant  => (_giantHealth, _giantHealth),
                _              => (0, 0),   // Acceptance/Resolved: ingen skade-bar
            };
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
