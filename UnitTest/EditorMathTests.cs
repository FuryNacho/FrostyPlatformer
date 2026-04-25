using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.States;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för EditorMath — verifierar att ScreenToTile och TileToScreen
    /// beräknar korrekta tile- och skärmkoordinater vid olika kamerapositioner.
    /// </summary>
    [TestClass]
    public class EditorMathTests
    {
        // Hjälpmetod: skapar en CameraView med given offset och 16px tiles
        private static CameraView MakeCam(float offsetX, float offsetY, int tileSize = 16)
        {
            float tileOffsetX = (offsetX - (int)offsetX) * tileSize;
            float tileOffsetY = (offsetY - (int)offsetY) * tileSize;
            return new CameraView(offsetX, offsetY, tileOffsetX, tileOffsetY,
                                  20, 14, tileSize, tileSize);
        }

        // ── ScreenToTile ──────────────────────────────────────────────────────────

        [TestMethod]
        public void ScreenToTile_AtOrigin_ReturnsTileZeroZero()
        {
            var cam = MakeCam(0f, 0f);
            var (tx, ty) = EditorMath.ScreenToTile(0, 0, cam);
            Assert.AreEqual(0, tx);
            Assert.AreEqual(0, ty);
        }

        [TestMethod]
        public void ScreenToTile_AtOneTileSize_ReturnsNextTile()
        {
            var cam = MakeCam(0f, 0f);
            var (tx, ty) = EditorMath.ScreenToTile(16, 16, cam);
            Assert.AreEqual(1, tx);
            Assert.AreEqual(1, ty);
        }

        [TestMethod]
        public void ScreenToTile_JustBeforeTileBoundary_ReturnsSameTile()
        {
            var cam = MakeCam(0f, 0f);
            var (tx, ty) = EditorMath.ScreenToTile(15, 15, cam);
            Assert.AreEqual(0, tx);
            Assert.AreEqual(0, ty);
        }

        [TestMethod]
        public void ScreenToTile_CameraScrolledByOneTile_OffsetsTileCoordinate()
        {
            var cam = MakeCam(1f, 0f);   // kamera exakt ett tile till höger
            var (tx, ty) = EditorMath.ScreenToTile(0, 0, cam);
            Assert.AreEqual(1, tx);
            Assert.AreEqual(0, ty);
        }

        [TestMethod]
        public void ScreenToTile_SubTileOffset_CalculatesCorrectTile()
        {
            // Kamera halvt ett tile till höger: TileOffsetX = 8
            var cam = MakeCam(0.5f, 0f);
            // Mus vid screenX=8 ska ge tile 1 (halvvägs in i nästa tile)
            var (tx, _) = EditorMath.ScreenToTile(8, 0, cam);
            Assert.AreEqual(1, tx);
        }

        [TestMethod]
        public void ScreenToTile_SubTileOffset_StillReturnsFirstTileJustBefore()
        {
            var cam = MakeCam(0.5f, 0f);
            // screenX=7 är just under nästa tile-gräns
            var (tx, _) = EditorMath.ScreenToTile(7, 0, cam);
            Assert.AreEqual(0, tx);
        }

        [TestMethod]
        public void ScreenToTile_LargerOffset_ReturnsCorrectMapTile()
        {
            var cam = MakeCam(5f, 3f);
            var (tx, ty) = EditorMath.ScreenToTile(0, 0, cam);
            Assert.AreEqual(5, tx);
            Assert.AreEqual(3, ty);
        }

        // ── TileToScreen ─────────────────────────────────────────────────────────

        [TestMethod]
        public void TileToScreen_TileZeroAtCameraOrigin_ReturnsOrigin()
        {
            var cam = MakeCam(0f, 0f);
            var (sx, sy) = EditorMath.TileToScreen(0, 0, cam);
            Assert.AreEqual(0, sx);
            Assert.AreEqual(0, sy);
        }

        [TestMethod]
        public void TileToScreen_TileOne_ReturnsOneTileSize()
        {
            var cam = MakeCam(0f, 0f);
            var (sx, sy) = EditorMath.TileToScreen(1, 1, cam);
            Assert.AreEqual(16, sx);
            Assert.AreEqual(16, sy);
        }

        [TestMethod]
        public void TileToScreen_CameraAtOneTile_FirstVisibleTileAtZero()
        {
            var cam = MakeCam(1f, 0f);
            var (sx, _) = EditorMath.TileToScreen(1, 0, cam);
            Assert.AreEqual(0, sx);
        }

        [TestMethod]
        public void ScreenToTile_RoundTrip_WithCameraOffset()
        {
            var cam = MakeCam(3f, 2f);
            int origTileX = 5;
            int origTileY = 4;
            var (screenX, screenY) = EditorMath.TileToScreen(origTileX, origTileY, cam);
            var (backTileX, backTileY) = EditorMath.ScreenToTile(screenX, screenY, cam);
            Assert.AreEqual(origTileX, backTileX, "Round-trip tileX ska stämma");
            Assert.AreEqual(origTileY, backTileY, "Round-trip tileY ska stämma");
        }
    }
}
