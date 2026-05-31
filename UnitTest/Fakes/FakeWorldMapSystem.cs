#nullable enable
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IWorldMapSystem. Returnerar förutsägbara värden utan
    /// beroende på GameConstants eller speldata.
    /// </summary>
    public class FakeWorldMapSystem : IWorldMapSystem
    {
        // ── Konfigurerbara returvärden ─────────────────────────────────────────

        /// <summary>Returvärde från GetStageEntry(). Null = stage hittades ej.</summary>
        public (string MapName, float X, float Y)? StageEntryResult { get; set; }
            = ("testmap", 2f, 23f);

        /// <summary>Returvärde från GetCurrentNode().</summary>
        public PlacedObject? CurrentNodeResult { get; set; }

        /// <summary>Returvärde från GetNextNode().</summary>
        public PlacedObject? NextNodeResult { get; set; }

        /// <summary>Returvärde från GetNodeState().</summary>
        public NodeState NodeStateResult { get; set; } = NodeState.Current;

        /// <summary>Returvärde från GetSpawnTile().</summary>
        public (int TileX, int TileY) SpawnTileResult { get; set; } = (3, 8);

        /// <summary>Returvärde från GetStageIndex().</summary>
        public int StageIndexResult { get; set; } = 1;

        // ── Anropsräknare ──────────────────────────────────────────────────────

        public int GetStageEntryCallCount { get; private set; }
        public int LastStageQueried       { get; private set; }

        // ── Implementationer ───────────────────────────────────────────────────

        public PlacedObject? GetCurrentNode(int tileX, int tileY) => CurrentNodeResult;

        public PlacedObject? GetNextNode(PlacedObject current, int dX, int dY) => NextNodeResult;

        public NodeState GetNodeState(PlacedObject node, int stageCompleted) => NodeStateResult;

        public (int TileX, int TileY) GetSpawnTile(int spawnAtWorldMap, int defaultTileX, int defaultTileY)
            => SpawnTileResult;

        public int GetStageIndex(string subType) => StageIndexResult;

        public (string MapName, float X, float Y)? GetStageEntry(int stage)
        {
            GetStageEntryCallCount++;
            LastStageQueried = stage;
            return StageEntryResult;
        }
    }
}
