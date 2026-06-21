#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models.Objects;

namespace UnitTest
{
    /// <summary>
    /// Tester för akt 3:s istapps-hazard. Fokus på Shatter() — istappen ska kunna krossas både mot
    /// marken och mot hjälten (samma skärv-effekt), och bli ofarlig + självborttagen efteråt.
    /// </summary>
    [TestClass]
    public class BossIcicleTests
    {
        private const float Dt = 1f / 60f;

        [TestMethod]
        public void Shatter_EntersShatterPhaseAndGoesInert()
        {
            var ic = new DynamicCreatureBossIcicle();
            ic.Configure(columnX: 5f, groundMarkerY: 12f);

            ic.Shatter();

            Assert.AreEqual(BossIciclePhase.Shatter, ic.Phase, "Ska gå till skärv-fasen.");
            Assert.IsFalse(ic.SolidVsDynamic, "Ska bli ofarlig medan skärvorna skingras.");
        }

        [TestMethod]
        public void Shatter_IsIdempotent()
        {
            var ic = new DynamicCreatureBossIcicle();
            ic.Configure(columnX: 5f, groundMarkerY: 12f);

            ic.Shatter();
            ic.Shatter();   // andra anropet (t.ex. dubbel-kollision) ska inte starta om effekten

            Assert.AreEqual(BossIciclePhase.Shatter, ic.Phase, "Ska fortfarande vara i skärv-fasen.");
            Assert.IsFalse(ic.Redundant, "Ett extra Shatter får inte rycka bort den i förtid.");
        }

        [TestMethod]
        public void Shatter_RemovesItselfAfterDuration()
        {
            var ic = new DynamicCreatureBossIcicle();
            ic.Configure(columnX: 5f, groundMarkerY: 12f);

            ic.Shatter();
            for (int i = 0; i < 30; i++) ic.Behaviour(Dt);   // ~0.5 s > ShatterDur (0.4)

            Assert.IsTrue(ic.Redundant, "Efter att skärvorna skingrats ska istappen tas bort.");
        }
    }
}
