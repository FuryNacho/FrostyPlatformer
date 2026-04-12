#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using System.Collections.Generic;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Läser JSON-kartfiler från disk via ReadWrite och returnerar deserialiserade LevelObj.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Repository
    ///
    /// MOTIVERING:
    /// Kapslar in all fil-I/O för kartdata bakom IMapRepository. Aggregate behöver
    /// inte längre känna till filsökvägar eller ReadWrite-anrop för kartladdning —
    /// det ansvaret bor nu här. Gör det möjligt att byta lagringsstrategi (t.ex.
    /// inbyggda resurser eller nätverk) utan att röra Aggregate.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Aggregate.Load() efter att ReadWrite initierats, och lagras
    /// som IMapRepository-fält. Anropas enbart av Aggregate.GetMapData() — inget
    /// annat system läser kartfiler direkt.
    /// </remarks>
    public class MapRepository : IMapRepository
    {
        private readonly ReadWrite _readWrite;
        private readonly string _pathMapData = @"\Resources\Assets\MapData";

        private static readonly string[] KnownMapIds =
        {
            "worldmap",
            "mapone", "maptwo", "mapthree", "mapfour", "mapfive",
            "mapsix", "mapseven", "mapeight", "mapnine"
        };

        /// <summary>Initierar repositoryt med en ReadWrite-instans för fil-I/O.</summary>
        public MapRepository(ReadWrite readWrite)
        {
            _readWrite = readWrite;
        }

        /// <summary>
        /// Laddar och deserialiserar kartfilen för angiven kart-ID.
        /// Returnerar null om filen saknas eller inte kan läsas.
        /// </summary>
        public LevelObj? Load(string mapId)
        {
            var fileName = $@"\{mapId}";
            return _readWrite.ReadJson<LevelObj>(_pathMapData, fileName, ".json", CreateFile: false);
        }

        /// <summary>Returnerar samtliga kart-ID:n som spelet känner till.</summary>
        public IEnumerable<string> GetAvailableMapIds() => KnownMapIds;
    }
}
