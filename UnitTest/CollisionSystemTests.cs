using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för CollisionSystem.
    /// Verifierar tile-baserad kollisionsdetektering mot en fake-karta
    /// utan hårdvara eller spelmotor.
    /// </summary>
    [TestClass]
    public class CollisionSystemTests
    {
        // ─────────────────────────────────────────────
        // FakeCollisionMap — hjälpklass för testerna
        // ─────────────────────────────────────────────

        private class FakeCollisionMap : IMapData
        {
            private readonly bool[,] _solids;
            public int Width  { get; }
            public int Height { get; }

            public FakeCollisionMap(int width, int height)
            {
                Width  = width;
                Height = height;
                _solids = new bool[height, width];
            }

            public void SetSolid(int x, int y) => _solids[y, x] = true;

            public int  GetIndex(int x, int y) => 0;
            public bool GetSolid(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                    return true; // kartgräns är solid
                return _solids[y, x];
            }
        }

        private const float Border = 0.01f; // CollisionBorderPrecision

        // ─────────────────────────────────────────────
        // ResolveHorizontal — rörelsevänster
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ResolveHorizontal_MovingLeft_NoSolid_ReturnsSamePosNoHit()
        {
            var map = new FakeCollisionMap(20, 20);
            float posY = 5.0f;
            float newPosX = 4.5f;

            var (adjX, hitWall) = CollisionSystem.ResolveHorizontal(posY, newPosX, velX: -1f, Border, map);

            Assert.AreEqual(newPosX, adjX, 0.001f);
            Assert.IsFalse(hitWall);
        }

        [TestMethod]
        public void ResolveHorizontal_MovingLeft_SolidAtTopCorner_HitsWall()
        {
            var map = new FakeCollisionMap(20, 20);
            map.SetSolid(4, 5); // solid vid (4, posY+0.0)
            float posY = 5.0f;
            float newPosX = 4.5f;

            var (adjX, hitWall) = CollisionSystem.ResolveHorizontal(posY, newPosX, velX: -1f, Border, map);

            Assert.IsTrue(hitWall);
            Assert.AreEqual((int)(newPosX + 0.9f), (int)adjX);
        }

        [TestMethod]
        public void ResolveHorizontal_MovingLeft_SolidAtBottomCorner_HitsWall()
        {
            var map = new FakeCollisionMap(20, 20);
            map.SetSolid(4, 5); // solid vid (4, posY+0.9 → 5)
            float posY = 5.0f;
            float newPosX = 4.5f;

            var (adjX, hitWall) = CollisionSystem.ResolveHorizontal(posY, newPosX, velX: -1f, Border, map);

            Assert.IsTrue(hitWall);
        }

        // ─────────────────────────────────────────────
        // ResolveHorizontal — rörelseåger
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ResolveHorizontal_MovingRight_NoSolid_ReturnsSamePosNoHit()
        {
            var map = new FakeCollisionMap(20, 20);
            float posY = 5.0f;
            float newPosX = 4.5f;

            var (adjX, hitWall) = CollisionSystem.ResolveHorizontal(posY, newPosX, velX: 1f, Border, map);

            Assert.AreEqual(newPosX, adjX, 0.001f);
            Assert.IsFalse(hitWall);
        }

        [TestMethod]
        public void ResolveHorizontal_MovingRight_SolidAtRightEdge_HitsWall()
        {
            var map = new FakeCollisionMap(20, 20);
            int solidX = (int)(4.5f + (1.0f - Border)); // höger kanten
            map.SetSolid(solidX, 5);
            float posY = 5.0f;
            float newPosX = 4.5f;

            var (adjX, hitWall) = CollisionSystem.ResolveHorizontal(posY, newPosX, velX: 1f, Border, map);

            Assert.IsTrue(hitWall);
            Assert.AreEqual((int)newPosX, (int)adjX);
        }

        // ─────────────────────────────────────────────
        // ResolveVertical — rörelse uppåt
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ResolveVertical_MovingUp_NoSolid_ReturnsSamePosNoHit()
        {
            var map = new FakeCollisionMap(20, 20);

            var (adjY, hitCeiling, grounded) = CollisionSystem.ResolveVertical(
                newPosX: 5.0f, newPosY: 4.5f, velY: -1f, map);

            Assert.AreEqual(4.5f, adjY, 0.001f);
            Assert.IsFalse(hitCeiling);
            Assert.IsFalse(grounded);
        }

        [TestMethod]
        public void ResolveVertical_MovingUp_SolidAtCeiling_HitsCeiling()
        {
            var map = new FakeCollisionMap(20, 20);
            map.SetSolid(5, 4); // solid vid newPosY
            float newPosX = 5.0f;
            float newPosY = 4.5f;

            var (adjY, hitCeiling, grounded) = CollisionSystem.ResolveVertical(
                newPosX, newPosY, velY: -1f, map);

            Assert.IsTrue(hitCeiling);
            Assert.IsFalse(grounded);
            Assert.AreEqual((int)newPosY + 1, (int)adjY);
        }

        // ─────────────────────────────────────────────
        // ResolveVertical — rörelse nedåt
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ResolveVertical_MovingDown_NoSolid_ReturnsSamePosNotGrounded()
        {
            var map = new FakeCollisionMap(20, 20);

            var (adjY, hitCeiling, grounded) = CollisionSystem.ResolveVertical(
                newPosX: 5.0f, newPosY: 4.5f, velY: 1f, map);

            Assert.AreEqual(4.5f, adjY, 0.001f);
            Assert.IsFalse(hitCeiling);
            Assert.IsFalse(grounded);
        }

        [TestMethod]
        public void ResolveVertical_MovingDown_SolidBelowFeet_Grounded()
        {
            var map = new FakeCollisionMap(20, 20);
            map.SetSolid(5, 6); // solid vid newPosY + 1
            float newPosX = 5.0f;
            float newPosY = 5.0f;

            var (adjY, hitCeiling, grounded) = CollisionSystem.ResolveVertical(
                newPosX, newPosY, velY: 1f, map);

            Assert.IsTrue(grounded);
            Assert.IsFalse(hitCeiling);
            Assert.AreEqual((int)newPosY, (int)adjY);
        }

        [TestMethod]
        public void ResolveVertical_MovingDown_SolidBelowFeetAtOffset_Grounded()
        {
            var map = new FakeCollisionMap(20, 20);
            map.SetSolid(5, 6); // solid vid (newPosX+0.9 → 5, newPosY+1 → 6)
            float newPosX = 4.5f;
            float newPosY = 5.0f;

            var (adjY, _, grounded) = CollisionSystem.ResolveVertical(
                newPosX, newPosY, velY: 1f, map);

            Assert.IsTrue(grounded);
            Assert.AreEqual((int)newPosY, (int)adjY);
        }

        // ─────────────────────────────────────────────
        // Kartgräns — utanför kartan är solid
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ResolveHorizontal_MovingLeft_OutsideMap_TreatedAsSolid()
        {
            var map = new FakeCollisionMap(10, 10);
            float posY = 5.0f;
            // -1.5f: (int)(-1.5 + 0.0) = -1, vilket är utanför kartans gräns → solid
            float newPosX = -1.5f;

            var (_, hitWall) = CollisionSystem.ResolveHorizontal(posY, newPosX, velX: -1f, Border, map);

            Assert.IsTrue(hitWall);
        }
    }
}
