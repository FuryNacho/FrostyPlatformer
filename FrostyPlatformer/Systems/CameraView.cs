#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Immutabel datastruktur med resultatet av en kameraberäkning för en frame.
    /// Innehåller tile-offsettar, sub-tile-offsettar och antal synliga tiles.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Value Object (readonly record struct)
    ///
    /// MOTIVERING:
    /// CameraView är ett rent dataobjekt utan beteende. Readonly record struct ger
    /// värdesemantik och oföränderlighet — kameraberäkningen producerar ett nytt
    /// värde varje frame utan sidoeffekter.
    ///
    /// ANVÄNDNING:
    /// Returneras av ICameraSystem.Calculate() och konsumeras av states som behöver
    /// veta vad som ska renderas och var. Skickas vidare till TileMapRenderer.
    /// </remarks>
    public readonly record struct CameraView
    {
        /// <summary>Tile-koordinat för vänster kant av vyn (kan vara bråktal).</summary>
        public float OffsetX { get; init; }

        /// <summary>Tile-koordinat för övre kant av vyn (kan vara bråktal).</summary>
        public float OffsetY { get; init; }

        /// <summary>Sub-tile pixel-offset horisontellt för mjuk scrollning.</summary>
        public float TileOffsetX { get; init; }

        /// <summary>Sub-tile pixel-offset vertikalt för mjuk scrollning.</summary>
        public float TileOffsetY { get; init; }

        /// <summary>Antal tiles som ryms horisontellt på skärmen.</summary>
        public int VisibleTilesX { get; init; }

        /// <summary>Antal tiles som ryms vertikalt på skärmen.</summary>
        public int VisibleTilesY { get; init; }

        /// <summary>Tile-storlek i pixlar (horisontellt).</summary>
        public int TileWidth { get; init; }

        /// <summary>Tile-storlek i pixlar (vertikalt).</summary>
        public int TileHeight { get; init; }

        public CameraView(
            float offsetX, float offsetY,
            float tileOffsetX, float tileOffsetY,
            int visibleTilesX, int visibleTilesY,
            int tileWidth, int tileHeight)
        {
            OffsetX       = offsetX;
            OffsetY       = offsetY;
            TileOffsetX   = tileOffsetX;
            TileOffsetY   = tileOffsetY;
            VisibleTilesX = visibleTilesX;
            VisibleTilesY = visibleTilesY;
            TileWidth     = tileWidth;
            TileHeight    = tileHeight;
        }
    }
}
