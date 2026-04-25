#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Rendering;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Testimplementation av IRenderContext som registrerar anrop utan att rita något.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Test Double (Spy) — del av Adapter-teststrategin
    ///
    /// MOTIVERING:
    /// Enhetstester för DrawSelf() och andra ritmetoder ska kunna köras utan
    /// att ett spelfönster öppnas. FakeRenderContext tar emot anrop, lagrar dem
    /// i DrawnSprites / DrawnTexts etc. och låter testerna verifiera att rätt
    /// sprites ritades med rätt koordinater — utan PixelEngine-beroende.
    ///
    /// ANVÄNDNING:
    /// Instantieras i testklass, injiceras i DrawSelf(). Kontrollera
    /// DrawnSprites.Count för att verifiera att rita-anrop gjordes.
    /// Konstruktorn sätter ScreenWidth/ScreenHeight till testlämpliga värden (256×240).
    /// </remarks>
    public class FakeRenderContext : IRenderContext
    {
        /// <summary>Registrerade DrawPartialSprite-anrop.</summary>
        public List<(SpriteId Sheet, int Sx, int Sy, int SrcX, int SrcY, int W, int H)> DrawnSprites { get; }
            = new List<(SpriteId, int, int, int, int, int, int)>();

        /// <summary>Registrerade DrawText-anrop.</summary>
        public List<(string Text, int X, int Y)> DrawnTexts { get; }
            = new List<(string, int, int)>();

        /// <summary>Antal gånger Clear() anropades.</summary>
        public int ClearCount { get; private set; }

        /// <summary>Registrerade RegisterSprite-anrop.</summary>
        public List<(SpriteId Id, string Path)> RegisteredSprites { get; }
            = new List<(SpriteId, string)>();

        /// <inheritdoc />
        public void RegisterSprite(SpriteId id, string filePath)
            => RegisteredSprites.Add((id, filePath));

        /// <inheritdoc />
        public int ScreenWidth { get; } = 256;

        /// <inheritdoc />
        public int ScreenHeight { get; } = 240;

        /// <inheritdoc />
        public void Clear(RenderColor color) => ClearCount++;

        /// <inheritdoc />
        public void DrawSprite(SpriteId id, int x, int y)
            => DrawnSprites.Add((id, x, y, 0, 0, 0, 0));

        /// <inheritdoc />
        public void DrawPartialSprite(SpriteId spriteSheet, int screenX, int screenY,
                                      int srcX, int srcY, int width, int height)
            => DrawnSprites.Add((spriteSheet, screenX, screenY, srcX, srcY, width, height));

        /// <inheritdoc />
        public void FillRect(int x, int y, int width, int height, RenderColor color) { }

        /// <inheritdoc />
        public void DrawPixel(int x, int y, RenderColor color) { }

        /// <inheritdoc />
        public void DrawLine(int x1, int y1, int x2, int y2, RenderColor color) { }

        /// <inheritdoc />
        public void DrawText(string text, int x, int y)
            => DrawnTexts.Add((text, x, y));
    }
}
