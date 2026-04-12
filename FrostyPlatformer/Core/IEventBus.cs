using System;

namespace FrostyPlatformer.Core
{
    /// <summary>
    /// Meddelandebuss för kommunikation mellan spelsystem utan direkta beroenden.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer (Publish-Subscribe)
    ///
    /// MOTIVERING:
    /// Utan en event-buss måste t.ex. AudioSystem hålla en referens till
    /// CollisionSystem för att reagera på kollisionshändelser — och ScoreSystem
    /// måste hålla en referens till AI-systemet för att veta när en fiende dött.
    /// Det återskapar samma tighta koppling vi försöker eliminera.
    /// Med IEventBus publicerar CollisionSystem en CollisionEvent och AudioSystem
    /// prenumererar på den — utan att veta om varandra. Varje system är ett
    /// slutet, testbart ansvarsområde.
    ///
    /// ANVÄNDNING:
    /// Injiceras i alla IGameSystem-implementationer via konstruktorn.
    /// System som producerar händelser anropar Publish&lt;T&gt;().
    /// System som konsumerar händelser anropar Subscribe&lt;T&gt;() under
    /// initialisering och Unsubscribe&lt;T&gt;() i sin cleanup-metod.
    /// EventBus skapas en gång i Program.cs (Composition Root).
    /// </remarks>
    public interface IEventBus
    {
        /// <summary>
        /// Registrerar en prenumerant för händelser av typ T.
        /// Handlern anropas synkront varje gång Publish&lt;T&gt;() körs.
        /// </summary>
        void Subscribe<T>(Action<T> handler);

        /// <summary>
        /// Avregistrerar en tidigare prenumerant.
        /// Om handlern inte är registrerad är anropet en no-op.
        /// </summary>
        void Unsubscribe<T>(Action<T> handler);

        /// <summary>
        /// Publicerar en händelse till alla registrerade prenumeranter av typ T.
        /// Om inga prenumeranter finns är anropet en no-op.
        /// </summary>
        void Publish<T>(T gameEvent);
    }
}
