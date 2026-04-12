#nullable enable
using System.Collections.Generic;
using PixelEngine;

namespace FrostyPlatformer.Rendering
{
    /// <summary>
    /// PixelEngine-implementationen av IRenderContext — den enda klassen i projektet
    /// som importerar PixelEngine-namnrymden och anropar spelmotorns ritmetoder.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Structural)
    ///
    /// MOTIVERING:
    /// Utan den här klassen är alla rita-anrop hårt kopplade till PixelEngine.Game
    /// i Program.cs och Creature-subklasser. PixelEngineRenderContext samlar hela
    /// motor-kopplingen på ett ställe. Vill man byta motor skrivs en ny IRenderContext-
    /// implementation (t.ex. MonoGameRenderContext) — spelkoden är oförändrad.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.cs (Composition Root) med en referens till
    /// Program-instansen (som ärver PixelEngine.Game). Populeras med sprites via
    /// RegisterSprite() under OnCreate(). MapTileSheet uppdateras vid kartbyte.
    /// Injiceras i DynamicGameObject.DrawSelf() och används direkt i Program.cs
    /// för HUD och övrig rendering.
    /// </remarks>
    public class PixelEngineRenderContext : IRenderContext
    {
        private readonly Game _game;
        private readonly Dictionary<SpriteId, Sprite> _sprites;

        /// <summary>
        /// Skapar en ny renderkontext kopplad till given PixelEngine.Game-instans.
        /// </summary>
        public PixelEngineRenderContext(Game game)
        {
            _game    = game;
            _sprites = new Dictionary<SpriteId, Sprite>();
        }

        /// <summary>
        /// Registrerar en sprite under ett motor-agnostiskt ID.
        /// Laddar sprite-filen från disk — anroparen skickar bara en filsökväg.
        /// Anropas från Program.cs (Composition Root) under initialisering och
        /// vid kartbyte (för SpriteId.MapTileSheet).
        /// </summary>
        public void RegisterSprite(SpriteId id, string filePath)
            => _sprites[id] = Sprite.Load(filePath);

        /// <inheritdoc />
        public int ScreenWidth => _game.ScreenWidth;

        /// <inheritdoc />
        public int ScreenHeight => _game.ScreenHeight;

        /// <inheritdoc />
        public void Clear(RenderColor color)
            => _game.Clear(ToPixel(color));

        /// <inheritdoc />
        public void DrawSprite(SpriteId id, int x, int y)
            => _game.DrawSprite(new Point(x, y), _sprites[id]);

        /// <inheritdoc />
        public void DrawPartialSprite(SpriteId spriteSheet, int screenX, int screenY,
                                      int srcX, int srcY, int width, int height)
            => _game.DrawPartialSprite(
                new Point(screenX, screenY),
                _sprites[spriteSheet],
                new Point(srcX, srcY),
                width, height);

        /// <inheritdoc />
        public void FillRect(int x, int y, int width, int height, RenderColor color)
            => _game.FillRect(new Point(x, y), width, height, ToPixel(color));

        /// <inheritdoc />
        public void DrawPixel(int x, int y, RenderColor color)
            => _game.Draw(new Point(x, y), ToPixel(color));

        /// <inheritdoc />
        public void DrawLine(int x1, int y1, int x2, int y2, RenderColor color)
            => _game.DrawLine(new Point(x1, y1), new Point(x2, y2), ToPixel(color));

        /// <summary>
        /// Ritar text med spelets bitmappsteckensnitt (SpriteId.Font, 8×8 px per tecken).
        /// Implementeras som upprepade DrawPartialSprite-anrop — ett per tecken i strängen.
        /// </summary>
        public void DrawText(string text, int x, int y)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int srcX = ((text[i] - 32) % 16) * 8;
                int srcY = ((text[i] - 32) / 16) * 8;
                DrawPartialSprite(SpriteId.Font, x + i * 8, y, srcX, srcY, 8, 8);
            }
        }

        private static Pixel ToPixel(RenderColor c)
            => new Pixel(c.R, c.G, c.B, c.A);
    }
}
