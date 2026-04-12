namespace FrostyPlatformer.Events
{
    /// <summary>
    /// Publiceras när hjälten når en checkpoint (sparläge).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter SaveLoadSystem automatiskt spara spelets tillstånd och UISystem
    /// visa en checkpoint-bekräftelse utan att kollisionssystemet kopplas till dessa.
    ///
    /// ANVÄNDNING:
    /// Publiceras av CollisionSystem. Prenumereras av SaveLoadSystem och UISystem.
    /// </remarks>
    public sealed class CheckpointReachedEvent
    {
        /// <summary>Checkpointens id-nummer.</summary>
        public int CheckpointId { get; }

        public CheckpointReachedEvent(int checkpointId)
        {
            CheckpointId = checkpointId;
        }
    }
}
