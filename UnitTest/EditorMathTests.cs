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

        // ── UpdateCursor — delad mus/gamepad-markör ──────────────────────────────

        [TestMethod]
        public void UpdateCursor_MouseMoved_SnapsToMousePosition()
        {
            // Senast rörda enhet vinner: musrörelse snäpper markören dit absolut.
            var (x, y) = EditorMath.UpdateCursor(
                prevX: 10f, prevY: 10f,
                mouseMoved: true, mouseX: 100, mouseY: 80,
                stickX: 0.9f, stickY: 0.9f, speed: 160f, dt: 0.016f,
                maxX: 255, maxY: 223);
            Assert.AreEqual(100f, x);
            Assert.AreEqual(80f, y);
        }

        [TestMethod]
        public void UpdateCursor_MouseMoved_ClampsToScreen()
        {
            var (x, y) = EditorMath.UpdateCursor(
                prevX: 0f, prevY: 0f,
                mouseMoved: true, mouseX: 9999, mouseY: -50,
                stickX: 0f, stickY: 0f, speed: 160f, dt: 0.016f,
                maxX: 255, maxY: 223);
            Assert.AreEqual(255f, x);
            Assert.AreEqual(0f, y);
        }

        [TestMethod]
        public void UpdateCursor_StickInDeadZone_KeepsPreviousPosition()
        {
            // Dödzonad stick = 0 → markören står still när musen inte rörts.
            var (x, y) = EditorMath.UpdateCursor(
                prevX: 50f, prevY: 60f,
                mouseMoved: false, mouseX: 0, mouseY: 0,
                stickX: 0f, stickY: 0f, speed: 160f, dt: 0.016f,
                maxX: 255, maxY: 223);
            Assert.AreEqual(50f, x);
            Assert.AreEqual(60f, y);
        }

        [TestMethod]
        public void UpdateCursor_StickMovesCursorIncrementally()
        {
            // Full stick åt höger i 1 sekund med 160 px/s → +160 px från prevX.
            var (x, _) = EditorMath.UpdateCursor(
                prevX: 50f, prevY: 60f,
                mouseMoved: false, mouseX: 0, mouseY: 0,
                stickX: 1f, stickY: 0f, speed: 160f, dt: 1f,
                maxX: 255, maxY: 223);
            Assert.AreEqual(210f, x);
        }

        [TestMethod]
        public void UpdateCursor_StickYPositive_MovesCursorUp()
        {
            // Skärm-Y växer nedåt; spak-Y positiv = uppåt → Y minskar.
            var (_, y) = EditorMath.UpdateCursor(
                prevX: 50f, prevY: 100f,
                mouseMoved: false, mouseX: 0, mouseY: 0,
                stickX: 0f, stickY: 1f, speed: 160f, dt: 1f,
                maxX: 255, maxY: 223);
            Assert.AreEqual(0f, y); // 100 - 160 = -60 → klampas till 0
        }

        [TestMethod]
        public void UpdateCursor_StickMovement_ClampsToScreenBounds()
        {
            var (x, y) = EditorMath.UpdateCursor(
                prevX: 250f, prevY: 5f,
                mouseMoved: false, mouseX: 0, mouseY: 0,
                stickX: 1f, stickY: 1f, speed: 160f, dt: 1f,
                maxX: 255, maxY: 223);
            Assert.AreEqual(255f, x);  // 250 + 160 → klampas till 255
            Assert.AreEqual(0f, y);    // 5 - 160 → klampas till 0
        }

        [TestMethod]
        public void UpdateCursor_MouseMovedTakesPriorityOverStick()
        {
            // Båda aktiva samma frame: mus vinner (mouseMoved kollas först).
            var (x, y) = EditorMath.UpdateCursor(
                prevX: 50f, prevY: 50f,
                mouseMoved: true, mouseX: 30, mouseY: 40,
                stickX: 1f, stickY: 1f, speed: 160f, dt: 1f,
                maxX: 255, maxY: 223);
            Assert.AreEqual(30f, x);
            Assert.AreEqual(40f, y);
        }
    }
}
