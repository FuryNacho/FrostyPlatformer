#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IItemSystem. Spårar Collect-anrop utan att röra GameContext.
    /// </summary>
    public class FakeItemSystem : IItemSystem
    {
        private readonly List<Item> _items = new List<Item>();

        public IReadOnlyList<Item> CollectedItems => _items;

        /// <summary>Antal gånger Collect() anropades.</summary>
        public int CollectCount { get; private set; }

        public void Collect(Item item)
        {
            _items.Add(item);
            CollectCount++;
        }
    }
}
