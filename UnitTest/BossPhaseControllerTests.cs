using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    [TestClass]
    public class BossPhaseControllerTests
    {
        private static BossPhaseController Make()
            => new BossPhaseController(mirrorHealth: 10, swarmHealth: 8, giantHealth: 12);

        // ── Startläge ─────────────────────────────────────────────────────────────
        [TestMethod]
        public void Start_InMirrorAct_FullHealth()
        {
            var c = Make();
            Assert.AreEqual(BossAct.Mirror, c.CurrentAct);
            Assert.AreEqual(10, c.BossHealth);
            Assert.AreEqual(10, c.BossMaxHealth);
            Assert.AreEqual(BossOutcome.Ongoing, c.Outcome);
            Assert.IsTrue(c.IsDamageAct);
        }

        [TestMethod]
        public void Start_InGiantAct_LoadsGiantBar()
        {
            var c = new BossPhaseController(
                mirrorHealth: 10, swarmHealth: 8, giantHealth: 12, startAct: BossAct.Giant);
            Assert.AreEqual(BossAct.Giant, c.CurrentAct);
            Assert.AreEqual(12, c.BossHealth);
            Assert.AreEqual(12, c.BossMaxHealth);
            Assert.AreEqual(BossOutcome.Ongoing, c.Outcome);
            Assert.IsTrue(c.IsDamageAct);
        }

        [TestMethod]
        public void Start_InAcceptanceAct_HasNoDamageBar()
        {
            var c = new BossPhaseController(
                mirrorHealth: 10, swarmHealth: 8, giantHealth: 12, startAct: BossAct.Acceptance);
            Assert.AreEqual(BossAct.Acceptance, c.CurrentAct);
            Assert.AreEqual(0, c.BossMaxHealth);
            Assert.IsFalse(c.IsDamageAct);
            Assert.AreEqual(BossOutcome.Ongoing, c.Outcome);
        }

        [TestMethod]
        public void Start_InGiantAct_DepletingAdvancesToAcceptance()
        {
            var c = new BossPhaseController(
                mirrorHealth: 10, swarmHealth: 8, giantHealth: 12, startAct: BossAct.Giant);
            c.TakeHit(12);
            Assert.AreEqual(BossAct.Acceptance, c.CurrentAct,
                "Att tömma jätte-baren från en dev-start ska gå vidare som vanligt.");
        }

        // ── Skada & akt-övergångar ─────────────────────────────────────────────────
        [TestMethod]
        public void TakeHit_ReducesBossHealth()
        {
            var c = Make();
            c.TakeHit(3);
            Assert.AreEqual(7, c.BossHealth);
        }

        [TestMethod]
        public void TakeHit_IgnoresNonPositiveDamage()
        {
            var c = Make();
            c.TakeHit(0);
            c.TakeHit(-5);
            Assert.AreEqual(10, c.BossHealth);
        }

        [TestMethod]
        public void DepletingMirror_AdvancesToSwarm_WithFakeOut()
        {
            var c = Make();
            c.TakeHit(10);
            Assert.AreEqual(BossAct.Swarm, c.CurrentAct);
            Assert.AreEqual(8, c.BossHealth, "Hälsan ska återställas till nästa akts max.");
            Assert.IsTrue(c.FakeOutPending, "En fake-out ska flaggas vid övergången.");
        }

        [TestMethod]
        public void DepletingSwarm_AdvancesToGiant()
        {
            var c = Make();
            c.TakeHit(10);          // Mirror -> Swarm
            c.TakeHit(8);           // Swarm -> Giant
            Assert.AreEqual(BossAct.Giant, c.CurrentAct);
            Assert.AreEqual(12, c.BossHealth);
        }

        [TestMethod]
        public void DepletingGiant_AdvancesToAcceptance_NoHealth()
        {
            var c = Make();
            c.TakeHit(10);          // -> Swarm
            c.TakeHit(8);           // -> Giant
            c.TakeHit(12);          // -> Acceptance (den stora vändningen)
            Assert.AreEqual(BossAct.Acceptance, c.CurrentAct);
            Assert.AreEqual(0, c.BossHealth);
            Assert.IsFalse(c.IsDamageAct, "Akt 4 är inte skadebaserad.");
        }

        [TestMethod]
        public void Overkill_DoesNotGoNegative()
        {
            var c = Make();
            c.TakeHit(999);
            Assert.AreEqual(8, c.BossHealth);   // gick vidare till Swarm, ingen negativ HP
            Assert.AreEqual(BossAct.Swarm, c.CurrentAct);
        }

        // ── Fake-out konsumeras en gång ────────────────────────────────────────────
        [TestMethod]
        public void ConsumeFakeOut_TrueOnceThenFalse()
        {
            var c = Make();
            c.TakeHit(10);
            Assert.IsTrue(c.ConsumeFakeOut());
            Assert.IsFalse(c.ConsumeFakeOut());
        }

        // ── Akt 4: acceptans ───────────────────────────────────────────────────────
        [TestMethod]
        public void Acceptance_TakeHitDoesNothing()
        {
            var c = Make();
            c.TakeHit(10); c.TakeHit(8); c.TakeHit(12);   // -> Acceptance
            c.TakeHit(5);
            Assert.AreEqual(BossAct.Acceptance, c.CurrentAct);
            Assert.AreEqual(BossOutcome.Ongoing, c.Outcome);
        }

        [TestMethod]
        public void Acceptance_ApproachingFully_WinsAndResolves()
        {
            var c = Make();
            c.TakeHit(10); c.TakeHit(8); c.TakeHit(12);   // -> Acceptance
            c.ApproachToward(0.4f);
            Assert.AreEqual(BossAct.Acceptance, c.CurrentAct);
            Assert.AreEqual(BossOutcome.Ongoing, c.Outcome);
            c.ApproachToward(0.7f);                        // totalt > 1
            Assert.AreEqual(BossAct.Resolved, c.CurrentAct);
            Assert.AreEqual(BossOutcome.PlayerWon, c.Outcome);
        }

        [TestMethod]
        public void Approach_IgnoredOutsideAcceptance()
        {
            var c = Make();
            c.ApproachToward(1f);                          // i Mirror — ska ignoreras
            Assert.AreEqual(0f, c.ApproachProgress);
            Assert.AreEqual(BossAct.Mirror, c.CurrentAct);
        }
    }
}
