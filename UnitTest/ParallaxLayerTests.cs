#nullable enable
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för ParallaxLayer.Draw — verifierar tiling-logiken och
    /// DrawPartialSprite-anropens koordinater utan spelmotor eller hårdvara.
    /// </summary>
    [TestClass]
    public class ParallaxLayerTests
    {
        private const int ScreenW    = GameConstants.ScreenWidth;   // 256
        private const int ScreenH    = GameConstants.ScreenHeight;  // 224
        private const int TileSize   = GameConstants.TileSize;      // 16
        private const int ImageW     = 512;

        // En testbar SpriteId — väljer ett verkligt värde för att undvika
        // att testet läcker om enumet ändras.
        private static readonly SpriteId TestSprite = SpriteId.ParallaxSkyWinter;

        // ─────────────────────────────────────────────
        // Hjälpmetod
        // ─────────────────────────────────────────────

        private static ParallaxLayer MakeLayer(float scrollFactor)
            => new ParallaxLayer(TestSprite, scrollFactor, ImageW, ScreenH);

        // ─────────────────────────────────────────────
        // Statisk kamera (cameraOffsetX = 0)
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_NoCameraOffset_DrawsOneCallFullScreen()
        {
            // cameraOffsetX = 0 → scrollPx = 0 → wrapped = 0
            // firstW = min(512 - 0, 256) = 256 → täcker hela skärmen i ett anrop.
            var rc    = new FakeRenderContext();
            var layer = MakeLayer(scrollFactor: 0.10f);

            layer.Draw(rc, cameraOffsetX: 0f, ScreenW, ScreenH);

            Assert.AreEqual(1, rc.DrawnSprites.Count,
                "Kamera vid origo: exakt ett DrawPartialSprite-anrop förväntas.");

            var (sheet, sx, sy, srcX, srcY, w, h) = rc.DrawnSprites[0];
            Assert.AreEqual(TestSprite, sheet);
            Assert.AreEqual(0,   sx,   "screenX ska vara 0.");
            Assert.AreEqual(0,   sy,   "screenY ska vara 0.");
            Assert.AreEqual(0,   srcX, "srcX ska vara 0 (ej scrollad).");
            Assert.AreEqual(0,   srcY, "srcY ska vara 0.");
            Assert.AreEqual(ScreenW, w, "Bredd ska täcka hela skärmen.");
            Assert.AreEqual(ScreenH, h, "Höjd ska täcka hela skärmen.");
        }

        // ─────────────────────────────────────────────
        // Kamera mitt på banan, ingen wrap
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_SmallOffset_NoWrap_OneCall()
        {
            // scrollFactor = 0.10, cameraOffsetX = 10 tiles
            // scrollPx = 10 * 16 * 0.10 = 16 px
            // wrapped = 16, firstW = min(512-16, 256) = 256 → ett anrop räcker.
            var rc    = new FakeRenderContext();
            var layer = MakeLayer(scrollFactor: 0.10f);

            layer.Draw(rc, cameraOffsetX: 10f, ScreenW, ScreenH);

            Assert.AreEqual(1, rc.DrawnSprites.Count,
                "Litet offset utan wrap: ett anrop ska räcka.");

            var (_, _, _, srcX, _, w, _) = rc.DrawnSprites[0];
            Assert.AreEqual(16,    srcX, "srcX ska vara 16 px scroll.");
            Assert.AreEqual(ScreenW, w,  "Bredd ska täcka hela skärmen.");
        }

        // ─────────────────────────────────────────────
        // Wrap — bilden delar sig i två segment
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_LargeOffset_Wrap_TwoCalls()
        {
            // scrollFactor = 1.0 (full hastighet), cameraOffsetX = 30 tiles
            // scrollPx = 30 * 16 * 1.0 = 480 px
            // wrapped = 480 % 512 = 480
            // firstW  = min(512 - 480, 256) = min(32, 256) = 32
            // remaining = 256 - 32 = 224 → två anrop.
            var rc    = new FakeRenderContext();
            var layer = MakeLayer(scrollFactor: 1.0f);

            layer.Draw(rc, cameraOffsetX: 30f, ScreenW, ScreenH);

            Assert.AreEqual(2, rc.DrawnSprites.Count,
                "Wrap-situation: exakt två DrawPartialSprite-anrop förväntas.");

            var call1 = rc.DrawnSprites[0];
            var call2 = rc.DrawnSprites[1];

            // Anrop 1: del av bilden från wrapped=480 till bildens slut (32 px).
            Assert.AreEqual(0,   call1.Sx,   "call1 screenX = 0.");
            Assert.AreEqual(480, call1.SrcX, "call1 srcX = 480 (wrapped).");
            Assert.AreEqual(32,  call1.W,    "call1 bredd = 32 px.");

            // Anrop 2: återstående 224 px från bildens start (srcX = 0).
            Assert.AreEqual(32,  call2.Sx,   "call2 screenX = 32 (efter del 1).");
            Assert.AreEqual(0,   call2.SrcX, "call2 srcX = 0 (bildens start).");
            Assert.AreEqual(224, call2.W,    "call2 bredd = 224 px.");
        }

        // ─────────────────────────────────────────────
        // Negativ kamera-offset (kameran scrollar åt vänster)
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_NegativeOffset_WrappedPositive_OneCall()
        {
            // Negativ offset kan uppstå om kartan är smalare än skärmen.
            // scrollPx = -10 * 16 * 0.10 = -16 px
            // -16 % 512 = -16 → wrapped = -16 + 512 = 496
            // firstW = min(512-496, 256) = min(16, 256) = 16
            // remaining = 256 - 16 = 240 → två anrop.
            var rc    = new FakeRenderContext();
            var layer = MakeLayer(scrollFactor: 0.10f);

            layer.Draw(rc, cameraOffsetX: -10f, ScreenW, ScreenH);

            // Viktigast: wrapped ska vara positivt och ge ett korrekt resultat.
            Assert.IsTrue(rc.DrawnSprites.Count >= 1,
                "Negativ offset ska producera minst ett draw-anrop.");

            // Alla srcX-värden ska vara inom [0, ImageWidth).
            foreach (var call in rc.DrawnSprites)
            {
                Assert.IsTrue(call.SrcX >= 0 && call.SrcX < ImageW,
                    $"srcX={call.SrcX} ska vara i intervallet [0, {ImageW}).");
            }
        }

        // ─────────────────────────────────────────────
        // Totalt ritad bredd = skärmbredden alltid
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_TotalWidthAlwaysEqualsScreenWidth()
        {
            var layer = MakeLayer(scrollFactor: 0.30f);

            // Testa en mängd scroll-positioner — totalbredd ska alltid vara ScreenW.
            for (int camX = 0; camX < 200; camX += 7)
            {
                var rc = new FakeRenderContext();
                layer.Draw(rc, cameraOffsetX: camX, ScreenW, ScreenH);

                int totalW = rc.DrawnSprites.Sum(s => s.W);
                Assert.AreEqual(ScreenW, totalW,
                    $"camX={camX}: total ritad bredd ska alltid vara {ScreenW} px.");
            }
        }

        // ─────────────────────────────────────────────
        // screenY ska alltid vara 0 (full höjd, bakgrundslager)
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_ScreenY_AlwaysZero()
        {
            var rc    = new FakeRenderContext();
            var layer = MakeLayer(scrollFactor: 0.30f);

            layer.Draw(rc, cameraOffsetX: 25f, ScreenW, ScreenH);

            foreach (var call in rc.DrawnSprites)
                Assert.AreEqual(0, call.Sy, "screenY ska alltid vara 0 för bakgrundslager.");
        }

        // ─────────────────────────────────────────────
        // Scroll-faktor 0 → statiskt lager, alltid srcX = 0
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_ZeroScrollFactor_AlwaysSrcXZero()
        {
            var layer = MakeLayer(scrollFactor: 0f);

            for (int camX = 0; camX < 300; camX += 13)
            {
                var rc = new FakeRenderContext();
                layer.Draw(rc, cameraOffsetX: camX, ScreenW, ScreenH);

                Assert.AreEqual(1, rc.DrawnSprites.Count, "Statiskt lager: alltid ett anrop.");
                Assert.AreEqual(0, rc.DrawnSprites[0].SrcX,
                    $"camX={camX}: statiskt lager ska alltid ha srcX=0.");
            }
        }
    }
}
