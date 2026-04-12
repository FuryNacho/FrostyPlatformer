namespace FrostyPlatformer.Events
{
    /// <summary>
    /// Publiceras när hjälten plockar upp ett föremål.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter AudioSystem spela plocka-upp-ljud och QuestSystem kontrollera
    /// item-baserade questmål utan att CollisionSystem kopplas till dessa.
    ///
    /// ANVÄNDNING:
    /// Publiceras av CollisionSystem (DynamicItem.OnInteract).
    /// Prenumereras av AudioSystem och QuestSystem.
    /// </remarks>
    public sealed class ItemCollectedEvent
    {
        /// <summary>Föremålets namn (t.ex. "energi").</summary>
        public string ItemId { get; }

        public ItemCollectedEvent(string itemId)
        {
            ItemId = itemId;
        }
    }
}
