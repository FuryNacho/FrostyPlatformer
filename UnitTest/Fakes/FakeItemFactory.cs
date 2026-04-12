#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Systems;
using System.Collections.Generic;

namespace UnitTest.Fakes
{
    /// <summary>
    /// In-memory-implementering av IItemFactory för testning.
    /// Spårar vilka items som skapats och med vilka koordinater.
    /// </summary>
    public class FakeItemFactory : IItemFactory
    {
        // ── Statistik för verifiering ────────────────────────────────────────

        public int CreateCallCount { get; private set; }
        public ItemType? LastCreatedType { get; private set; }
        public List<(ItemType type, float x, float y, int id)> CreatedItems { get; }
            = new List<(ItemType, float, float, int)>();

        // ── IItemFactory ──────────────────────────────────────────────────────

        /// <summary>
        /// Skapar ett DynamicItem med null-item (ingen sprite-beroende).
        /// Koordinater och id spåras för verifiering i tester.
        /// </summary>
        public DynamicItem Create(ItemType type, float x, float y, IAssets assets, int collectable = 0, int id = 0)
        {
            CreateCallCount++;
            LastCreatedType = type;
            CreatedItems.Add((type, x, y, id));
            return new DynamicItem(x, y, null!, collectable, id);
        }
    }
}
