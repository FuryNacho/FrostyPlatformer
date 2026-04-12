namespace FrostyPlatformer.Events
{
    /// <summary>
    /// Publiceras när hjältens hälsa når noll och spelet ska övergå till Game Over.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer — händelseklass (ren data, inga metoder)
    ///
    /// MOTIVERING:
    /// Låter GameStateManager (eller Program.cs) trigga Game Over-tillståndet
    /// och AudioSystem spela Game Over-musik utan att kollisionssystemet känner
    /// till tillståndshanteringen direkt.
    ///
    /// ANVÄNDNING:
    /// Publiceras av CollisionSystem när Hero.Health &lt;= 0.
    /// Prenumereras av GameStateManager (framtida extraktion) och AudioSystem.
    /// </remarks>
    public sealed class PlayerDiedEvent
    {
        // Inga data — händelsens existens är budskapet.
    }
}
