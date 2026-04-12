#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using Actions = FrostyPlatformer.Enum.Actions;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för OnWallCollision och OnStuckCheck — den polymorfiska AI-responsen
    /// som ersätter is-baserade dispatcher i GameplayState.
    /// </summary>
    [TestClass]
    public class EnemyWallCollisionTests
    {
        // ── FakeMap ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Minimal IMapData-implementation som låter testet styra vilka rutor som är solida.
        /// </summary>
        private class FakeMap : IMapData
        {
            private readonly bool _allSolid;
            public FakeMap(bool allSolid = true)  { _allSolid = allSolid; }
            public int Width  => 100;
            public int Height => 100;
            public int  GetIndex(int x, int y) => 0;
            public bool GetSolid(int x, int y) => _allSolid;
        }

        // ── Walrus – rör sig åt vänster ────────────────────────────────────────────

        [TestMethod]
        public void Walrus_MovingLeft_WallHit_ReversesToRight()
        {
            var walrus = new DynamicCreatureEnemyWalrus { py = 5f };
            walrus.Patrol = Actions.Left;
            float newX = 3f;
            var solidMap = new FakeMap(allSolid: true);

            // turnPatrol=true simulerar att kollisionssystemet slagit i vänstervägg
            walrus.OnWallCollision(ref newX, turnPatrol: true, movingLeft: true, solidMap, fBorder: 0.1f);

            Assert.AreEqual(Actions.Right, walrus.Patrol);
            Assert.AreEqual(2f, walrus.vx);
        }

        [TestMethod]
        public void Walrus_MovingLeft_NoWall_NoGroundBelow_ReversesToRight()
        {
            var walrus = new DynamicCreatureEnemyWalrus { py = 5f };
            walrus.Patrol = Actions.Left;
            float newX = 3f;
            // Karta utan solid mark nedan — Walrus vänder för att undvika fall
            var emptyMap = new FakeMap(allSolid: false);

            walrus.OnWallCollision(ref newX, turnPatrol: false, movingLeft: true, emptyMap, fBorder: 0.1f);

            Assert.AreEqual(Actions.Right, walrus.Patrol);
        }

        [TestMethod]
        public void Walrus_MovingLeft_SolidGround_NoWall_ContinuesLeft()
        {
            var walrus = new DynamicCreatureEnemyWalrus { py = 5f };
            walrus.Patrol = Actions.Left;
            float newX = 3f;
            var solidMap = new FakeMap(allSolid: true);

            // Ingen väggträff, solid mark nedan → patrull fortsätter åt vänster
            walrus.OnWallCollision(ref newX, turnPatrol: false, movingLeft: true, solidMap, fBorder: 0.1f);

            Assert.AreEqual(Actions.Left, walrus.Patrol);
        }

        [TestMethod]
        public void Walrus_MovingRight_WallHit_ReversesToLeft()
        {
            var walrus = new DynamicCreatureEnemyWalrus { py = 5f };
            walrus.Patrol = Actions.Right;
            float newX = 3f;
            var solidMap = new FakeMap(allSolid: true);

            walrus.OnWallCollision(ref newX, turnPatrol: true, movingLeft: false, solidMap, fBorder: 0.1f);

            Assert.AreEqual(Actions.Left, walrus.Patrol);
            Assert.AreEqual(-2f, walrus.vx);
        }

        [TestMethod]
        public void Walrus_MovingRight_SolidGround_NoWall_ContinuesRight()
        {
            var walrus = new DynamicCreatureEnemyWalrus { py = 5f };
            walrus.Patrol = Actions.Right;
            float newX = 3f;
            var solidMap = new FakeMap(allSolid: true);

            walrus.OnWallCollision(ref newX, turnPatrol: false, movingLeft: false, solidMap, fBorder: 0.1f);

            Assert.AreEqual(Actions.Right, walrus.Patrol);
        }

        // ── Frost – OnWallCollision ────────────────────────────────────────────────

        [TestMethod]
        public void Frost_MovingLeft_WallHit_NoSpecialAction_ReversesToRight()
        {
            var frost = new DynamicCreatureEnemyFrost { py = 5f };
            frost.Patrol = Actions.Left;
            frost.DoSpecialAction = false;
            float newX = 3f;
            var solidMap = new FakeMap(allSolid: true);

            frost.OnWallCollision(ref newX, turnPatrol: true, movingLeft: true, solidMap, fBorder: 0.1f);

            Assert.AreEqual(Actions.Right, frost.Patrol);
            Assert.AreEqual(1f, frost.vx);
        }

        [TestMethod]
        public void Frost_MovingLeft_NoWall_TicksUp_SetsDoSpecialActionAfterThreshold()
        {
            var frost = new DynamicCreatureEnemyFrost { py = 5f };
            frost.Patrol = Actions.Left;
            frost.DoSpecialAction = false;
            frost.Ticker = 0;
            float newX = 5f;
            var solidMap = new FakeMap(allSolid: true);

            // Anropa 9 gånger (Ticker 1–9): vid Ticker > 8 ska DoSpecialAction sättas
            for (int i = 0; i < 9; i++)
                frost.OnWallCollision(ref newX, turnPatrol: false, movingLeft: true, solidMap, fBorder: 0.1f);

            Assert.IsTrue(frost.DoSpecialAction);
        }

        [TestMethod]
        public void Frost_MovingRight_WallHit_NoSpecialAction_ReversesToLeft()
        {
            var frost = new DynamicCreatureEnemyFrost { py = 5f };
            frost.Patrol = Actions.Right;
            frost.DoSpecialAction = false;
            float newX = 3f;
            var solidMap = new FakeMap(allSolid: true);

            frost.OnWallCollision(ref newX, turnPatrol: true, movingLeft: false, solidMap, fBorder: 0.1f);

            Assert.AreEqual(Actions.Left, frost.Patrol);
            Assert.AreEqual(-1f, frost.vx);
        }

        // ── Frost – OnStuckCheck ───────────────────────────────────────────────────

        [TestMethod]
        public void Frost_OnStuckCheck_SamplesAtExpectedTicks()
        {
            var frost = new DynamicCreatureEnemyFrost();
            frost.px = 5f;
            frost.PrevTick = 0;

            // Tick fram till 10 — check sker innan inkrement, kräver 11 anrop för att nå PrevTick==10
            for (int i = 0; i < 11; i++) frost.OnStuckCheck();
            Assert.AreEqual(5f, frost.SampleOne, "SampleOne ska sättas vid tick 10");

            frost.px = 5f;
            // Tick fram till 30
            for (int i = 0; i < 20; i++) frost.OnStuckCheck();
            Assert.AreEqual(5f, frost.SampleTow, "SampleTow ska sättas vid tick 30");

            frost.px = 5f;
            // Tick fram till 50
            for (int i = 0; i < 20; i++) frost.OnStuckCheck();
            Assert.AreEqual(5f, frost.SampleThree, "SampleThree ska sättas vid tick 50");
        }

        [TestMethod]
        public void Frost_OnStuckCheck_StuckPosition_SetsJumpVelocity()
        {
            var frost = new DynamicCreatureEnemyFrost();
            frost.px = 5f;
            frost.PrevTick = 0;

            // Tick 60 gånger med samma position — ska detektera stuck och sätta vy = -2
            for (int i = 0; i < 60; i++) frost.OnStuckCheck();

            Assert.AreEqual(-2f, frost.vy, "Frost ska hoppa (-2 vy) när stuck detekteras");
        }

        [TestMethod]
        public void Frost_OnStuckCheck_MovingPosition_DoesNotJump()
        {
            var frost = new DynamicCreatureEnemyFrost();
            frost.vy = 0f;

            // Variera px kraftigt — ska inte trigga stuck-detektion
            for (int i = 0; i < 60; i++)
            {
                frost.px = i * 2f; // rör sig 2 enheter per tick
                frost.OnStuckCheck();
            }

            Assert.AreNotEqual(-2f, frost.vy, "Frost ska inte hoppa när den rör sig normalt");
        }

        // ── Basklass – ingen effekt ────────────────────────────────────────────────

        [TestMethod]
        public void DynamicGameObject_OnWallCollision_BaseClass_NoOp()
        {
            var obj = new DynamicCreatureEnemyPenguin();
            obj.Patrol = Actions.Left;
            float newX = 5f;
            var map = new FakeMap();

            // Ska inte kasta och inte ändra patrol
            obj.OnWallCollision(ref newX, turnPatrol: true, movingLeft: true, map, fBorder: 0.1f);

            Assert.AreEqual(Actions.Left, obj.Patrol);
        }

        [TestMethod]
        public void DynamicGameObject_OnStuckCheck_BaseClass_NoOp()
        {
            var obj = new DynamicCreatureEnemyPenguin();
            obj.vy = 0f;

            // Ska inte kasta
            obj.OnStuckCheck();

            Assert.AreEqual(0f, obj.vy);
        }
    }
}
