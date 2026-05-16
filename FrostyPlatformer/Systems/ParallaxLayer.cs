#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Representerar ett enda parallax-bakgrundslager med horisontell tiling och scroll-faktor.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Value Object med Draw-beteende
    ///
    /// MOTIVERING:
    /// Varje lager definieras av tre oföränderliga värden: sprite-identitet, scroll-faktor
    /// och bilddimensioner. Ritlogiken är helt fristående från kamerasystemet — lagret
    /// tar bara emot kamerans X-offset och räknar ut sin position självständigt.
    ///
    /// TILING:
    /// Bakgrundsbilden är 512 spelpixlar bred (2× skärmbredden 256 px), vilket garanterar
    /// att skärmen alltid täcks med max två DrawPartialSprite-anrop oavsett scroll-position.
    ///
    ///   [────────────── bild 512 px ──────────────]
    ///   [wrapped→|←del 1→|][←del 2→|←osynlig del→]
    ///   [─────── skärm 256 px ────────────────────]
    ///
    /// TRANSPARENS:
    /// Midground-bilder har alpha-kanal (PNG RGBA). SpriteBatch körs med
    /// BlendState.AlphaBlend i Program.cs — genomskinliga pixlar compositar korrekt
    /// mot det underliggande himmellagret utan extra kod här.
    ///
    /// ANVÄNDNING:
    /// Skapas av ParallaxSystem.BuildLayerMap(). Draw() anropas av ParallaxSystem.Draw()
    /// en gång per frame, efter FillRect och innan TileMapRenderer.
    /// </remarks>
    public sealed class ParallaxLayer
    {
        /// <summary>Sprite-arket som representerar detta bakgrundslager.</summary>
        public SpriteId SpriteId { get; }

        /// <summary>
        /// Scroll-faktor relativt kamerans rörelse. Intervall [0, 1].
        /// 0 = helt statiskt lager, 1 = följer kameran exakt (som tiles).
        /// Rekommenderade värden: 0.10 (himmel/bakre lager), 0.30 (mellanskikt).
        /// </summary>
        public float ScrollFactor { get; }

        /// <summary>Bildens bredd i spelpixlar — används för tiling-beräkning.</summary>
        public int ImageWidth { get; }

        /// <summary>Bildens höjd i spelpixlar.</summary>
        public int ImageHeight { get; }

        /// <summary>
        /// Skapar ett nytt parallax-lager.
        /// </summary>
        /// <param name="spriteId">Sprite-ID för bakgrundsbilden.</param>
        /// <param name="scrollFactor">Rörelsefaktor 0–1 relativt kamerans X-offset.</param>
        /// <param name="imageWidth">Bildens bredd i spelpixlar. Standard: 512 (2× skärmbredd).</param>
        /// <param name="imageHeight">Bildens höjd i spelpixlar. Standard: 224 (full skärmhöjd).</param>
        public ParallaxLayer(SpriteId spriteId, float scrollFactor,
                             int imageWidth = 512, int imageHeight = 224)
        {
            SpriteId     = spriteId;
            ScrollFactor = scrollFactor;
            ImageWidth   = imageWidth;
            ImageHeight  = imageHeight;
        }

        /// <summary>
        /// Ritar lagret med horisontell tiling baserat på kamerans X-offset.
        /// </summary>
        /// <param name="rc">Motor-agnostisk renderkontext.</param>
        /// <param name="cameraOffsetX">
        /// Kamerans X-offset i tile-enheter (CameraView.OffsetX).
        /// Konverteras till spelpixlar via TileSize (16 px/tile).
        /// </param>
        /// <param name="screenWidth">Skärmens bredd i spelpixlar.</param>
        /// <param name="screenHeight">Skärmens höjd i spelpixlar.</param>
        public void Draw(IRenderContext rc, float cameraOffsetX, int screenWidth, int screenHeight)
        {
            // Omvandla tile-offset till spelpixlar och applicera scroll-faktorn.
            // ScrollFactor < 1 → lagret rör sig långsammare än kameran → djupkänsla.
            float scrollPx = cameraOffsetX * GameConstants.TileSize * ScrollFactor;

            // Wrappa scroll-pixlar inom [0, ImageWidth) för sömlös tiling.
            // C#:s modulo returnerar negativt värde för negativa operander —
            // addition av ImageWidth korrigerar till positivt intervall.
            int wrapped = (int)(scrollPx % ImageWidth);
            if (wrapped < 0) wrapped += ImageWidth;

            // Del 1: från wrapped-position till bildens högerkant, ritas vid screenX = 0.
            // Om ImageWidth - wrapped >= screenWidth täcks hela skärmen av ett anrop.
            int firstW = Math.Min(ImageWidth - wrapped, screenWidth);
            rc.DrawPartialSprite(SpriteId, 0, 0, wrapped, 0, firstW, screenHeight);

            // Del 2: om del 1 inte täckte full skärmbredd ritas resten från bildens start.
            // Detta inträffar när wrapped > ImageWidth - screenWidth, dvs. bilden "svänger".
            int remaining = screenWidth - firstW;
            if (remaining > 0)
                rc.DrawPartialSprite(SpriteId, firstW, 0, 0, 0, remaining, screenHeight);
        }
    }
}
