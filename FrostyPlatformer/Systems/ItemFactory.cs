#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;
using System;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Skapar DynamicItem-instanser utifrån ItemType-enumet och assets.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Factory Method
    ///
    /// MOTIVERING:
    /// Centraliserar skapandet av spelbara items. Ny itemtyp = nytt case här,
    /// inga ändringar i kartkod. Tar bort hårdkodade strängnycklar ("energi")
    /// ur kartklasserna.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Aggregate.Load() och injiceras i Map-konstruktorer.
    /// </remarks>
    public class ItemFactory : IItemFactory
    {
        /// <summary>
        /// Skapar rätt DynamicItem-subtyp för angiven ItemType.
        /// Kastar ArgumentOutOfRangeException för okänd typ.
        /// </summary>
        public DynamicItem Create(ItemType type, float x, float y, IAssets assets, int collectable = 0, int id = 0) =>
            type switch
            {
                ItemType.Energi => new DynamicItem(x, y, assets.GetItem(ItemRef.Energi)!, collectable, id),
                _               => throw new ArgumentOutOfRangeException(nameof(type), type, $"Okänd itemtyp: {type}")
            };
    }
}
