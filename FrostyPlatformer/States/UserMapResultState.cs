#nullable enable
using System;
using FrostyPlatformer.Core;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Visar resultatet efter att spelaren klarat en editorskapad bana.
    /// Preview-körning (F5 från editorn): visar tid, inga highscore-operationer.
    /// My Maps-körning: jämför mot rekord och triggar namnsinmatning vid nytt rekord.
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
    /// Preview: skapas av GameplayState när portalen nås och IsPreviewMode är satt.
    ///          slotId = null → ingen rekordkoll, Enter/Esc → returnState (EditState).
    /// My Maps: skapas av GameplayState när portalen nås och UserMapSlotId är satt.
    ///          slotId != null → jämför med UserMapScoreRepository; nytt rekord →
    ///          EnterHighScoreState med callback som sparar via UserMapScores.
    /// </remarks>
    internal sealed class UserMapResultState : IGameState
    {
        private readonly GameServices   _services;
        private readonly IRenderContext _rc;
        private readonly TimeSpan       _runTime;
        private readonly IGameState     _returnState;
        private readonly string?        _slotId;

        /// <summary>
        /// Skapar ett nytt UserMapResultState.
        /// </summary>
        /// <param name="services">Gemensamma speltjänster.</param>
        /// <param name="runTime">Uppmätt genomspelningstid.</param>
        /// <param name="returnState">State att gå tillbaka till vid Enter/Esc.</param>
        /// <param name="slotId">
        /// Slot-ID för My Maps-körning. Null = preview-läge (ingen rekordkoll).
        /// </param>
        public UserMapResultState(GameServices services, TimeSpan runTime,
            IGameState returnState, string? slotId = null)
        {
            _services    = services;
            _rc          = services.RenderContext;
            _runTime     = runTime;
            _returnState = returnState;
            _slotId      = slotId;
        }

        public void Enter(GameContext context)
        {
            context.IsPreviewMode      = false;
            context.PreviewReturnState = null;
            context.UserMapSlotId      = null;
            _services.Input.ButtonsHasGoneIdle = false;
        }

        public void Update(GameContext context, float elapsed)
        {
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            if (_slotId != null)
                UpdateUserRun(context);
            else
                UpdatePreview(context);
        }

        public void Draw(IRenderContext renderContext) { }

        public void Exit(GameContext context) { }

        // ── Preview-variant (F5 från editorn) ────────────────────────────────────

        private void UpdatePreview(GameContext context)
        {
            if (_services.Input.ButtonsHasGoneIdle &&
                (_services.Input.IsConfirmPressed || _services.Input.IsCancelPressed))
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(_returnState, context);
                return;
            }

            int cx = context.ScreenWidth  / 2;
            int cy = context.ScreenHeight / 2;
            _rc.DrawText("Level complete!",  cx - 56, cy - 20);
            _rc.DrawText($"Time: {FormatTime(_runTime)}", cx - 36, cy - 8);
            _rc.DrawText("Enter/Esc = back", cx - 60, cy + 8);
        }

        // ── My Maps-variant (rekordkoll + namnsinmatning) ─────────────────────────

        private void UpdateUserRun(GameContext context)
        {
            var existing = _services.UserMapScores.GetRecord(_slotId!);
            bool isNewRecord = existing == null || _runTime < existing.BestTime;

            if (_services.Input.ButtonsHasGoneIdle)
            {
                if (isNewRecord && _services.Input.IsConfirmPressed)
                {
                    _services.Input.ButtonsHasGoneIdle = false;
                    var slotId  = _slotId!;
                    var runTime = _runTime;
                    _services.StateManager.Transition(
                        new EnterHighScoreState(
                            _services,
                            onSave: handle => _services.UserMapScores.SaveRecord(slotId, handle, runTime),
                            returnState: _returnState),
                        context);
                    return;
                }
                if (_services.Input.IsCancelPressed ||
                    (!isNewRecord && _services.Input.IsConfirmPressed))
                {
                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(_returnState, context);
                    return;
                }
            }

            DrawUserRunResult(context, existing?.BestTime, isNewRecord);
        }

        private void DrawUserRunResult(GameContext context,
            TimeSpan? bestTime, bool isNewRecord)
        {
            int cx = context.ScreenWidth  / 2;
            int cy = context.ScreenHeight / 2;

            _rc.DrawText("Level complete!",         cx - 56, cy - 30);
            _rc.DrawText($"Time: {FormatTime(_runTime)}", cx - 36, cy - 18);

            if (isNewRecord)
            {
                _rc.DrawText("NEW RECORD!",          cx - 40, cy - 4);
                _rc.DrawText("Enter = save name",    cx - 64, cy + 10);
                _rc.DrawText("Esc = skip",           cx - 36, cy + 20);
            }
            else
            {
                _rc.DrawText($"Best: {FormatTime(bestTime!.Value)}", cx - 36, cy - 4);
                _rc.DrawText("Enter/Esc = back",     cx - 60, cy + 10);
            }
        }

        /// <summary>Formaterar TimeSpan som M:SS.ff</summary>
        private static string FormatTime(TimeSpan t)
        {
            int minutes    = (int)t.TotalMinutes;
            int seconds    = t.Seconds;
            int hundredths = t.Milliseconds / 10;
            return $"{minutes}:{seconds:D2}.{hundredths:D2}";
        }
    }
}
