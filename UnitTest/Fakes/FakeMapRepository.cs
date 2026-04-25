#nullable enable
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;
using System.Collections.Generic;

namespace UnitTest.Fakes
{
    /// <summary>
    /// In-memory-implementering av IMapRepository för testning.
    /// Låter tester kontrollera vilka kartposter som finns tillgängliga
    /// utan fil-I/O eller Aggregate-beroende.
    /// </summary>
    public class FakeMapRepository : IMapRepository
    {
        private readonly Dictionary<string, LevelObj> _maps;

        // ── Statistik för verifiering ────────────────────────────────────────

        public int LoadCallCount { get; private set; }
        public string? LastLoadedMapId { get; private set; }
        public int SaveCallCount { get; private set; }
        public string? LastSavedMapId { get; private set; }

        // ── Konstruktorer ────────────────────────────────────────────────────

        /// <summary>Skapar ett tomt repository — alla Load-anrop returnerar null.</summary>
        public FakeMapRepository()
        {
            _maps = new Dictionary<string, LevelObj>();
        }

        /// <summary>Skapar ett repository förifyllt med angivna kartposter.</summary>
        public FakeMapRepository(Dictionary<string, LevelObj> maps)
        {
            _maps = maps;
        }

        // ── Testhjälpare ─────────────────────────────────────────────────────

        /// <summary>Lägger till eller ersätter en kartpost i faken.</summary>
        public void AddMap(string mapId, LevelObj levelObj) => _maps[mapId] = levelObj;

        // ── IMapRepository ───────────────────────────────────────────────────

        /// <summary>Returnerar den förifyllda kartposten, eller null om den saknas.</summary>
        public LevelObj? Load(string mapId)
        {
            LoadCallCount++;
            LastLoadedMapId = mapId;
            return _maps.TryGetValue(mapId, out var map) ? map : null;
        }

        /// <summary>Returnerar de kart-ID:n som lagts till i faken.</summary>
        public IEnumerable<string> GetAvailableMapIds() => _maps.Keys;

        /// <summary>Lagrar kartan i minnet — simulerar sparning utan fil-I/O.</summary>
        public void Save(string mapId, LevelObj level)
        {
            SaveCallCount++;
            LastSavedMapId = mapId;
            _maps[mapId] = level;
        }
    }
}
