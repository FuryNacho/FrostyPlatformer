#nullable enable

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
                case 1: return ("mapone",   2f, 23f);
                case 2: return ("maptwo",   2f, 23f);
                case 3: return ("mapthree", 2f, 20f);
                case 4: return ("mapfour",  2f,  3f);
                case 5: return ("mapfive",  2f, 33f);
                case 6: return ("mapsix",   2f, 22f);
                case 7: return ("mapseven", 3f, 18f);
                case 8: return ("mapeight", 4f, 41f);
                default: return null;
            }
        }

        /// <inheritdoc/>
        public (float X, float Y) GetSpawnPosition(int spawnAtWorldMap)
        {
            if (spawnAtWorldMap >= 1 && spawnAtWorldMap <= 8)
                return (3f + (spawnAtWorldMap - 1) * 3f, 8f);

            return (3f, 8f);
        }
    }
}
