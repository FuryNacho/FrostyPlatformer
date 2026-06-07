#nullable enable
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för ParallaxSystem och SeasonHelper.
    /// Verifierar SetSeason-delegation, Draw-ordning och kantfall.
    /// </summary>
    [TestClass]
    public class ParallaxSystemTests
    {
        private const int ScreenW  = GameConstants.ScreenWidth;
        private const int ScreenH  = GameConstants.ScreenHeight;

        // ─────────────────────────────────────────────
        // SeasonHelper.FromMapName
        // ─────────────────────────────────────────────

        [TestMethod]
        public void FromMapName_MapOne_ReturnsSpring()
            => Assert.AreEqual(Season.Spring, SeasonHelper.FromMapName(MapName.MapOne));

        [TestMethod]
        public void FromMapName_MapTwo_ReturnsSpring()
            => Assert.AreEqual(Season.Spring, SeasonHelper.FromMapName(MapName.MapTwo));

        [TestMethod]
        public void FromMapName_MapThree_ReturnsSummer()
            => Assert.AreEqual(Season.Summer, SeasonHelper.FromMapName(MapName.MapThree));

        [TestMethod]
        public void FromMapName_MapFour_ReturnsSummer()
            => Assert.AreEqual(Season.Summer, SeasonHelper.FromMapName(MapName.MapFour));

        [TestMethod]
        public void FromMapName_MapFive_ReturnsFall()
            => Assert.AreEqual(Season.Fall, SeasonHelper.FromMapName(MapName.MapFive));

        [TestMethod]
        public void FromMapName_MapSix_ReturnsFall()
            => Assert.AreEqual(Season.Fall, SeasonHelper.FromMapName(MapName.MapSix));

        [TestMethod]
        public void FromMapName_MapSeven_ReturnsWinter()
            => Assert.AreEqual(Season.Winter, SeasonHelper.FromMapName(MapName.MapSeven));

        [TestMethod]
        public void FromMapName_MapEight_ReturnsWinter()
            => Assert.AreEqual(Season.Winter, SeasonHelper.FromMapName(MapName.MapEight));

        [TestMethod]
        public void FromMapName_MapNine_ReturnsWinter()
            => Assert.AreEqual(Season.Winter, SeasonHelper.FromMapName(MapName.MapNine));

        [TestMethod]
        public void FromMapName_MapTen_ReturnsBoss()
            => Assert.AreEqual(Season.Boss, SeasonHelper.FromMapName(MapName.MapTen));

        [TestMethod]
        public void FromMapName_WorldMap_ReturnsNull()
            => Assert.IsNull(SeasonHelper.FromMapName(MapName.WorldMap),
                "Världskartan ska returnera null — ingen parallax-bakgrund.");

        [TestMethod]
        public void FromMapName_NullInput_ReturnsNull()
            => Assert.IsNull(SeasonHelper.FromMapName(null),
                "Null-inmatning ska returnera null utan undantag.");

        [TestMethod]
        public void FromMapName_UnknownMap_ReturnsNull()
            => Assert.IsNull(SeasonHelper.FromMapName("usermap_xyz"),
                "Okänt kartnamn ska returnera null.");

        // ─────────────────────────────────────────────
        // SeasonHelper.FromTilesetSource
        // ─────────────────────────────────────────────

        [TestMethod]
        public void FromTilesetSource_Spring_ReturnsSpring()
            => Assert.AreEqual(Season.Spring, SeasonHelper.FromTilesetSource("tilesheetspring.tsx"));

        [TestMethod]
        public void FromTilesetSource_Summer_ReturnsSummer()
            => Assert.AreEqual(Season.Summer, SeasonHelper.FromTilesetSource("tilesheetsummer.tsx"));

        [TestMethod]
        public void FromTilesetSource_Fall_ReturnsFall()
            => Assert.AreEqual(Season.Fall, SeasonHelper.FromTilesetSource("tilesheetfall.tsx"));

        [TestMethod]
        public void FromTilesetSource_Winter_ReturnsWinter()
            => Assert.AreEqual(Season.Winter, SeasonHelper.FromTilesetSource("tilesheetwinter.tsx"));

        [TestMethod]
        public void FromTilesetSource_Custom_ReturnsCustom()
            => Assert.AreEqual(Season.Custom, SeasonHelper.FromTilesetSource("tilesheetcustom.tsx"));

        [TestMethod]
        public void FromTilesetSource_UnknownOrNull_FallsBackToCustom()
        {
            Assert.AreEqual(Season.Custom, SeasonHelper.FromTilesetSource("nonsense.tsx"));
            Assert.AreEqual(Season.Custom, SeasonHelper.FromTilesetSource(null));
        }

        [TestMethod]
        public void FromTilesetSource_IsCaseInsensitiveAndTrims()
            => Assert.AreEqual(Season.Winter, SeasonHelper.FromTilesetSource("  TileSheetWinter.TSX  "));

        // ─────────────────────────────────────────────
        // SpriteRef.TileSheetForTileset
        // ─────────────────────────────────────────────

        [TestMethod]
        public void TileSheetForTileset_Spring_ReturnsSpringSheet()
            => Assert.AreEqual(SpriteRef.TileSheetSpring, SpriteRef.TileSheetForTileset("tilesheetspring.tsx"));

        [TestMethod]
        public void TileSheetForTileset_Winter_ReturnsWinterSheet()
            => Assert.AreEqual(SpriteRef.TileSheetWinter, SpriteRef.TileSheetForTileset("tilesheetwinter.tsx"));

        [TestMethod]
        public void TileSheetForTileset_Custom_ReturnsCustomSheet()
            => Assert.AreEqual(SpriteRef.TileSheetCustom, SpriteRef.TileSheetForTileset("tilesheetcustom.tsx"));

        [TestMethod]
        public void TileSheetForTileset_Boss_ReturnsBossSheet()
            => Assert.AreEqual(SpriteRef.TileSheetBoss, SpriteRef.TileSheetForTileset("tilesheetboss.tsx"));

        [TestMethod]
        public void TileSheetForTileset_WorldMap_ReturnsWorldMapSheet()
            => Assert.AreEqual(SpriteRef.TileSheetWorldMap, SpriteRef.TileSheetForTileset("tilesheetwm.tsx"));

        [TestMethod]
        public void TileSheetForTileset_UnknownOrNull_FallsBackToCustomSheet()
        {
            Assert.AreEqual(SpriteRef.TileSheetCustom, SpriteRef.TileSheetForTileset("nonsense.tsx"));
            Assert.AreEqual(SpriteRef.TileSheetCustom, SpriteRef.TileSheetForTileset(null));
        }

        // ─────────────────────────────────────────────
        // ParallaxSystem — inget lager innan SetSeason
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_BeforeSetSeason_DrawsNothing()
        {
            // Nyskapad system har inga aktiva lager → Draw ska inte rita något.
            var rc  = new FakeRenderContext();
            var sys = new ParallaxSystem();

            sys.Draw(rc, cameraOffsetX: 0f, ScreenW, ScreenH);

            Assert.AreEqual(0, rc.DrawnSprites.Count,
                "Utan SetSeason ska inga sprites ritas.");
        }

        // ─────────────────────────────────────────────
        // ParallaxSystem — två lager per årstid
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_AfterSetSeason_DrawsTwoLayers()
        {
            // Varje årstid ska ha exakt 2 lager (sky + mid).
            // Var och ett ger minst 1 DrawPartialSprite-anrop → minst 2 totalt.
            foreach (var season in new[] { Season.Spring, Season.Summer, Season.Fall, Season.Winter })
            {
                var rc  = new FakeRenderContext();
                var sys = new ParallaxSystem();

                sys.SetSeason(season);
                sys.Draw(rc, cameraOffsetX: 0f, ScreenW, ScreenH);

                Assert.IsTrue(rc.DrawnSprites.Count >= 2,
                    $"{season}: minst 2 DrawPartialSprite-anrop förväntas (ett per lager).");
            }
        }

        // ─────────────────────────────────────────────
        // Rätt sprites per årstid
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_Spring_UsesSpringSpriteIds()
        {
            var rc  = new FakeRenderContext();
            var sys = new ParallaxSystem();

            sys.SetSeason(Season.Spring);
            sys.Draw(rc, cameraOffsetX: 0f, ScreenW, ScreenH);

            var sheets = rc.DrawnSprites.Select(s => s.Sheet).Distinct().ToList();
            CollectionAssert.Contains(sheets, SpriteId.ParallaxSkySpring,
                "Vår-bakgrund ska använda ParallaxSkySpring.");
            CollectionAssert.Contains(sheets, SpriteId.ParallaxMidSpring,
                "Vår-bakgrund ska använda ParallaxMidSpring.");
        }

        [TestMethod]
        public void Draw_Winter_UsesWinterSpriteIds()
        {
            var rc  = new FakeRenderContext();
            var sys = new ParallaxSystem();

            sys.SetSeason(Season.Winter);
            sys.Draw(rc, cameraOffsetX: 0f, ScreenW, ScreenH);

            var sheets = rc.DrawnSprites.Select(s => s.Sheet).Distinct().ToList();
            CollectionAssert.Contains(sheets, SpriteId.ParallaxSkyWinter,
                "Vinter-bakgrund ska använda ParallaxSkyWinter.");
            CollectionAssert.Contains(sheets, SpriteId.ParallaxMidWinter,
                "Vinter-bakgrund ska använda ParallaxMidWinter.");
        }

        [TestMethod]
        public void Draw_Boss_UsesBossSpriteIds()
        {
            var rc  = new FakeRenderContext();
            var sys = new ParallaxSystem();

            sys.SetSeason(Season.Boss);
            sys.Draw(rc, cameraOffsetX: 0f, ScreenW, ScreenH);

            var sheets = rc.DrawnSprites.Select(s => s.Sheet).Distinct().ToList();
            CollectionAssert.Contains(sheets, SpriteId.ParallaxSkyBoss,
                "Boss-bakgrund ska använda ParallaxSkyBoss.");
            CollectionAssert.Contains(sheets, SpriteId.ParallaxMidBoss,
                "Boss-bakgrund ska använda ParallaxMidBoss.");
        }

        // ─────────────────────────────────────────────
        // Ritordning: sky-lager ritas före mid-lager
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Draw_SkyLayerDrawnBeforeMidLayer()
        {
            // Sky (SpriteId.ParallaxSkyWinter) ska rita sitt första anrop
            // innan Mid (SpriteId.ParallaxMidWinter) ritar sitt första anrop.
            var rc  = new FakeRenderContext();
            var sys = new ParallaxSystem();

            sys.SetSeason(Season.Winter);
            sys.Draw(rc, cameraOffsetX: 0f, ScreenW, ScreenH);

            int firstSkyIdx = rc.DrawnSprites
                .Select((s, i) => (s.Sheet, i))
                .First(x => x.Sheet == SpriteId.ParallaxSkyWinter).i;

            int firstMidIdx = rc.DrawnSprites
                .Select((s, i) => (s.Sheet, i))
                .First(x => x.Sheet == SpriteId.ParallaxMidWinter).i;

            Assert.IsTrue(firstSkyIdx < firstMidIdx,
                "Sky-lagret ska ritas före mid-lagret (bakre → främre ordning).");
        }

        // ─────────────────────────────────────────────
        // SetSeason byter korrekt lager
        // ─────────────────────────────────────────────

        [TestMethod]
        public void SetSeason_TwiceSwitchesLayers()
        {
            // Sätt Vinter, verifiera winter-sprites. Byt till Vår, verifiera vår-sprites.
            var sys = new ParallaxSystem();

            sys.SetSeason(Season.Winter);
            var rc1 = new FakeRenderContext();
            sys.Draw(rc1, 0f, ScreenW, ScreenH);
            var winterSheets = rc1.DrawnSprites.Select(s => s.Sheet).Distinct().ToList();
            CollectionAssert.Contains(winterSheets, SpriteId.ParallaxSkyWinter);

            sys.SetSeason(Season.Spring);
            var rc2 = new FakeRenderContext();
            sys.Draw(rc2, 0f, ScreenW, ScreenH);
            var springSheets = rc2.DrawnSprites.Select(s => s.Sheet).Distinct().ToList();
            CollectionAssert.Contains(springSheets, SpriteId.ParallaxSkySpring,
                "Efter SetSeason(Spring) ska vårsprites användas.");
            CollectionAssert.DoesNotContain(springSheets, SpriteId.ParallaxSkyWinter,
                "Vintersprites ska inte ritas efter byte till vår.");
        }
    }
}
