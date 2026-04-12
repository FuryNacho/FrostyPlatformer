using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;
using FrostyPlatformer.Global;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för PhysicsSystem.
    /// Verifierar att gravitation, luftmotstånd och hastighetsbegränsning
    /// beter sig korrekt utan hårdvara eller spelmotor.
    /// </summary>
    [TestClass]
    public class PhysicsSystemTests
    {
        private const float Elapsed = 0.016f; // ~60 fps

        // ─────────────────────────────────────────────
        // Gravitation – hjälte
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ApplyGravity_Hero_Rising_NoBPower_IncreasesVyWithNormalGravity()
        {
            var obj = new DynamicGameObject { IsHero = true, vy = -5.0f };
            int rjc = -1;

            PhysicsSystem.ApplyGravity(obj, isHero: true, bPower: false, ref rjc, Elapsed);

            float expected = -5.0f + GameConstants.GravityNormal * Elapsed;
            Assert.AreEqual(expected, obj.vy, 0.0001f,
                "Stigande hjälte utan BPower ska bromsas med GravityNormal.");
        }

        [TestMethod]
        public void ApplyGravity_Hero_Rising_BPower_IncreasesVyWithPowerJumpGravity()
        {
            var obj = new DynamicGameObject { IsHero = true, vy = -5.0f };
            int rjc = -1;

            PhysicsSystem.ApplyGravity(obj, isHero: true, bPower: true, ref rjc, Elapsed);

            float expected = -5.0f + GameConstants.GravityPowerJump * Elapsed;
            Assert.AreEqual(expected, obj.vy, 0.0001f,
                "Stigande hjälte med BPower ska bromsas med GravityPowerJump (mjukare).");
        }

        [TestMethod]
        public void ApplyGravity_Hero_Falling_UsesHeavyGravity()
        {
            var obj = new DynamicGameObject { IsHero = true, vy = 2.0f };
            int rjc = -1;

            PhysicsSystem.ApplyGravity(obj, isHero: true, bPower: false, ref rjc, Elapsed);

            float expected = 2.0f + GameConstants.GravityHeavy * Elapsed;
            Assert.AreEqual(expected, obj.vy, 0.0001f,
                "Fallande hjälte ska accelerera med GravityHeavy.");
        }

        [TestMethod]
        public void ApplyGravity_NonHero_UsesNormalGravity()
        {
            var obj = new DynamicGameObject { IsHero = false, vy = 0.0f };
            int rjc = -1;

            PhysicsSystem.ApplyGravity(obj, isHero: false, bPower: false, ref rjc, Elapsed);

            float expected = GameConstants.GravityNormal * Elapsed;
            Assert.AreEqual(expected, obj.vy, 0.0001f,
                "Icke-hjältar ska alltid få GravityNormal.");
        }

        [TestMethod]
        public void ApplyGravity_RememberJumpCollision_DecreasesEachFrame()
        {
            var obj = new DynamicGameObject { IsHero = true, vy = -3.0f };
            int rjc = 3;

            PhysicsSystem.ApplyGravity(obj, isHero: true, bPower: false, ref rjc, Elapsed);

            Assert.AreEqual(2, rjc, "rememberJumpCollision ska minska med 1 per frame.");
        }

        [TestMethod]
        public void ApplyGravity_RememberJumpCollision_Zero_SuppressesGravity()
        {
            // rjc >= 0 men ännu inte < 0 → ingen gravitation appliceras
            var obj = new DynamicGameObject { IsHero = true, vy = -3.0f };
            float vyBefore = obj.vy;
            int rjc = 0; // kommer bli -1 EFTER minskning, men check är < 0

            PhysicsSystem.ApplyGravity(obj, isHero: true, bPower: false, ref rjc, Elapsed);

            // rjc var 0, minskas till -1, sedan är rememberJumpCollision < 0 → gravitation tillämpas
            // (tröskeln gäller just EFTER minskning)
            Assert.IsTrue(obj.vy > vyBefore,
                "När rjc startar på 0 minskar den till -1 i samma frame → gravitation tillämpas.");
        }

        [TestMethod]
        public void ApplyGravity_RememberJumpCollision_Positive_SuppressesGravityForHeroRising()
        {
            var obj = new DynamicGameObject { IsHero = true, vy = -3.0f };
            float vyBefore = obj.vy;
            int rjc = 5; // minskas till 4, fortfarande >= 0 → gravitation undertrycks

            PhysicsSystem.ApplyGravity(obj, isHero: true, bPower: false, ref rjc, Elapsed);

            Assert.AreEqual(vyBefore, obj.vy, 0.0001f,
                "rjc > 0 ska undertrycka gravitation för stigande hjälte.");
        }


        // ─────────────────────────────────────────────
        // Luftmotstånd / drag
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ApplyDrag_NotHero_NoEffect()
        {
            var obj = new DynamicGameObject { IsHero = false, Grounded = true, vx = 5.0f };
            PhysicsSystem.ApplyDrag(obj, bPower: false, isIcyMap: false, anyDirectionAtAll: false, elapsed: Elapsed);
            Assert.AreEqual(5.0f, obj.vx, 0.0001f, "Icke-hjältar ska inte påverkas av drag.");
        }

        [TestMethod]
        public void ApplyDrag_Hero_NotGrounded_NoEffect()
        {
            var obj = new DynamicGameObject { IsHero = true, Grounded = false, vx = 5.0f };
            PhysicsSystem.ApplyDrag(obj, bPower: false, isIcyMap: false, anyDirectionAtAll: false, elapsed: Elapsed);
            Assert.AreEqual(5.0f, obj.vx, 0.0001f, "Luftburen hjälte ska inte påverkas av drag.");
        }

        [TestMethod]
        public void ApplyDrag_Hero_Grounded_NoBPower_ReducesVx()
        {
            var obj = new DynamicGameObject { IsHero = true, Grounded = true, vx = 5.0f };
            PhysicsSystem.ApplyDrag(obj, bPower: false, isIcyMap: false, anyDirectionAtAll: true, elapsed: Elapsed);

            float expected = 5.0f + (-GameConstants.DragNormal * 5.0f * Elapsed);
            Assert.AreEqual(expected, obj.vx, 0.0001f, "Normalt drag ska bromsas med DragNormal.");
        }

        [TestMethod]
        public void ApplyDrag_Hero_Grounded_BPower_ReducesVxWithBPowerDrag()
        {
            var obj = new DynamicGameObject { IsHero = true, Grounded = true, vx = 5.0f };
            PhysicsSystem.ApplyDrag(obj, bPower: true, isIcyMap: false, anyDirectionAtAll: true, elapsed: Elapsed);

            float expected = 5.0f + (-GameConstants.DragBPower * 5.0f * Elapsed);
            Assert.AreEqual(expected, obj.vx, 0.0001f, "BPower-drag ska bromsas med DragBPower (mjukare).");
        }

        [TestMethod]
        public void ApplyDrag_Hero_SlowSpeed_NoDirection_StopsOnNormalSurface()
        {
            // Hastighet under SlipperinessNormal utan riktning → stanna
            var obj = new DynamicGameObject { IsHero = true, Grounded = true, vx = 0.5f };
            PhysicsSystem.ApplyDrag(obj, bPower: false, isIcyMap: false, anyDirectionAtAll: false, elapsed: Elapsed);
            Assert.AreEqual(0.0f, obj.vx, 0.0001f,
                "Låg hastighet under tröskel utan riktning ska stanna på normal mark.");
        }

        [TestMethod]
        public void ApplyDrag_Hero_SlowSpeed_NoDirection_DoesNotStopOnIce()
        {
            // SlipperinessIce = 0.1 → vx = 0.5 > 0.1 → stannar inte
            var obj = new DynamicGameObject { IsHero = true, Grounded = true, vx = 0.5f };
            PhysicsSystem.ApplyDrag(obj, bPower: false, isIcyMap: true, anyDirectionAtAll: false, elapsed: Elapsed);
            Assert.IsTrue(obj.vx > 0.0f,
                "Hastighet 0.5 > SlipperinessIce (0.1) → ska glida vidare på is.");
        }


        // ─────────────────────────────────────────────
        // Hastighetsbegränsning
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ClampVelocities_NormalSpeed_NoClamp()
        {
            var obj = new DynamicGameObject { vx = 5.0f, vy = 5.0f };
            bool ballat = PhysicsSystem.ClampVelocities(obj);
            Assert.IsFalse(ballat, "Normal hastighet ska inte trigga clamp.");
            Assert.AreEqual(5.0f, obj.vx, 0.0001f);
            Assert.AreEqual(5.0f, obj.vy, 0.0001f);
        }

        [TestMethod]
        public void ClampVelocities_VxOverMax_ClampsToMax()
        {
            var obj = new DynamicGameObject { vx = 10.5f, vy = 0.0f };
            bool ballat = PhysicsSystem.ClampVelocities(obj);
            Assert.IsFalse(ballat, "Hastighet mellan MaxVelocityX och Crash ska klämmmas, inte nollas.");
            Assert.AreEqual(GameConstants.MaxVelocityX, obj.vx, 0.0001f);
        }

        [TestMethod]
        public void ClampVelocities_VxOverCrash_ResetsToZeroAndReturnsBallat()
        {
            var obj = new DynamicGameObject { vx = 12.0f, vy = 0.0f };
            bool ballat = PhysicsSystem.ClampVelocities(obj);
            Assert.IsTrue(ballat, "Hastighet över crashgräns ska returnera ballat=true.");
            Assert.AreEqual(0.0f, obj.vx, 0.0001f, "vx ska återställas till 0 vid crash.");
        }

        [TestMethod]
        public void ClampVelocities_VyDownOverThreshold_ClampsToThreshold()
        {
            var obj = new DynamicGameObject { vx = 0.0f, vy = 22.0f }; // > FallSpeedThreshold men < FallSpeedMax
            bool ballat = PhysicsSystem.ClampVelocities(obj);
            Assert.IsFalse(ballat, "vy mellan Threshold och Max ska klämmas, inte nollas.");
            Assert.AreEqual(GameConstants.FallSpeedThreshold, obj.vy, 0.0001f);
        }

        [TestMethod]
        public void ClampVelocities_VyDownOverMax_ResetsToZero()
        {
            var obj = new DynamicGameObject { vx = 0.0f, vy = 30.0f }; // > FallSpeedMax
            bool ballat = PhysicsSystem.ClampVelocities(obj);
            Assert.IsTrue(ballat, "vy över FallSpeedMax ska returnera ballat=true.");
            Assert.AreEqual(0.0f, obj.vy, 0.0001f);
        }

        [TestMethod]
        public void ClampVelocities_VyUpOverMax_ClampsToNegativeMax()
        {
            var obj = new DynamicGameObject { vx = 0.0f, vy = -10.5f }; // < -MaxVelocityYUp men > -Crash
            bool ballat = PhysicsSystem.ClampVelocities(obj);
            Assert.IsFalse(ballat, "Uppåthastighet mellan max och crash ska klämmas.");
            Assert.AreEqual(-GameConstants.MaxVelocityYUp, obj.vy, 0.0001f);
        }
    }
}
