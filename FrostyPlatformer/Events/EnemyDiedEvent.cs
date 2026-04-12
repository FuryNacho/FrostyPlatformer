namespace FrostyPlatformer.Events
{
    /// <summary>
    /// Publiceras när en fiende dödas (Health &lt;= 0 och IsAttackable sätts till false).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter AudioSystem spela dödljud och ScoreSystem öka poängen utan att
    /// kollisionssystemet behöver känna till dessa system direkt.
    ///
    /// ANVÄNDNING:
    /// Publiceras av CollisionSystem (eller AISystem) när en fiende bekräftas
    /// ha dött. Prenumereras av AudioSystem och ScoreSystem.
    /// </remarks>
    public sealed class EnemyDiedEvent
    {
        /// <summary>Fiendens typ-id (t.ex. "enemyone", "enemytwo").</summary>
        public string EnemyName { get; }

        /// <summary>Fiendens world-space X-position vid dödstillfället.</summary>
        public float PositionX { get; }

        /// <summary>Fiendens world-space Y-position vid dödstillfället.</summary>
        public float PositionY { get; }

        public EnemyDiedEvent(string enemyName, float positionX, float positionY)
        {
            EnemyName = enemyName;
            PositionX = positionX;
            PositionY = positionY;
        }
    }
}
