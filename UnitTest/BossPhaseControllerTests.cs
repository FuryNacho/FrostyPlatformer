using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    [TestClass]
    public class BossPhaseControllerTests
    {
        private static BossPhaseController Make()
            => new BossPhaseController(
                mirrorHealth: 10, swarmHealth: 8, giantHealth: 12,
                maxWarmth: 100f, warmthDrainPerSecond: 2f, warmthRegenPerSecond: 5f);

        // ── Startläge ─────────────────────────────────────────────────────────────
        [TestMethod]
        public void Start_InMirrorAct_FullHealthAndWarmth()
        {
            var c = Make();
            Assert.AreEqual(BossAct.Mirror, c.CurrentAct);
            Assert.AreEqual(10, c.BossHealth);
            Assert.AreEqual(10, c.BossMaxHealth);
            Assert.AreEqual(100f, c.Warmth);
            Assert.AreEqual(BossOutcome.Ongoing, c.Outcome);
            Assert.IsTrue(c.IsDamageAct);
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

        // ── Värmedränering ─────────────────────────────────────────────────────────
        [TestMethod]
        public void Tick_DrainsWarmthInDamageActs()
        {
            var c = Make();
            c.Tick(2f);                          // 2 s * 2/s = 4
            Assert.AreEqual(96f, c.Warmth, 0.001f);
        }

        [TestMethod]
        public void Tick_WarmthHittingZero_PlayerLoses()
        {
            var c = Make();
            c.Tick(60f);                         // mer än nog för att tömma
            Assert.AreEqual(0f, c.Warmth);
            Assert.AreEqual(BossOutcome.PlayerLost, c.Outcome);
        }

        [TestMethod]
        public void Tick_NoOpAfterLoss()
        {
            var c = Make();
            c.Tick(60f);                         // förlust
            c.TakeHit(5);
            Assert.AreEqual(10, c.BossHealth, "TakeHit ska vara no-op efter förlust.");
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
        public void Acceptance_TickReversesDrain_WarmthRegenerates()
        {
            var c = Make();
            c.TakeHit(10); c.TakeHit(8);                  // -> Giant (skade-akt)
            c.Tick(10f);                                  // dränera: 10s * 2/s = 20 -> 80
            Assert.AreEqual(80f, c.Warmth, 0.001f);
            c.TakeHit(12);                                // -> Acceptance
            c.Tick(2f);                                   // regen: 2s * 5/s = 10 -> 90
            Assert.AreEqual(90f, c.Warmth, 0.001f);
        }

        [TestMethod]
        public void Acceptance_RegenCapsAtMax()
        {
            var c = Make();
            c.TakeHit(10); c.TakeHit(8); c.TakeHit(12);   // -> Acceptance (värme full)
            c.Tick(100f);
            Assert.AreEqual(c.MaxWarmth, c.Warmth, 0.001f);
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

        // ── Energi-länkad dränering (helig regel) ──────────────────────────────────
        [TestMethod]
        public void ComputeWarmthDrain_ZeroEnergy_ReturnsHarshestDrain()
            => Assert.AreEqual(5f, BossPhaseController.ComputeWarmthDrain(0, 100, 5f, 1f), 0.001f);

        [TestMethod]
        public void ComputeWarmthDrain_FullEnergy_ReturnsGentlestDrain()
            => Assert.AreEqual(1f, BossPhaseController.ComputeWarmthDrain(100, 100, 5f, 1f), 0.001f);

        [TestMethod]
        public void ComputeWarmthDrain_HalfEnergy_Interpolates()
            => Assert.AreEqual(3f, BossPhaseController.ComputeWarmthDrain(50, 100, 5f, 1f), 0.001f);

        [TestMethod]
        public void ComputeWarmthDrain_ClampsAboveMax()
            => Assert.AreEqual(1f, BossPhaseController.ComputeWarmthDrain(200, 100, 5f, 1f), 0.001f);

        [TestMethod]
        public void ComputeWarmthDrain_ZeroMaxEnergy_FallsBackToHarshest()
            => Assert.AreEqual(5f, BossPhaseController.ComputeWarmthDrain(0, 0, 5f, 1f), 0.001f);
    }
}
