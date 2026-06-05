#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Varifrån pausen öppnades — styr vilka val som visas och vart "Resume" återvänder.
    /// </summary>
    internal enum PauseOrigin
    {
        Gameplay,
        WorldMap
    }

    /// <summary>
    /// Den gemensamma pausmenyn. Öppnas både från en bana (GameplayState) och från
    /// världskartan (WorldMapState) och har identiska kontroller oavsett ursprung —
    /// upp/ner navigerar, A/Enter väljer, B/Esc/Start återupptar. Innehållet i listan
    /// anpassas efter var pausen öppnades.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Tidigare fanns två skilda pauslägen: en textoverlay i PauseState (under banan)
    /// och en separat list-meny i MenuState (från världskartan). De hade olika
    /// kontroller och kändes inkonsekventa. Den här klassen förenar dem till en enda
    /// pausmeny parametrerad av PauseOrigin så att "Resume" alltid återvänder rätt.
    ///
    /// ANVÄNDNING:
    /// Skapas med ett PauseOrigin. Navigering sker på riktnings-edges (en flytt per
    /// tryck), så ButtonsHasGoneIdle-flaggan lämnas orörd och Konami-detektorn fungerar
    /// precis som förr.
    /// </remarks>
    internal sealed class PauseState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;
        private readonly PauseOrigin _origin;

        private readonly List<string> _items;
        private int _selected;

        private KonamiObj _konami = new KonamiObj();

        public PauseState(GameServices services, PauseOrigin origin = PauseOrigin.Gameplay)
        {
            _services = services;
            _rc       = services.RenderContext;
            _origin   = origin;
            _items    = BuildItems(origin);
        }

        private static List<string> BuildItems(PauseOrigin origin)
        {
            return origin == PauseOrigin.WorldMap
                ? new List<string> { "Resume", "Save Game", "Quit to Menu" }
                : new List<string> { "Resume", "Quit to Map" };
        }

        public void Enter(GameContext context)
        {
            _selected = 0;
            _konami   = new KonamiObj();

            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundWorld);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundGame);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundEnd);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Input.Poll();

            if (!_services.Input.IsWindowFocused) return;

            // Återställ idle-flaggan som vanligt — Konami-detektorn nedan beror på den.
            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            // Återuppta: cancel (B/Esc) eller paus-toggle (Start) stänger pausen.
            if (_services.Input.IsCancelPressed || _services.Input.IsPausePressed)
            {
                Resume(context);
                return;
            }

            // Navigering på edges → en flytt per tryck, rör inte ButtonsHasGoneIdle.
            if (_services.Input.IsUpPressed)
                _selected = _selected <= 0 ? _items.Count - 1 : _selected - 1;
            if (_services.Input.IsDownPressed)
                _selected = _selected >= _items.Count - 1 ? 0 : _selected + 1;

            // Välj
            if (_services.Input.IsConfirmPressed)
            {
                HandleSelection(_items[_selected], context);
                return;
            }

            // Konami-koden lever kvar oförändrad och delar Upp/Ner-trycken med markören.
            if (_services.Input.ButtonsHasGoneIdle && !_services.Input.IsIdle)
                HandleKonami(context);
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            const int FontWidth = 8;
            HudRenderer.Draw(_rc, context);

            string title = "Pause";
            int tx = (context.ScreenWidth / 2) - ((title.Length * FontWidth) / 2);
            _rc.DrawText(title, tx, 25);

            for (int i = 0; i < _items.Count; i++)
            {
                string row = (i == _selected) ? "> " + _items[i] + " <" : _items[i];
                int rx = (context.ScreenWidth / 2) - ((row.Length * FontWidth) / 2);
                _rc.DrawText(row, rx, 45 + i * 12);
            }
        }

        public void Exit(GameContext context) { }

        // ── Navigering ────────────────────────────────────────────────────────────

        private void Resume(GameContext context)
        {
            _services.Input.ButtonsHasGoneIdle = false;
            IGameState next = _origin == PauseOrigin.WorldMap
                ? new WorldMapState(_services)
                : new GameplayState(_services);
            _services.StateManager.Transition(next, context);
        }

        private void HandleSelection(string selected, GameContext context)
        {
            _services.Input.ButtonsHasGoneIdle = false;
            switch (selected)
            {
                case "Resume":
                    Resume(context);
                    break;

                case "Quit to Map":
                    _services.StateManager.Transition(new WorldMapState(_services), context);
                    break;

                case "Save Game":
                    context.MenuNavigation = Enum.MenuState.Save;
                    _services.StateManager.Transition(new SettingsState(_services), context);
                    break;

                case "Quit to Menu":
                    context.MenuNavigation = Enum.MenuState.StartMenu;
                    _services.StateManager.Transition(new MenuState(_services), context);
                    break;
            }
        }

        // ── Konami-kod ───────────────────────────────────────────────────────────
        private void HandleKonami(GameContext context)
        {
            if (_services.Input.IsUpDown || _konami.up)
            {
                if (!_konami.up) { _konami.up = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                if (_services.Input.IsUpDown || _konami.upUp)
                {
                    if (!_konami.upUp) { _konami.upUp = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                    if (_services.Input.IsDownDown || _konami.down)
                    {
                        if (!_konami.down) { _konami.down = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                        if (_services.Input.IsDownDown || _konami.downDown)
                        {
                            if (!_konami.downDown) { _konami.downDown = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                            if (_services.Input.IsLeftDown || _konami.left)
                            {
                                if (!_konami.left) { _konami.left = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                                if (_services.Input.IsRightDown || _konami.right)
                                {
                                    if (!_konami.right) { _konami.right = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                                    if (_services.Input.IsLeftDown || _konami.leftLeft)
                                    {
                                        if (!_konami.leftLeft) { _konami.leftLeft = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                                        if (_services.Input.IsRightDown || _konami.rightRight)
                                        {
                                            if (!_konami.rightRight) { _konami.rightRight = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                                            if (_services.Input.IsRunDown || _konami.B)
                                            {
                                                if (!_konami.B) { _konami.B = true; _services.Input.ButtonsHasGoneIdle = false; return; }
                                                if (_services.Input.IsJumpDown || _konami.A)
                                                {
                                                    _konami.A = true;
                                                    _services.Input.ButtonsHasGoneIdle = false;
                                                    // Konami-effekt: låser upp alla banor
                                                    context.ActualTotalTime += new System.TimeSpan(1, 0, 0);
                                                    _services.StateManager.Transition(new WorldMapState(_services, unlockAll: true), context);
                                                    _konami.nope();
                                                    return;
                                                }
                                                else _konami.nope();
                                            }
                                            else _konami.nope();
                                        }
                                        else _konami.nope();
                                    }
                                    else _konami.nope();
                                }
                                else _konami.nope();
                            }
                            else _konami.nope();
                        }
                        else _konami.nope();
                    }
                    else _konami.nope();
                }
                else _konami.nope();
            }
            else _konami.nope();
        }
    }
}
