#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar spelets huvudmeny och alla sub-menyer (StartMenu, SettingsMenu,
    /// CreditsMenu). Pausen hanteras separat av PauseState.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayMenu (ca 260 rader). Isolerar meny-logiken
    /// och den lokala selectedMenuItem-räknaren från resten av spelet.
    /// context.MenuNavigation styr vilken undermeny som visas och persisteras
    /// mellan state-övergångar.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från SplashState, SettingsState, WorldMapState, GameOverState och
    /// EndState. selectedMenuItem återställs i Enter() för att alltid börja överst.
    /// </remarks>
    internal sealed class MenuState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        private int          _selectedItem    = 1;
        private List<string> _currentMenuList = new List<string>();
        private string       _currentHeader   = "Menu";

        public MenuState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            _selectedItem = 1;
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Audio.PauseAll();
            _services.Input.Poll();

            _currentHeader   = "Menu";
            _currentMenuList = BuildMenuList(context, ref _currentHeader);

            if (!_services.Input.IsWindowFocused) return;

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            // Upp
            if (_selectedItem > 1 && _services.Input.IsUpPressed && _services.Input.ButtonsHasGoneIdle)
            {
                if (context.MenuNavigation != Enum.MenuState.CreditsMenu)
                {
                    _selectedItem--;
                    _services.Input.ButtonsHasGoneIdle = false;
                }
            }

            // Ner
            if (_selectedItem < _currentMenuList.Count && _services.Input.IsDownPressed && _services.Input.ButtonsHasGoneIdle)
            {
                _selectedItem++;
                _services.Input.ButtonsHasGoneIdle = false;
            }

            // Tillbaka (cancel) — backa konsekvent, aldrig samma sak som "välj"
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsCancelPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                HandleCancel(context);
                return;
            }

            // Välj (confirm)
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsConfirmPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                HandleSelection(_currentMenuList[_selectedItem - 1], context);
            }
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            int hx = (context.ScreenWidth / 2) - ((_currentHeader.Length * 8) / 2);
            _rc.DrawText(_currentHeader, hx, 4);

            // Centrera hela menyblocket baserat på det längsta alternativet
            const int IconToTextGap = 25; // Avstånd från ikonens vänsterkant till textens vänsterkant
            int maxLen = 0;
            foreach (var item in _currentMenuList)
                if (item.Length > maxLen) maxLen = item.Length;
            int screenX = (context.ScreenWidth / 2) - (IconToTextGap + maxLen * GameConstants.FontCharWidth) / 2;

            for (int i = 0; i < _currentMenuList.Count; i++)
            {
                int screenY = 20 + i * 20;
                int srcX    = (i + 1 == _selectedItem) ? 0 : 16;
                _rc.DrawPartialSprite(SpriteId.Items, screenX, screenY, srcX, 48, 16, 16);
                _rc.DrawText(_currentMenuList[i], screenX + 25, screenY + 5);
            }
        }

        public void Exit(GameContext context) { }

        // ── Hjälpmetoder ────────────────────────────────────────────────────────

        private List<string> BuildMenuList(GameContext context, ref string header)
        {
            switch (context.MenuNavigation)
            {
                case Enum.MenuState.StartMenu:
                    return new List<string>
                    {
                        "Start New Game", "Load Saved Game", "View High Score",
                        "Settings", "Credits", "Level Editor", "My Maps", "Exit Game"
                    };

                case Enum.MenuState.SettingsMenu:
                    header = "Menu - Settings";
                    return new List<string>
                    {
                        "Audio", "Screen", "Clear High Score", "Clear Saved Game", "Clear My Maps", "Back"
                    };

                case Enum.MenuState.CreditsMenu:
                    header = "Credits";
                    var credits = new List<string>
                    {
                        "Developer:", "FuryNacho",
                        "Music and Sound:", "Fiskifickorna",
                        //"olcPixelGameEngine:", "Javidx9",
                        //"Game Engine Port:", "DevChrome", //(Not used when upgrading to .net 8)
                        "",
                        "", "Back"
                    };
                    _selectedItem = credits.Count; // markören parkeras på sista raden ("Back")
                    return credits;

                default:
                    return new List<string>();
            }
        }

        /// <summary>
        /// Hanterar "tillbaka"-knappen (cancel) konsekvent för varje meny-läge.
        /// I huvudmenyn finns ingen förälder att backa till — då hoppar markören
        /// istället ner till "Exit Game". Undermenyer backar till huvudmenyn.
        /// </summary>
        private void HandleCancel(GameContext context)
        {
            switch (context.MenuNavigation)
            {
                case Enum.MenuState.StartMenu:
                    int exitIndex = _currentMenuList.IndexOf("Exit Game");
                    if (exitIndex >= 0) _selectedItem = exitIndex + 1;
                    break;

                case Enum.MenuState.SettingsMenu:
                case Enum.MenuState.CreditsMenu:
                    _selectedItem = 1;
                    context.MenuNavigation = Enum.MenuState.StartMenu;
                    break;
            }
        }

        private void HandleSelection(string selected, GameContext context)
        {
            switch (selected)
            {
                case "Start New Game":
                    _selectedItem = 1;
                    _services.Reset();
                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(new WorldMapState(_services), context);
                    break;

                case "Load Saved Game":
                    _selectedItem = 1;
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.Load;
                    _services.StateManager.Transition(new SettingsState(_services), context);
                    break;

                case "Settings":
                    _selectedItem = 1;
                    context.MenuNavigation = Enum.MenuState.SettingsMenu;
                    _services.Input.ButtonsHasGoneIdle = false;
                    break;

                case "Audio":
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.Audio;
                    _services.StateManager.Transition(new SettingsState(_services), context);
                    break;

                case "Screen":
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.Screen;
                    _services.StateManager.Transition(new SettingsState(_services), context);
                    break;

                case "Clear High Score":
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.ClearHighScore;
                    _services.StateManager.Transition(new SettingsState(_services), context);
                    break;

                case "Clear Saved Game":
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.ClearSavedGame;
                    _services.StateManager.Transition(new SettingsState(_services), context);
                    break;

                case "Clear My Maps":
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.ClearMyMaps;
                    _services.StateManager.Transition(new SettingsState(_services), context);
                    break;

                case "Back":
                    _selectedItem = 1;
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.StartMenu;
                    break;

                case "Level Editor":
                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(new EditorState(_services), context);
                    break;

                case "My Maps":
                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(new UserMapsState(_services), context);
                    break;

                case "Exit Game":
                    _services.Audio.CleanUp();
                    _services.ExitGame();
                    break;

                case "View High Score":
                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(new HighScoreState(_services), context);
                    break;

                case "Credits":
                    _selectedItem = 3;
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.CreditsMenu;
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine(
                        "MenuState.HandleSelection - okänt val: " + selected);
                    break;
            }
        }
    }
}
