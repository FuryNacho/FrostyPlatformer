#nullable enable
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Core
{
    /// <summary>
    /// Baskontrakt för alla spelsystem — ett system hanterar en avgränsad
    /// spelkoncern (kollision, AI, ljud, poäng m.m.) och anropas varje frame.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: System-arkitektur (inspirerat av ECS utan full entitetskomposition)
    ///
    /// MOTIVERING:
    /// Utan ett gemensamt interface lever all spellogik i Program.cs som direkt
    /// metodanrop utan tydliga gränser. IGameSystem gör varje koncern till ett
    /// slutet, utbytbart ansvarsområde. System kommunicerar aldrig direkt med
    /// varandra — all koordination sker via IEventBus (publish-subscribe) och
    /// delad data via GameContext.
    ///
    /// ANVÄNDNING:
    /// Implementeras av varje konkret system (CollisionSystem, AudioSystem etc.).
    /// Instansieras i Program.cs (Composition Root), registreras i GameStateManager
    /// och anropas i ordning varje frame via Update och Draw. Draw har en
    /// default-implementation som inte gör något — system utan renderingsbehov
    /// behöver inte överskugga den.
    /// </remarks>
    public interface IGameSystem
    {
        /// <summary>
        /// Kör systemets spellogik för denna frame.
        /// Läs och modifiera speldata via <paramref name="context"/>.
        /// Kommunicera sidoeffekter via IEventBus.Publish — inte via direktanrop
        /// till andra system.
        /// </summary>
        void Update(GameContext context, float deltaTime);

        /// <summary>
        /// Renderar systemets visuella element för denna frame.
        /// Anropas efter Update. Implementeras bara av system som ritar något
        /// (t.ex. UISystem, DialogSystem) — övriga system kan ignorera metoden.
        /// </summary>
        void Draw(IRenderContext renderContext);
    }
}
