#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Models.Items;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Hanterar spelarens itemlager — tar emot insamlade items och exponerar listan.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion
    ///
    /// MOTIVERING:
    /// Program.GiveItem() är ett publikt API som DynamicItem anropade via den
    /// statiska DynamicGameObject.Engine-referensen. Det gör att itemlogiken är
    /// hårt kopplad till Program som Gudobjekt. IItemSystem extraherar inventariets
    /// mutationer bakom ett testbart interface.
    ///
    /// Notering: ItemEnergi.OnInteract() returnerar false, vilket innebär att
    /// Collect() aldrig anropas i nuvarande kod. Interfacet är förberett för
    /// framtida itemtyper som returnerar true och läggs till i inventariet.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new ItemSystem(context).
    /// Program.GiveItem() delegerar till Collect().
    /// Injiceras via GameServices.Items för framtida state-åtkomst.
    /// </remarks>
    public interface IItemSystem
    {
        /// <summary>Spelarens insamlade items. Skrivskyddad vy mot inventariet.</summary>
        IReadOnlyList<Item> CollectedItems { get; }

        /// <summary>
        /// Lägger till ett item i spelarens inventarie.
        /// Anropas av Program.GiveItem(), som i sin tur nås via DynamicItem.OnInteract().
        /// </summary>
        void Collect(Item item);
    }
}
