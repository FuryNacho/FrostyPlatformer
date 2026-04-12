#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Immutabel datastruktur för ett enskilt tile-ritningsanrop.
    /// Anger var på skärmen och vilken del av spritesheetet som ska ritas.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Value Object (readonly record struct)
    ///
    /// MOTIVERING:
    /// Separerar beräkning (vilka tiles, var) från ritning (DrawPartialSprite).
    /// TileMapRenderer producerar en sekvens av TileDrawCall — anroparen ritar dem
    /// via IRenderContext. Inga PixelEngine-typer; koordinaterna är rena heltal
    /// enkla att kontrollera i enhetstester.
    ///
    /// ANVÄNDNING:
    /// Returneras av ITileMapRenderer.GetDrawCalls() och konsumeras av states
    /// som skickar varje anrop vidare till IRenderContext.DrawPartialSprite.
    /// </remarks>
    public readonly record struct TileDrawCall
    {
        /// <summary>Pixelposition X på skärmen.</summary>
        public int ScreenX { get; init; }

        /// <summary>Pixelposition Y på skärmen.</summary>
        public int ScreenY { get; init; }

        /// <summary>Käll-X i spritesheetet (pixlar).</summary>
        public int SpriteX { get; init; }

        /// <summary>Käll-Y i spritesheetet (pixlar).</summary>
        public int SpriteY { get; init; }

        /// <summary>Tile-bredd i pixlar.</summary>
        public int TileWidth { get; init; }

        /// <summary>Tile-höjd i pixlar.</summary>
        public int TileHeight { get; init; }

        public TileDrawCall(int screenX, int screenY, int spriteX, int spriteY, int tileWidth, int tileHeight)
        {
            ScreenX   = screenX;
            ScreenY   = screenY;
            SpriteX   = spriteX;
            SpriteY   = spriteY;
            TileWidth  = tileWidth;
            TileHeight = tileHeight;
        }
    }
}
