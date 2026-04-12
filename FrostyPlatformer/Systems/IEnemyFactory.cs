#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Skapar fiendeentiteter utan att konsumenten behöver känna till konkreta klasser.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Factory Method
    ///
    /// MOTIVERING:
    /// Kartklassernas PopulateDynamics innehöll direkta new-anrop mot konkreta
    /// fiendeklasser (new DynamicCreatureEnemyWalrus() osv). En ny fiendetyp
    /// krävde ändringar i kartkoden — ett brott mot OCP. Med IEnemyFactory
    /// känner kartorna bara till EnemyType-enumet; fabriken kapslar in vilket
    /// konkret objekt som ska skapas.
    ///
    /// ANVÄNDNING:
    /// Skapas i Aggregate.Load() som new EnemyFactory() och injiceras i alla
    /// Map-subklasser via konstruktorn. I tester används FakeEnemyFactory som
    /// returnerar förkonfigurerade fiendeobjekt.
    /// </remarks>
    public interface IEnemyFactory
    {
        /// <summary>
        /// Skapar en ny fiendeinstans av angiven typ.
        /// Anroparen ansvarar för att sätta position (px, py) och namn efter skapandet.
        /// </summary>
        Creature Create(EnemyType type, IAssets assets);
    }
}
