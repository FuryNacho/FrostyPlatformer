#nullable enable
using System;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Statiska hjälpberäkningar för level editorn — konverterar mellan
    /// skärmpixlar och tile-koordinater med hänsyn till kameraoffset.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Utility (stateless pure functions)
    ///
    /// MOTIVERING:
    /// Tile-koordinatberäkningen är inte trivial när kameran har ett sub-tile
    /// scrolloffset. Att lyfta ut den till en separat klass med statiska metoder
    /// gör den enkelt testbar utan att instansiera EditorState (SRP). Andra
    /// editor-system (E2–E4) kan återanvända samma beräkningar utan att duplicera
    /// logiken.
    ///
    /// ANVÄNDNING:
    /// Anropas från EditorState.Update() för att beräkna vilken tile musen
    /// pekar på och var markörens skärmposition ska ritas. Testas i EditorMathTests.
    /// </remarks>
    internal static class EditorMath
    {
        /// <summary>
        /// Konverterar en skärmpixelposition till kartans tile-koordinater.
        /// Tar hänsyn till kamerans heltal- och sub-tile-offset.
        /// </summary>
        /// <param name="mouseX">Musens X-position i skärmpixlar.</param>
        /// <param name="mouseY">Musens Y-position i skärmpixlar.</param>
        /// <param name="cam">Kameravyn för aktuell frame.</param>
        /// <returns>Tile-koordinaten (tileX, tileY) i kartan som musen pekar på.</returns>
        public static (int tileX, int tileY) ScreenToTile(int mouseX, int mouseY, CameraView cam)
        {
            int tileX = (int)Math.Floor((double)mouseX / cam.TileWidth  + cam.OffsetX);
            int tileY = (int)Math.Floor((double)mouseY / cam.TileHeight + cam.OffsetY);
            return (tileX, tileY);
        }

        /// <summary>
        /// Returnerar skärmkoordinaten för en tiles övre vänstra hörn.
        /// Används för att rita markören exakt över rätt tile.
        /// </summary>
        /// <param name="tileX">Tile-koordinat X i kartan.</param>
        /// <param name="tileY">Tile-koordinat Y i kartan.</param>
        /// <param name="cam">Kameravyn för aktuell frame.</param>
        /// <returns>Skärmposition (screenX, screenY) för tilens övre vänstra hörn.</returns>
        public static (int screenX, int screenY) TileToScreen(int tileX, int tileY, CameraView cam)
        {
            int x       = tileX - (int)cam.OffsetX;
            int y       = tileY - (int)cam.OffsetY;
            int screenX = (int)(x * cam.TileWidth  - cam.TileOffsetX);
            int screenY = (int)(y * cam.TileHeight - cam.TileOffsetY);
            return (screenX, screenY);
        }

        /// <summary>
        /// Beräknar editorns delade markörposition för nästa frame.
        /// Mus och analogspak är alltid aktiva samtidigt — "senast rörda enhet vinner":
        /// rör spelaren musen snäpper markören dit (absolut), annars flyttar
        /// analogspaken markören inkrementellt från sin nuvarande position.
        /// Resultatet klampas till skärmen.
        /// </summary>
        /// <param name="prevX">Markörens X föregående frame (skärmpixlar).</param>
        /// <param name="prevY">Markörens Y föregående frame (skärmpixlar).</param>
        /// <param name="mouseMoved">True om musen rördes/klickades denna frame.</param>
        /// <param name="mouseX">Musens X (skärmpixlar).</param>
        /// <param name="mouseY">Musens Y (skärmpixlar).</param>
        /// <param name="stickX">Vänster analogspak X, dödzonad (−1..1).</param>
        /// <param name="stickY">Vänster analogspak Y, dödzonad (−1..1, positiv = uppåt).</param>
        /// <param name="speed">Markörfart i pixlar/sekund vid full spakutslag.</param>
        /// <param name="dt">Tid sedan förra framen (sekunder).</param>
        /// <param name="maxX">Största tillåtna X (skärmbredd − 1).</param>
        /// <param name="maxY">Största tillåtna Y (skärmhöjd − 1).</param>
        /// <returns>Markörens nya (x, y) i skärmpixlar.</returns>
        public static (float x, float y) UpdateCursor(
            float prevX, float prevY,
            bool mouseMoved, int mouseX, int mouseY,
            float stickX, float stickY, float speed, float dt,
            int maxX, int maxY)
        {
            if (mouseMoved)
                return (Clamp(mouseX, maxX), Clamp(mouseY, maxY));

            if (stickX != 0f || stickY != 0f)
            {
                // Skärm-Y växer nedåt; spak-Y positiv = uppåt → subtrahera.
                float nx = Clamp(prevX + stickX * speed * dt, maxX);
                float ny = Clamp(prevY - stickY * speed * dt, maxY);
                return (nx, ny);
            }

            return (prevX, prevY);
        }

        private static float Clamp(float v, int max) => v < 0f ? 0f : (v > max ? max : v);
    }
}
