#nullable enable
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Läser Tiled JSON-kartfiler från disk och konverterar dem till LevelObj.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Repository + Adapter
    ///
    /// MOTIVERING:
    /// Implementerar IMapRepository för Tiled JSON-formatet. Konverterar Tiled's
    /// lager-struktur och GID-offset (+1) till ett vanligt LevelObj med TileIndex och
    /// AttributeIndex. Resten av spelet (TileMapRenderer, CollisionSystem, CameraSystem)
    /// ser bara ett LevelObj och är okänsligt för kartformat.
    ///
    /// ANVÄNDNING:
    /// Skapas i Aggregate.Load() med sökvägen till mappen med Tiled-kartor (MapData/Tiled/).
    /// Kartfiler genereras av Tools/ConvertMaps eller sparas direkt av in-game-editorn.
    /// ParseTiledJson är internal för att kunna testas utan fil-I/O via InternalsVisibleTo.
    /// </remarks>
    public class TiledMapRepository : IMapRepository
    {
        private readonly string _basePath;
        private readonly bool   _scanDirectory;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly string[] KnownMapIds =
        {
            MapName.WorldMap,
            MapName.MapOne,   MapName.MapTwo,   MapName.MapThree,
            MapName.MapFour,  MapName.MapFive,  MapName.MapSix,
            MapName.MapSeven, MapName.MapEight, MapName.MapNine
        };

        /// <summary>
        /// Initierar repositoryt med sökvägen till mappen med Tiled JSON-kartor.
        /// </summary>
        /// <param name="basePath">Sökväg till kartmappen.</param>
        /// <param name="scanDirectory">
        /// True = <see cref="GetAvailableMapIds"/> skannar filsystemet istället för
        /// att returnera den hårdkodade listan. Används för UserMaps/ där innehållet
        /// varierar beroende på vad användaren har sparat.
        /// </param>
        public TiledMapRepository(string basePath, bool scanDirectory = false)
        {
            _basePath      = basePath;
            _scanDirectory = scanDirectory;
        }

        /// <summary>
        /// Laddar och konverterar en Tiled JSON-kartfil till LevelObj.
        /// Returnerar null om filen saknas eller om JSON-strukturen är ogiltig.
        /// </summary>
        public LevelObj? Load(string mapId)
        {
            var filePath = Path.Combine(_basePath, mapId + ".json");
            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = File.ReadAllText(filePath);
                return ParseTiledJson(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returnerar samtliga kart-ID:n. Om <c>scanDirectory</c> är satt skannas
        /// filsystemet efter .json-filer; annars returneras den hårdkodade listan.
        /// </summary>
        public IEnumerable<string> GetAvailableMapIds()
        {
            if (!_scanDirectory) return KnownMapIds;
            if (!Directory.Exists(_basePath)) return Array.Empty<string>();
            return Directory.GetFiles(_basePath, "*.json")
                .Select(Path.GetFileNameWithoutExtension)!;
        }

        // ── Konverteringslogik (internal för enhetstestning) ──────────────────

        /// <summary>
        /// Parsar en Tiled JSON-sträng och konverterar till LevelObj.
        /// Förväntar sig lagret "Tiles" (obligatoriskt) och "Collision" (valfritt).
        /// Returnerar null om "Tiles"-lagret saknas eller om JSON är ogiltig.
        /// </summary>
        internal static LevelObj? ParseTiledJson(string json)
        {
            var map = JsonSerializer.Deserialize<TiledMapDto>(json, JsonOptions);
            if (map == null) return null;

            var tilesLayer     = map.Layers.FirstOrDefault(l => l.Name == "Tiles");
            var collisionLayer = map.Layers.FirstOrDefault(l => l.Name == "Collision");
            var objectsLayer   = map.Layers.FirstOrDefault(l => l.Type == "objectgroup" && l.Name == "Objects");

            // "Tiles"-lagret är obligatoriskt — utan det kan inget renderas
            if (tilesLayer == null) return null;

            // firstgid är offset som Tiled lägger till alla GID:n; subtrahera för
            // att komma tillbaka till sprite-sheet-index som spelet förväntar sig
            int firstGid = map.Tilesets.Count > 0 ? map.Tilesets[0].FirstGid : 1;

            var level = new LevelObj
            {
                Width          = map.Width,
                Height         = map.Height,
                TileIndex      = ConvertTileData(tilesLayer.Data, firstGid),
                AttributeIndex = collisionLayer?.Data ?? new int[map.Width * map.Height],
                TilesetSource  = map.Tilesets.Count > 0 ? map.Tilesets[0].Source : "spring.tsx"
            };

            if (objectsLayer != null)
            {
                var spawn = objectsLayer.Objects.FirstOrDefault(
                    o => o.Name == "PlayerSpawn");
                if (spawn != null)
                {
                    level.SpawnX = (int)(spawn.X / GameConstants.TileSize);
                    level.SpawnY = (int)(spawn.Y / GameConstants.TileSize);
                }

                foreach (var obj in objectsLayer.Objects)
                {
                    if (obj.Name == "PlayerSpawn") continue;
                    if (string.IsNullOrEmpty(obj.Type)) continue;

                    level.Objects.Add(new PlacedObject
                    {
                        ObjectType = obj.Type,
                        SubType    = obj.Name,
                        TileX      = (int)(obj.X / GameConstants.TileSize),
                        TileY      = (int)(obj.Y / GameConstants.TileSize)
                    });
                }
            }

            return level;
        }

        /// <summary>
        /// Sparar ett LevelObj till disk i Tiled JSON-format.
        /// Skapar målmappen automatiskt om den inte finns.
        /// </summary>
        public void Save(string mapId, LevelObj level)
        {
            Directory.CreateDirectory(_basePath);
            var filePath = Path.Combine(_basePath, mapId + ".json");
            File.WriteAllText(filePath, BuildTiledJson(level));
        }

        // ── Konverteringslogik (internal för enhetstestning) ──────────────────

        /// <summary>
        /// Konverterar ett LevelObj till en Tiled JSON-sträng.
        /// Omvänd operation mot ParseTiledJson — lägger tillbaka GID-offset.
        /// </summary>
        internal static string BuildTiledJson(LevelObj level)
        {
            var layers = new JsonArray();
            layers.Add(BuildTileLayer("Tiles",     level.Width, level.Height,
                level.TileIndex.Select(id => id > 0 ? id + 1 : 0).ToArray()));
            layers.Add(BuildTileLayer("Collision", level.Width, level.Height,
                level.AttributeIndex));

            if (level.HasSpawn || level.Objects.Count > 0)
                layers.Add(BuildObjectsLayer(level));

            var root = new JsonObject
            {
                ["width"]      = level.Width,
                ["height"]     = level.Height,
                ["tilewidth"]  = GameConstants.TileSize,
                ["tileheight"] = GameConstants.TileSize,
                ["tilesets"]   = new JsonArray
                {
                    new JsonObject { ["firstgid"] = 1, ["source"] = level.TilesetSource }
                },
                ["layers"] = layers
            };

            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private static JsonObject BuildTileLayer(string name, int width, int height, int[] data)
        {
            var arr = new JsonArray();
            foreach (var v in data) arr.Add(JsonValue.Create(v));
            return new JsonObject
            {
                ["name"]   = name,
                ["type"]   = "tilelayer",
                ["width"]  = width,
                ["height"] = height,
                ["data"]   = arr
            };
        }

        private static JsonObject BuildObjectsLayer(LevelObj level)
        {
            var objects = new JsonArray();

            if (level.HasSpawn)
            {
                objects.Add(new JsonObject
                {
                    ["name"]   = "PlayerSpawn",
                    ["x"]      = level.SpawnX * GameConstants.TileSize,
                    ["y"]      = level.SpawnY * GameConstants.TileSize,
                    ["width"]  = GameConstants.TileSize,
                    ["height"] = GameConstants.TileSize
                });
            }

            foreach (var obj in level.Objects)
            {
                objects.Add(new JsonObject
                {
                    ["name"]   = obj.SubType,
                    ["type"]   = obj.ObjectType,
                    ["x"]      = obj.TileX * GameConstants.TileSize,
                    ["y"]      = obj.TileY * GameConstants.TileSize,
                    ["width"]  = GameConstants.TileSize,
                    ["height"] = GameConstants.TileSize
                });
            }

            return new JsonObject
            {
                ["name"]    = "Objects",
                ["type"]    = "objectgroup",
                ["objects"] = objects
            };
        }

        /// <summary>
        /// Subtraherar GID-offset från varje tile-värde.
        /// GID 0 betyder "ingen tile" i Tiled och mappas till sprite-index 0 (lufttile).
        /// </summary>
        private static int[] ConvertTileData(int[] data, int firstGid)
            => data.Select(gid => gid > 0 ? gid - firstGid : 0).ToArray();

        // ── Tiled JSON-DTOs (interna, används bara vid deserialisering) ────────

        private sealed class TiledMapDto
        {
            public int Width    { get; set; }
            public int Height   { get; set; }
            public List<TiledLayerDto>   Layers   { get; set; } = new();
            public List<TiledTilesetDto> Tilesets { get; set; } = new();
        }

        private sealed class TiledLayerDto
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "tilelayer";
            public int[]  Data { get; set; } = Array.Empty<int>();
            public List<TiledObjectDto> Objects { get; set; } = new();
        }

        private sealed class TiledObjectDto
        {
            public string Name   { get; set; } = "";
            public string Type   { get; set; } = "";
            public float  X      { get; set; }
            public float  Y      { get; set; }
        }

        private sealed class TiledTilesetDto
        {
            public int    FirstGid { get; set; } = 1;
            public string Source   { get; set; } = "";
        }
    }
}
