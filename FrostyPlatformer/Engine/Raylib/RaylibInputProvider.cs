#nullable enable
using Raylib_cs;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.Engine.Raylib
{
    /// <summary>
    /// Raylib-implementationen av IInputProvider — den enda klassen i projektet
    /// som anropar Raylib-cs för tangentbord och gamepad-input.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter
    ///
    /// MOTIVERING:
    /// Ersätter InputManager (PixelEngine + SlimDX). Spelkoden beror på IInputProvider
    /// och känner inte till Raylib. Alla tangentnamnsmappningar och gamepad-mappningar
    /// är isolerade hit — ett nytt input-bibliotek kräver bara en ny implementation.
    ///
    /// TANGENTMAPPNING (bibehållen från InputManager):
    ///   Rörelse   : piltangenter
    ///   Hoppa     : Upp / Space
    ///   Bekräfta  : Space / X / S
    ///   Avbryt    : Escape / P
    ///   Springa   : Z / B
    ///
    /// GAMEPAD (Xbox-layout, port 0):
    ///   D-Pad     : LeftFace*
    ///   A (hoppa/bekräfta) : RightFaceDown
    ///   B (springa)        : RightFaceRight
    ///   Select             : MiddleLeft
    ///   Start (paus)       : MiddleRight
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.cs (Composition Root) och injiceras i GameServices.
    /// Poll() anropas en gång i början av varje frame av spelloopen.
    /// </remarks>
    public class RaylibInputProvider : IInputProvider
    {
        private const int GamepadId = 0;

        // ─── Hopprelaterat tillstånd ───────────────────────────────────────────
        public int  JumpButtonState           { get; set; }
        public bool JumpButtonPressRelease    { get; set; }
        public bool JumpButtonDownRelease     { get; set; }
        public bool JumpButtonDownReleaseOnce { get; set; }
        public int  JumpButtonCounter         { get; set; }

        // ─── Idle-tillstånd ───────────────────────────────────────────────────
        public bool ButtonsHasGoneIdle { get; set; }

        // ─── Uppdatering ──────────────────────────────────────────────────────

        /// <summary>
        /// Uppdaterar hoppknapp-tillståndet. Kallas en gång i början av varje frame.
        /// </summary>
        public void Poll() => UpdateJumpButtonState();

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

        private static bool GamepadDown(GamepadButton btn)
            => Raylib_cs.Raylib.IsGamepadAvailable(GamepadId) &&
               Raylib_cs.Raylib.IsGamepadButtonDown(GamepadId, btn);

        private static bool GamepadPressed(GamepadButton btn)
            => Raylib_cs.Raylib.IsGamepadAvailable(GamepadId) &&
               Raylib_cs.Raylib.IsGamepadButtonPressed(GamepadId, btn);

        // ─── Rörelseåtgärder ──────────────────────────────────────────────────
        public bool IsRightDown     => Raylib_cs.Raylib.IsKeyDown(KeyboardKey.Right)    || GamepadDown(GamepadButton.LeftFaceRight);
        public bool IsLeftDown      => Raylib_cs.Raylib.IsKeyDown(KeyboardKey.Left)     || GamepadDown(GamepadButton.LeftFaceLeft);
        public bool IsUpDown        => Raylib_cs.Raylib.IsKeyDown(KeyboardKey.Up)       || GamepadDown(GamepadButton.LeftFaceUp);
        public bool IsDownDown      => Raylib_cs.Raylib.IsKeyDown(KeyboardKey.Down)     || GamepadDown(GamepadButton.LeftFaceDown);

        public bool IsRightPressed  => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Right) || GamepadPressed(GamepadButton.LeftFaceRight);
        public bool IsLeftPressed   => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Left)  || GamepadPressed(GamepadButton.LeftFaceLeft);
        public bool IsUpPressed     => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Up)    || GamepadPressed(GamepadButton.LeftFaceUp);
        public bool IsDownPressed   => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Down)  || GamepadPressed(GamepadButton.LeftFaceDown);

        public bool IsRightReleased => Raylib_cs.Raylib.IsKeyReleased(KeyboardKey.Right);
        public bool IsLeftReleased  => Raylib_cs.Raylib.IsKeyReleased(KeyboardKey.Left);
        public bool IsUpReleased    => Raylib_cs.Raylib.IsKeyReleased(KeyboardKey.Up);
        public bool IsDownReleased  => Raylib_cs.Raylib.IsKeyReleased(KeyboardKey.Down);

        // ─── Actionknappar ────────────────────────────────────────────────────
        public bool IsJumpDown      => Raylib_cs.Raylib.IsKeyDown(KeyboardKey.Up)       || Raylib_cs.Raylib.IsKeyDown(KeyboardKey.Space)
                                    || GamepadDown(GamepadButton.LeftFaceUp)             || GamepadDown(GamepadButton.RightFaceDown);

        public bool IsJumpPressed   => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Up)    || Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Space)
                                    || GamepadPressed(GamepadButton.LeftFaceUp)          || GamepadPressed(GamepadButton.RightFaceDown);

        public bool IsJumpReleased  => Raylib_cs.Raylib.IsKeyReleased(KeyboardKey.Up)   || Raylib_cs.Raylib.IsKeyReleased(KeyboardKey.Space);

        public bool IsConfirmPressed => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.X)
                                     || Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.S)     || GamepadPressed(GamepadButton.RightFaceDown);

        public bool IsCancelPressed  => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.P)
                                     || GamepadPressed(GamepadButton.MiddleRight);

        public bool IsPausePressed   => Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.P)
                                     || GamepadPressed(GamepadButton.MiddleRight);

        public bool IsRunDown        => Raylib_cs.Raylib.IsKeyDown(KeyboardKey.Z)    || Raylib_cs.Raylib.IsKeyDown(KeyboardKey.B)
                                     || GamepadDown(GamepadButton.RightFaceRight);

        public bool IsSelectDown     => GamepadDown(GamepadButton.MiddleLeft);

        public bool IsAnyKeyPressed  => Raylib_cs.Raylib.GetKeyPressed() != 0;

        // ─── Fönsterfokus ─────────────────────────────────────────────────────
        public bool IsWindowFocused  => Raylib_cs.Raylib.IsWindowFocused();

        // ─── Idle ─────────────────────────────────────────────────────────────

        /// <summary>
        /// True om ingen rörelse- eller actionknapp är nedtryckt just nu.
        /// Används av menylogik för att kräva att spelaren släpper knapparna
        /// innan ny navigering registreras.
        /// </summary>
        public bool IsIdle =>
            !IsRightDown && !IsLeftDown && !IsUpDown    && !IsDownDown &&
            !IsJumpDown  && !IsRunDown  && !IsSelectDown &&
            Raylib_cs.Raylib.GetKeyPressed() == 0;

        /// <summary>Nollställer idle-flaggan.</summary>
        public void ResetIdle() => ButtonsHasGoneIdle = false;
    }
}
