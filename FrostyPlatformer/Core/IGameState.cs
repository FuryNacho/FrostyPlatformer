#nullable enable
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Core
{
    /// <summary>
    /// Baskontrakt för ett spelläge — ett tillstånd äger sin Update/Draw-logik
    /// under den tid det är aktivt i GameStateManager.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Program.cs innehåller en enorm switch-baserad state machine där varje
    /// spelläge (splash, gameplay, världskarta, paus, game over m.fl.) är en
    /// stor Display*-metod i samma klass. Det gör det omöjligt att förstå,
    /// testa eller ändra ett läge utan att riskera att påverka alla andra.
    ///
    /// Med IGameState blir varje läge en isolerad klass med tydliga livscykel-
    /// metoder. Övergångar hanteras av GameStateManager, inte av flaggor i
    /// Program.cs. Ny spelstate = ny klass som implementerar IGameState, utan
    /// ändringar i befintlig kod (OCP).
    ///
    /// ANVÄNDNING:
    /// Implementeras av SplashState, GameplayState, WorldMapState, DialogState,
    /// PauseState och GameOverState. Instansieras och hanteras av GameStateManager.
    /// Enter/Exit anropas av GameStateManager vid övergångar — inte manuellt.
    /// </remarks>
    public interface IGameState
    {
        /// <summary>
        /// Körs en gång när tillståndet aktiveras. Initialisera tillståndsspecifik
        /// data och prenumerera på relevanta IEventBus-händelser här.
        /// </summary>
        void Enter(GameContext context);

        /// <summary>
        /// Körs varje frame medan tillståndet är aktivt. Hanterar input, spellogik
        /// och tillståndsövergångar (via GameStateManager — inte direkt).
        /// </summary>
        void Update(GameContext context, float deltaTime);

        /// <summary>
        /// Renderar tillståndets visuella innehåll för denna frame.
        /// Anropas av GameStateManager direkt efter Update.
        /// </summary>
        void Draw(IRenderContext renderContext);

        /// <summary>
        /// Körs en gång när tillståndet lämnas. Avregistrera IEventBus-prenumerationer
        /// och städa upp tillståndsspecifika resurser här.
        /// </summary>
        void Exit(GameContext context);
    }
}
