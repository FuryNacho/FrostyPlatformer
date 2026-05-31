#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Implementerar världskartans datauppslagningar — stage-ingångspunkter och spawn-positioner.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Stateless Service (pure functions)
    ///
    /// MOTIVERING:
    /// Alla metoder är deterministiska och innehåller noll sidoeffekter.
    /// WorldMapSystem behöver inget tillstånd — den är stateless och kan
    /// instansieras en gång i Composition Root och återanvändas fritt.
    ///
    /// GetStageEntry kapslar in den switch-sats som tidigare låg i
    /// WorldMapState.TryEnterStage. GetSpawnPosition kapslar in formeln
    /// corrX = 3 + (spawn - 1) * 3f från WorldMapState.Update.
    ///
    /// ANVÄNDNING:
    /// new WorldMapSystem() i Program.OnCreate(). Injiceras via GameServices.WorldMap.
    /// </remarks>
    public class WorldMapSystem : IWorldMapSystem
    {
        /// <inheritdoc/>
        public (string MapName, float X, float Y)? GetStageEntry(int stage)
        {
            switch (stage)
            {
                case 1: return (MapName.MapOne,   2f, 23f);
                case 2: return (MapName.MapTwo,   2f, 23f);
                case 3: return (MapName.MapThree, 2f, 20f);
                case 4: return (MapName.MapFour,  2f,  3f);
                case 5: return (MapName.MapFive,  2f, 33f);
                case 6: return (MapName.MapSix,   2f, 22f);
                case 7: return (MapName.MapSeven, 3f, 18f);
                case 8: return (MapName.MapEight, 4f, 41f);
                case 9: return (MapName.MapNine,  2f,  5f);
                default: return null;
            }
        }

        // ── Nod-navigation (ej stödd av legacy-implementationen) ─────────────────

        /// <inheritdoc/>
        /// <remarks>Stöds ej av WorldMapSystem — använd TiledWorldMapSystem.</remarks>
        public PlacedObject? GetCurrentNode(int tileX, int tileY) => null;

        /// <inheritdoc/>
        /// <remarks>Stöds ej av WorldMapSystem — använd TiledWorldMapSystem.</remarks>
        public PlacedObject? GetNextNode(PlacedObject current, int dX, int dY) => null;

        /// <inheritdoc/>
        /// <remarks>Stöds ej av WorldMapSystem — använd TiledWorldMapSystem.</remarks>
        public NodeState GetNodeState(PlacedObject node, int stageCompleted) => NodeState.Locked;

        /// <inheritdoc/>
        /// <remarks>Stöds ej av WorldMapSystem — använd TiledWorldMapSystem.</remarks>
        public IEnumerable<(int TileX, int TileY)> GetCompletedNodeTiles(int stageCompleted)
            => Array.Empty<(int, int)>();

        /// <inheritdoc/>
        public (int TileX, int TileY) GetSpawnTile(int spawnAtWorldMap, int defaultTileX, int defaultTileY)
        {
            var (fx, fy) = GetSpawnPosition(spawnAtWorldMap);
            return ((int)fx, (int)fy);
        }

        /// <inheritdoc/>
        public int GetStageIndex(string subType) => subType switch
        {
            "mapone"   => 1, "maptwo"   => 2, "mapthree" => 3,
            "mapfour"  => 4, "mapfive"  => 5, "mapsix"   => 6,
            "mapseven" => 7, "mapeight" => 8, "mapnine"  => 9,
            _          => -1
        };

        /// <inheritdoc/>
        public (float X, float Y) GetSpawnPosition(int spawnAtWorldMap)
        {
            if (spawnAtWorldMap >= 1 && spawnAtWorldMap <= 9)
                return (3f + (spawnAtWorldMap - 1) * 3f, 8f);

            return (3f, 8f);
        }
    }
}
