#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FrostyPlatformer.Global;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.Engine.MonoGame
{
    /// <summary>
    /// MonoGame-implementationen av IInputProvider — den enda klassen i projektet
    /// som anropar Microsoft.Xna.Framework.Input för tangentbord och gamepad.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter
    ///
    /// MOTIVERING:
    /// Spelkoden beror på IInputProvider och känner inte till MonoGame. Alla
    /// tangentnamnsmappningar och gamepad-mappningar är isolerade hit.
    ///
    /// TANGENTMAPPNING (bibehållen från RaylibInputProvider):
    ///   Rörelse   : piltangenter
    ///   Hoppa     : Upp / Space
    ///   Bekräfta  : Space / X / S
    ///   Avbryt    : Escape / P
    ///   Springa   : Z / B
    ///
    /// GAMEPAD (Xbox-layout, PlayerIndex.One):
    ///   D-Pad / vänster analog : rörelseknappar (inkl. upp för meny/karta)
    ///   A (hoppa/bekräfta)     : Buttons.A
    ///   X (springa)            : Buttons.X  — ersätter NES-B
    ///   Back  (select)         : Buttons.Back
    ///   Start (paus)           : Buttons.Start
    ///
    /// PRESSED-LOGIK:
    /// MonoGame saknar direkt "just tryckt"-API. Klassen håller föregående frames
    /// KeyboardState och GamePadState för att beräkna IsKeyPressed = nedtryckt nu
    /// men inte i förra framen. Poll() uppdaterar båda states — anropas en gång per frame.
    ///
    /// ANVÄNDNING:
    /// Skapas i FrostyGame.Initialize() (Composition Root) och injiceras i GameServices.
    /// Konstruktorn tar en Func&lt;bool&gt; som returnerar om spelfönstret är aktivt
    /// (anslut med () => this.IsActive i FrostyGame).
    /// </remarks>
    public class MonoGameInputProvider : IInputProvider
    {
        private KeyboardState _current;
        private KeyboardState _previous;
        private GamePadState  _pad;
        private GamePadState  _prevPad;
        private MouseState    _mouse;
        private MouseState    _prevMouse;

        private readonly Func<bool> _isWindowActive;

        // ─── Hopprelaterat tillstånd ───────────────────────────────────────────
        public int  JumpButtonState           { get; set; }
        public bool JumpButtonPressRelease    { get; set; }
        public bool JumpButtonDownRelease     { get; set; }
        public bool JumpButtonDownReleaseOnce { get; set; }
        public int  JumpButtonCounter         { get; set; }

        // ─── Idle-tillstånd ───────────────────────────────────────────────────
        public bool ButtonsHasGoneIdle { get; set; }

        /// <summary>
        /// Skapar en ny input-provider.
        /// </summary>
        /// <param name="isWindowActive">
        /// Returnerar true om spelfönstret är aktivt (koppla till FrostyGame.IsActive).
        /// </param>
        public MonoGameInputProvider(Func<bool> isWindowActive)
        {
            _isWindowActive = isWindowActive;
            _current    = Keyboard.GetState();
            _previous   = _current;
            _pad        = GamePad.GetState(PlayerIndex.One);
            _prevPad    = _pad;
            _mouse      = Mouse.GetState();
            _prevMouse  = _mouse;
        }

        // ─── Uppdatering ──────────────────────────────────────────────────────

        /// <summary>
        /// Sparar föregående frame och läser ny tangentbord + gamepad-snapshot.
        /// Kallas en gång i början av varje frame.
        /// </summary>
        public void Poll()
        {
            _previous   = _current;
            _current    = Keyboard.GetState();
            _prevPad    = _pad;
            _pad        = GamePad.GetState(PlayerIndex.One);
            _prevMouse  = _mouse;
            _mouse      = Mouse.GetState();
            UpdateJumpButtonState();
        }

        private void UpdateJumpButtonState()
        {
            bool jumpDown = IsJumpDown;

            if (jumpDown)
            {
                if (JumpButtonState < 3) JumpButtonState++;
                JumpButtonCounter++;
                JumpButtonPressRelease = true;
            }
            else
            {
                JumpButtonState   = 0;
                JumpButtonCounter = 0;

                if (JumpButtonPressRelease)
                {
                    JumpButtonDownRelease  = true;
                    JumpButtonPressRelease = false;
                }
            }

            if (IsJumpPressed && !JumpButtonDownReleaseOnce)
                JumpButtonDownReleaseOnce = true;
        }

        // ─── Hjälpmetoder ─────────────────────────────────────────────────────

        private bool KeyDown(Keys key)
            => _current.IsKeyDown(key);

        private bool KeyPressed(Keys key)
            => _current.IsKeyDown(key) && _previous.IsKeyUp(key);

        private bool KeyReleased(Keys key)
            => _current.IsKeyUp(key) && _previous.IsKeyDown(key);

        private bool PadDown(Buttons btn)
            => _pad.IsConnected && _pad.IsButtonDown(btn);

        private bool PadPressed(Buttons btn)
            => _pad.IsConnected && _pad.IsButtonDown(btn) && !_prevPad.IsButtonDown(btn);

        // ─── Rörelseåtgärder ──────────────────────────────────────────────────
        public bool IsRightDown     => KeyDown(Keys.Right)    || PadDown(Buttons.DPadRight);
        public bool IsLeftDown      => KeyDown(Keys.Left)     || PadDown(Buttons.DPadLeft);
        public bool IsUpDown        => KeyDown(Keys.Up)       || PadDown(Buttons.DPadUp);
        public bool IsDownDown      => KeyDown(Keys.Down)     || PadDown(Buttons.DPadDown);

        public bool IsRightPressed  => KeyPressed(Keys.Right)  || PadPressed(Buttons.DPadRight);
        public bool IsLeftPressed   => KeyPressed(Keys.Left)   || PadPressed(Buttons.DPadLeft);
        public bool IsUpPressed     => KeyPressed(Keys.Up)     || PadPressed(Buttons.DPadUp);
        public bool IsDownPressed   => KeyPressed(Keys.Down)   || PadPressed(Buttons.DPadDown);

        public bool IsRightReleased => KeyReleased(Keys.Right);
        public bool IsLeftReleased  => KeyReleased(Keys.Left);
        public bool IsUpReleased    => KeyReleased(Keys.Up);
        public bool IsDownReleased  => KeyReleased(Keys.Down);

        // ─── Actionknappar ────────────────────────────────────────────────────
        public bool IsJumpDown     => KeyDown(Keys.Up)    || KeyDown(Keys.Space)
                                   || PadDown(Buttons.A);

        public bool IsJumpPressed  => KeyPressed(Keys.Up) || KeyPressed(Keys.Space)
                                   || PadPressed(Buttons.A);

        public bool IsJumpReleased => KeyReleased(Keys.Up) || KeyReleased(Keys.Space);

        public bool IsConfirmPressed => KeyPressed(Keys.Enter) || KeyPressed(Keys.Space)
                                     || KeyPressed(Keys.X)     || KeyPressed(Keys.S)
                                     || PadPressed(Buttons.A);

        public bool IsCancelPressed  => KeyPressed(Keys.Escape) || KeyPressed(Keys.P)
                                     || PadPressed(Buttons.Start);

        public bool IsPausePressed   => KeyPressed(Keys.Escape) || KeyPressed(Keys.P)
                                     || PadPressed(Buttons.Start);

        public bool IsRunDown        => KeyDown(Keys.Z)    || KeyDown(Keys.B)
                                     || PadDown(Buttons.X);

        public bool IsSelectDown     => PadDown(Buttons.Back);

        /// <summary>
        /// True om minst en tangent övergick från upp till ner den här framen.
        /// Ersätter Raylibas GetKeyPressed() != 0.
        /// </summary>
        public bool IsAnyKeyPressed
        {
            get
            {
                foreach (Keys key in _current.GetPressedKeys())
                    if (_previous.IsKeyUp(key)) return true;
                return false;
            }
        }

        // ─── Editorn ──────────────────────────────────────────────────────────
        public bool IsEditorToggleCollision => KeyPressed(Keys.C);
        public bool IsEditorToggleSpawn     => KeyPressed(Keys.G);
        public bool IsEditorToggleGoal      => KeyPressed(Keys.T);
        public bool IsEditorTogglePickup    => KeyPressed(Keys.P);
        public bool IsEditorSave            => KeyPressed(Keys.S) && KeyDown(Keys.LeftControl);
        public bool IsEditorLoad            => KeyPressed(Keys.L);
        public bool IsEditorNew             => KeyPressed(Keys.N);
        public bool IsEditorUndoDown        => KeyDown(Keys.U);
        public bool IsEditorUndoPressed     => KeyPressed(Keys.U);

        // ─── Mus-input ────────────────────────────────────────────────────────
        // Mouse.GetState() returnerar fysiska skärmpixlar. Spellogiken arbetar i
        // logiska pixlar (fysisk / PixelWidth). Dela med scale-faktorn för att matcha.
        public int  MouseX             => _mouse.X / GameConstants.PixelWidth;
        public int  MouseY             => _mouse.Y / GameConstants.PixelHeight;
        public bool IsMouseLeftDown    => _mouse.LeftButton  == ButtonState.Pressed;
        public bool IsMouseRightDown   => _mouse.RightButton == ButtonState.Pressed;
        public bool IsMouseLeftPressed =>
            _mouse.LeftButton  == ButtonState.Pressed &&
            _prevMouse.LeftButton  == ButtonState.Released;
        public bool IsMouseRightPressed =>
            _mouse.RightButton == ButtonState.Pressed &&
            _prevMouse.RightButton == ButtonState.Released;
        public int  MouseScrollDelta   => _mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;

        // ─── Fönsterfokus ─────────────────────────────────────────────────────
        public bool IsWindowFocused => _isWindowActive();

        // ─── Idle ─────────────────────────────────────────────────────────────

        /// <summary>
        /// True om ingen rörelse- eller actionknapp är nedtryckt och ingen tangent
        /// just trycktes ner denna frame.
        /// </summary>
        public bool IsIdle =>
            !IsRightDown && !IsLeftDown && !IsUpDown    && !IsDownDown &&
            !IsJumpDown  && !IsRunDown  && !IsSelectDown &&
            !IsAnyKeyPressed;

        /// <summary>Nollställer idle-flaggan.</summary>
        public void ResetIdle() => ButtonsHasGoneIdle = false;
    }
}
