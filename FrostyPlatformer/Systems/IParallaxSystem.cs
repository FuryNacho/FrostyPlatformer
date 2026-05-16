#nullable enable
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Kontrakt för parallax-scrollningssystemet.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Strategy (via interface + dependency inversion)
    ///
    /// MOTIVERING:
    /// Interfacet möjliggör testning utan MonoGame-beroenden (FakeParallaxSystem i tester)
    /// och gör det enkelt att byta implementering — t.ex. till en render-target-baserad
    /// variant (Alt 2, se RELEASE_PLAN.md Fynd 6) utan att röra GameplayState.
    ///
    /// FLÖDE:
    ///   GameplayState.Enter  → SetSeason(season)   — byter aktiva lager vid kartbyte
    ///   GameplayState.Draw   → Draw(rc, cam, w, h)  — ritar alla aktiva lager per frame
    ///
    /// ANVÄNDNING:
    /// Skapas i GameplayState-konstruktorn. SetSeason anropas i Enter() med
    /// SeasonHelper.FromMapName(context.CurrentLevel?.Name). Draw anropas i Draw()
    /// efter FillRect och innan TileMapRenderer — bakgrunden syns igenom transparenta tiles.
    /// </remarks>
    public interface IParallaxSystem
    {
        /// <summary>
        /// Väljer vilka parallax-lager som ska ritas baserat på årstid.
        /// Anropas varje gång en ny spelkarta laddas (i GameplayState.Enter).
        /// </summary>
        /// <param name="season">Årstiden för den aktuella banan.</param>
        void SetSeason(Season season);

        /// <summary>
        /// Ritar alla aktiva parallax-lager i ordning (bakre → främre).
        /// </summary>
        /// <param name="rc">Motor-agnostisk renderkontext.</param>
        /// <param name="cameraOffsetX">
        /// Kamerans X-offset i tile-enheter (CameraView.OffsetX).
        /// Används för att beräkna scroll-position per lager.
        /// </param>
        /// <param name="screenWidth">Skärmens bredd i spelpixlar.</param>
        /// <param name="screenHeight">Skärmens höjd i spelpixlar.</param>
        void Draw(IRenderContext rc, float cameraOffsetX, int screenWidth, int screenHeight);
    }
}
