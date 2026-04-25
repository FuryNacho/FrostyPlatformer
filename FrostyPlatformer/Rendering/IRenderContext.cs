namespace FrostyPlatformer.Rendering
{
    /// <summary>
    /// Abstraktionslager för all renderingsoutput — kapslar in spelmotorns
    /// ritmetoder bakom ett motor-agnostiskt kontrakt.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Structural)
    ///
    /// MOTIVERING:
    /// Spelet anropade tidigare DrawPartialSprite, FillRect, DrawLine m.fl. direkt
    /// på PixelEngine.Game-klassen som Program ärver från. Det innebär att all
    /// spelkod är hårt kopplad till PixelEngine — ett motorbyte kräver ändringar
    /// i hundratals anrop utspridda i Program.cs och Creature-subklasser.
    /// Med IRenderContext koncentreras alla motoranrop till en enda klass:
    /// PixelEngineRenderContext. Vill man byta motor skrivs en ny implementation
    /// av det här interfacet — ingen spellogik behöver röras.
    ///
    /// ANVÄNDNING:
    /// Injiceras i DynamicGameObject.DrawSelf() för att rita spelkaraktärer.
    /// Används direkt i Program.cs för HUD, bakgrund och text.
    /// PixelEngineRenderContext skapas en gång i Program.cs (Composition Root)
    /// och populeras med sprites via RegisterSprite() under OnCreate().
    /// FakeRenderContext används i enhetstester för att testa DrawSelf() utan
    /// att ett spelfönster öppnas.
    /// </remarks>
    public interface IRenderContext
    {
        /// <summary>Skärmens bredd i pixlar.</summary>
        int ScreenWidth { get; }

        /// <summary>
        /// Laddar en bildfil och kopplar den till ett sprite-ID.
        /// Anropas vid initialisering och vid kartbyte (MapTileSheet).
        /// </summary>
        void RegisterSprite(SpriteId id, string filePath);

        /// <summary>Skärmens höjd i pixlar.</summary>
        int ScreenHeight { get; }

        /// <summary>
        /// Fyller hela skärmen med angiven färg.
        /// Anropas i början av varje frame för att rensa föregående frames innehåll.
        /// </summary>
        void Clear(RenderColor color);

        /// <summary>
        /// Ritar en hel sprite med övre vänstra hörnet i (x, y).
        /// Används för fullskärmsbilder som splash-skärmar.
        /// </summary>
        void DrawSprite(SpriteId id, int x, int y);

        /// <summary>
        /// Ritar ett urklipp ur ett sprite-ark på skärmpositionen (screenX, screenY).
        /// Källregionen definieras av (srcX, srcY, width, height) i sprite-arket.
        /// Används för tiles, karaktärer och föremål.
        /// </summary>
        void DrawPartialSprite(SpriteId spriteSheet, int screenX, int screenY,
                               int srcX, int srcY, int width, int height);

        /// <summary>
        /// Fyller en rektangel på skärmen med angiven färg.
        /// Används för HUD-bakgrunder och färgfält.
        /// </summary>
        void FillRect(int x, int y, int width, int height, RenderColor color);

        /// <summary>
        /// Ritar en enstaka pixel. Används för partikeleffekter (t.ex. teleportflimmer).
        /// </summary>
        void DrawPixel(int x, int y, RenderColor color);

        /// <summary>
        /// Ritar en linje mellan (x1, y1) och (x2, y2).
        /// Används för energimätaren i HUD.
        /// </summary>
        void DrawLine(int x1, int y1, int x2, int y2, RenderColor color);

        /// <summary>
        /// Ritar text med spelets bitmappsteckensnitt (SpriteId.Font).
        /// Varje tecken är 8×8 pixlar. Teckensnittet täcker ASCII 32–127.
        /// </summary>
        void DrawText(string text, int x, int y);
    }
}
