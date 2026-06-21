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
    }
}
