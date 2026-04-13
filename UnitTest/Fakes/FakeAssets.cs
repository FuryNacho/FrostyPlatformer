#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Testubbning av IAssets. Returnerar null för alla tillgångar så att
    /// konstruktorer för fiender och kartobjekt kan anropas utan PixelEngine.
    /// </summary>
    public class FakeAssets : IAssets
    {
        public string?  GetSpritePath(string name) => null;
        public Item?    GetItem(string name)        => null;
        public LevelObj? GetMapData(string name)    => null;
    }
}
