#nullable enable
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IWorldMapSystem. Returnerar förutsägbara värden utan
    /// beroende på GameConstants eller speldata.
    /// </summary>
    public class FakeWorldMapSystem : IWorldMapSystem
    {
        /// <summary>Returvärde från GetStageEntry(). Null = stage hittades ej.</summary>
        public (string MapName, float X, float Y)? StageEntryResult { get; set; }
            = ("testmap", 2f, 23f);

        /// <summary>Returvärde från GetSpawnPosition().</summary>
        public (float X, float Y) SpawnPositionResult { get; set; } = (3f, 8f);

        public int GetStageEntryCallCount { get; private set; }
        public int LastStageQueried       { get; private set; }

        public (string MapName, float X, float Y)? GetStageEntry(int stage)
        {
            GetStageEntryCallCount++;
            LastStageQueried = stage;
            return StageEntryResult;
        }

        public (float X, float Y) GetSpawnPosition(int spawnAtWorldMap)
            => SpawnPositionResult;
    }
}
