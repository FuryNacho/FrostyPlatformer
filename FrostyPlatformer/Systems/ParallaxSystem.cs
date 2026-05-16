#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Hanterar parallax-bakgrundslager — väljer lager per årstid och ritar dem i rätt ordning.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Strategy + Composite
    ///
    /// MOTIVERING:
    /// Varje årstid har en ordnad lista av ParallaxLayer (bakre → främre). Systemet håller
    /// ett Dictionary[Season → ParallaxLayer[]] och byter aktiv lista vid SetSeason — inga
    /// dynamiska beräkningar vid ritning. Ritordning garanteras av array-ordningen i
    /// BuildLayerMap (index 0 = bakre lager, index 1 = mellanskikt).
    ///
    /// SCROLL-FAKTORER (konfigurerade i BuildLayerMap):
    ///   Sky-lager  0.10 → nästan statisk himmel, ger illusion av avlägsen horisont
    ///   Mid-lager  0.30 → rör sig tydligt men markant långsammare än tiles (1.0)
    /// Dessa värden är subjektiva och bör justeras interaktivt vid speltest.
    /// Att höja sky till 0.15 ger mer rörelseenergi; att sänka mid till 0.20 ger mer djup.
    ///
    /// TRANSPARENS:
    /// Mid-lager är PNG med alpha-kanal. SpriteBatch körs med BlendState.AlphaBlend
    /// i Program.cs → genomskinliga pixlar compositar korrekt mot föregående lager utan
    /// extra kod i ParallaxSystem.
    ///
    /// UTÖKNING:
    /// Lägg till en ny årstid: skapa ParallaxLayer-par i BuildLayerMap och lägg till
    /// Season-värde. Befintliga lager påverkas inte. Bildfilerna registreras i
    /// Program.cs.RegisterSprites() och läggs till i SpriteId-enumet.
    ///
    /// FRAMTIDA ALT 2 (render target):
    /// Byt IParallaxSystem-implementering — GameplayState.Draw behöver inte ändras.
    ///
    /// ANVÄNDNING:
    /// Skapas i GameplayState-konstruktorn. SetSeason anropas i Enter().
    /// Draw anropas i Draw() efter FillRect och innan TileMapRenderer.
    /// </remarks>
    public sealed class ParallaxSystem : IParallaxSystem
    {
        private readonly Dictionary<Season, ParallaxLayer[]> _layers;
        private ParallaxLayer[] _currentLayers = Array.Empty<ParallaxLayer>();

        /// <summary>Skapar systemet och bygger lager-tabellen för alla årstider.</summary>
        public ParallaxSystem()
        {
            _layers = BuildLayerMap();
        }

        /// <inheritdoc />
        public void SetSeason(Season season)
        {
            _currentLayers = _layers.TryGetValue(season, out var layers)
                ? layers
                : Array.Empty<ParallaxLayer>();
        }

        /// <inheritdoc />
        public void Draw(IRenderContext rc, float cameraOffsetX, int screenWidth, int screenHeight)
        {
            foreach (var layer in _currentLayers)
                layer.Draw(rc, cameraOffsetX, screenWidth, screenHeight);
        }

        // ── Lager-konfiguration ─────────────────────────────────────────────────────
        // Lager anges i bakre → främre ordning (index 0 ritas först).
        // ScrollFactor: 0 = statisk, 1 = följer kameran. Lägre = mer i bakgrunden.

        private static Dictionary<Season, ParallaxLayer[]> BuildLayerMap() => new()
        {
            [Season.Spring] = new[]
            {
                new ParallaxLayer(SpriteId.ParallaxSkySpring, scrollFactor: GameConstants.ParallaxScrollSky),
                new ParallaxLayer(SpriteId.ParallaxMidSpring, scrollFactor: GameConstants.ParallaxScrollMid),
            },
            [Season.Summer] = new[]
            {
                new ParallaxLayer(SpriteId.ParallaxSkySummer, scrollFactor: GameConstants.ParallaxScrollSky),
                new ParallaxLayer(SpriteId.ParallaxMidSummer, scrollFactor: GameConstants.ParallaxScrollMid),
            },
            [Season.Fall] = new[]
            {
                new ParallaxLayer(SpriteId.ParallaxSkyFall,   scrollFactor: GameConstants.ParallaxScrollSky),
                new ParallaxLayer(SpriteId.ParallaxMidFall,   scrollFactor: GameConstants.ParallaxScrollMid),
            },
            [Season.Winter] = new[]
            {
                new ParallaxLayer(SpriteId.ParallaxSkyWinter, scrollFactor: GameConstants.ParallaxScrollSky),
                new ParallaxLayer(SpriteId.ParallaxMidWinter, scrollFactor: GameConstants.ParallaxScrollMid),
            },
        };
    }
}
