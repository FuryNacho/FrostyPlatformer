#nullable enable
using System;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
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
    ///          slotId = null → ingen rekordkoll, valfri knapp → returnState (EditState).
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

        // My Maps-menyns tillstånd (cachas i Enter så rekordkollen inte varierar per frame)
        private UserMapScore? _existing;
        private bool          _isNewRecord;
        private int           _selectedIndex;

        /// <summary>
        /// Skapar ett nytt UserMapResultState.
        /// </summary>
        /// <param name="services">Gemensamma speltjänster.</param>
        /// <param name="runTime">Uppmätt genomspelningstid.</param>
        /// <param name="returnState">State att gå tillbaka till efter resultatet.</param>
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
            _selectedIndex = 0;

            if (_slotId != null)
            {
                _existing    = _services.UserMapScores.GetRecord(_slotId);
                _isNewRecord = _existing == null || _runTime < _existing.BestTime;
            }
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Input.Poll();

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            if (_slotId != null)
                UpdateUserRun(context);
            else
                UpdatePreview(context);
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            if (_slotId != null)
                DrawUserRun(context);
            else
                DrawPreview(context);
        }

        public void Exit(GameContext context) { }

        // ── Preview-variant (F5 från editorn) ────────────────────────────────────

        private void UpdatePreview(GameContext context)
        {
            // Det finns inget annat att göra här än att gå tillbaka — vilken knapp
            // som helst (tangent eller gamepad) duger. Samma mönster som EndState.
            if (_services.Input.ButtonsHasGoneIdle &&
                (_services.Input.IsAnyKeyPressed || !_services.Input.IsIdle))
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(_returnState, context);
            }
        }

        private void DrawPreview(GameContext context)
        {
            int cx = context.ScreenWidth  / 2;
            int cy = context.ScreenHeight / 2;
            _rc.DrawText("Level complete!",  cx - 56, cy - 20);
            _rc.DrawText($"Time: {FormatTime(_runTime)}", cx - 36, cy - 8);
            _rc.DrawText("Press any button", cx - 64, cy + 8);
        }

        // ── My Maps-variant (rekordkoll + namnsinmatning) ─────────────────────────

        private void UpdateUserRun(GameContext context)
        {
            if (!_services.Input.ButtonsHasGoneIdle) return;

            // Konsolanpassad meny: nytt rekord ger två val (Save initials / Skip),
            // annars ett enda (OK). Navigeras med upp/ner, väljs med Confirm.
            int optionCount = _isNewRecord ? 2 : 1;

            if (_services.Input.IsUpPressed && _selectedIndex > 0)
            {
                _selectedIndex--;
                _services.Input.ButtonsHasGoneIdle = false;
                return;
            }
            if (_services.Input.IsDownPressed && _selectedIndex < optionCount - 1)
            {
                _selectedIndex++;
                _services.Input.ButtonsHasGoneIdle = false;
                return;
            }

            // Esc/B = tillbaka utan att spara (motsvarar Skip/OK) — samma genväg som övriga menyer.
            if (_services.Input.IsCancelPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(_returnState, context);
                return;
            }

            if (_services.Input.IsConfirmPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                if (_isNewRecord && _selectedIndex == 0)
                    GoToNameEntry(context);                                  // Save initials
                else
                    _services.StateManager.Transition(_returnState, context); // Skip / OK
            }
        }

        private void GoToNameEntry(GameContext context)
        {
            var slotId  = _slotId!;
            var runTime = _runTime;
            _services.StateManager.Transition(
                new EnterHighScoreState(
                    _services,
                    onSave: handle => _services.UserMapScores.SaveRecord(slotId, handle, runTime),
                    returnState: _returnState),
                context);
        }

        private void DrawUserRun(GameContext context)
        {
            int cx = context.ScreenWidth  / 2;
            int cy = context.ScreenHeight / 2;

            DrawCentered("Level complete!",            cx, cy - 40);
            DrawCentered($"Time: {FormatTime(_runTime)}", cx, cy - 28);

            if (_isNewRecord)
            {
                DrawCentered("NEW RECORD!", cx, cy - 12);
                DrawRow(cx, cy + 4,  "Save initials", _selectedIndex == 0);
                DrawRow(cx, cy + 22, "Skip",          _selectedIndex == 1);
            }
            else
            {
                DrawCentered($"Best: {FormatTime(_existing!.BestTime)}   {_existing.Handle}", cx, cy - 12);
                DrawRow(cx, cy + 6, "OK", _selectedIndex == 0);
            }
        }

        /// <summary>Ritar en centrerad textrad kring cx.</summary>
        private void DrawCentered(string text, int cx, int y)
            => _rc.DrawText(text, cx - (text.Length * GameConstants.FontCharWidth) / 2, y);

        /// <summary>Ritar en menyrad med ikon-markör, centrerad kring cx — samma stil som UserMapsState.</summary>
        private void DrawRow(int cx, int y, string label, bool selected)
        {
            const int IconToTextGap = 25;
            int screenX = cx - (IconToTextGap + label.Length * GameConstants.FontCharWidth) / 2;
            int srcX    = selected ? 0 : 16;
            _rc.DrawPartialSprite(SpriteId.Items, screenX, y, srcX, 48, 16, 16);
            _rc.DrawText(label, screenX + IconToTextGap, y + 5);
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
