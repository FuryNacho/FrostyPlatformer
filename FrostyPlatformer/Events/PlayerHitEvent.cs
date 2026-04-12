namespace FrostyPlatformer.Events
{
    /// <summary>
    /// Publiceras när hjälten tar skada från en fiende eller miljöfara.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter AudioSystem spela skadeljud och UISystem uppdatera hälsomätaren
    /// utan att kollisionssystemet kopplas direkt till dessa.
    ///
    /// ANVÄNDNING:
    /// Publiceras av CollisionSystem när hjälten kolliderar med en fientlig
    /// DynamicGameObject. Prenumereras av AudioSystem och UISystem.
    /// </remarks>
    public sealed class PlayerHitEvent
    {
        /// <summary>Mängden skada som togs.</summary>
        public int Damage { get; }

        public PlayerHitEvent(int damage)
        {
            Damage = damage;
        }
    }
}
