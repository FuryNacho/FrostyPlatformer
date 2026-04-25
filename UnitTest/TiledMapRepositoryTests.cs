#nullable enable
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för TiledMapRepository — verifierar att Tiled JSON-format
    /// läses korrekt och konverteras till LevelObj med rätt TileIndex och AttributeIndex.
    /// ParseTiledJson testas direkt (internal via InternalsVisibleTo) för rena
    /// konverteringstester utan fil-I/O. Load() testas med temporära filer.
    /// </summary>
    [TestClass]
    public class TiledMapRepositoryTests
    {
        // ── Hjälpmetod för minimal giltig Tiled JSON ──────────────────────────

        // 2×1-karta: Tiles-lagret [1, 8], Collision-lagret [0, 1], firstgid=1
        // Förväntat TileIndex: [0, 7]  (1-1=0, 8-1=7)
        // Förväntat AttributeIndex: [0, 1]
        private const string MinimalJson = @"{
            ""width"": 2,
            ""height"": 1,
            ""tilesets"": [{ ""firstgid"": 1, ""source"": ""tiles.tsx"" }],
            ""layers"": [
                { ""name"": ""Tiles"",     ""data"": [1, 8] },
                { ""name"": ""Collision"", ""data"": [0, 1] }
            ]
        }";

        // ── ParseTiledJson — dimensioner ──────────────────────────────────────

        [TestMethod]
        public void ParseTiledJson_ValidJson_ReturnsCorrectWidth()
        {
            var result = TiledMapRepository.ParseTiledJson(MinimalJson);

            Assert.AreEqual(2, result!.Width);
        }

        [TestMethod]
        public void ParseTiledJson_ValidJson_ReturnsCorrectHeight()
        {
            var result = TiledMapRepository.ParseTiledJson(MinimalJson);

            Assert.AreEqual(1, result!.Height);
        }

        // ── ParseTiledJson — GID-offset ───────────────────────────────────────

        [TestMethod]
        public void ParseTiledJson_TileGid_SubtractsFirstGid()
        {
            // GID 1 med firstgid=1 → sprite-index 0 (lufttile)
            // GID 8 med firstgid=1 → sprite-index 7
            var result = TiledMapRepository.ParseTiledJson(MinimalJson);

            CollectionAssert.AreEqual(new[] { 0, 7 }, result!.TileIndex);
        }

        [TestMethod]
        public void ParseTiledJson_TileGidZero_MappedToZero()
        {
            // GID 0 = "ingen tile" i Tiled → sprite-index 0 (lufttile)
            var json = @"{
                ""width"": 1, ""height"": 1,
                ""tilesets"": [{ ""firstgid"": 1, ""source"": ""t.tsx"" }],
                ""layers"": [{ ""name"": ""Tiles"", ""data"": [0] }]
            }";

            var result = TiledMapRepository.ParseTiledJson(json);

            Assert.AreEqual(0, result!.TileIndex[0]);
        }

        [TestMethod]
        public void ParseTiledJson_FirstGidTwo_SubtractsCorrectly()
        {
            // Om firstgid=2: GID 3 → sprite-index 1, GID 2 → sprite-index 0
            var json = @"{
                ""width"": 2, ""height"": 1,
                ""tilesets"": [{ ""firstgid"": 2, ""source"": ""t.tsx"" }],
                ""layers"": [{ ""name"": ""Tiles"", ""data"": [2, 3] }]
            }";

            var result = TiledMapRepository.ParseTiledJson(json);

            CollectionAssert.AreEqual(new[] { 0, 1 }, result!.TileIndex);
        }

        // ── ParseTiledJson — kollisionslager ──────────────────────────────────

        [TestMethod]
        public void ParseTiledJson_CollisionLayer_PreservesZeroAndOne()
        {
            // Kollisionslagret har inga GID:n — 0 och 1 är boolean-flaggor
            var result = TiledMapRepository.ParseTiledJson(MinimalJson);

            CollectionAssert.AreEqual(new[] { 0, 1 }, result!.AttributeIndex);
        }

        [TestMethod]
        public void ParseTiledJson_NoCollisionLayer_ReturnsAllZeroAttributeIndex()
        {
            // Utan Collision-lager ska AttributeIndex vara en nollarray
            var json = @"{
                ""width"": 3, ""height"": 1,
                ""tilesets"": [{ ""firstgid"": 1, ""source"": ""t.tsx"" }],
                ""layers"": [{ ""name"": ""Tiles"", ""data"": [1, 2, 3] }]
            }";

            var result = TiledMapRepository.ParseTiledJson(json);

            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, result!.AttributeIndex);
        }

        // ── ParseTiledJson — ogiltiga indata ─────────────────────────────────

        [TestMethod]
        public void ParseTiledJson_MissingTilesLayer_ReturnsNull()
        {
            // "Tiles"-lagret är obligatoriskt — utan det kan inget renderas
            var json = @"{
                ""width"": 2, ""height"": 1,
                ""tilesets"": [{ ""firstgid"": 1, ""source"": ""t.tsx"" }],
                ""layers"": [{ ""name"": ""Collision"", ""data"": [0, 1] }]
            }";

            var result = TiledMapRepository.ParseTiledJson(json);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void ParseTiledJson_EmptyString_ReturnsNull()
        {
            var result = TiledMapRepository.ParseTiledJson("{}");

            // Inget Tiles-lager → null
            Assert.IsNull(result);
        }

        // ── ParseTiledJson — firstgid saknas ─────────────────────────────────

        [TestMethod]
        public void ParseTiledJson_NoTilesets_DefaultsToFirstGidOne()
        {
            // Utan tilesets-array antas firstgid=1 (vanligaste fallet)
            var json = @"{
                ""width"": 1, ""height"": 1,
                ""tilesets"": [],
                ""layers"": [{ ""name"": ""Tiles"", ""data"": [5] }]
            }";

            var result = TiledMapRepository.ParseTiledJson(json);

            // firstgid=1 (default) → GID 5 → sprite-index 4
            Assert.AreEqual(4, result!.TileIndex[0]);
        }

        // ── Load — fil-I/O ────────────────────────────────────────────────────

        private string? _tempDir;

        [TestInitialize]
        public void Setup() => _tempDir = Path.Combine(Path.GetTempPath(), "TiledMapRepositoryTests_" + System.Guid.NewGuid());

        [TestCleanup]
        public void Cleanup()
        {
            if (_tempDir != null && Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [TestMethod]
        public void Load_ExistingFile_ReturnsMappedLevelObj()
        {
            Directory.CreateDirectory(_tempDir!);
            File.WriteAllText(Path.Combine(_tempDir!, "mapone.json"), MinimalJson);
            var repo = new TiledMapRepository(_tempDir!);

            var result = repo.Load("mapone");

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result!.Width);
        }

        [TestMethod]
        public void Load_MissingFile_ReturnsNull()
        {
            Directory.CreateDirectory(_tempDir!);
            var repo = new TiledMapRepository(_tempDir!);

            Assert.IsNull(repo.Load("doesnotexist"));
        }

        // ── ParseTiledJson — spawn-punkt (E2d) ───────────────────────────────

        private const string JsonWithSpawn = @"{
            ""width"": 2, ""height"": 2,
            ""tilesets"": [{ ""firstgid"": 1, ""source"": ""t.tsx"" }],
            ""layers"": [
                { ""name"": ""Tiles"",     ""type"": ""tilelayer"",   ""data"": [1, 1, 1, 1] },
                { ""name"": ""Objects"",   ""type"": ""objectgroup"",
                  ""objects"": [{ ""name"": ""PlayerSpawn"", ""x"": 32.0, ""y"": 16.0 }] }
            ]
        }";

        [TestMethod]
        public void ParseTiledJson_WithSpawnObject_SetsSpawnX()
        {
            // x=32, TileSize=16 → SpawnX=2
            var result = TiledMapRepository.ParseTiledJson(JsonWithSpawn);

            Assert.AreEqual(2, result!.SpawnX);
        }

        [TestMethod]
        public void ParseTiledJson_WithSpawnObject_SetsSpawnY()
        {
            // y=16, TileSize=16 → SpawnY=1
            var result = TiledMapRepository.ParseTiledJson(JsonWithSpawn);

            Assert.AreEqual(1, result!.SpawnY);
        }

        [TestMethod]
        public void ParseTiledJson_WithSpawnObject_HasSpawnIsTrue()
        {
            var result = TiledMapRepository.ParseTiledJson(JsonWithSpawn);

            Assert.IsTrue(result!.HasSpawn);
        }

        [TestMethod]
        public void ParseTiledJson_NoObjectsLayer_SpawnIsMinusOne()
        {
            // Utan Objects-lager ska SpawnX och SpawnY vara -1 (standardvärdet)
            var result = TiledMapRepository.ParseTiledJson(MinimalJson);

            Assert.AreEqual(-1, result!.SpawnX);
            Assert.AreEqual(-1, result!.SpawnY);
        }

        [TestMethod]
        public void ParseTiledJson_NoObjectsLayer_HasSpawnIsFalse()
        {
            var result = TiledMapRepository.ParseTiledJson(MinimalJson);

            Assert.IsFalse(result!.HasSpawn);
        }

        [TestMethod]
        public void ParseTiledJson_ObjectsLayerWithoutPlayerSpawn_SpawnIsMinusOne()
        {
            // Objects-lagret finns men saknar PlayerSpawn-objekt
            var json = @"{
                ""width"": 1, ""height"": 1,
                ""tilesets"": [{ ""firstgid"": 1, ""source"": ""t.tsx"" }],
                ""layers"": [
                    { ""name"": ""Tiles"",   ""type"": ""tilelayer"",   ""data"": [1] },
                    { ""name"": ""Objects"", ""type"": ""objectgroup"",
                      ""objects"": [{ ""name"": ""SomethingElse"", ""x"": 16.0, ""y"": 16.0 }] }
                ]
            }";

            var result = TiledMapRepository.ParseTiledJson(json);

            Assert.AreEqual(-1, result!.SpawnX);
            Assert.IsFalse(result!.HasSpawn);
        }

        // ── GetAvailableMapIds ────────────────────────────────────────────────

        [TestMethod]
        public void GetAvailableMapIds_ContainsTenMaps()
        {
            var repo = new TiledMapRepository(_tempDir ?? ".");
            var ids  = new System.Collections.Generic.List<string>(repo.GetAvailableMapIds());

            Assert.AreEqual(10, ids.Count);
        }

        [TestMethod]
        public void GetAvailableMapIds_ContainsWorldmap()
        {
            var repo = new TiledMapRepository(_tempDir ?? ".");

            CollectionAssert.Contains(
                new System.Collections.Generic.List<string>(repo.GetAvailableMapIds()),
                "worldmap");
        }
    }
}
