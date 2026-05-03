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

        // Dödzon i tile-enheter: kameran rör sig inte förrän spelaren lämnar zonen.
        // Horisontell slack absorberar vänster/höger-vändning utan kameradarr.
        // Vertikal slack (halv) absorberar landningsjitter utan att dölja plattformar ovan.
        private const float DeadZoneX = 0.5f;
        private const float DeadZoneY = 0.25f;

        // Framåtblick: kameran tittar LookAheadAmount tiles framåt i rörelseriktningen.
        // LookAheadLerp styr hur snabbt offset-en easer in/ut — lägre = mjukare.
        //private const float LookAheadAmount = 1.5f;
        private const float LookAheadAmount = 0f;
        private const float LookAheadLerp   = 0.06f;

        private float _smoothX;
        private float _smoothY;
        // Kameramål efter dödzonskorrigering — lerp-basen glider mot detta värde.
        private float _cameraTargetX;
        private float _cameraTargetY;
        // Horisontell look-ahead-offset — eases mot ±LookAheadAmount eller 0.
        private float _lookAheadX;
        private float _prevTargetX;
        private bool  _initialized;

        /// <inheritdoc />
        public void SnapTo(float targetX, float targetY)
        {
            _cameraTargetX = targetX;
            _cameraTargetY = targetY;
            _smoothX       = targetX;
            _smoothY       = targetY;
            _lookAheadX    = 0f;
            _prevTargetX   = targetX;
            _initialized   = true;
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

            // Dödzon ("push"-modell): kameramålet skjuts undan bara när spelaren
            // lämnar zonen. Så länge spelaren rör sig inom zonen står kameran still.
            // Effekt: korta vändningsrörelser och landningsjitter syns inte i kameran.
            if (targetX < _cameraTargetX - DeadZoneX) _cameraTargetX = targetX + DeadZoneX;
            else if (targetX > _cameraTargetX + DeadZoneX) _cameraTargetX = targetX - DeadZoneX;
            if (targetY < _cameraTargetY - DeadZoneY) _cameraTargetY = targetY + DeadZoneY;
            else if (targetY > _cameraTargetY + DeadZoneY) _cameraTargetY = targetY - DeadZoneY;

            // Framåtblick: bestäm riktning från positionsförändring sedan förra frame.
            // Offset eases mot ±LookAheadAmount när spelaren rör sig, mot 0 när de stannar.
            float deltaX        = targetX - _prevTargetX;
            float lookAheadGoal = deltaX > 0.01f ? LookAheadAmount
                                : deltaX < -0.01f ? -LookAheadAmount
                                : 0f;
            float tLook  = 1f - MathF.Pow(1f - LookAheadLerp, elapsed * 60f);
            _lookAheadX += (lookAheadGoal - _lookAheadX) * tLook;
            _prevTargetX = targetX;

            // Exponentiell lerp mot dödzons- + look-ahead-korrigerat mål — frame-rate-oberoende.
            // Derivation: vid varje frame ska kameran täcka LerpFactor av återstående avstånd.
            // Vid variabel elapsed normaliseras mot 60 Hz via potensformeln nedan.
            float t = 1f - MathF.Pow(1f - LerpFactor, elapsed * 60f);
            _smoothX += (_cameraTargetX + _lookAheadX - _smoothX) * t;
            _smoothY += (_cameraTargetY - _smoothY) * t;
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
