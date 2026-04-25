#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Animerar och visar "Player Dead"-skärmen efter att hjälten dött.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayGameOver (ca 165 rader). Isolerar
    /// AnimateCirkle och AnimationCount som tidigare var fält i Program.cs.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från GameplayState när Hero.Health &lt; 1. Övergår till MenuState
    /// när spelaren trycker en knapp. Reset() anropas vid bekräftelse för att
    /// starta om spelet.
    /// </remarks>
    internal sealed class GameOverState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        private int _animCount = 10;

        public GameOverState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            _animCount = 10;

            // Ge tillbaka lite liv
            if (context.Player != null && context.Player.Health < 1)
                context.Player.Health = 10;
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Script.Tick(elapsed);
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            if (_services.Input.ButtonsHasGoneIdle &&
                (_services.Input.IsAnyKeyPressed || !_services.Input.IsIdle))
            {
                if (_services.Input.IsConfirmPressed)
                    _services.Reset();

                _services.Input.ButtonsHasGoneIdle = false;
                context.MenuNavigation = Enum.MenuState.StartMenu;
                _services.StateManager.Transition(new MenuState(_services), context);
                _animCount = 10;
                return;
            }

            var color = RenderColor.Random();
            int hw = context.ScreenWidth  / 2;
            int hh = context.ScreenHeight / 2;

            if (_animCount >= 7)
            {
                // animCount 10–9: DrawPath-anrop var en bugg i original (tomt array) — utelämnad
            }
            else if (_animCount >= 5)
            {
                _rc.DrawLine(hw - 65, hh - 1, hw + 65, hh - 1, color);
                _rc.DrawLine(hw - 110, hh,     hw + 110, hh,    color);
                _rc.DrawLine(hw - 50, hh + 1,  hw + 50, hh + 1, color);
            }
            else if (_animCount >= 3)
            {
                _rc.DrawLine(hw - 45, hh - 1, hw + 45, hh - 1, color);
                _rc.DrawLine(hw - 110, hh,    hw + 110, hh,    color);
                _rc.DrawLine(hw - 50, hh + 1, hw + 50, hh + 1, color);
            }
            else if (_animCount >= 1)
            {
                _rc.DrawLine(hw - 15, hh - 1, hw + 15, hh - 1, color);
                _rc.DrawLine(hw - 100, hh,    hw + 100, hh,    color);
                _rc.DrawLine(hw - 20, hh + 1, hw + 20, hh + 1, color);
            }
            else if (_animCount == 0)
            {
                _rc.DrawLine(hw - 5, hh - 1, hw + 5, hh - 1, color);
                _rc.DrawLine(hw - 90, hh,    hw + 90, hh,    color);
                _rc.DrawLine(hw - 2, hh + 1, hw + 2, hh + 1, color);
            }
            else
            {
                _rc.Clear(RenderColor.Black);
                _rc.DrawText("Player Dead", 8, 4);
                const int BottomTextMargin = 7; // Pixlar från skärmens nederkant till textens överkant
                _rc.DrawText("Press any button", 8, context.ScreenHeight - BottomTextMargin);
            }

            _animCount--;
        }

        public void Draw(IRenderContext renderContext) { }

        public void Exit(GameContext context) { }
    }
}
