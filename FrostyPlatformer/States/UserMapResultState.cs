#nullable enable
using System;
using FrostyPlatformer.Core;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Visar resultatet efter att spelaren klarat en editorskapad bana.
    /// Preview-körning (F5): visar tid, inga highscore-operationer.
    /// My Maps-körning (Step 4): jämför mot rekord och triggar namnsinmatning vid nytt rekord.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Separerar resultatvisningen från GameplayState — konsekvent med hur EndState
    /// hanterar det inbyggda spelets slut. Håller GameplayState fri från
    /// highscore-logik som bara är relevant för user maps.
    ///
    /// ANVÄNDNING:
    /// Skapas av GameplayState när portalen nås i preview-läge (IsPreviewMode = true).
    /// Konstruktorn tar uppmätt bans-tid och det IGameState som ska aktiveras när
    /// spelaren bekräftar. Enter/Escape återvänder till det angivna returnState.
    /// </remarks>
    internal sealed class UserMapResultState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;
        private readonly TimeSpan _runTime;
        private readonly IGameState _returnState;

        public UserMapResultState(GameServices services, TimeSpan runTime, IGameState returnState)
        {
            _services    = services;
            _rc          = services.RenderContext;
            _runTime     = runTime;
            _returnState = returnState;
        }

        public void Enter(GameContext context)
        {
            context.IsPreviewMode      = false;
            context.PreviewReturnState = null;
            _services.Input.ButtonsHasGoneIdle = false;
        }

        public void Update(GameContext context, float elapsed)
        {
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            if (_services.Input.ButtonsHasGoneIdle &&
                (_services.Input.IsConfirmPressed || _services.Input.IsCancelPressed))
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(_returnState, context);
                return;
            }

            DrawResult(context);
        }

        public void Draw(IRenderContext renderContext) { }

        public void Exit(GameContext context) { }

        private void DrawResult(GameContext context)
        {
            int cx = context.ScreenWidth  / 2;
            int cy = context.ScreenHeight / 2;

            string timeStr = FormatTime(_runTime);

            _rc.DrawText("Level complete!",      cx - 56, cy - 20);
            _rc.DrawText($"Time: {timeStr}",     cx - 36, cy - 8);
            _rc.DrawText("Enter/Esc = back",     cx - 60, cy + 8);
        }

        /// <summary>Formaterar TimeSpan som M:SS.ff</summary>
        private static string FormatTime(TimeSpan t)
        {
            int minutes = (int)t.TotalMinutes;
            int seconds = t.Seconds;
            int hundredths = t.Milliseconds / 10;
            return $"{minutes}:{seconds:D2}.{hundredths:D2}";
        }
    }
}
