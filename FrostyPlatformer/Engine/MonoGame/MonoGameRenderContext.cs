#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;
using XnaColor     = Microsoft.Xna.Framework.Color;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;
using XnaVector2   = Microsoft.Xna.Framework.Vector2;

namespace FrostyPlatformer.Engine.MonoGame
{
    /// <summary>
    /// MonoGame-implementationen av IRenderContext — den enda klassen i projektet
    /// som importerar Microsoft.Xna.Framework.Graphics för rendering.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Structural)
    ///
    /// MOTIVERING:
    /// Spelkoden anropar IRenderContext utan att känna till MonoGame. Den här klassen
    /// är den enda platsen där MonoGame-specifika typer (Texture2D, SpriteBatch, Color)
    /// förekommer. Precis som RaylibRenderContext kapslar den in motordetaljer —
    /// ett eventuellt framtida motorbyte kräver bara en ny IRenderContext-implementation.
    ///
    /// ANVÄNDNING:
    /// Skapas i FrostyGame.LoadContent() (Composition Root) efter att SpriteBatch är
    /// initialiserad. Sprites registreras via RegisterSprite() under LoadContent.
    /// Anroparen ansvarar för att SpriteBatch.Begin()/End() omger varje frame
    /// (se FrostyGame.Draw). UnloadAll() anropas vid programavslut.
    ///
    /// KOORDINATSYSTEM:
    /// Spelets logiska koordinater räknas i "spellogiska pixlar" (256×224).
    /// PixelWidth/PixelHeight (standard 4×4) skalar upp till faktiska skärmpixlar.
    /// SamplerState.PointClamp i SpriteBatch.Begin() bevarar pixelart-skärpan.
    /// </remarks>
    public class MonoGameRenderContext : IRenderContext
    {
        private readonly GraphicsDevice                  _gd;
        private readonly SpriteBatch                     _batch;
        private readonly Dictionary<SpriteId, Texture2D> _textures = new();
        private readonly Texture2D                       _pixel;
        private readonly int                             _scaleX;
        private readonly int                             _scaleY;

        /// <summary>
        /// Skapar en ny renderkontext. Skapar också en intern 1×1-textur som
        /// används av FillRect, DrawPixel och DrawLine.
        /// </summary>
        /// <param name="gd">GraphicsDevice — krävs för att skapa och ladda texturer.</param>
        /// <param name="batch">SpriteBatch som ska vara aktiv (Begin/End) under alla anrop.</param>
        /// <param name="scaleX">Horisontell skalfaktor spelpixel → skärmpixel.</param>
        /// <param name="scaleY">Vertikal skalfaktor spelpixel → skärmpixel.</param>
        public MonoGameRenderContext(
            GraphicsDevice gd,
            SpriteBatch    batch,
            int scaleX = GameConstants.PixelWidth,
            int scaleY = GameConstants.PixelHeight)
        {
            _gd     = gd;
            _batch  = batch;
            _scaleX = scaleX;
            _scaleY = scaleY;

            // 1×1 vit textur — bas för FillRect, DrawPixel och DrawLine.
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { XnaColor.White });
        }

        /// <summary>
        /// Laddar en textur från disk och registrerar den under ett motor-agnostiskt ID.
        /// Anropas från FrostyGame.LoadContent() och vid kartbyte (MapTileSheet).
        /// </summary>
        public void RegisterSprite(SpriteId id, string filePath)
            => _textures[id] = Texture2D.FromFile(_gd, filePath);

        /// <summary>
        /// Frigör alla laddade GPU-texturer inklusive den interna 1×1-texturen.
        /// Anropas vid programavslut.
        /// </summary>
        public void UnloadAll()
        {
            foreach (var t in _textures.Values)
                t.Dispose();
            _textures.Clear();
            _pixel.Dispose();
        }

        /// <inheritdoc />
        /// <remarks>Läser från aktuell viewport — uppdateras automatiskt vid fönsterändring.</remarks>
        public int ScreenWidth  => _gd.Viewport.Width  / _scaleX;

