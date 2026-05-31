#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Kapslar in världskartans nod-baserade logik: stoppunkt-navigation, nod-tillstånd
    /// och spawn-positioner.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion
    ///
    /// MOTIVERING:
    /// Den ursprungliga WorldMapState använde hårdkodade X-koordinater (WorldMapStage1X …
    /// WorldMapStage9X) för att avgöra vilken nod spelaren befann sig på. Det kopplade
    /// karttopologin hårt till koden och omöjliggjorde icke-linjära världskartor och
    /// enhetstestning. IWorldMapSystem exponerar istället rena, testbara kontrakt som
    /// arbetar mot PlacedObject-noder från worldmap.json.
    ///
    /// ANVÄNDNING:
    /// TiledWorldMapSystem (produktion) injiceras via GameServices.WorldMap och tar
    /// StopPoint-noderna från worldmap.json i sin konstruktor. WorldMapState anropar
    /// GetCurrentNode, GetNextNode och GetNodeState istället för koordinat-switchar.
    /// GetStageEntry och GetStageIndex används fortfarande för att starta banor och
    /// spara progression.
    /// </remarks>
    public interface IWorldMapSystem
    {
        // ── Nod-navigation ────────────────────────────────────────────────────────

        /// <summary>
        /// Returnerar den stoppunkt spelaren befinner sig exakt på, eller null om
        /// spelaren är i rörelse mellan noder.
        /// </summary>
        PlacedObject? GetCurrentNode(int tileX, int tileY);

        /// <summary>
        /// Returnerar nästa stoppunkt i given riktning (dX/dY är -1, 0 eller 1).
        /// Söker längs axeln med ett toleransfönster på en tile i den korsande axeln.
        /// Returnerar null om ingen nod finns i riktningen.
        /// </summary>
        PlacedObject? GetNextNode(PlacedObject current, int dX, int dY);

        /// <summary>
        /// Klassificerar en nods tillstånd givet spelarens nuvarande progression.
        /// Tom SubType ger alltid Junction oavsett stageCompleted.
        /// </summary>
        NodeState GetNodeState(PlacedObject node, int stageCompleted);

        /// <summary>
        /// Returnerar tile-koordinaterna spelaren ska placeras på när världskartan öppnas.
        /// spawnAtWorldMap 1–9 slår upp stoppunkten vars SubType matchar banan.
        /// spawnAtWorldMap 0 eller okänt värde returnerar (defaultTileX, defaultTileY).
        /// </summary>
        (int TileX, int TileY) GetSpawnTile(int spawnAtWorldMap, int defaultTileX, int defaultTileY);

        /// <summary>
        /// Returnerar steg-index (1–9) för en bana-nods SubType.
        /// Returnerar -1 för korsningar (tom SubType) och okända SubTypes.
        /// </summary>
        int GetStageIndex(string subType);

        // ── Banstart ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returnerar kartnamn och spawn-koordinater inuti banan för angivet steg (1–9).
        /// Returnerar null om steg är utanför giltigt intervall.
        /// </summary>
        (string MapName, float X, float Y)? GetStageEntry(int stage);
    }
}
