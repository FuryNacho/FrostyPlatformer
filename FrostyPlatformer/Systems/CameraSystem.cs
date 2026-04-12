#nullable enable
using FrostyPlatformer.Global;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Beräknar kamerans position och scrollning för en tile-baserad värld.
    /// Centrerar på målet och klämer mot kartgränser för att undvika tomrum utanför kartan.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: System (tillståndslös beräkningsklass)
    ///
    /// MOTIVERING:
    /// Kameralogiken låg inbäddad i DisplayStage och var kopplad till PixelEngine-typer.
    /// Extraheringen gör beräkningarna tillståndslösa och testbara utan spelmotor (SRP, DIP).
    ///
    /// ANVÄNDNING:
    /// Injiceras via ICameraSystem i GameServices och anropas från states som behöver
    /// kameravy. Tar kartdimensioner och målposition; returnerar en CameraView-record.
    /// </remarks>
    public class CameraSystem : ICameraSystem
    {
        /// <inheritdoc />
        public CameraView Calculate(
            float targetX, float targetY,
            int mapWidth, int mapHeight,
            int screenWidth, int screenHeight)
        {
            int tileWidth  = GameConstants.TileSize;
            int tileHeight = GameConstants.TileSize;
            int visibleTilesX = screenWidth  / tileWidth;
            int visibleTilesY = screenHeight / tileHeight;

            // Centrera kameran på målet
            float offsetX = targetX - visibleTilesX / 2.0f;
            float offsetY = targetY - visibleTilesY / 2.0f;

            // Kläm mot kartgränser så att inga tomrum visas.
            // maxOffset sätts aldrig negativt — om kartan är smalare än skärmen
            // stannar kameran vid 0 (kartan renderas från sin vänster/övre kant).
            float maxOffsetX = mapWidth  - visibleTilesX > 0 ? mapWidth  - visibleTilesX : 0;
            float maxOffsetY = mapHeight - visibleTilesY > 0 ? mapHeight - visibleTilesY : 0;
            if (offsetX < 0) offsetX = 0;
            if (offsetY < 0) offsetY = 0;
            if (offsetX > maxOffsetX) offsetX = maxOffsetX;
            if (offsetY > maxOffsetY) offsetY = maxOffsetY;

            // Sub-tile offset för mjuk scrollning
            float tileOffsetX = (offsetX - (int)offsetX) * tileWidth;
            float tileOffsetY = (offsetY - (int)offsetY) * tileHeight;

            return new CameraView(
                offsetX, offsetY,
                tileOffsetX, tileOffsetY,
                visibleTilesX, visibleTilesY,
                tileWidth, tileHeight);
        }
    }
}
