namespace FrostyPlatformer.Events
{
    /// <summary>
    /// Publiceras när hjälten klarat en nivå och når en utgångspunkt.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter QuestSystem uppdatera avklarad nivå, AudioSystem byta musik och
    /// GameStateManager trigga kartövergång — allt utan direkta kopplingar.
    ///
    /// ANVÄNDNING:
    /// Publiceras av CollisionSystem (teleport-interaktion) eller GameplayState.
    /// Prenumereras av QuestSystem, AudioSystem och GameStateManager.
    /// </remarks>
    public sealed class LevelCompletedEvent
    {
        /// <summary>Kartnamnet/ID för den avklarade nivån (t.ex. "mapone").</summary>
        public string LevelId { get; }

        public LevelCompletedEvent(string levelId)
        {
            LevelId = levelId;
        }
    }
}
