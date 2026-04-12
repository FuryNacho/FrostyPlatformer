#nullable enable
using System;
using Gamepad.Library;
using FrostyPlatformer.Global;
using PixelEngine;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Kapslar in tangentbords- och gamepad-input och exponerar semantiska spelåtgärder.
    /// Implementerar IInputProvider för att möjliggöra testning utan hårdvara.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter
    ///
    /// MOTIVERING:
    /// PixelEngine och SlimDX exponerar råa knapptillstånd kopplade till specifika
    /// tangentbords- och gamepad-API:er. InputManager adapterar dessa till ett
    /// speldomänspecifikt interface (IsJumpPressed, IsConfirmPressed osv.) vilket
    /// bryter beroendet mot konkret hårdvara i resten av koden (DIP).
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() och skickas till GameServices. States anropar
    /// IInputProvider via _services.Input — aldrig InputManager direkt.
    /// I tester används FakeInputProvider för hårdvarufri simulering.
    /// </remarks>
    public class InputManager : IInputProvider
    {
        private readonly Game _game;
        private readonly SlimDXGamepad? _gamepad;
        private IsItPressed _iip;

        // ─────────────────────────────────────────────
        // Hopprelaterat tillstånd (komplex logik bevarad intakt)
        // ─────────────────────────────────────────────

        /// <summary>
        /// 0 = spelaren har släppt hoppknapp.
        /// 1–3 = antal frames spelaren hållt nere hoppknappen.
        /// </summary>
        public int  JumpButtonState           { get; set; }   // set: Program.cs justerar per frame
        public bool JumpButtonPressRelease    { get; set; }   // set: jump-systemet nollställer
        public bool JumpButtonDownRelease     { get; set; }   // set: jump-systemet nollställer
        public bool JumpButtonDownReleaseOnce { get; set; }   // set: jump-systemet nollställer
        public int  JumpButtonCounter         { get; set; }   // set: jump-systemet nollställer

        // ─────────────────────────────────────────────
        // Idle-tillstånd (menyer väntar på att knapparna gått i vila)
        // ─────────────────────────────────────────────
        public bool ButtonsHasGoneIdle { get; set; }

        public InputManager(Game game)
        {
            _game = game;
            try
            {
                _gamepad = new SlimDXGamepad();
                _gamepad.SetUp();
                _iip = _gamepad.IIP;
                _gamepad.timer_Tick();
            }
            catch (Exception)
            {
                // Gamepad-biblioteket (SlimDX) ej tillgängligt – kör med tangentbord.
                // Kräver VC++ 2010 x86 runtime (vcredist_x86.exe) för gamepad-stöd.
                _gamepad = null;
                _iip = new IsItPressed();
            }
        }

        // ─────────────────────────────────────────────
        // Uppdatering – anropas en gång i börjanden av varje frame
        // ─────────────────────────────────────────────

        /// <summary>
        /// Uppdaterar gamepad-tillståndet. Ska anropas i början av varje Display*-metod.
        /// </summary>
        public void Poll()
        {
            if (_gamepad != null)
            {
                _gamepad.timer_Tick();
                _iip = _gamepad.IIP;
            }
            UpdateJumpButtonState();
        }

        private void UpdateJumpButtonState()
        {
            bool jumpDown = _game.GetKey(Key.Up).Down || _iip.up ||
                            _game.GetKey(Key.Space).Down || _iip.Button0;

            bool jumpPressed = _game.GetKey(Key.Up).Pressed || _game.GetKey(Key.Space).Pressed;

            if (jumpDown)
            {
                if (JumpButtonState < 3) JumpButtonState++;
                JumpButtonCounter++;
                JumpButtonPressRelease = true;
            }
            else
            {
                JumpButtonState = 0;
                JumpButtonCounter = 0;

                if (JumpButtonPressRelease)
                {
                    JumpButtonDownRelease = true;
                    JumpButtonPressRelease = false;
                }
            }

            if (jumpPressed && !JumpButtonDownReleaseOnce)
                JumpButtonDownReleaseOnce = true;
        }

        // ─────────────────────────────────────────────
        // Rörelseåtgärder
        // ─────────────────────────────────────────────
        public bool IsRightDown   => _game.GetKey(Key.Right).Down  || _iip.right;
        public bool IsLeftDown    => _game.GetKey(Key.Left).Down   || _iip.left;
        public bool IsUpDown      => _game.GetKey(Key.Up).Down     || _iip.up;
        public bool IsDownDown    => _game.GetKey(Key.Down).Down   || _iip.down;

        public bool IsRightPressed  => _game.GetKey(Key.Right).Pressed;
        public bool IsLeftPressed   => _game.GetKey(Key.Left).Pressed;
        public bool IsUpPressed     => _game.GetKey(Key.Up).Pressed    || _iip.up;
        public bool IsDownPressed   => _game.GetKey(Key.Down).Pressed  || _iip.down;

        public bool IsRightReleased => _game.GetKey(Key.Right).Released || _iip.right;
        public bool IsLeftReleased  => _game.GetKey(Key.Left).Released  || _iip.left;
        public bool IsUpReleased    => _game.GetKey(Key.Up).Released    || _iip.up;
        public bool IsDownReleased  => _game.GetKey(Key.Down).Released  || _iip.down;

        // ─────────────────────────────────────────────
        // Actionknappar
        // ─────────────────────────────────────────────
        public bool IsJumpDown      => _game.GetKey(Key.Up).Down    || _iip.up ||
                                       _game.GetKey(Key.Space).Down || _iip.Button0;
        public bool IsJumpPressed   => _game.GetKey(Key.Up).Pressed  || _game.GetKey(Key.Space).Pressed;
        public bool IsJumpReleased  => _game.GetKey(Key.Up).Released || _game.GetKey(Key.Space).Released;

        public bool IsConfirmPressed => _game.GetKey(Key.Space).Pressed || _game.GetKey(Key.X).Pressed ||
                                       _game.GetKey(Key.S).Pressed   || _iip.Button0;
        public bool IsCancelPressed  => _game.GetKey(Key.Escape).Pressed || _game.GetKey(Key.P).Pressed || _iip.Button7;
        public bool IsPausePressed   => _game.GetKey(Key.Escape).Pressed || _game.GetKey(Key.P).Pressed || _iip.Button7;

        public bool IsRunDown        => _game.GetKey(Key.Z).Down || _game.GetKey(Key.B).Down || _iip.Button1;
        public bool IsSelectDown     => _iip.Button6;   // gamepad Select-knapp
        public bool IsRunPressed     => _game.GetKey(Key.Z).Pressed || _iip.Button1;

        public bool IsAnyKeyPressed  => _game.GetKey(Key.Any).Pressed;

        // ─────────────────────────────────────────────
        // Fönsterfokus
        // ─────────────────────────────────────────────
        public bool IsWindowFocused  => _game.Focus;

        // ─────────────────────────────────────────────
        // Dev / debug
        // ─────────────────────────────────────────────
        public bool IsDevSkipPressed => _game.GetKey(Key.F1).Pressed;

        // ─────────────────────────────────────────────
        // Idle-hjälpare (används i menylogik)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Returnerar true om gamepad är i vila och ingen tangent är nedtryckt.
        /// </summary>
        public bool IsIdle => _iip.idle && !_game.GetKey(Key.Any).Pressed;

        /// <summary>
        /// Nollställer idle-flaggan (anropas när en åtgärd registreras i menyerna).
        /// </summary>
        public void ResetIdle() => ButtonsHasGoneIdle = false;
    }
}
