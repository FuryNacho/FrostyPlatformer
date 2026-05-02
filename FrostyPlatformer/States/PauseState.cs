#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Pausskärmen som visas under gameplay — visar HUD + pause-text och lyssnar på
    /// resume/worldmap/Konami-input.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayPause (ca 205 rader). Isolerar KonamiObj
    /// som tidigare var ett fält i Program.cs.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från GameplayState (Escape/P). Escape/P återgår till GameplayState;
    /// Select/S går till WorldMapState. Konami-koden ger speciella effekter.
    /// </remarks>
    internal sealed class PauseState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        private KonamiObj _konami = new KonamiObj();

        public PauseState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            _konami = new KonamiObj();

            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundWorld);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundGame);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundEnd);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Input.Poll();

            if (!_services.Input.IsWindowFocused) return;

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            if (_services.Input.IsConfirmPressed || _services.Input.IsPausePressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(new WorldMapState(_services), context);
                return;
            }

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsCancelPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(new GameplayState(_services), context);
                return;
            }

            if (_services.Input.ButtonsHasGoneIdle && !_services.Input.IsIdle)
                HandleKonami(context);
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            HudRenderer.Draw(_rc, context, "pause");
        }

        public void Exit(GameContext context) { }

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
