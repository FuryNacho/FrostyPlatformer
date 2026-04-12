#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using Raylib_cs;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Engine.Raylib
{
    /// <summary>
    /// Raylib-implementationen av IRenderContext — den enda klassen i projektet
    /// som importerar Raylib_cs-namnrymden för rendering.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Structural)
    ///
    /// MOTIVERING:
    /// Spelkoden anropar IRenderContext utan att känna till Raylib. Den här klassen
    /// är den enda platsen där Raylib-specifika typer (Texture2D, Rectangle, Color)
    /// förekommer. Ett motorbyte till MonoGame kräver bara en ny implementation av
    /// IRenderContext — ingen spellogik behöver röras.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.cs (Composition Root). Sprites registreras via
    /// RegisterSprite() under initialisering. Raylib-fönstret initieras separat
    /// (i Program.cs) innan RaylibRenderContext skapas. Anroparen ansvarar för att
    /// omge varje frame med BeginDrawing()/EndDrawing(). UnloadAll() anropas vid
    /// programavslut för att frigöra GPU-minne.
    /// </remarks>
    public class RaylibRenderContext : IRenderContext
    {
        private readonly Dictionary<SpriteId, Texture2D> _textures = new();

        // Skalfaktorer: ett spellogiskt "pixel" = ScaleX × ScaleY faktiska pixlar.
        private readonly int _scaleX;
        private readonly int _scaleY;

        /// <summary>
        /// Skapar en ny renderkontext. Skalfaktorerna styr hur ett spel-pixel
        /// mappar till faktiska skärmpixlar (standard: 4×4 per spelpixel).
        /// </summary>
        public RaylibRenderContext(
            int scaleX = GameConstants.PixelWidth,
            int scaleY = GameConstants.PixelHeight)
        {
            _scaleX = scaleX;
            _scaleY = scaleY;
        }

        /// <summary>
        /// Registrerar en sprite under ett motor-agnostiskt ID.
        /// Laddar texturen från disk — anroparen skickar en filsökväg.
        /// Anropas från Program.cs (Composition Root) under initialisering
        /// och vid kartbyte (för SpriteId.MapTileSheet).
        /// </summary>
        public void RegisterSprite(SpriteId id, string filePath)
            => _textures[id] = LoadTextureFromPath(filePath);

        /// <summary>
        /// Frigör alla laddade GPU-texturer. Anropas vid programavslut.
        /// </summary>
        public void UnloadAll()
        {
            foreach (var tex in _textures.Values)
                Raylib_cs.Raylib.UnloadTexture(tex);
            _textures.Clear();
        }

        /// <inheritdoc />
        public int ScreenWidth  => GameConstants.ScreenWidth;

        /// <inheritdoc />
        public int ScreenHeight => GameConstants.ScreenHeight;

        /// <inheritdoc />
        public void Clear(RenderColor color)
            => Raylib_cs.Raylib.ClearBackground(ToColor(color));

        /// <inheritdoc />
        public void DrawSprite(SpriteId id, int x, int y)
        {
            var tex    = _textures[id];
            var source = new Rectangle(0, 0, tex.Width, tex.Height);
            var dest   = new Rectangle(x * _scaleX, y * _scaleY,
                                       tex.Width  * _scaleX,
                                       tex.Height * _scaleY);
            Raylib_cs.Raylib.DrawTexturePro(tex, source, dest, Vector2.Zero, 0f, Color.White);
        }

        /// <inheritdoc />
        public void DrawPartialSprite(SpriteId spriteSheet,
                                      int screenX, int screenY,
                                      int srcX,    int srcY,
                                      int width,   int height)
        {
            var tex    = _textures[spriteSheet];
            var source = new Rectangle(srcX,           srcY,           width,           height);
            var dest   = new Rectangle(screenX * _scaleX, screenY * _scaleY,
                                       width   * _scaleX, height  * _scaleY);
            Raylib_cs.Raylib.DrawTexturePro(tex, source, dest, Vector2.Zero, 0f, Color.White);
        }

        /// <inheritdoc />
        public void FillRect(int x, int y, int width, int height, RenderColor color)
            => Raylib_cs.Raylib.DrawRectangle(
                x * _scaleX, y * _scaleY,
                width * _scaleX, height * _scaleY,
                ToColor(color));

        /// <inheritdoc />
        public void DrawPixel(int x, int y, RenderColor color)
            => Raylib_cs.Raylib.DrawRectangle(
                x * _scaleX, y * _scaleY,
                _scaleX, _scaleY,
                ToColor(color));

        /// <inheritdoc />
        public void DrawLine(int x1, int y1, int x2, int y2, RenderColor color)
            => Raylib_cs.Raylib.DrawLine(
                x1 * _scaleX, y1 * _scaleY,
                x2 * _scaleX, y2 * _scaleY,
                ToColor(color));

        /// <inheritdoc />
        /// <remarks>
        /// Ritar text med spelets bitmappsteckensnitt (SpriteId.Font, 8×8 px per tecken).
        /// Implementeras som upprepade DrawPartialSprite-anrop — ett per tecken.
        /// Identisk logik som PixelEngineRenderContext.DrawText.
        /// </remarks>
        public void DrawText(string text, int x, int y)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int srcX = ((text[i] - 32) % 16) * 8;
                int srcY = ((text[i] - 32) / 16) * 8;
                DrawPartialSprite(SpriteId.Font, x + i * 8, y, srcX, srcY, 8, 8);
            }
        }

        // ── Hjälpmetoder ──────────────────────────────────────────────────────

        private static Color ToColor(RenderColor c)
            => new Color(c.R, c.G, c.B, c.A);

        /// <summary>
        /// Laddar en Raylib-textur från en filsökväg.
        /// Använder unsafe för att konvertera C#-strängen till den sbyte*
        /// som Raylib-cs P/Invoke-bindningen kräver.
        /// </summary>
        private static unsafe Texture2D LoadTextureFromPath(string filePath)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(filePath + '\0');
            fixed (byte* ptr = bytes)
                return Raylib_cs.Raylib.LoadTexture((sbyte*)ptr);
        }
    }
}
