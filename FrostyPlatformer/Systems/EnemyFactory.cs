#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using System;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Skapar konkreta fiendeinstanser utifrån EnemyType-enumet.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Factory Method
    ///
    /// MOTIVERING:
    /// Centraliserar all new-logik för fiender på ett ställe. Ny fiendetyp =
    /// lägg till ett case i switch-uttrycket här — ingen annan kod behöver ändras.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Aggregate.Load() och skickas till alla Map-konstruktorer.
    /// Anropas från PopulateDynamics i respektive kartklass.
    /// </remarks>
    public class EnemyFactory : IEnemyFactory
    {
        /// <summary>
        /// Skapar rätt Creature-subklass för angiven EnemyType.
        /// Kastar ArgumentOutOfRangeException för okänd typ.
        /// </summary>
        public Creature Create(EnemyType type, IAssets assets) => type switch
        {
            EnemyType.Penguin => new DynamicCreatureEnemyPenguin(),
            EnemyType.Walrus  => new DynamicCreatureEnemyWalrus(),
            EnemyType.Frost   => new DynamicCreatureEnemyFrost(),
            EnemyType.Icicle  => new DynamicCreatureEnemyIcicle(),
            EnemyType.Boss    => new DynamicCreatureEnemyBoss(),
            EnemyType.Wind    => new DynamicCreatureEnemyWind(),
            _                 => throw new ArgumentOutOfRangeException(nameof(type), type, $"Okänd fiendetyp: {type}")
        };
    }
}
