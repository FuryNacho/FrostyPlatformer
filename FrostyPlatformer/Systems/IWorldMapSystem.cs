#nullable enable

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Kapslar in världskartans statiska data: stage-ingångspunkter och spawn-positioner.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion
    ///
    /// MOTIVERING:
    /// WorldMapState innehöll två hårdkodade datauppslagningar: switch-satsen i
    /// TryEnterStage (stage → kartnamn + koordinater) och spawn-formeln i Update.
    /// Dessa är rena funktioner utan sidoeffekter men var otestbara eftersom de
    /// låg inbäddade i staten. IWorldMapSystem exponerar dem som testbara kontrakt.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new WorldMapSystem() och injiceras via
    /// GameServices.WorldMap. WorldMapState anropar GetStageEntry() och
    /// GetSpawnPosition() istället för switch-satser och inline-formler.
    /// </remarks>
    public interface IWorldMapSystem
    {
        /// <summary>
        /// Returnerar kartnamn och spawn-koordinater för angivet stage (1–9).
        /// Returnerar null om stage är utanför giltigt intervall.
        /// </summary>
        (string MapName, float X, float Y)? GetStageEntry(int stage);

        /// <summary>
        /// Beräknar hjältens position på världskartan utifrån senast besökt stage.
        /// SpawnAtWorldMap = 0 → startposition (3, 8). Värden 1–9 → jämt fördelade.
        /// </summary>
        (float X, float Y) GetSpawnPosition(int spawnAtWorldMap);
    }
}
