#nullable enable
using System;

namespace FrostyPlatformer.Rendering
{
    /// <summary>
    /// Motor-agnostisk färgrepresentation för renderingsgränssnittet.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Value Object (del av Adapter-mönstret kring IRenderContext)
    ///
    /// MOTIVERING:
    /// PixelEngine använder sin egna <c>Pixel</c>-typ med RGBA-komponenter.
    /// Spelkoden ska inte känna till den typen — den ska uttrycka färger i
    /// motor-agnostiska termer. RenderColor är den termer spelkoden pratar i;
    /// PixelEngineRenderContext konverterar till Pixel internt.
    ///
    /// ANVÄNDNING:
    /// Används som parameter i IRenderContext-metoder. Skapa via konstruktorn
    /// eller via de fördefinierade statiska konstanterna (RenderColor.Black etc).
    /// RenderColor.Random() används för visuella effekter (t.ex. teleportflimmer).
    /// </remarks>
    public readonly struct RenderColor
    {
        /// <summary>Röd kanal, 0–255.</summary>
        public byte R { get; }

        /// <summary>Grön kanal, 0–255.</summary>
        public byte G { get; }

        /// <summary>Blå kanal, 0–255.</summary>
        public byte B { get; }

        /// <summary>Alfakanal, 0–255. 255 = helt ogenomskinlig.</summary>
        public byte A { get; }

        /// <summary>Skapar en färg med angiven RGB och full opacitet.</summary>
        public RenderColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r; G = g; B = b; A = a;
        }

        // ── Statiska presets (RGB-värden hämtade från PixelEngine.Pixel.Presets) ──

        /// <summary>R=0 G=0 B=0</summary>
        public static readonly RenderColor Black = new RenderColor(0, 0, 0);

        /// <summary>R=255 G=255 B=255</summary>
        public static readonly RenderColor White = new RenderColor(255, 255, 255);

        /// <summary>R=0 G=255 B=0</summary>
        public static readonly RenderColor Green = new RenderColor(0, 255, 0);

        /// <summary>R=0 G=139 B=0</summary>
        public static readonly RenderColor DarkGreen = new RenderColor(0, 139, 0);

        /// <summary>R=139 G=0 B=0</summary>
        public static readonly RenderColor DarkRed = new RenderColor(139, 0, 0);

        /// <summary>R=255 G=255 B=0</summary>
        public static readonly RenderColor Yellow = new RenderColor(255, 255, 0);

        /// <summary>R=154 G=99 B=36</summary>
        public static readonly RenderColor Brown = new RenderColor(154, 99, 36);

        /// <summary>R=255 G=0 B=0</summary>
        public static readonly RenderColor Red = new RenderColor(255, 0, 0);

        /// <summary>R=0 G=0 B=255</summary>
        public static readonly RenderColor Blue = new RenderColor(0, 0, 255);

        /// <summary>R=64 G=64 B=64</summary>
        public static readonly RenderColor DarkGrey = new RenderColor(64, 64, 64);

        /// <summary>
        /// Skapar en slumpmässig färg — används för visuella effekter som teleportflimmer.
        /// </summary>
        public static RenderColor Random()
        {
            var rng = new Random();
            return new RenderColor(
                (byte)rng.Next(256),
                (byte)rng.Next(256),
                (byte)rng.Next(256));
        }
    }
}
