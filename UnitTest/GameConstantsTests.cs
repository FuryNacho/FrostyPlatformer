// MSTEST0032: GameConstants är compile-time-konstanter — villkoren är alltid sanna per definition.
// Testerna är ändå meningsfulla: de fångar sign-fel vid framtida ändringar och fungerar som
// levande dokumentation av vad varje konstant representerar.
#pragma warning disable MSTEST0032
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Global;

namespace UnitTest
{
    /// <summary>
    /// Tester för GameConstants.
    /// Verifierar att konstanter är fysikaliskt rimliga och
    /// inbördes konsekventa – fångar typos och felaktiga tecken.
    /// </summary>
    [TestClass]
    public class GameConstantsTests
    {
        // ─────────────────────────────────────────────
        // Skärm
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ScreenDimensions_ArePositive()
        {
            Assert.IsTrue(GameConstants.ScreenWidth  > 0, "ScreenWidth måste vara positiv");
            Assert.IsTrue(GameConstants.ScreenHeight > 0, "ScreenHeight måste vara positiv");
            Assert.IsTrue(GameConstants.PixelWidth   > 0, "PixelWidth måste vara positiv");
            Assert.IsTrue(GameConstants.PixelHeight  > 0, "PixelHeight måste vara positiv");
        }

        [TestMethod]
        public void TileSize_IsPositive()
        {
            Assert.IsTrue(GameConstants.TileSize > 0);
        }

        // ─────────────────────────────────────────────
        // Fysik – gravitationsvärden
        // ─────────────────────────────────────────────

        [TestMethod]
        public void GravityValues_ArePositive()
        {
            Assert.IsTrue(GameConstants.GravityPowerJump > 0f, "GravityPowerJump ska vara positiv (verkar nedåt via +=)");
            Assert.IsTrue(GameConstants.GravityNormal    > 0f, "GravityNormal ska vara positiv");
            Assert.IsTrue(GameConstants.GravityHeavy     > 0f, "GravityHeavy ska vara positiv");
        }

        [TestMethod]
        public void GravityValues_AreOrderedCorrectly()
        {
            // Tyngre gravitation är starkare
            Assert.IsTrue(GameConstants.GravityHeavy >= GameConstants.GravityNormal,
                "GravityHeavy bör vara >= GravityNormal");
            Assert.IsTrue(GameConstants.GravityNormal >= GameConstants.GravityPowerJump,
                "GravityNormal bör vara >= GravityPowerJump (power-läget är mest luftig)");
        }

        [TestMethod]
        public void FallSpeedLimits_AreOrderedCorrectly()
        {
            Assert.IsTrue(GameConstants.FallSpeedMax > GameConstants.FallSpeedThreshold,
                "FallSpeedMax måste vara högre än FallSpeedThreshold");
            Assert.IsTrue(GameConstants.FallSpeedThreshold > 0f);
        }

        // ─────────────────────────────────────────────
        // Fysik – hoppvärden
        // ─────────────────────────────────────────────

        [TestMethod]
        public void JumpVelocities_AreNegative()
        {
            // Negativa = uppåt i spelkoordinatsystemet (y ökar neråt)
            Assert.IsTrue(GameConstants.JumpVelocity      < 0f, "JumpVelocity ska vara negativ");
            Assert.IsTrue(GameConstants.JumpDamageRebound < 0f, "JumpDamageRebound ska vara negativ");
            Assert.IsTrue(GameConstants.EnemyJumpVelocity < 0f, "EnemyJumpVelocity ska vara negativ");
        }

        [TestMethod]
        public void JumpDamageRebound_IsWeakerThanFullJump()
        {
            // Studsen efter hoppspark ska vara svagare än ett fullt hopp
            Assert.IsTrue(GameConstants.JumpDamageRebound > GameConstants.JumpVelocity,
                "Hoppsparks-studsen bör vara svagare (mindre negativ) än ett fullt hopp");
        }

        // ─────────────────────────────────────────────
        // Rörelse
        // ─────────────────────────────────────────────

        [TestMethod]
        public void MovementAcceleration_GroundFasterThanAir()
        {
            Assert.IsTrue(GameConstants.MoveAccelerationGround > GameConstants.MoveAccelerationAir,
                "Markacceleration ska vara snabbare än luftacceleration");
        }

        [TestMethod]
        public void MaxSpeeds_PowerFasterThanNormal()
        {
            Assert.IsTrue(GameConstants.MaxSpeedPower > GameConstants.MaxSpeedNormal,
                "Power-hastighet ska vara högre än normal hastighet");
        }

        [TestMethod]
        public void CollisionBorderPrecision_IsVerySmall()
        {
            Assert.IsTrue(GameConstants.CollisionBorderPrecision > 0f);
            Assert.IsTrue(GameConstants.CollisionBorderPrecision < 0.001f,
                "Kollisionsprecisionen ska vara extremt liten");
        }

        // ─────────────────────────────────────────────
        // Spellogik
        // ─────────────────────────────────────────────

        [TestMethod]
        public void GraceWindowSeconds_ArePositive()
        {
            Assert.IsTrue(GameConstants.JumpBufferSeconds > 0f);
            Assert.IsTrue(GameConstants.CeilingBonkSeconds > 0f);
            Assert.IsTrue(GameConstants.EnemyStompWindowSeconds > 0f);
            Assert.IsTrue(GameConstants.CoyoteFallCutoffSeconds > 0f);
            Assert.IsTrue(GameConstants.CoyoteFallCapSeconds > GameConstants.CoyoteFallCutoffSeconds);
        }

        [TestMethod]
        public void IdleTimeout_IsReasonable()
        {
            Assert.IsTrue(GameConstants.IdleTimeout > 0);
            Assert.IsTrue(GameConstants.IdleTimeout < 1000,
                "IdleTimeout bör inte vara extremt högt (frames, inte ms)");
        }

        [TestMethod]
        public void PerfectEndingHealth_IsPositive()
        {
            Assert.IsTrue(GameConstants.PerfectEndingHealth > 0);
        }
    }
}
