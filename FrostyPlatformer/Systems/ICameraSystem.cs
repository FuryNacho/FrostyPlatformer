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
        /// Beräknar kameravyn för en given frame.
        /// </summary>
        /// <param name="targetX">Målets tile-koordinat X (t.ex. hjältens px).</param>
        /// <param name="targetY">Målets tile-koordinat Y (t.ex. hjältens py).</param>
        /// <param name="mapWidth">Kartans bredd i tiles.</param>
        /// <param name="mapHeight">Kartans höjd i tiles.</param>
        /// <param name="screenWidth">Skärmens bredd i pixlar.</param>
        /// <param name="screenHeight">Skärmens höjd i pixlar.</param>
        /// <returns>En <see cref="CameraView"/> med offsettar och synliga tiles.</returns>
        CameraView Calculate(
            float targetX, float targetY,
            int mapWidth, int mapHeight,
            int screenWidth, int screenHeight);
    }
}
