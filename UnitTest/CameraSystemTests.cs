using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Global;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för CameraSystem.
    /// Verifierar centrering, kantklämning och sub-tile-offsettar
    /// utan spelmotor eller hårdvara.
    /// </summary>
    [TestClass]
    public class CameraSystemTests
    {
        // Spelkonstanter som testerna förhåller sig till
        private const int ScreenW     = GameConstants.ScreenWidth;   // 256
        private const int ScreenH     = GameConstants.ScreenHeight;  // 224
        private const int TileSize    = GameConstants.TileSize;      // 16

        // Synliga tiles på en standard 256x224-skärm med 16px tiles
        private const int VisX = ScreenW / TileSize;  // 16
        private const int VisY = ScreenH / TileSize;  // 14

        private ICameraSystem _camera;

        [TestInitialize]
        public void Setup()
        {
            _camera = new CameraSystem();
        }

        // ─────────────────────────────────────────────
        // VisibleTiles
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Calculate_ReturnsCorrectVisibleTileCount()
        {
            var view = _camera.Calculate(10, 10, 100, 100, ScreenW, ScreenH);

            Assert.AreEqual(VisX, view.VisibleTilesX, "VisibleTilesX ska vara ScreenWidth / TileSize.");
            Assert.AreEqual(VisY, view.VisibleTilesY, "VisibleTilesY ska vara ScreenHeight / TileSize.");
        }

        [TestMethod]
        public void Calculate_ReturnsCorrectTileSize()
        {
            var view = _camera.Calculate(10, 10, 100, 100, ScreenW, ScreenH);

            Assert.AreEqual(TileSize, view.TileWidth);
            Assert.AreEqual(TileSize, view.TileHeight);
        }

        // ─────────────────────────────────────────────
        // Centrering utan klämning
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Calculate_TargetAtCenter_OffsetCenteredOnTarget()
        {
            // Karta tillräckligt stor för att undvika klämning
            float targetX = 40.0f;
            float targetY = 30.0f;
            var view = _camera.Calculate(targetX, targetY, 200, 200, ScreenW, ScreenH);

            float expectedOffsetX = targetX - VisX / 2.0f;
            float expectedOffsetY = targetY - VisY / 2.0f;

            Assert.AreEqual(expectedOffsetX, view.OffsetX, 0.001f, "OffsetX ska vara centrerad på målet.");
            Assert.AreEqual(expectedOffsetY, view.OffsetY, 0.001f, "OffsetY ska vara centrerad på målet.");
        }

        // ─────────────────────────────────────────────
        // Klämning mot vänster/övre kant
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Calculate_TargetNearLeftEdge_OffsetX_ClampedToZero()
        {
            // Target vid tile 0 → offset vill bli negativ → kläms till 0
            var view = _camera.Calculate(0, 30, 200, 200, ScreenW, ScreenH);

            Assert.AreEqual(0.0f, view.OffsetX, 0.001f, "OffsetX ska klämpas till 0 vid vänster kant.");
        }

        [TestMethod]
        public void Calculate_TargetNearTopEdge_OffsetY_ClampedToZero()
        {
            var view = _camera.Calculate(40, 0, 200, 200, ScreenW, ScreenH);

            Assert.AreEqual(0.0f, view.OffsetY, 0.001f, "OffsetY ska klämpas till 0 vid övre kant.");
        }

        // ─────────────────────────────────────────────
        // Klämning mot höger/nedre kant
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Calculate_TargetNearRightEdge_OffsetX_ClampedToMax()
        {
            int mapWidth = 30;
            float targetX = mapWidth; // Utanför höger kant
            var view = _camera.Calculate(targetX, 20, mapWidth, 100, ScreenW, ScreenH);

            float expectedMax = mapWidth - VisX;
            Assert.AreEqual(expectedMax, view.OffsetX, 0.001f, "OffsetX ska klämpas till mapWidth - VisibleTilesX.");
        }

        [TestMethod]
        public void Calculate_TargetNearBottomEdge_OffsetY_ClampedToMax()
        {
            int mapHeight = 25;
            float targetY = mapHeight;
            var view = _camera.Calculate(40, targetY, 200, mapHeight, ScreenW, ScreenH);

            float expectedMax = mapHeight - VisY;
            Assert.AreEqual(expectedMax, view.OffsetY, 0.001f, "OffsetY ska klämpas till mapHeight - VisibleTilesY.");
        }

        // ─────────────────────────────────────────────
        // Sub-tile offsettar för mjuk scrollning
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Calculate_WholeNumberTarget_TileOffsetsAreZero()
        {
            // Heltals-target → ingen sub-tile förskjutning
            var view = _camera.Calculate(20.0f, 20.0f, 200, 200, ScreenW, ScreenH);

            Assert.AreEqual(0.0f, view.TileOffsetX, 0.001f, "Heltals-offsetX ger TileOffsetX = 0.");
            Assert.AreEqual(0.0f, view.TileOffsetY, 0.001f, "Heltals-offsetY ger TileOffsetY = 0.");
        }

        [TestMethod]
        public void Calculate_FractionalOffset_TileOffsetsScaleCorrectly()
        {
            // Offset med 0.5 bråk → TileOffset = 0.5 * TileSize = 8px
            // targetX = VisX/2 + 20.5 ger offsetX = 20.5 (ingen klämning)
            float targetX = VisX / 2.0f + 20.5f;
            float targetY = VisY / 2.0f + 15.5f;
            var view = _camera.Calculate(targetX, targetY, 200, 200, ScreenW, ScreenH);

            Assert.AreEqual(0.5f * TileSize, view.TileOffsetX, 0.001f, "0.5 bråk-offset ger halv tile-storlek.");
            Assert.AreEqual(0.5f * TileSize, view.TileOffsetY, 0.001f);
        }

        // ─────────────────────────────────────────────
        // Kameran rör sig inte när kartan är för liten
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Calculate_MapSmallerThanScreen_OffsetStaysAtZero()
        {
            // Kartan är smalare än skärmen → offset ska alltid vara 0
            int tinyMap = VisX - 2;
            var view = _camera.Calculate(50, 50, tinyMap, tinyMap, ScreenW, ScreenH);

            Assert.AreEqual(0.0f, view.OffsetX, 0.001f, "För liten karta → OffsetX = 0.");
            Assert.AreEqual(0.0f, view.OffsetY, 0.001f, "För liten karta → OffsetY = 0.");
        }
    }
}
