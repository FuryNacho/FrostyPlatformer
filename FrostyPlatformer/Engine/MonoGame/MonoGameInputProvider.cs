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

        private bool PadRelease(Buttons btn)
            => _pad.IsConnected && !_pad.IsButtonDown(btn) && _prevPad.IsButtonDown(btn);

        // ─── Analogspak-hjälpare (vänster spak, med dödzon) ──────────────────
        private const float AnalogDeadZone = 0.25f;

        private bool AnalogRight     => _pad.IsConnected && _pad.ThumbSticks.Left.X      >  AnalogDeadZone;
        private bool AnalogLeft      => _pad.IsConnected && _pad.ThumbSticks.Left.X      < -AnalogDeadZone;
        private bool AnalogUp        => _pad.IsConnected && _pad.ThumbSticks.Left.Y      >  AnalogDeadZone;
        private bool AnalogDown      => _pad.IsConnected && _pad.ThumbSticks.Left.Y      < -AnalogDeadZone;
        private bool PrevAnalogRight => _pad.IsConnected && _prevPad.ThumbSticks.Left.X  >  AnalogDeadZone;
        private bool PrevAnalogLeft  => _pad.IsConnected && _prevPad.ThumbSticks.Left.X  < -AnalogDeadZone;
        private bool PrevAnalogUp    => _pad.IsConnected && _prevPad.ThumbSticks.Left.Y  >  AnalogDeadZone;
        private bool PrevAnalogDown  => _pad.IsConnected && _prevPad.ThumbSticks.Left.Y  < -AnalogDeadZone;

        // ─── Rörelseåtgärder ──────────────────────────────────────────────────
        public bool IsRightDown     => KeyDown(Keys.Right)    || PadDown(Buttons.DPadRight)    || AnalogRight;
        public bool IsLeftDown      => KeyDown(Keys.Left)     || PadDown(Buttons.DPadLeft)     || AnalogLeft;
        public bool IsUpDown        => KeyDown(Keys.Up)       || PadDown(Buttons.DPadUp)       || AnalogUp;
        public bool IsDownDown      => KeyDown(Keys.Down)     || PadDown(Buttons.DPadDown)     || AnalogDown;

        public bool IsRightPressed  => KeyPressed(Keys.Right)  || PadPressed(Buttons.DPadRight)  || (AnalogRight && !PrevAnalogRight);
        public bool IsLeftPressed   => KeyPressed(Keys.Left)   || PadPressed(Buttons.DPadLeft)   || (AnalogLeft  && !PrevAnalogLeft);
        public bool IsUpPressed     => KeyPressed(Keys.Up)     || PadPressed(Buttons.DPadUp)     || (AnalogUp    && !PrevAnalogUp);
        public bool IsDownPressed   => KeyPressed(Keys.Down)   || PadPressed(Buttons.DPadDown)   || (AnalogDown  && !PrevAnalogDown);

        public bool IsRightReleased => KeyReleased(Keys.Right) || PadRelease(Buttons.DPadRight)  || (!AnalogRight && PrevAnalogRight);
        public bool IsLeftReleased  => KeyReleased(Keys.Left)  || PadRelease(Buttons.DPadLeft)   || (!AnalogLeft  && PrevAnalogLeft);
        public bool IsUpReleased    => KeyReleased(Keys.Up)    || PadRelease(Buttons.DPadUp)     || (!AnalogUp    && PrevAnalogUp);
        public bool IsDownReleased  => KeyReleased(Keys.Down)  || PadRelease(Buttons.DPadDown)   || (!AnalogDown  && PrevAnalogDown);

        // ─── Actionknappar ────────────────────────────────────────────────────
        public bool IsJumpDown     => KeyDown(Keys.Up)    || KeyDown(Keys.Space)
                                   || PadDown(Buttons.A);

        public bool IsJumpPressed  => KeyPressed(Keys.Up) || KeyPressed(Keys.Space)
                                   || PadPressed(Buttons.A);

        public bool IsJumpReleased => KeyReleased(Keys.Up) || KeyReleased(Keys.Space) || PadRelease(Buttons.A);

        // Modern joypad-konvention: A = bekräfta, B = tillbaka, Start = paus.
        // Confirm/Cancel/Pause är medvetet åtskilda så att "backa" och "pausa"
        // aldrig kan bli samma sak (vilket gav inkonsekvent menynavigering förut).
        public bool IsConfirmPressed => KeyPressed(Keys.Enter) || KeyPressed(Keys.Space)
                                     || PadPressed(Buttons.A);

        public bool IsCancelPressed  => KeyPressed(Keys.Escape) || KeyPressed(Keys.Back)
                                     || PadPressed(Buttons.B);

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
        public bool IsEditorTogglePickup    => KeyPressed(Keys.I);
        public bool IsEditorToggleEnemy     => KeyPressed(Keys.E);
        public bool IsEditorToggleStopPoint => KeyPressed(Keys.W);
        public bool IsEditorSave            => KeyPressed(Keys.S) && KeyDown(Keys.LeftControl);
        public bool IsEditorLoad            => KeyPressed(Keys.L);
        public bool IsEditorNew             => KeyPressed(Keys.N);
        public bool IsEditorUndoDown        => KeyDown(Keys.U);
        public bool IsEditorUndoPressed     => KeyPressed(Keys.U);
        public bool IsEditorPreviewPlay     => KeyPressed(Keys.F5);

        // ─── Editorns gamepad-styrning ────────────────────────────────────────
        // Analogspaken driver markören (som musen); d-pad/piltangenter scrollar
        // kameran. De hålls åtskilda här så editorn kan styra markör och kamera
        // oberoende — till skillnad från IsLeftDown m.fl. som slår ihop alla källor.
        public float LeftStickX => _pad.IsConnected ? DeadZone(_pad.ThumbSticks.Left.X) : 0f;
        public float LeftStickY => _pad.IsConnected ? DeadZone(_pad.ThumbSticks.Left.Y) : 0f;

        private static float DeadZone(float v) => Math.Abs(v) < AnalogDeadZone ? 0f : v;

        public bool IsEditorScrollLeft  => KeyDown(Keys.Left)  || PadDown(Buttons.DPadLeft);
        public bool IsEditorScrollRight => KeyDown(Keys.Right) || PadDown(Buttons.DPadRight);
        public bool IsEditorScrollUp    => KeyDown(Keys.Up)    || PadDown(Buttons.DPadUp);
        public bool IsEditorScrollDown  => KeyDown(Keys.Down)  || PadDown(Buttons.DPadDown);

        public bool IsEditorPrimaryDown      => PadDown(Buttons.A);
        public bool IsEditorPrimaryPressed   => PadPressed(Buttons.A);
        public bool IsEditorSecondaryDown    => PadDown(Buttons.X);
        public bool IsEditorSecondaryPressed => PadPressed(Buttons.X);

        // ─── Mus-input ────────────────────────────────────────────────────────
        // Mouse.GetState() returnerar fysiska skärmpixlar. Spellogiken arbetar i
        // logiska pixlar. Med virtuell upplösning (letterbox) ritas spelet i en
        // centrerad, skalad ruta på skärmen, så musen måste kompenseras för både
        // offset (svarta kanter) och skala. FrostyGame sätter transformen varje gång
        // skärmstorleken ändras. Standard (offset 0, skala PixelWidth) = ingen
        // letterbox → identiskt med tidigare beteende (fönster i designstorlek).
        public int   ViewportOffsetX { get; set; }
        public int   ViewportOffsetY { get; set; }
        public float ViewportScale   { get; set; } = GameConstants.PixelWidth;

        public int  MouseX             => (int)((_mouse.X - ViewportOffsetX) / ViewportScale);
        public int  MouseY             => (int)((_mouse.Y - ViewportOffsetY) / ViewportScale);
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
