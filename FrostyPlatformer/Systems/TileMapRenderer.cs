#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Beräknar vilka tiles som är synliga och var de ska ritas för en given kameravy.
    /// Returnerar rena TileDrawCall-värden utan PixelEngine-beroende.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: System
    ///
    /// MOTIVERING:
    /// Tile-ritlogiken låg i DisplayStage och var direkt kopplad till PixelEngine.
    /// Extraheringen gör beräkningarna motorfria och enkla att testa (SRP, DIP).
    /// yield return ger lazy evaluering — tiles beräknas bara när anroparen konsumerar dem.
    ///
    /// ANVÄNDNING:
    /// Injiceras via ITileMapRenderer i GameServices. States itererar över GetDrawCalls()
    /// och skickar varje TileDrawCall till IRenderContext.DrawPartialSprite.
    /// </remarks>
    public class TileMapRenderer : ITileMapRenderer
    {
        /// <inheritdoc />
        public IEnumerable<TileDrawCall> GetDrawCalls(CameraView cam, IMapData map)
        {
            for (int x = -1; x < cam.VisibleTilesX + 1; x++)
            {
                for (int y = -1; y < cam.VisibleTilesY + 1; y++)
                {
                    int idx = map.GetIndex((int)(x + cam.OffsetX), (int)(y + cam.OffsetY));

                    int sx = idx % GameConstants.TileSheetColumns;
                    int sy = idx / GameConstants.TileSheetColumns;

                    int screenX = (int)(x * cam.TileWidth  - cam.TileOffsetX);
                    int screenY = (int)(y * cam.TileHeight - cam.TileOffsetY);
                    int spriteX = sx * cam.TileWidth;
                    int spriteY = sy * cam.TileHeight;

                    yield return new TileDrawCall(screenX, screenY, spriteX, spriteY, cam.TileWidth, cam.TileHeight);
                }
            }
        }
    }
}
