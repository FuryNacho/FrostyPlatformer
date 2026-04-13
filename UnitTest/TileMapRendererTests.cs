using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för TileMapRenderer.
    /// Verifierar att rätt antal TileDrawCall genereras och att
    /// skärm- och spritesheet-koordinater beräknas korrekt —
    /// utan spelmotor eller hårdvara.
    /// </summary>
    [TestClass]
    public class TileMapRendererTests
    {
        private const int ScreenW  = GameConstants.ScreenWidth;   // 256
        private const int ScreenH  = GameConstants.ScreenHeight;  // 224
        private const int TileSize = GameConstants.TileSize;      // 16
        private const int Cols     = GameConstants.TileSheetColumns; // 5

        private const int VisX = ScreenW / TileSize; // 16
        private const int VisY = ScreenH / TileSize; // 14

        private ITileMapRenderer _renderer = null!;
        private ICameraSystem    _camera   = null!;

        [TestInitialize]
        public void Setup()
        {
            _renderer = new TileMapRenderer();
            _camera   = new CameraSystem();
        }

        // ─────────────────────────────────────────────
        // Antal draw calls
        // ─────────────────────────────────────────────

        [TestMethod]
        public void GetDrawCalls_ReturnsOneCallPerVisibleTilePlusBuffer()
        {
            // Kameran centrerad mitt på kartan — inga gränser kläms
            var cam  = _camera.Calculate(40, 30, 200, 200, ScreenW, ScreenH);
            var map  = new FakeMapData(200, 200);
            var calls = _renderer.GetDrawCalls(cam, map).ToList();

            // Loop: x i [-1, VisX], y i [-1, VisY] → (VisX+2) * (VisY+2)
            int expected = (VisX + 2) * (VisY + 2);
            Assert.AreEqual(expected, calls.Count,
                "Antal draw calls ska vara (VisibleTilesX+2) * (VisibleTilesY+2).");
        }

        // ─────────────────────────────────────────────
        // Skärmkoordinater
        // ─────────────────────────────────────────────

        [TestMethod]
        public void GetDrawCalls_FirstCall_ScreenPosition_IsMinusOneTile()
        {
            // Med offset = heltal och TileOffset = 0 ska första anropet (x=-1, y=-1)
            // ge ScreenX = -TileSize, ScreenY = -TileSize
            var cam  = _camera.Calculate(VisX / 2.0f + 10, VisY / 2.0f + 10, 200, 200, ScreenW, ScreenH);
            var map  = new FakeMapData(200, 200);
            var first = _renderer.GetDrawCalls(cam, map).First();

            Assert.AreEqual(-TileSize, first.ScreenX, "Första tile ScreenX ska vara -TileSize.");
            Assert.AreEqual(-TileSize, first.ScreenY, "Första tile ScreenY ska vara -TileSize.");
        }

        // ─────────────────────────────────────────────
        // Spritesheet-koordinater
        // ─────────────────────────────────────────────

        [TestMethod]
        public void GetDrawCalls_TileIndex_MapsToCorrectSpriteColumn()
        {
            // Tile index 3 → kolumn 3 (3 % 5 = 3) → SpriteX = 3 * TileSize
            var map = new FakeMapData(200, 200, fixedIndex: 3);
            var cam = _camera.Calculate(40, 30, 200, 200, ScreenW, ScreenH);
            var call = _renderer.GetDrawCalls(cam, map).First();

            Assert.AreEqual(3 * TileSize, call.SpriteX, "Index 3 ska ge kolumn 3 i spritesheetet.");
        }

        [TestMethod]
        public void GetDrawCalls_TileIndex_MapsToCorrectSpriteRow()
        {
            // Tile index 7 → rad 1 (7 / 5 = 1) → SpriteY = 1 * TileSize
            var map = new FakeMapData(200, 200, fixedIndex: 7);
            var cam = _camera.Calculate(40, 30, 200, 200, ScreenW, ScreenH);
            var call = _renderer.GetDrawCalls(cam, map).First();

            Assert.AreEqual(1 * TileSize, call.SpriteY, "Index 7 ska ge rad 1 i spritesheetet.");
        }

        [TestMethod]
        public void GetDrawCalls_TileIndex_Zero_MapsToOriginOfSpriteSheet()
        {
            var map = new FakeMapData(200, 200, fixedIndex: 0);
            var cam = _camera.Calculate(40, 30, 200, 200, ScreenW, ScreenH);
            var call = _renderer.GetDrawCalls(cam, map).First();

            Assert.AreEqual(0, call.SpriteX, "Index 0 ska ge SpriteX = 0.");
            Assert.AreEqual(0, call.SpriteY, "Index 0 ska ge SpriteY = 0.");
        }

        // ─────────────────────────────────────────────
        // Tile-storlek i draw calls
        // ─────────────────────────────────────────────

        [TestMethod]
        public void GetDrawCalls_TileSizeMatchesGameConstants()
        {
            var cam  = _camera.Calculate(40, 30, 200, 200, ScreenW, ScreenH);
            var map  = new FakeMapData(200, 200);
            var call = _renderer.GetDrawCalls(cam, map).First();

            Assert.AreEqual(TileSize, call.TileWidth);
            Assert.AreEqual(TileSize, call.TileHeight);
        }

        // ─────────────────────────────────────────────
        // FakeMapData — hjälpklass för testerna
        // ─────────────────────────────────────────────

        private class FakeMapData : IMapData
        {
            private readonly int _fixedIndex;

            public int Width  { get; }
            public int Height { get; }

            public FakeMapData(int width, int height, int fixedIndex = 0)
            {
                Width       = width;
                Height      = height;
                _fixedIndex = fixedIndex;
            }

            public int GetIndex(int x, int y) => _fixedIndex;
            public bool GetSolid(int x, int y) => false;
        }
    }
}
