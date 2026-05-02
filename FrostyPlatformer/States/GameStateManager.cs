#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar det aktiva spelläget och alla övergångar mellan lägen.
    /// Ansvarar för att anropa Enter/Exit vid övergång samt Update/Draw varje frame.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (Context)
    ///
    /// MOTIVERING:
    /// Program.cs hade en switch-sats i OnUpdate som manuellt kallade rätt Display*-metod
    /// och lät HasSwitchedState-flaggan koordinera övergångar. Det band samman alla
    /// spellägens initierings- och avslutslogik i ett enda stort block.
    /// GameStateManager kapslar in övergångsmekanismen: ett IGameState-objekt är alltid
    /// aktivt, övergångar är uppskjutna tills slutet av nuvarande frame för att undvika
    /// re-entransproblem.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.cs (Composition Root). Program.OnCreate() anropar
    /// SetInitial() med startstaten. Program.OnUpdate() anropar Update() och Draw()
    /// varje frame. States anropar StateManager.Transition() för att byta läge.
    /// Enter/Exit anropas automatiskt — states ska aldrig anropa dem manuellt.
    /// </remarks>
    public class GameStateManager
    {
        private IGameState? _current;
        private IGameState? _pending;

        /// <summary>Det just nu aktiva spelläget.</summary>
        public IGameState? Current => _current;

        /// <summary>
        /// Sätter det initiala spelläget utan att anropa Exit på något föregående läge.
        /// Ska bara anropas en gång från Program.OnCreate().
        /// </summary>
        public void SetInitial(IGameState state, GameContext context)
        {
            _current = state;
            _current.Enter(context);
        }

        /// <summary>
        /// Köar en övergång till ett nytt spelläge. Övergången verkställs i början
        /// av nästa Update-anrop (uppskjuten för att undvika re-entrans).
        /// Det är säkert att anropa Transition mitt i ett Update — det nuvarande
        /// läget avslutas inte förrän Update returnerat.
        /// </summary>
        public void Transition(IGameState next, GameContext context)
        {
            _pending = next;
        }

        /// <summary>
        /// Verkställer eventuell uppskjuten övergång och anropar sedan det aktiva
        /// lädets Update. Kallas varje frame från Program.OnUpdate().
        /// </summary>
        public void Update(GameContext context, float deltaTime)
        {
            if (_pending != null)
            {
                _current?.Exit(context);
                _current = _pending;
                _pending = null;
                _current.Enter(context);
            }

            _current?.Update(context, deltaTime);
        }

        /// <summary>
        /// Anropar det aktiva lädets Draw. Kallas direkt efter Update varje frame.
        /// </summary>
        public void Draw(IRenderContext renderContext, GameContext context)
        {
            _current?.Draw(renderContext, context);
        }
    }
}
