#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar spelinställningar: ljud, rensa high score, spara och ladda spel.
    /// Vilken undermeny som visas styrs av context.MenuNavigation.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplaySettings (ca 285 rader) och DefaultListSave.
    /// Isolerar inställningslogiken och selectIndex-räknaren.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från MenuState. context.MenuNavigation styr vilken vy som visas.
    /// Tillbaka-knappen byter MenuNavigation och övergår till MenuState.
    /// </remarks>
    internal sealed class SettingsState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        private int              _selectIndex     = 1;
        private List<OptionsObj> _currentOptions  = new List<OptionsObj>();
        private string           _currentHeader   = "";
        private string           _currentBread    = "";

        public SettingsState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            _selectIndex = 1;
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Input.Poll();

            _currentHeader  = "";
            _currentBread   = "";
            _currentOptions = BuildOptions(context, ref _currentHeader, ref _currentBread);

            if (!_services.Input.IsWindowFocused) return;

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsUpPressed)
            {
                _selectIndex = _selectIndex <= 1 ? _currentOptions.Count : _selectIndex - 1;
                _services.Input.ButtonsHasGoneIdle = false;
            }
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsDownPressed)
            {
                _selectIndex = _selectIndex >= _currentOptions.Count ? 1 : _selectIndex + 1;
                _services.Input.ButtonsHasGoneIdle = false;
            }

            // Tillbaka (cancel) — backar alltid, oavsett markörens position
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsCancelPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                HandleBack(context);
                return;
            }

            // Välj (confirm)
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsConfirmPressed)
            {
                var sel = _currentOptions[_selectIndex - 1];
                _services.Input.ButtonsHasGoneIdle = false;
                HandleSelection(sel, context);
            }
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            int hx = (context.ScreenWidth / 2) - ((_currentHeader.Length * 8) / 2);
            _rc.DrawText(_currentHeader, hx, 4);

            int bx = (context.ScreenWidth / 2) - ((_currentBread.Length * 8) / 2);
            _rc.DrawText(_currentBread, bx, 18);

            for (int idx = 0; idx < _currentOptions.Count; idx++)
            {
                string row = _currentOptions[idx].Display;
                if (_selectIndex == idx + 1)
                    row = "> " + row + " <";
                int ox = (context.ScreenWidth / 2) - ((row.Length * 8) / 2);
                _rc.DrawText(row, ox, 26 + (idx + 1) * 12);
            }
        }

        public void Exit(GameContext context) { }

        // ── Hjälpmetoder ────────────────────────────────────────────────────────

        private List<OptionsObj> BuildOptions(GameContext context,
            ref string header, ref string bread)
        {
            switch (context.MenuNavigation)
            {
                case Enum.MenuState.Audio:
                    header = "Audio";
                    bread  = "";
                    string gameState   = _services.Settings.AudioOn       ? "on" : "off";
                    string editorState = _services.Settings.EditorAudioOn ? "on" : "off";
                    return new List<OptionsObj>
                    {
                        new OptionsObj { Display = "Gamesound (" + gameState + ")" },
                        new OptionsObj { Display = "Editor sound (" + editorState + ")" },
                        new OptionsObj { Display = "Back", OptionIsBack = true }
                    };

                case Enum.MenuState.Screen:
                    header = "Screen";
                    bread  = "";
                    ScreenMode mode = _services.Settings.ScreenMode ?? ScreenMode.Windowed;
                    string modeName = mode == ScreenMode.BorderlessFullscreen
                        ? "Borderless Fullscreen" : "Windowed";
                    return new List<OptionsObj>
                    {
                        new OptionsObj { Display = "Mode (" + modeName + ")" },
                        new OptionsObj { Display = "Back", OptionIsBack = true }
                    };

                case Enum.MenuState.ClearHighScore:
                    header = "Clear High Score";
                    bread  = "Clear The High Score List?";
                    return new List<OptionsObj>
                    {
                        new OptionsObj { Display = "Yes" },
                        new OptionsObj { Display = "No" },
                        new OptionsObj { Display = "Back", OptionIsBack = true }
                    };

                case Enum.MenuState.Save:
                    header = "Select Slot To Save Your Game";
                    bread  = _selectIndex switch
                    {
                        1 => "Selected Slot  One", 2 => "Selected Slot  Two",
                        3 => "Selected Slot  Three", _ => ""
                    };
                    return DefaultListSave();

                case Enum.MenuState.Load:
                    header = "Select Game To Load";
                    bread  = _selectIndex switch
                    {
                        1 => "Selected Slot  One", 2 => "Selected Slot  Two",
                        3 => "Selected Slot  Three", _ => ""
                    };
                    return DefaultListSave();

                case Enum.MenuState.ClearSavedGame:
                    header = "Clear Saved Game";
                    bread  = _selectIndex switch
                    {
                        1 => "Clear Slot One", 2 => "Clear Slot Two",
                        3 => "Clear Slot Three", _ => ""
                    };
                    return DefaultListSave();

                case Enum.MenuState.ClearMyMaps:
                    header = "Clear My Maps";
                    bread  = "Delete a map and its high score";
                    return DefaultListUserMaps();

                default:
                    return new List<OptionsObj>();
            }
        }

        /// <summary>
        /// Backar ut ur den aktiva inställningsvyn till rätt förälder-meny.
        /// Anropas både av "Back"-alternativet (confirm) och av cancel-knappen.
        /// </summary>
        private void HandleBack(GameContext context)
        {
            _services.Input.ButtonsHasGoneIdle = false;

            // Save nås bara från pausmenyn på världskartan → backa tillbaka dit.
            if (context.MenuNavigation == Enum.MenuState.Save)
            {
                _services.StateManager.Transition(new WorldMapState(_services), context);
                return;
            }

            context.MenuNavigation = context.MenuNavigation switch
            {
                Enum.MenuState.Load => Enum.MenuState.StartMenu,
                _                  => Enum.MenuState.SettingsMenu
            };
            _services.StateManager.Transition(new MenuState(_services), context);
        }

        private void HandleSelection(OptionsObj sel, GameContext context)
        {
            if (sel.OptionIsBack)
            {
                HandleBack(context);
                return;
            }

            switch (context.MenuNavigation)
            {
                case Enum.MenuState.Audio:
                    if (sel.Display.StartsWith("Gamesound"))
                    {
                        _services.Settings.AudioOn = !_services.Settings.AudioOn;
                        if (_services.Settings.AudioOn) _services.Audio.UnMute();
                        else                            _services.Audio.Mute();
                        _services.Settings.Save();
                    }
                    else if (sel.Display.StartsWith("Editor sound"))
                    {
                        _services.Settings.EditorAudioOn = !_services.Settings.EditorAudioOn;
                        _services.Settings.Save();
                    }
                    break;

                case Enum.MenuState.Screen:
                    if (sel.Display.StartsWith("Mode"))
                    {
                        ScreenMode current = _services.Settings.ScreenMode ?? ScreenMode.Windowed;
                        ScreenMode next = current == ScreenMode.BorderlessFullscreen
                            ? ScreenMode.Windowed : ScreenMode.BorderlessFullscreen;
                        _services.SetScreenMode(next);   // synkar Settings.ScreenMode med faktiskt läge
                        _services.Settings.Save();
                    }
                    break;

                case Enum.MenuState.ClearHighScore:
                    if (sel.Display == "Yes")
                        _services.Score.Reset();
                    context.MenuNavigation = Enum.MenuState.SettingsMenu;
                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(new MenuState(_services), context);
                    break;

                case Enum.MenuState.ClearSavedGame:
                    if (sel.OptionIsSlotOne)        _services.Settings.ClearSaveSlot(1);
                    else if (sel.OptionIsSlotTwo)   _services.Settings.ClearSaveSlot(2);
                    else if (sel.OptionIsSlotThree) _services.Settings.ClearSaveSlot(3);
                    _services.Settings.Save();
                    break;

                case Enum.MenuState.ClearMyMaps:
                    // Kartan och dess high score-rekord hör ihop — rensa båda.
                    if (sel.SlotIsUsed && sel.SlotId.Length > 0)
                    {
                        _services.UserMaps.Delete(sel.SlotId);
                        _services.UserMapScores.DeleteRecord(sel.SlotId);
                    }
                    break;

                case Enum.MenuState.Load:
                    if (sel.SlotIsUsed)
                    {
                        if (sel.OptionIsSlotOne)        _services.SaveLoad.Load(1);
                        else if (sel.OptionIsSlotTwo)   _services.SaveLoad.Load(2);
                        else if (sel.OptionIsSlotThree) _services.SaveLoad.Load(3);

                        _services.Input.ButtonsHasGoneIdle = false;
                        _services.StateManager.Transition(new WorldMapState(_services), context);
                    }
                    break;

                case Enum.MenuState.Save:
                    if (sel.OptionIsSlotOne)        _services.SaveLoad.Save(1);
                    else if (sel.OptionIsSlotTwo)   _services.SaveLoad.Save(2);
                    else if (sel.OptionIsSlotThree) _services.SaveLoad.Save(3);
                    _services.Settings.Save();
                    break;
            }
        }

        private List<OptionsObj> DefaultListSave()
        {
            var slots = _services.Settings.SaveSlots;

            string d1 = slots.SlotOne.IsUsed
                ? slots.SlotOne.DateTime.ToString("dd MMM yy") + " " + slots.SlotOne.StageCompleted
                : "Empty";
            string d2 = slots.SlotTwo.IsUsed
                ? slots.SlotTwo.DateTime.ToString("dd MMM yy") + " " + slots.SlotTwo.StageCompleted
                : "Empty";
            string d3 = slots.SlotThree.IsUsed
                ? slots.SlotThree.DateTime.ToString("dd MMM yy") + " " + slots.SlotThree.StageCompleted
                : "Empty";

            return new List<OptionsObj>
            {
                new OptionsObj { Display = "1 " + d1, OptionIsSlotOne   = true,
                                 SlotIsUsed = slots.SlotOne.IsUsed },
                new OptionsObj { Display = "2 " + d2, OptionIsSlotTwo   = true,
                                 SlotIsUsed = slots.SlotTwo.IsUsed },
                new OptionsObj { Display = "3 " + d3, OptionIsSlotThree = true,
                                 SlotIsUsed = slots.SlotThree.IsUsed },
                new OptionsObj { Display = "Back", OptionIsBack = true }
            };
        }

        /// <summary>
        /// Bygger listan över de 7 user map-slottarna (slot1..slot7) för Clear My
        /// Maps. Tomma slots visas men är inte valbara (SlotIsUsed = false).
        /// </summary>
        private List<OptionsObj> DefaultListUserMaps()
        {
            const int MaxUserSlots = 7;
            var existing = new HashSet<string>(_services.UserMaps.GetAvailableMapIds());

            var list = new List<OptionsObj>();
            for (int i = 1; i <= MaxUserSlots; i++)
            {
                string slotId = "slot" + i;
                bool   isUsed = existing.Contains(slotId);
                list.Add(new OptionsObj
                {
                    Display    = isUsed ? $"{i} {slotId}" : $"{i} Empty",
                    SlotId     = slotId,
                    SlotIsUsed = isUsed
                });
            }
            list.Add(new OptionsObj { Display = "Back", OptionIsBack = true });
            return list;
        }
    }
}
