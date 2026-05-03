#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Kontrakt för kamerasystemet — beräknar scroll-offset och synliga tiles per frame.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: System (interface för DIP)
    ///
    /// MOTIVERING:
    /// Gör kameralogiken utbytbar och testbar. States beror på ICameraSystem, inte på
    /// CameraSystem direkt, vilket möjliggör utbyte och fakad testning (DIP).
    ///
    /// ANVÄNDNING:
    /// Implementeras av CameraSystem (produktion) och FakeCameraSystem (tester som
    /// kräver förutsägbar kameravy). Injiceras i GameServices och sprids till states.
    /// </remarks>
    public interface ICameraSystem
    {
        /// <summary>
        /// Beräknar kameravyn direkt från angiven position — ingen lerp.
        /// Används av EditorState och liknande tillstånd med manuell kamerastyrning.
        /// </summary>
        CameraView Calculate(
            float targetX, float targetY,
            int mapWidth, int mapHeight,
            int screenWidth, int screenHeight);

        /// <summary>
        /// Uppdaterar den interna mjuka kameraposition mot målet via exponentiell lerp.
        /// Anropas en gång per Update-tick (inte från Draw).
        /// Vid första anropet efter SnapTo snäpper kameran direkt utan interpolation.
        /// </summary>
        void Advance(float targetX, float targetY, float elapsed);

        /// <summary>
        /// Placerar kameran omedelbart på angiven position utan interpolation.
        /// Anropas vid kartiinläsning (ChangeMap) för att undvika lerp-glid till startpositionen.
        /// </summary>
        void SnapTo(float targetX, float targetY);

        /// <summary>
        /// Returnerar en CameraView baserad på den nuvarande mjuka positionen.
        /// Anropas från Draw efter att Advance körts i Update.
        /// </summary>
        CameraView GetView(int mapWidth, int mapHeight, int screenWidth, int screenHeight);
    }
}
