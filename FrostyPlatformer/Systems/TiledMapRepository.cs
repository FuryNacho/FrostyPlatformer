#nullable enable
using FrostyPlatformer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly string[] KnownMapIds =
        {
            "worldmap",
            "mapone", "maptwo", "mapthree", "mapfour", "mapfive",
            "mapsix", "mapseven", "mapeight", "mapnine"
        };

        /// <summary>Initierar repositoryt med sökvägen till mappen med Tiled JSON-kartor.</summary>
        public TiledMapRepository(string basePath)
        {
            _basePath = basePath;
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

        /// <summary>Returnerar samtliga kart-ID:n som repositoryt känner till.</summary>
        public IEnumerable<string> GetAvailableMapIds() => KnownMapIds;

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

            // "Tiles"-lagret är obligatoriskt — utan det kan inget renderas
            if (tilesLayer == null) return null;

            // firstgid är offset som Tiled lägger till alla GID:n; subtrahera för
            // att komma tillbaka till sprite-sheet-index som spelet förväntar sig
            int firstGid = map.Tilesets.Count > 0 ? map.Tilesets[0].FirstGid : 1;

            return new LevelObj
            {
                Width          = map.Width,
                Height         = map.Height,
                TileIndex      = ConvertTileData(tilesLayer.Data, firstGid),
                AttributeIndex = collisionLayer?.Data ?? new int[map.Width * map.Height]
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
            public int[]  Data { get; set; } = Array.Empty<int>();
        }

        private sealed class TiledTilesetDto
        {
            public int    FirstGid { get; set; } = 1;
            public string Source   { get; set; } = "";
        }
    }
}
