using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.States;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för LevelObjMapAdapter — verifierar att tile- och
    /// kollisionsdata läses och skrivs korrekt, och att bounds-kontroll
    /// fungerar vid gränsen av kartan.
    /// </summary>
    [TestClass]
    public class LevelObjMapAdapterTests
    {
        // Skapar ett 3x3 LevelObj med kända värden
        private static LevelObj MakeLevel()
        {
            return new LevelObj
            {
                Width          = 3,
                Height         = 3,
                TileIndex      = new[] { 1, 2, 3,   4, 5, 6,   7, 8, 9 },
                AttributeIndex = new[] { 0, 1, 0,   1, 0, 1,   0, 1, 0 }
            };
        }

        // ── GetIndex ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void GetIndex_ReturnsCorrectValue_ForValidCoordinate()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.AreEqual(1, adapter.GetIndex(0, 0));
            Assert.AreEqual(5, adapter.GetIndex(1, 1));
            Assert.AreEqual(9, adapter.GetIndex(2, 2));
        }

        [TestMethod]
        public void GetIndex_ReturnsZero_ForNegativeX()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.AreEqual(0, adapter.GetIndex(-1, 0));
        }

        [TestMethod]
        public void GetIndex_ReturnsZero_ForNegativeY()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.AreEqual(0, adapter.GetIndex(0, -1));
        }

        [TestMethod]
        public void GetIndex_ReturnsZero_ForX_EqualToWidth()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.AreEqual(0, adapter.GetIndex(3, 0));
        }

        [TestMethod]
        public void GetIndex_ReturnsZero_ForY_EqualToHeight()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.AreEqual(0, adapter.GetIndex(0, 3));
        }

        // ── GetSolid ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void GetSolid_ReturnsFalse_WhenAttributeIsZero()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.IsFalse(adapter.GetSolid(0, 0));   // AttributeIndex[0] = 0
        }

        [TestMethod]
        public void GetSolid_ReturnsTrue_WhenAttributeIsNonZero()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.IsTrue(adapter.GetSolid(1, 0));    // AttributeIndex[1] = 1
        }

        [TestMethod]
        public void GetSolid_ReturnsTrue_OutsideMapBounds()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.IsTrue(adapter.GetSolid(-1, 0));
            Assert.IsTrue(adapter.GetSolid(0, -1));
            Assert.IsTrue(adapter.GetSolid(99, 99));
        }

        // ── SetTile ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void SetTile_UpdatesTileIndex_ForValidCoordinate()
        {
            var level   = MakeLevel();
            var adapter = new LevelObjMapAdapter(level);

            adapter.SetTile(1, 1, 42);

            Assert.AreEqual(42, adapter.GetIndex(1, 1));
            Assert.AreEqual(42, level.TileIndex[1 * 3 + 1]);  // direkt i LevelObj
        }

        [TestMethod]
        public void SetTile_IsNoOp_ForNegativeCoordinates()
        {
            var level   = MakeLevel();
            var adapter = new LevelObjMapAdapter(level);
            int[] original = (int[])level.TileIndex.Clone();

            adapter.SetTile(-1, 0, 99);
            adapter.SetTile(0, -1, 99);

            CollectionAssert.AreEqual(original, level.TileIndex);
        }

        [TestMethod]
        public void SetTile_IsNoOp_ForOutOfBoundsCoordinates()
        {
            var level   = MakeLevel();
            var adapter = new LevelObjMapAdapter(level);
            int[] original = (int[])level.TileIndex.Clone();

            adapter.SetTile(3, 0, 99);
            adapter.SetTile(0, 3, 99);

            CollectionAssert.AreEqual(original, level.TileIndex);
        }

        // ── SetSolid ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void SetSolid_UpdatesAttributeIndex_ForValidCoordinate()
        {
            var level   = MakeLevel();
            var adapter = new LevelObjMapAdapter(level);

            adapter.SetSolid(0, 0, true);

            Assert.IsTrue(adapter.GetSolid(0, 0));
            Assert.AreEqual(1, level.AttributeIndex[0]);
        }

        [TestMethod]
        public void SetSolid_CanClearSolidFlag()
        {
            var level   = MakeLevel();
            var adapter = new LevelObjMapAdapter(level);

            adapter.SetSolid(1, 0, false);  // var 1 (solid)

            Assert.IsFalse(adapter.GetSolid(1, 0));
            Assert.AreEqual(0, level.AttributeIndex[1]);
        }

        [TestMethod]
        public void SetSolid_IsNoOp_ForOutOfBoundsCoordinates()
        {
            var level   = MakeLevel();
            var adapter = new LevelObjMapAdapter(level);
            int[] original = (int[])level.AttributeIndex.Clone();

            adapter.SetSolid(-1, 0, true);
            adapter.SetSolid(99, 99, true);

            CollectionAssert.AreEqual(original, level.AttributeIndex);
        }

        // ── Width och Height ─────────────────────────────────────────────────────

        [TestMethod]
        public void Width_And_Height_MatchLevelObj()
        {
            var adapter = new LevelObjMapAdapter(MakeLevel());
            Assert.AreEqual(3, adapter.Width);
            Assert.AreEqual(3, adapter.Height);
        }
    }
}
