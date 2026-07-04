using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för jättebossens materialiserings-effekt (akt 3): fas-ordning, bollens tillväxt/
    /// kollaps, gnist-förloppet och spawn-grinden. Ren logik — ingen rendering.
    /// Fas-längder: charge 0.80s, fade 0.50s → totalt 1.30s; gnist-burst 0.40s in i fade.
    /// </summary>
    [TestClass]
    public class GiantMaterializationTests
    {
        // ── Startläge ─────────────────────────────────────────────────────────────
        [TestMethod]
        public void Start_InCharge_NothingSpawnedYet()
        {
            var m = new GiantMaterialization();
            Assert.AreEqual(GiantMaterialization.Phase.Charge, m.CurrentPhase);
            Assert.AreEqual(0f, m.BallRadius01, 1e-4);
            Assert.AreEqual(0f, m.SparkProgress01, 1e-4);
            Assert.IsFalse(m.SpawnReady);
            Assert.IsFalse(m.IsComplete);
        }

        // ── Charge: bollen växer ──────────────────────────────────────────────────
        [TestMethod]
        public void Charge_BallGrows_NoSpawnNoSparks()
        {
            var m = new GiantMaterialization();
            m.Update(0.4f);   // halvvägs genom charge (0.8)
            Assert.AreEqual(GiantMaterialization.Phase.Charge, m.CurrentPhase);
            Assert.AreEqual(0.5f, m.BallRadius01, 0.02f);   // smoothstep(0.5) = 0.5
            Assert.AreEqual(0f, m.SparkProgress01, 1e-4);
            Assert.IsFalse(m.SpawnReady);
        }

        [TestMethod]
        public void Charge_BallReachesFull_AtChargeEnd()
        {
            var m = new GiantMaterialization();
            m.Update(0.8f);
            Assert.IsTrue(m.SpawnReady, "jätten ska spawna när bollen laddat klart");
            Assert.AreEqual(1f, m.BallRadius01, 1e-4, "bollen är på topp vid framträdandet");
            Assert.AreEqual(0f, m.SparkProgress01, 1e-4, "gnistorna börjar först vid framträdandet");
        }

        // ── SpawnReady latchar ────────────────────────────────────────────────────
        [TestMethod]
        public void SpawnReady_FalseBeforeChargeEnd_TrueAfter()
        {
            var m = new GiantMaterialization();
            m.Update(0.5f);
            Assert.IsFalse(m.SpawnReady);
            m.Update(0.4f);   // totalt 0.9 > 0.8
            Assert.IsTrue(m.SpawnReady);
            m.Update(0.4f);   // ligger kvar sant (latchar)
            Assert.IsTrue(m.SpawnReady);
        }

        // ── Reveal: bollen kollapsar, gnistorna flyger ────────────────────────────
        [TestMethod]
        public void Reveal_BallCollapses_SparksExpand()
        {
            var m = new GiantMaterialization();
            m.Update(0.8f);                 // vid framträdandet
            m.Update(0.25f);                // 0.25 in i fade (av 0.5)
            Assert.AreEqual(GiantMaterialization.Phase.Reveal, m.CurrentPhase);
            Assert.AreEqual(0.5f, m.BallRadius01, 0.02f);      // 1 - smoothstep(0.5)
            Assert.AreEqual(0.625f, m.SparkProgress01, 0.02f); // 0.25 / 0.40
        }

        [TestMethod]
        public void BallRadius_RisesThenFalls()
        {
            var m = new GiantMaterialization();
            m.Update(0.8f);
            float peak = m.BallRadius01;
            Assert.AreEqual(1f, peak, 1e-4);

            float prev = peak;
            for (int i = 0; i < 5; i++)
            {
                m.Update(0.1f);
                Assert.IsTrue(m.BallRadius01 <= prev, "bollen ska bara krympa efter toppen");
                prev = m.BallRadius01;
            }
        }

        // ── Klart ─────────────────────────────────────────────────────────────────
        [TestMethod]
        public void Complete_AfterTotalDuration()
        {
            var m = new GiantMaterialization();
            m.Update(1.35f);   // förbi totala 1.30
            Assert.AreEqual(GiantMaterialization.Phase.Done, m.CurrentPhase);
            Assert.IsTrue(m.IsComplete);
            Assert.AreEqual(0f, m.BallRadius01, 1e-4);
        }

        [TestMethod]
        public void PhaseProgression_FollowsTheChoreography()
        {
            var m = new GiantMaterialization();
            Assert.AreEqual(GiantMaterialization.Phase.Charge, m.CurrentPhase);
            m.Update(0.8f); Assert.AreEqual(GiantMaterialization.Phase.Reveal, m.CurrentPhase);
            m.Update(0.5f); Assert.AreEqual(GiantMaterialization.Phase.Done, m.CurrentPhase);
        }

        // ── Robusthet ─────────────────────────────────────────────────────────────
        [TestMethod]
        public void Update_IgnoresNonPositiveSteps()
        {
            var m = new GiantMaterialization();
            m.Update(-1f);
            m.Update(0f);
            Assert.AreEqual(0f, m.Elapsed, 1e-4);
            Assert.AreEqual(GiantMaterialization.Phase.Charge, m.CurrentPhase);
        }
    }
}
