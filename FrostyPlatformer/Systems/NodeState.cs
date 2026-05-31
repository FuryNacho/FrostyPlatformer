#nullable enable

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Tillståndet för en stoppunkt-nod på världskartan.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Value Object (diskriminerad union via enum)
    ///
    /// MOTIVERING:
    /// Klassificerar varje nod i fyra tydliga tillstånd utifrån spelarens
    /// progression. WorldMapState använder tillståndet för att avgöra om
    /// spelaren kan navigera till noden, stannar, eller passerar fritt.
    /// Separerar "vad är noden?" från "vad gör spelaren här?" —
    /// TiledWorldMapSystem räknar ut tillståndet, WorldMapState agerar på det.
    ///
    /// ANVÄNDNING:
    /// Returneras av IWorldMapSystem.GetNodeState(). Konsumeras av
    /// WorldMapState för navigations- och interaktionslogik.
    /// </remarks>
    public enum NodeState
    {
        /// <summary>
        /// Korsningsnod utan kopplad bana (SubType är tom sträng).
        /// Alltid passerbar — spelaren stannar inte och ingen bana kan öppnas.
        /// </summary>
        Junction,

        /// <summary>
        /// Bana avklarad — spelaren kan passera noden fritt i båda riktningarna.
        /// </summary>
        Completed,

        /// <summary>
        /// Bana nådd men ej avklarad — spelaren stannar här.
        /// Confirm öppnar banan, backa går tillbaka.
        /// </summary>
        Current,

        /// <summary>
        /// Bana ännu ej nådd — spelaren kan inte navigera hit.
        /// Stigen är blockerad tills föregående bana avklaras.
        /// </summary>
        Locked
    }
}
