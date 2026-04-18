#nullable enable
using FrostyPlatformer.Models;
using System.Collections.Generic;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Dataåtkomst för kartfiler — laddar JSON-kartdata från disk.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Repository
    ///
    /// MOTIVERING:
    /// Aggregate.LoadAllMapData() anropade tidigare ReadWrite.ReadJson direkt för varje
    /// karta, vilket blandade fil-I/O med Aggregates övriga ansvar och omöjliggjorde
    /// testning utan tillgång till filsystemet. IMapRepository isolerar detta ansvar
    /// bakom ett interface så att beroende kod kan testas med en in-memory-fake.
    ///
    /// ANVÄNDNING:
    /// Skapas i Aggregate.Load() som new TiledMapRepository(basePath) och används internt
    /// för att ladda LevelObj-data. I tester används FakeMapRepository med förifyllda
    /// kartposter.
    /// </remarks>
    public interface IMapRepository
    {
        /// <summary>
        /// Laddar kartdata för angiven kart-ID. Returnerar null om kartan inte finns.
        /// </summary>
        LevelObj? Load(string mapId);

        /// <summary>
        /// Returnerar de kart-ID:n som repositoryt känner till.
        /// </summary>
        IEnumerable<string> GetAvailableMapIds();
    }
}
