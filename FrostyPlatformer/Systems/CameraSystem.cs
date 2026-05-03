#nullable enable
using System;
using FrostyPlatformer.Global;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Beräknar kamerans position och scrollning för en tile-baserad värld.
    /// Stöder både direkt snap och mjuk lerp-följning via intern tillståndsmaskinen.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Stateful Service
    ///
    /// MOTIVERING:
    /// Den ursprungliga stateless Calculate-metoden gav en kamera som teleporterade
    /// till spelaren varje frame — visuellt korrekt men utan känsla av vikt eller
    /// trögrörlighet. Kameran skakade vid minsta rörelseimpuls (landning, kollision).
    ///
    /// Mjuk lerp-följning kräver att föregående kameraposition sparas mellan frames,
    /// vilket gör klassen stateful. Lerp-faktorn är exponentiell och frame-rate-oberoende:
    ///   t = 1 - (1 - LerpFactor)^(elapsed * 60)
    /// Formeln ger identiskt visuellt resultat vid 30, 60 eller 144 FPS.
    ///
    /// FLÖDE:
    ///   ChangeMap         → SnapTo(x, y)           — omedelbar snap, nollställer lerp-state
    ///   GameState.Update  → Advance(px, py, elapsed) — rör kameran mot spelaren
    ///   GameState.Draw    → GetView(...)             — hämtar mjuk kameravy för rendering
    ///
    /// ANVÄNDNING:
    /// Injiceras via ICameraSystem i GameServices. Calculate (stateless) används
    /// fortfarande av EditorState som styr kameran manuellt.
    /// </remarks>
    public class CameraSystem : ICameraSystem
    {
        // Hur snabbt kameran glider mot målet. 0 = aldrig, 1 = direkt snap.
        // Värden 0.10–0.20 ger mjuk plattforms-känsla; 0.12 är en bra startpunkt.
        private const float LerpFactor = 0.12f;

        private float _smoothX;
        private float _smoothY;
        private bool  _initialized;

        /// <inheritdoc />
        public void SnapTo(float targetX, float targetY)
        {
            _smoothX      = targetX;
            _smoothY      = targetY;
            _initialized  = true;
        }

        /// <inheritdoc />
        public void Advance(float targetX, float targetY, float elapsed)
        {
            if (!_initialized)
            {
                // Första anropet efter konstruktion eller SnapTo: snap direkt.
                SnapTo(targetX, targetY);
                return;
            }

            // Exponentiell lerp — frame-rate-oberoende.
            // Derivation: vid varje frame ska kameran täcka LerpFactor av återstående avstånd.
            // Vid variabel elapsed normaliseras mot 60 Hz via potensformeln nedan.
            float t = 1f - MathF.Pow(1f - LerpFactor, elapsed * 60f);
            _smoothX += (targetX - _smoothX) * t;
            _smoothY += (targetY - _smoothY) * t;
        }

        /// <inheritdoc />
        public CameraView GetView(int mapWidth, int mapHeight, int screenWidth, int screenHeight)
            => Calculate(_smoothX, _smoothY, mapWidth, mapHeight, screenWidth, screenHeight);

        /// <inheritdoc />
        public CameraView Calculate(
            float targetX, float targetY,
            int mapWidth, int mapHeight,
            int screenWidth, int screenHeight)
        {
            int tileWidth     = GameConstants.TileSize;
            int tileHeight    = GameConstants.TileSize;
            int visibleTilesX = screenWidth  / tileWidth;
            int visibleTilesY = screenHeight / tileHeight;

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
