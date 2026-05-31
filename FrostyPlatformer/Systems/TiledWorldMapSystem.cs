#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Data-driven implementation av IWorldMapSystem. Läser StopPoint-noder från
    /// worldmap.json och beräknar navigation och tillstånd utan hårdkodade koordinater.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Stateful Service (nod-lista injiceras i konstruktorn)
    ///
    /// MOTIVERING:
    /// Den gamla WorldMapSystem använde en hårdkodad formel (corrX = 3 + (N-1)*3)
    /// och konstanter i GameConstants för att avgöra var spelaren befann sig på
    /// världskartan. Det innebar att karttopologin var inbakad i koden — flytta en
    /// nod och koden slutar fungera tyst. TiledWorldMapSystem läser topologin direkt
    /// från PlacedObject-listan i worldmap.json. Inga koordinatkonstanter behövs.
    /// Rörelselogiken är strikt 4-riktningsbaserad (höger/vänster/upp/ned) med ett
    /// toleransfönster på en tile i den korsande axeln — precis som i SMB3.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() med StopPoint-noder från Aggregate.GetMapData().
    /// Injiceras via GameServices.WorldMap. WorldMapState anropar GetCurrentNode,
    /// GetNextNode och GetNodeState per frame; GetSpawnTile vid enter av världskartan.
    /// Om node-listan är tom faller metoderna igenom till säkra null/defaultvärden
    /// så att spelet fungerar även under övergångsperioden innan worldmap.json är
    /// byggt med stoppunkter.
    /// </remarks>
    public sealed class TiledWorldMapSystem : IWorldMapSystem
    {
        // Max tillåten avvikelse i korsande axel vid nod-sökning (i tiles).
        // En tile tolerans tillåter lätt förskjutna noder i kartdesignen.
        private const int CrossAxisTolerance = 1;

        private readonly IReadOnlyList<PlacedObject> _nodes;

        /// <param name="nodes">
        /// Alla StopPoint-noder från worldmap.json (ObjectType == "StopPoint").
        /// Kan vara tom lista under övergångsperioden — metoderna returnerar
        /// då null/defaultvärden utan krasch.
        /// </param>
        public TiledWorldMapSystem(IReadOnlyList<PlacedObject> nodes)
        {
            _nodes = nodes;
        }

        // ── Nod-navigation ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        public PlacedObject? GetCurrentNode(int tileX, int tileY)
            => _nodes.FirstOrDefault(n => n.TileX == tileX && n.TileY == tileY);

        /// <inheritdoc/>
        public PlacedObject? GetNextNode(PlacedObject current, int dX, int dY)
        {
            // Horisontell rörelse — sök längs X-axeln med Y-tolerans
            if (dX != 0 && dY == 0)
            {
                var candidates = _nodes.Where(n =>
                    (dX > 0 ? n.TileX > current.TileX : n.TileX < current.TileX) &&
                    Math.Abs(n.TileY - current.TileY) <= CrossAxisTolerance);

                return dX > 0
                    ? candidates.OrderBy(n => n.TileX).FirstOrDefault()
                    : candidates.OrderByDescending(n => n.TileX).FirstOrDefault();
            }

            // Vertikal rörelse — sök längs Y-axeln med X-tolerans
            if (dY != 0 && dX == 0)
            {
                var candidates = _nodes.Where(n =>
                    (dY > 0 ? n.TileY > current.TileY : n.TileY < current.TileY) &&
                    Math.Abs(n.TileX - current.TileX) <= CrossAxisTolerance);

                return dY > 0
                    ? candidates.OrderBy(n => n.TileY).FirstOrDefault()
                    : candidates.OrderByDescending(n => n.TileY).FirstOrDefault();
            }

            return null;
        }

        /// <inheritdoc/>
        public NodeState GetNodeState(PlacedObject node, int stageCompleted)
        {
            // Tom SubType = korsning, alltid passerbar
            if (node.SubType == "") return NodeState.Junction;

            int idx = GetStageIndex(node.SubType);
            if (idx < 0) return NodeState.Locked;   // okänd SubType behandlas som låst

            if (stageCompleted >= idx) return NodeState.Completed;
            if (stageCompleted == idx - 1) return NodeState.Current;
            return NodeState.Locked;
        }

        /// <inheritdoc/>
        public (int TileX, int TileY) GetSpawnTile(int spawnAtWorldMap, int defaultTileX, int defaultTileY)
        {
            if (spawnAtWorldMap >= 1 && spawnAtWorldMap <= 9)
            {
                string subType = StageIndexToSubType(spawnAtWorldMap);
                var node = _nodes.FirstOrDefault(n => n.SubType == subType);
                if (node != null) return (node.TileX, node.TileY);
            }
            return (defaultTileX, defaultTileY);
        }

        /// <inheritdoc/>
        public int GetStageIndex(string subType) => subType switch
        {
            "mapone"   => 1, "maptwo"   => 2, "mapthree" => 3,
            "mapfour"  => 4, "mapfive"  => 5, "mapsix"   => 6,
            "mapseven" => 7, "mapeight" => 8, "mapnine"  => 9,
            _          => -1
        };

        // ── Banstart ───────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public (string MapName, float X, float Y)? GetStageEntry(int stage) => stage switch
        {
            1 => (MapName.MapOne,   2f, 23f),
            2 => (MapName.MapTwo,   2f, 23f),
            3 => (MapName.MapThree, 2f, 20f),
            4 => (MapName.MapFour,  2f,  3f),
            5 => (MapName.MapFive,  2f, 33f),
            6 => (MapName.MapSix,   2f, 22f),
            7 => (MapName.MapSeven, 3f, 18f),
            8 => (MapName.MapEight, 4f, 41f),
            9 => (MapName.MapNine,  2f,  5f),
            _ => null
        };

        // ── Intern hjälp ───────────────────────────────────────────────────────

        private static string StageIndexToSubType(int index) => index switch
        {
            1 => "mapone",   2 => "maptwo",   3 => "mapthree",
            4 => "mapfour",  5 => "mapfive",  6 => "mapsix",
            7 => "mapseven", 8 => "mapeight", 9 => "mapnine",
            _ => ""
        };
    }
}
