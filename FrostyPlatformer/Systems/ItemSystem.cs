#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models.Items;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Implementerar IItemSystem med GameContext som lagringsbackend.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Stateful Service (med Blackboard som lagringsbackend)
    ///
    /// MOTIVERING:
    /// ItemSystem äger enbart mutationerna på inventariet (Collect) och
    /// exponerar en skrivskyddad vy. GameContext.CollectedItems är fortfarande
    /// tillgänglig för states som bara behöver läsa — t.ex. EndState
    /// och EnterHighScoreState som kontrollerar CollectedEnergiIds.Count.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new ItemSystem(_context) och lagras
    /// i _itemSystem-fältet. Program.GiveItem() delegerar till Collect().
    /// Injiceras i GameServices.Items för framtida state-åtkomst.
    /// </remarks>
    public class ItemSystem : IItemSystem
    {
        private readonly GameContext _context;

        /// <summary>
        /// Skapar en ItemSystem som läser och skriver till context.CollectedItems.
        /// </summary>
        public ItemSystem(GameContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Item> CollectedItems => _context.CollectedItems;

        /// <inheritdoc/>
        public void Collect(Item item) => _context.CollectedItems.Add(item);
    }
}
