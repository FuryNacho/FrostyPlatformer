#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar spelets huvudmeny och alla sub-menyer (StartMenu, PauseMenu,
    /// SettingsMenu, CreditsMenu).
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

        private int _selectedItem = 1;

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

            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            string header = "Menu";
            var menuList  = BuildMenuList(context, ref header);

            // Rita
            int hx = (context.ScreenWidth / 2) - ((header.Length * 8) / 2);
            _rc.DrawText(header, hx, 4);

            // Centrera hela menyblocket baserat på det längsta alternativet
            const int IconToTextGap = 25; // Avstånd från ikonens vänsterkant till textens vänsterkant
            int maxLen = 0;
            foreach (var item in menuList)
                if (item.Length > maxLen) maxLen = item.Length;
            int screenX = (context.ScreenWidth / 2) - (IconToTextGap + maxLen * GameConstants.FontCharWidth) / 2;

            for (int i = 0; i < menuList.Count; i++)
            {
                int screenY = 20 + i * 20;
                int srcX    = (i + 1 == _selectedItem) ? 0 : 16;
                _rc.DrawPartialSprite(SpriteId.Items, screenX, screenY, srcX, 48, 16, 16);
                _rc.DrawText(menuList[i], screenX + 25, screenY + 5);
            }

            // Input
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
            if (_selectedItem < menuList.Count && _services.Input.IsDownPressed && _services.Input.ButtonsHasGoneIdle)
            {
                _selectedItem++;
                _services.Input.ButtonsHasGoneIdle = false;
            }

            // Välj
            if (_services.Input.ButtonsHasGoneIdle &&
                (_services.Input.IsCancelPressed || _services.Input.IsConfirmPressed))
            {
                _services.Input.ButtonsHasGoneIdle = false;
                HandleSelection(menuList[_selectedItem - 1], context);
            }
        }

        public void Draw(IRenderContext renderContext) { }

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

                case Enum.MenuState.PauseMenu:
                    return new List<string> { "Resume", "Save", "Quit" };

                case Enum.MenuState.SettingsMenu:
                    header = "Menu - Settings";
                    return new List<string>
                    {
                        "Audio", "Clear High Score", "Clear Saved Game", "Back"
                    };

                case Enum.MenuState.CreditsMenu:
                    header = "Credits";
                    _selectedItem = 10; // sätt till sista (Back)
                    return new List<string>
                    {
                        "Developer:", "FuryNacho",
                        "olcPixelGameEngine:", "Javidx9",
                        //"Game Engine Port:", "DevChrome", //(Not used when upgrading to .net 8)
                        "Music and Sound:", "Fiskifickorna",
                        "", "Back"
                    };

                default:
                    return new List<string>();
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

                case "Resume":
                    _selectedItem = 1;
                    _services.StateManager.Transition(new WorldMapState(_services), context);
                    _services.Input.ButtonsHasGoneIdle = false;
                    break;

                case "Save":
                    _selectedItem = 1;
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.Save;
                    _services.StateManager.Transition(new SettingsState(_services), context);
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

                case "Back":
                    _selectedItem = 1;
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.StartMenu;
                    break;

                case "Quit":
                    context.MenuNavigation = Enum.MenuState.StartMenu;
                    _services.Input.ButtonsHasGoneIdle = false;
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
