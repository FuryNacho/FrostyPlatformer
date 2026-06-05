#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Visar high score-listan och väntar på knapptryckning.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayHighScore (ca 65 rader). Isolerar
    /// returnToEndAfterHighScore-logiken — den persisteras nu i context.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från MenuState ("View High Score") eller EnterHighScoreState.
    /// context.ReturnToEndAfterHighScore styr om vi går tillbaka till EndState
    /// eller MenuState efter att ha stängt listan.
    /// </remarks>
    internal sealed class HighScoreState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        public HighScoreState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context) { }

        public void Update(GameContext context, float elapsed)
        {
            _services.Script.Tick(elapsed);
            _services.Input.Poll();

            if (_services.Input.IsWindowFocused)
            {
                if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                    _services.Input.ButtonsHasGoneIdle = true;

                if (_services.Input.ButtonsHasGoneIdle &&
                    (_services.Input.IsAnyKeyPressed || !_services.Input.IsIdle))
                {
                    _services.Input.ButtonsHasGoneIdle = false;

                    if (context.ReturnToEndAfterHighScore)
                    {
                        context.ReturnToEndAfterHighScore = false;
                        _services.StateManager.Transition(new EndState(_services), context);
                    }
                    else
                    {
                        context.MenuNavigation = Enum.MenuState.StartMenu;
                        _services.StateManager.Transition(new MenuState(_services), context);
                    }
                    return;
                }
            }
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            const int FontWidth = 8;

            string header = "Penguin After All High Score";
            int hx = (context.ScreenWidth / 2) - ((header.Length * FontWidth) / 2);
            _rc.DrawText(header, hx, 10);

            // Tabellen centreras som block: kolumnrubriken och alla rader delar samma
            // vänsterkant så kolumnerna fortsätter ligga i linje.
            string colHeader = "    Name  Time        %";
            int tableX = (context.ScreenWidth - colHeader.Length * FontWidth) / 2;
            _rc.DrawText(colHeader, tableX, 45);

            int idx = 0;
            foreach (var row in _services.Score.GetList())
            {
                idx++;
                int y = idx * 10 + 50;
                string handle = row.Handle.Length < 5 ? row.Handle + "  " : row.Handle;
                _rc.DrawText(" " + idx + ". " + handle + " " +
                             row.TimeSpan.ToString("hh':'mm':'ss") +
                             "    " + row.Percent, tableX, y);
            }

            const int BottomTextMargin = 14; // Pixlar från skärmens nederkant till textens överkant
            string prompt = "Press any button";
            int px = (context.ScreenWidth / 2) - ((prompt.Length * FontWidth) / 2);
            _rc.DrawText(prompt, px, context.ScreenHeight - BottomTextMargin);
        }

        public void Exit(GameContext context) { }
    }
}
