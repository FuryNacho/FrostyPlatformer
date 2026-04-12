#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;
using System.Collections.Generic;

namespace UnitTest.Fakes
{
    /// <summary>
    /// In-memory-implementering av IEnemyFactory för testning.
    /// Returnerar förkonfigurerade fiendeobjekt och spårar vilka typer som skapats.
    /// </summary>
    public class FakeEnemyFactory : IEnemyFactory
    {
        private readonly Dictionary<EnemyType, Creature> _prototypes;

        // ── Statistik för verifiering ────────────────────────────────────────

        public int CreateCallCount { get; private set; }
        public EnemyType? LastCreatedType { get; private set; }
        public List<EnemyType> CreatedTypes { get; } = new List<EnemyType>();

        // ── Konstruktorer ─────────────────────────────────────────────────────

        /// <summary>
        /// Skapar en fake som returnerar nya instanser av riktiga fiendeklasser.
        /// Används när testet bara bryr sig om typ, inte beteende.
        /// </summary>
        public FakeEnemyFactory()
        {
            _prototypes = new Dictionary<EnemyType, Creature>();
        }

        /// <summary>
        /// Skapar en fake med förkonfigurerade prototyper per typ.
        /// Testet kontrollerar exakt vilka objekt som returneras.
        /// </summary>
        public FakeEnemyFactory(Dictionary<EnemyType, Creature> prototypes)
        {
            _prototypes = prototypes;
        }

        // ── IEnemyFactory ─────────────────────────────────────────────────────

        /// <summary>
        /// Returnerar prototypen för angiven typ, eller en ny standardinstans
        /// om ingen prototyp konfigurerats.
        /// </summary>
        public Creature Create(EnemyType type, IAssets assets)
        {
            CreateCallCount++;
            LastCreatedType = type;
            CreatedTypes.Add(type);

            if (_prototypes.TryGetValue(type, out var prototype))
                return prototype;

            // Fallback: skapa riktig instans för enkla typkontroller
            return type switch
            {
                EnemyType.Penguin => new DynamicCreatureEnemyPenguin(),
                EnemyType.Walrus  => new DynamicCreatureEnemyWalrus(),
                EnemyType.Frost   => new DynamicCreatureEnemyFrost(),
                EnemyType.Icicle  => new DynamicCreatureEnemyIcicle(),
                EnemyType.Boss    => new DynamicCreatureEnemyBoss(),
                EnemyType.Wind    => new DynamicCreatureEnemyWind(),
                _                 => new DynamicCreatureEnemyPenguin()
            };
        }
    }
}
