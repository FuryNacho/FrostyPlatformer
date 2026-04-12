#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för IEnemyFactory — verifierar att EnemyFactory och
    /// FakeEnemyFactory uppfyller kontraktet korrekt.
    /// Ingen spelmotor, fil-I/O eller Aggregate krävs.
    /// </summary>
    [TestClass]
    public class EnemyFactoryTests
    {
        private readonly FakeAssets _assets = new FakeAssets();

        // ── EnemyFactory — rätt typ returneras ───────────────────────────────

        [TestMethod]
        public void Create_Penguin_ReturnsDynamicCreatureEnemyPenguin()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Penguin, _assets);
            Assert.IsInstanceOfType(enemy, typeof(DynamicCreatureEnemyPenguin));
        }

        [TestMethod]
        public void Create_Walrus_ReturnsDynamicCreatureEnemyWalrus()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Walrus, _assets);
            Assert.IsInstanceOfType(enemy, typeof(DynamicCreatureEnemyWalrus));
        }

        [TestMethod]
        public void Create_Frost_ReturnsDynamicCreatureEnemyFrost()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Frost, _assets);
            Assert.IsInstanceOfType(enemy, typeof(DynamicCreatureEnemyFrost));
        }

        [TestMethod]
        public void Create_Icicle_ReturnsDynamicCreatureEnemyIcicle()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Icicle, _assets);
            Assert.IsInstanceOfType(enemy, typeof(DynamicCreatureEnemyIcicle));
        }

        [TestMethod]
        public void Create_Boss_ReturnsDynamicCreatureEnemyBoss()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Boss, _assets);
            Assert.IsInstanceOfType(enemy, typeof(DynamicCreatureEnemyBoss));
        }

        [TestMethod]
        public void Create_Wind_ReturnsDynamicCreatureEnemyWind()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Wind, _assets);
            Assert.IsInstanceOfType(enemy, typeof(DynamicCreatureEnemyWind));
        }

        // ── EnemyFactory — okänd typ ─────────────────────────────────────────

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Create_UnknownType_ThrowsArgumentOutOfRangeException()
        {
            var factory = new EnemyFactory();
            factory.Create((EnemyType)999, _assets);
        }

        // ── EnemyFactory — varje anrop ger ny instans ────────────────────────

        [TestMethod]
        public void Create_CalledTwice_ReturnsDistinctInstances()
        {
            var factory = new EnemyFactory();
            var a = factory.Create(EnemyType.Walrus, _assets);
            var b = factory.Create(EnemyType.Walrus, _assets);
            Assert.AreNotSame(a, b);
        }

        // ── EnemyFactory — grundegenskaper är korrekt satta ─────────────────

        [TestMethod]
        public void Create_Penguin_HasCorrectFriendlyFlag()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Penguin, _assets);
            Assert.IsFalse(enemy.Friendly);
        }

        [TestMethod]
        public void Create_Icicle_IsIndestructible()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Icicle, _assets);
            Assert.IsTrue(enemy.IsIndestructible);
        }

        [TestMethod]
        public void Create_Wind_IsFriendly()
        {
            var factory = new EnemyFactory();
            var enemy = factory.Create(EnemyType.Wind, _assets);
            Assert.IsTrue(enemy.Friendly);
        }

        // ── FakeEnemyFactory — spårning ──────────────────────────────────────

        [TestMethod]
        public void FakeEnemyFactory_TracksCallCount()
        {
            var fake = new FakeEnemyFactory();
            fake.Create(EnemyType.Penguin, _assets);
            fake.Create(EnemyType.Walrus, _assets);
            Assert.AreEqual(2, fake.CreateCallCount);
        }

        [TestMethod]
        public void FakeEnemyFactory_TracksLastCreatedType()
        {
            var fake = new FakeEnemyFactory();
            fake.Create(EnemyType.Penguin, _assets);
            fake.Create(EnemyType.Boss, _assets);
            Assert.AreEqual(EnemyType.Boss, fake.LastCreatedType);
        }

        [TestMethod]
        public void FakeEnemyFactory_RecordsAllCreatedTypes()
        {
            var fake = new FakeEnemyFactory();
            fake.Create(EnemyType.Penguin, _assets);
            fake.Create(EnemyType.Walrus, _assets);
            fake.Create(EnemyType.Frost,  _assets);

            Assert.AreEqual(3, fake.CreatedTypes.Count);
            Assert.AreEqual(EnemyType.Penguin, fake.CreatedTypes[0]);
            Assert.AreEqual(EnemyType.Walrus,  fake.CreatedTypes[1]);
            Assert.AreEqual(EnemyType.Frost,   fake.CreatedTypes[2]);
        }

        [TestMethod]
        public void FakeEnemyFactory_WithoutPrototype_ReturnsFallbackInstance()
        {
            var fake = new FakeEnemyFactory();
            var enemy = fake.Create(EnemyType.Walrus, _assets);
            Assert.IsNotNull(enemy);
            Assert.IsInstanceOfType(enemy, typeof(DynamicCreatureEnemyWalrus));
        }
    }
}
