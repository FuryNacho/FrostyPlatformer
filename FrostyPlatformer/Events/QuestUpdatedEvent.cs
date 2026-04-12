namespace FrostyPlatformer.Events
{
    /// <summary>Möjliga tillstånd för ett quest.</summary>
    public enum QuestStatus
    {
        /// <summary>Questen är aktiv men inte avklarad.</summary>
        Active,

        /// <summary>Questen har klarats.</summary>
        Completed,

        /// <summary>Questen är misslyckad (används om spelet stöder det).</summary>
        Failed,
    }

    /// <summary>
    /// Publiceras när ett quests status förändras.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter UISystem visa questmeddelanden och AudioSystem spela fanfar
    /// utan att QuestSystem kopplas direkt till dessa.
    ///
    /// ANVÄNDNING:
    /// Publiceras av QuestSystem. Prenumereras av UISystem och AudioSystem.
    /// </remarks>
    public sealed class QuestUpdatedEvent
    {
        /// <summary>Quest-identifieraren (t.ex. "killallwalrus").</summary>
        public string QuestId { get; }

        /// <summary>Det nya tillståndet för questen.</summary>
        public QuestStatus Status { get; }

        public QuestUpdatedEvent(string questId, QuestStatus status)
        {
            QuestId = questId;
            Status  = status;
        }
    }
}
