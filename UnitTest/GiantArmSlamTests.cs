#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;

namespace UnitTest
{
    /// <summary>
    /// Tester för jätte-armens nedslags-signal (akt 3). När näven slår i marken (Dropping→Stuck) ska
    /// ConsumeSlamLanded() utlösas exakt EN gång — GameplayState använder det för att släppa en extra
    /// is-skur per slag, nära nedslagets kolumn (ImpactX).
    /// </summary>
    [TestClass]
    public class GiantArmSlamTests
    {
        private const float Dt = 1f / 60f;

        /// <summary>Heltäckande golv på rad 13+.</summary>
        private sealed class FloorMap : IMapData
        {
            public int Width => 30;
            public int Height => 16;
            public int GetIndex(int x, int y) => 0;
            public bool GetSolid(int x, int y) => y >= 13;
        }

        private static DynamicCreatureGiantArm SlammingArm(IMapData map)
        {
            var giant = new DynamicCreatureGiant { px = 12f, py = 3f };
            var arm = new DynamicCreatureGiantArm();
            arm.Configure(shoulderX: 11f, shoulderY: 5f, isLeft: false, giant: giant, arena: map);
            return arm;
        }

        [TestMethod]
        public void ConsumeSlamLanded_FiresExactlyOnce_PerSlam()
        {
            var map = new FloorMap();
            var arm = SlammingArm(map);
            var player = new DynamicCreatureEnemyPenguin { px = 12f, py = 12f };

            arm.TriggerSlam();
            Assert.IsFalse(arm.ConsumeSlamLanded(), "Innan nedslaget ska ingen signal finnas.");

            int landed = 0;
            for (int i = 0; i < 200; i++)   // telegraf → drop → stuck → recoil → rest
            {
                arm.Behaviour(Dt, player);
                if (arm.ConsumeSlamLanded()) landed++;
            }

            Assert.AreEqual(1, landed, "Nedslags-signalen ska utlösas exakt en gång per slag.");
        }

        [TestMethod]
        public void ImpactX_LocksToHeroColumn()
        {
            var map = new FloorMap();
            var arm = SlammingArm(map);
            var player = new DynamicCreatureEnemyPenguin { px = 8f, py = 12f };

            arm.TriggerSlam();
            for (int i = 0; i < 120; i++) arm.Behaviour(Dt, player);

            Assert.AreEqual(8f, arm.ImpactX, 0.001f, "Näven (och därmed is-skuren) ska sikta på spelarens kolumn.");
        }

        // ── Hammarslag (overhead-leverans, men stampbart + triggar istappar som vanligt) ────
        [TestMethod]
        public void HammerSlam_RisesAboveShoulder_ThenBecomesStompable()
        {
            var map = new FloorMap();
            var arm = SlammingArm(map);   // ShoulderY = 5
            var player = new DynamicCreatureEnemyPenguin { px = 12f, py = 12f };

            arm.TriggerSlam(hammer: true);

            float minPy = float.MaxValue;
            bool everStuck = false, everAttackable = false;
            for (int i = 0; i < 200; i++)
            {
                arm.Behaviour(Dt, player);
                if (arm.py < minPy) minPy = arm.py;
                if (arm.Phase == GiantArmPhase.Stuck) everStuck = true;
                if (arm.IsAttackable) everAttackable = true;
            }

            Assert.IsTrue(minPy < 5f, $"Hammaren ska lyftas ovanför axeln (apex) under leveransen, minPy={minPy}.");
            Assert.IsTrue(everStuck, "Hammaren ska bli stampbar (når Stuck).");
            Assert.IsTrue(everAttackable, "Hammaren ska exponera svagpunkten (stampbar).");
        }

        [TestMethod]
        public void HammerSlam_TriggersIcicleBurst()
        {
            var map = new FloorMap();
            var arm = SlammingArm(map);
            var player = new DynamicCreatureEnemyPenguin { px = 12f, py = 12f };

            arm.TriggerSlam(hammer: true);
            bool landed = false;
            for (int i = 0; i < 200; i++) { arm.Behaviour(Dt, player); if (arm.ConsumeSlamLanded()) landed = true; }

            Assert.IsTrue(landed, "Hammaren ska trigga is-skuren (når Stuck → landed-signal).");
        }

        // ── RecoilNow: näven studsar tillbaka direkt vid hjälte-träff ───────────────
        [TestMethod]
        public void RecoilNow_FromDropping_RetractsImmediately()
        {
            var map = new FloorMap();
            var arm = SlammingArm(map);
            var player = new DynamicCreatureEnemyPenguin { px = 12f, py = 12f };

            arm.TriggerSlam();
            for (int i = 0; i < 200 && arm.Phase != GiantArmPhase.Dropping; i++) arm.Behaviour(Dt, player);
            Assert.AreEqual(GiantArmPhase.Dropping, arm.Phase, "Förutsätter att vi nådde Dropping.");

            arm.RecoilNow();

            Assert.AreEqual(GiantArmPhase.Recoiling, arm.Phase, "RecoilNow ska sätta Recoiling direkt.");
            Assert.IsFalse(arm.IsAttackable, "Ska inte vara stampbar under retur.");
        }

        [TestMethod]
        public void RecoilNow_DuringTelegraph_IsNoOp()
        {
            var map = new FloorMap();
            var arm = SlammingArm(map);
            var player = new DynamicCreatureEnemyPenguin { px = 12f, py = 12f };

            arm.TriggerSlam();
            arm.Behaviour(Dt, player);   // i Telegraph
            Assert.AreEqual(GiantArmPhase.Telegraph, arm.Phase);

            arm.RecoilNow();   // slaget har inte landat än → ska inte avbrytas

            Assert.AreEqual(GiantArmPhase.Telegraph, arm.Phase, "RecoilNow under telegraf ska vara no-op.");
        }
    }
}