        /// <inheritdoc />
        /// <remarks>Läser från aktuell viewport — uppdateras automatiskt vid fönsterändring.</remarks>
        public int ScreenHeight => _gd.Viewport.Height / _scaleY;

        /// <inheritdoc />
        /// <remarks>
        /// No-op: GraphicsDevice.Clear() måste anropas FÖRE SpriteBatch.Begin().
        /// FrostyGame.Draw() anropar GraphicsDevice.Clear(Black) innan SpriteBatch
        /// öppnas — states kan anropa detta fritt utan att bryta batchen.
        /// </remarks>
        public void Clear(RenderColor color)
        {
            // Avsiktligt tom — se remarks.
        }

        /// <inheritdoc />
        public void DrawSprite(SpriteId id, int x, int y)
        {
            var tex  = _textures[id];
            var dest = new XnaRectangle(
                x * _scaleX, y * _scaleY,
                tex.Width  * _scaleX,
                tex.Height * _scaleY);
            _batch.Draw(tex, dest, XnaColor.White);
        }

        /// <inheritdoc />
        public void DrawPartialSprite(SpriteId spriteSheet,
                                      int screenX, int screenY,
                                      int srcX,    int srcY,
                                      int width,   int height)
        {
            var tex    = _textures[spriteSheet];
            var source = new XnaRectangle(srcX, srcY, width, height);
            var dest   = new XnaRectangle(
                screenX * _scaleX, screenY * _scaleY,
                width   * _scaleX, height  * _scaleY);
            _batch.Draw(tex, dest, source, XnaColor.White);
        }

        /// <inheritdoc />
        public void DrawPartialSpriteFlippedX(SpriteId spriteSheet,
                                              int screenX, int screenY,
                                              int srcX,    int srcY,
                                              int width,   int height)
        {
            var tex    = _textures[spriteSheet];
            var source = new XnaRectangle(srcX, srcY, width, height);
            var dest   = new XnaRectangle(
                screenX * _scaleX, screenY * _scaleY,
                width   * _scaleX, height  * _scaleY);
            _batch.Draw(tex, dest, source, XnaColor.White, 0f, XnaVector2.Zero,
                        SpriteEffects.FlipHorizontally, 0f);
        }

        /// <inheritdoc />
        public void FillRect(int x, int y, int width, int height, RenderColor color)
            => _batch.Draw(_pixel,
                           new XnaRectangle(
                               x     * _scaleX, y      * _scaleY,
                               width * _scaleX, height * _scaleY),
                           ToXna(color));

        /// <inheritdoc />
        public void DrawPixel(int x, int y, RenderColor color)
            => _batch.Draw(_pixel,
                           new XnaRectangle(x * _scaleX, y * _scaleY, _scaleX, _scaleY),
                           ToXna(color));

        /// <inheritdoc />
        /// <remarks>
        /// Implementeras via SpriteBatch.Draw med rotation av en 1×1 textur.
        /// Tjockleken på linjen är _scaleY pixlar (en spelpixel hög).
        /// </remarks>
        public void DrawLine(int x1, int y1, int x2, int y2, RenderColor color)
        {
            var start = new XnaVector2(x1 * _scaleX, y1 * _scaleY);
            var end   = new XnaVector2(x2 * _scaleX, y2 * _scaleY);
            var edge  = end - start;
            float len = edge.Length();
            if (len < 0.5f) return;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            _batch.Draw(_pixel, start, null, ToXna(color),
                        angle, XnaVector2.Zero,
                        new XnaVector2(len, _scaleY),
                        SpriteEffects.None, 0f);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Ritar text med spelets bitmappsteckensnitt (SpriteId.Font, 8×8 px per tecken).
        /// Identisk logik som RaylibRenderContext.DrawText — teckensnittet täcker ASCII 32–127.
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

        // ── Hjälpmetod ────────────────────────────────────────────────────────

        private static XnaColor ToXna(RenderColor c)
            => new XnaColor(c.R, c.G, c.B, c.A);
    }
}
