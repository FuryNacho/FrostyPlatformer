#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för IItemFactory — verifierar att FakeItemFactory uppfyller
    /// kontraktet och att koordinater och ID:n spåras korrekt.
    /// Ingen spelmotor, fil-I/O eller Aggregate krävs.
    /// </summary>
    [TestClass]
    public class ItemFactoryTests
    {
        private readonly FakeAssets _assets = new FakeAssets();

        // ── FakeItemFactory — returnerar DynamicItem ─────────────────────────

        [TestMethod]
        public void Create_Energi_ReturnsDynamicItem()
        {
            var factory = new FakeItemFactory();
            var item = factory.Create(ItemType.Energi, 10f, 20f, _assets, 0, 1);
            Assert.IsInstanceOfType(item, typeof(DynamicItem));
        }

        [TestMethod]
        public void Create_SetsCorrectPosition()
        {
            var factory = new FakeItemFactory();
            var item = factory.Create(ItemType.Energi, 15f, 22f, _assets, 0, 5);
            Assert.AreEqual(15f, item.px);
            Assert.AreEqual(22f, item.py);
        }

        [TestMethod]
        public void Create_SetsCorrectCoinId()
        {
            var factory = new FakeItemFactory();
            var item = factory.Create(ItemType.Energi, 0f, 0f, _assets, 0, 42);
            Assert.AreEqual(42, item.CoinId);
        }

        // ── FakeItemFactory — spårning ────────────────────────────────────────

        [TestMethod]
        public void Create_TracksCallCount()
        {
            var factory = new FakeItemFactory();
            factory.Create(ItemType.Energi, 1f, 1f, _assets, 0, 1);
            factory.Create(ItemType.Energi, 2f, 2f, _assets, 0, 2);
            Assert.AreEqual(2, factory.CreateCallCount);
        }

        [TestMethod]
        public void Create_TracksLastCreatedType()
        {
            var factory = new FakeItemFactory();
            factory.Create(ItemType.Energi, 1f, 1f, _assets, 0, 1);
            Assert.AreEqual(ItemType.Energi, factory.LastCreatedType);
        }

        [TestMethod]
        public void Create_RecordsAllCreatedItems()
        {
            var factory = new FakeItemFactory();
            factory.Create(ItemType.Energi, 10f, 23f, _assets, 0, 1);
            factory.Create(ItemType.Energi, 25f, 23f, _assets, 0, 2);

            Assert.AreEqual(2, factory.CreatedItems.Count);
            Assert.AreEqual(10f, factory.CreatedItems[0].x);
            Assert.AreEqual(25f, factory.CreatedItems[1].x);
            Assert.AreEqual(1,   factory.CreatedItems[0].id);
            Assert.AreEqual(2,   factory.CreatedItems[1].id);
        }

        [TestMethod]
        public void Create_MultipleItems_EachHasUniqueId()
        {
            var factory = new FakeItemFactory();
            factory.Create(ItemType.Energi, 1f, 1f, _assets, 0, 10);
            factory.Create(ItemType.Energi, 2f, 2f, _assets, 0, 11);
            factory.Create(ItemType.Energi, 3f, 3f, _assets, 0, 12);

            Assert.AreEqual(10, factory.CreatedItems[0].id);
            Assert.AreEqual(11, factory.CreatedItems[1].id);
            Assert.AreEqual(12, factory.CreatedItems[2].id);
        }

        // ── FakeItemFactory — collectablevärde ───────────────────────────────

        [TestMethod]
        public void Create_WithCollectable_SetsCollectableOnItem()
        {
            var factory = new FakeItemFactory();
            var item = factory.Create(ItemType.Energi, 0f, 0f, _assets, collectable: 16, id: 1);
            Assert.AreEqual(16, item.Collectable);
        }

        [TestMethod]
        public void Create_WithZeroCollectable_ItemIsNotTempEnergi()
        {
            var factory = new FakeItemFactory();
            var item = factory.Create(ItemType.Energi, 0f, 0f, _assets, collectable: 0, id: 1);
            Assert.IsFalse(item.IsTempEnergi);
        }
    }
}
