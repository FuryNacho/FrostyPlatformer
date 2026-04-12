#nullable enable
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;

namespace FrostyPlatformer.Core
{
    /// <summary>
    /// Abstraherar åtkomst till speltillgångar (sprites, items, kartdata).
    /// Implementeras av Aggregate i produktion och FakeAssets i tester.
    /// </summary>
    public interface IAssets
    {
        /// <summary>
        /// Returnerar den fullständiga filsökvägen till en sprite med angivet namn.
        /// Returnerar null om ingen sprite med det namnet finns registrerad.
        /// </summary>
        string? GetSpritePath(string name);

        /// <summary>Hämtar ett item-objekt med angivet namn. Returnerar null om det inte finns.</summary>
        Item? GetItem(string name);

        /// <summary>Hämtar kartdata (LevelObj) med angivet namn. Returnerar null om den inte finns.</summary>
        LevelObj? GetMapData(string name);
    }
}
