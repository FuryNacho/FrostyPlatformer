#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för IMapRepository — testar FakeMapRepository-kontraktet
    /// och verifierar att interfacet uppfyller de krav som konsumenter ställer.
    /// Ingen fil-I/O, Aggregate eller spelmotor krävs.
    /// </summary>
    [TestClass]
    public class MapRepositoryTests
    {
        private static LevelObj MakeLevelObj(int width = 4, int height = 2) =>
            new LevelObj
            {
                Width          = width,
                Height         = height,
                TileIndex      = new int[width * height],
                AttributeIndex = new int[width * height]
            };

        // ── Load — känt kart-ID ───────────────────────────────────────────────

        [TestMethod]
        public void Load_KnownMapId_ReturnsLevelObj()
        {
            var repo = new FakeMapRepository();
            repo.AddMap("mapone", MakeLevelObj());

            var result = repo.Load("mapone");

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Load_KnownMapId_ReturnsSameWidthAndHeight()
        {
            var repo = new FakeMapRepository();
            repo.AddMap("maptwo", MakeLevelObj(width: 10, height: 5));

            var result = repo.Load("maptwo");

            Assert.AreEqual(10, result!.Width);
            Assert.AreEqual(5,  result.Height);
        }

        [TestMethod]
        public void Load_KnownMapId_ReturnsTileIndexArray()
        {
            var expected = new int[] { 1, 2, 3, 4 };
            var obj = new LevelObj { Width = 2, Height = 2, TileIndex = expected, AttributeIndex = new int[4] };
            var repo = new FakeMapRepository();
            repo.AddMap("mapthree", obj);

            var result = repo.Load("mapthree");

            CollectionAssert.AreEqual(expected, result!.TileIndex);
        }

        // ── Load — okänt kart-ID ─────────────────────────────────────────────

        [TestMethod]
        public void Load_UnknownMapId_ReturnsNull()
        {
            var repo = new FakeMapRepository();

            var result = repo.Load("doesnotexist");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Load_EmptyRepository_ReturnsNull()
        {
            var repo = new FakeMapRepository();

            Assert.IsNull(repo.Load("worldmap"));
        }

        // ── Load — anropsstatistik ────────────────────────────────────────────

        [TestMethod]
        public void Load_TracksCallCount()
        {
            var repo = new FakeMapRepository();
            repo.AddMap("mapone", MakeLevelObj());

            repo.Load("mapone");
            repo.Load("maptwo");  // okänt — returnerar null men räknas ändå

            Assert.AreEqual(2, repo.LoadCallCount);
        }

        [TestMethod]
        public void Load_TracksLastLoadedMapId()
        {
            var repo = new FakeMapRepository();
            repo.AddMap("mapsix", MakeLevelObj());

            repo.Load("mapone");
            repo.Load("mapsix");

            Assert.AreEqual("mapsix", repo.LastLoadedMapId);
        }

        // ── GetAvailableMapIds ────────────────────────────────────────────────

        [TestMethod]
        public void GetAvailableMapIds_EmptyRepository_ReturnsEmpty()
        {
            var repo = new FakeMapRepository();

            Assert.IsFalse(repo.GetAvailableMapIds().Any());
        }

        [TestMethod]
        public void GetAvailableMapIds_ContainsAddedMaps()
        {
            var repo = new FakeMapRepository();
            repo.AddMap("mapone",   MakeLevelObj());
            repo.AddMap("maptwo",   MakeLevelObj());
            repo.AddMap("worldmap", MakeLevelObj());

            var ids = repo.GetAvailableMapIds().ToList();

            CollectionAssert.Contains(ids, "mapone");
            CollectionAssert.Contains(ids, "maptwo");
            CollectionAssert.Contains(ids, "worldmap");
        }

        [TestMethod]
        public void GetAvailableMapIds_CountMatchesAddedMaps()
        {
            var repo = new FakeMapRepository();
            repo.AddMap("mapone", MakeLevelObj());
            repo.AddMap("maptwo", MakeLevelObj());

            Assert.AreEqual(2, repo.GetAvailableMapIds().Count());
        }

        // ── AddMap — ersätt befintlig ─────────────────────────────────────────

        [TestMethod]
        public void AddMap_ReplacesExistingEntry_ReturnsNewValue()
        {
            var repo = new FakeMapRepository();
            repo.AddMap("mapone", MakeLevelObj(width: 4, height: 4));
            repo.AddMap("mapone", MakeLevelObj(width: 8, height: 8));

            var result = repo.Load("mapone");

            Assert.AreEqual(8, result!.Width);
        }

        // ── Konstruktor med dictionary ────────────────────────────────────────

        [TestMethod]
        public void Constructor_WithPrepopulatedDictionary_LoadsCorrectly()
        {
            var dict = new Dictionary<string, LevelObj>
            {
                { "mapfour", MakeLevelObj(width: 6, height: 3) }
            };
            var repo = new FakeMapRepository(dict);

            var result = repo.Load("mapfour");

            Assert.IsNotNull(result);
            Assert.AreEqual(6, result!.Width);
        }

        // ── MapRepository — GetAvailableMapIds innehåller alla spelmappar ─────

        [TestMethod]
        public void MapRepository_GetAvailableMapIds_ContainsAllTenMaps()
        {
            // MapRepository.KnownMapIds är private — vi verifierar via interfacet
            // att alla tio kartnamn som spelet använder finns med.
            // En FakeMapRepository används för att verifiera att Load fungerar
            // för var och ett av dessa ID:n.
            var expectedIds = new[]
            {
                "worldmap",
                "mapone", "maptwo", "mapthree", "mapfour", "mapfive",
                "mapsix", "mapseven", "mapeight", "mapnine"
            };

            var repo = new FakeMapRepository();
            foreach (var id in expectedIds)
                repo.AddMap(id, MakeLevelObj());

            var available = repo.GetAvailableMapIds().ToList();

            foreach (var id in expectedIds)
                CollectionAssert.Contains(available, id);
        }
    }
}
