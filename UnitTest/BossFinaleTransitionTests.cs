using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för slut-övergångens koreografi (akt 4 → slutskärm): fas-ordning, boss-glitch-
    /// ramp, cirkelns tillväxt, poff-triggern och klart-flaggan. Ren logik — ingen rendering.
    /// Fas-längderna: glitch-hold 0.45s, poff+växa 0.95s, vit-hold 0.20s → totalt 1.60s.
    /// </summary>
    [TestClass]
    public class BossFinaleTransitionTests
    {
        // ── Startläge ─────────────────────────────────────────────────────────────
        [TestMethod]
        public void Start_InGlitchHold_NothingVisibleYet()
        {
            var t = new BossFinaleTransition();
            Assert.AreEqual(BossFinaleTransition.Phase.GlitchHold, t.CurrentPhase);
            Assert.AreEqual(0f, t.CircleGrowth01, 1e-4);
            Assert.AreEqual(0f, t.BossGlitch01, 1e-4);
            Assert.AreEqual(0f, t.PoffProgress01, 1e-4);
            Assert.IsFalse(t.BossPoffed);
            Assert.IsFalse(t.IsComplete);
        }

        // ── Fas A: glitch-hold ────────────────────────────────────────────────────
        [TestMethod]
        public void GlitchHold_RampsBossGlitch_NoCircleYet()
        {
            var t = new BossFinaleTransition();
            t.Update(0.225f);   // halvvägs genom glitch-hold (0.45)
            Assert.AreEqual(BossFinaleTransition.Phase.GlitchHold, t.CurrentPhase);
            Assert.AreEqual(0.5f, t.BossGlitch01, 0.02f);
            Assert.AreEqual(0f, t.CircleGrowth01, 1e-4);
            Assert.IsFalse(t.BossPoffed);
        }

        [TestMethod]
        public void GlitchHold_ReachesFullGlitch_AtEndOfHold()
        {
            var t = new BossFinaleTransition();
            t.Update(0.45f);
            Assert.AreEqual(1f, t.BossGlitch01, 1e-4);
        }

        // ── Poff ──────────────────────────────────────────────────────────────────
        [TestMethod]
        public void TryConsumePoff_FalseBeforePoff()
        {
            var t = new BossFinaleTransition();
            t.Update(0.30f);   // fortfarande i glitch-hold
            Assert.IsFalse(t.TryConsumePoff());
            Assert.IsFalse(t.BossPoffed);
        }

        [TestMethod]
        public void TryConsumePoff_TrueExactlyOnce_AtPoffStart()
        {
            var t = new BossFinaleTransition();
            t.Update(0.50f);   // förbi glitch-hold → poffen ska utlösas
            Assert.IsTrue(t.BossPoffed);
            Assert.IsTrue(t.TryConsumePoff(), "poffen ska utlösas första gången");
            Assert.IsFalse(t.TryConsumePoff(), "poffen ska bara utlösas en gång");
            Assert.IsFalse(t.TryConsumePoff());
        }

        [TestMethod]
        public void PoffProgress_GrowsAfterPoff_ReachesFull()
        {
            var t = new BossFinaleTransition();
            t.Update(0.45f);                       // precis vid poffen
            Assert.AreEqual(0f, t.PoffProgress01, 1e-4);
            t.Update(0.20f);                       // 0.20 in i explosionen (av 0.40)
            Assert.AreEqual(0.5f, t.PoffProgress01, 0.02f);
            t.Update(0.30f);                       // klart förbi 0.40 → full
            Assert.AreEqual(1f, t.PoffProgress01, 1e-4);
        }

        // ── Fas B: cirkeln växer ──────────────────────────────────────────────────
        [TestMethod]
        public void CircleGrowth_IsMonotonicAndReachesFull()
        {
            var t = new BossFinaleTransition();
            t.Update(0.45f);                       // just innan cirkeln börjar
            float prev = t.CircleGrowth01;
            Assert.AreEqual(0f, prev, 1e-4);

            for (int i = 0; i < 10; i++)
            {
                t.Update(0.10f);
                Assert.IsTrue(t.CircleGrowth01 >= prev, "tillväxten ska aldrig minska");
                prev = t.CircleGrowth01;
            }
            // Efter 0.45 + 1.00 = 1.45s är vi förbi poff-grow-fasen (slutar 1.40) → full täckning.
            Assert.AreEqual(1f, t.CircleGrowth01, 1e-4);
        }

        [TestMethod]
        public void CircleGrowth_ClampedToOne_DuringWhiteHold()
        {
            var t = new BossFinaleTransition();
            t.Update(1.45f);   // in i vit-hold-fasen (1.40–1.60)
            Assert.AreEqual(BossFinaleTransition.Phase.WhiteHold, t.CurrentPhase);
            Assert.AreEqual(1f, t.CircleGrowth01, 1e-4);
            Assert.IsFalse(t.IsComplete);
        }

        // ── Fas C + klart ─────────────────────────────────────────────────────────
        [TestMethod]
        public void Complete_AfterTotalDuration()
        {
            var t = new BossFinaleTransition();
            t.Update(1.65f);   // förbi totala 1.60
            Assert.AreEqual(BossFinaleTransition.Phase.Done, t.CurrentPhase);
            Assert.IsTrue(t.IsComplete);
        }

        [TestMethod]
        public void PhaseProgression_FollowsTheChoreography()
        {
            var t = new BossFinaleTransition();
            Assert.AreEqual(BossFinaleTransition.Phase.GlitchHold, t.CurrentPhase);
            t.Update(0.45f); Assert.AreEqual(BossFinaleTransition.Phase.PoffGrow, t.CurrentPhase);
            t.Update(0.95f); Assert.AreEqual(BossFinaleTransition.Phase.WhiteHold, t.CurrentPhase);
            t.Update(0.20f); Assert.AreEqual(BossFinaleTransition.Phase.Done, t.CurrentPhase);
        }

        // ── Robusthet ─────────────────────────────────────────────────────────────
        [TestMethod]
        public void Update_IgnoresNonPositiveSteps()
        {
            var t = new BossFinaleTransition();
            t.Update(-1f);
            t.Update(0f);
            Assert.AreEqual(0f, t.Elapsed, 1e-4);
            Assert.AreEqual(BossFinaleTransition.Phase.GlitchHold, t.CurrentPhase);
        }
    }
}
