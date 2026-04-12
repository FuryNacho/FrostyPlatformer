#nullable enable
namespace FrostyPlatformer.Models
{
    /// <summary>
    /// Minsta kontraktet en karta behöver uppfylla för att renderingssystemet
    /// ska kunna fråga efter tile-index och kartdimensioner.
    ///
    /// Gör det möjligt att testa TileMapRenderer med en fake utan att
    /// ladda filer eller ha en spelmotor igång.
    /// </summary>
    public interface IMapData
    {
        int Width  { get; }
        int Height { get; }

        /// <summary>
        /// Returnerar tile-indexet på position (x, y) i kartan.
        /// Returnerar 0 för koordinater utanför kartgränserna.
        /// </summary>
        int GetIndex(int x,