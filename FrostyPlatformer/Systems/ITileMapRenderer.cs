#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Kontrakt för tile-kartans renderingslogik.
    /// Returnerar en sekvens av TileDrawCall utan att faktiskt rita — anroparen ritar.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: System (interface för DIP)
    ///
    /// MOTIVERING:
    /// Tile-renderingslogiken var inbäddad i Program.cs och direkt kopplad till PixelEngine.
    /// Extraheringen till ett interface separerar beräkning (vilka tiles, var) från
    /// ritning (DrawPartialSprite) och gör beräkningen testbar utan spelmotor (SRP, DIP).
    ///
    /// ANVÄNDNING:
    /// Implementeras av TileMapRenderer. Injiceras via GameServices i states som renderar
    /// kartor (GameplayState, WorldMapState). I tester kan en fake injiceras.
    /// </remarks>
    public interface ITileMapRenderer