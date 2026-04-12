#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Skapar DynamicItem-instanser utan att konsumenten refererar till konkreta itemklasser.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Factory Method
    ///
    /// MOTIVERING:
    /// Kartklassernas PopulateDynamics anropade Assets.GetItem("energi") direkt
    /// och skapade DynamicItem inline — ett hårdkodat beroende mot itemnamn som
    /// sträng och mot Assets-singletonen. IItemFactory kapslar in detta och gör
    /// det möjligt att lägga till nya itemtyper utan att ändra kartkod.
    ///
    /// ANVÄNDNING:
    /// Skapas i Aggregate.Load() som new ItemFactory() och injiceras i Map-konstruktorer.
    /// I tester används FakeItemFactory.
    /// </remarks>
    public interface IItemFactory
    {
        /// <summary>
        /// Skapar ett placerat DynamicItem av angiven typ vid koordinaterna (x, y).
        /// collectable styr animationsräknaren; id är spelets unika coin-ID.
        /// </summary>
        DynamicItem Create(ItemType type, float x, float y, IAssets assets, int collectable = 0, int id = 0);
    }
}
