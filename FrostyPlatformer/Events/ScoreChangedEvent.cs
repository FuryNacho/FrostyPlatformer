namespace FrostyPlatformer.Events
{
    /// <summary>
    /// Publiceras när spelarens poäng förändras.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter UISystem uppdatera poängvisningen utan att ScoreSystem kopplas
    /// direkt till renderingskoden.
    ///
    /// ANVÄNDNING:
    /// Publiceras av ScoreSystem när poängen räknas upp.
    /// Prenumereras av UISystem.
    /// </remarks>
    public sealed class ScoreChangedEvent
    {
        /// <summary>Det nya poängvärdet efter förändringen.</summary>
        public int NewScore { get; }

        public ScoreChangedEvent(int newScore)
        {
            NewScore = newScore;
        }
    }
}
