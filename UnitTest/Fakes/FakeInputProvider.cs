using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Falsk input-implementation för enhetstester.
    /// Alla knappar är avstängda som standard; sätt egenskaper
    /// explicit per test för att simulera input.
    /// </summary>
    public class FakeInputProvider : IInputProvider
    {
        // Rörelseknappar
        public bool IsRightDown     { get; set; }
        public bool IsLeftDown      { get; set; }
        public bool IsUpDown        { get; set; }
        public bool IsDownDown      { get; set; }
        public bool IsRightPressed  { get; set; }
        public bool IsLeftPressed   { get; set; }
        public bool IsUpPressed     { get; set; }
        public bool IsDownPressed   { get; set; }
        public bool IsRightReleased { get; set; }
        public bool IsLeftReleased  { get; set; }
        public bool IsUpReleased    { get; set; }
        public bool IsDownReleased  { get; set; }

        // Actionknappar
        public bool IsJumpDown       { get; set; }
        public bool IsJumpPressed    { get; set; }
        public bool IsJumpReleased   { get; set; }
        public bool IsConfirmPressed { get; set; }
        public bool IsCancelPressed  { get; set; }
        public bool IsPausePressed   { get; set; }
        public bool IsRunDown        { get; set; }
        public bool IsSelectDown     { get; set; }
        public bool IsAnyKeyPressed  { get; set; }

        // Hoppknappens tillstånd
        public int  JumpButtonState           { get; set; }
        public bool JumpButtonPressRelease    { get; set; }
        public bool JumpButtonDownRelease     { get; set; }
        public bool JumpButtonDownReleaseOnce { get; set; }
        public int  JumpButtonCounter         { get; set; }

        // Editorn
        public bool IsEditorToggleCollision { get; set; }

        // Mus-input
        public int  MouseX              { get; set; }
        public int  MouseY              { get; set; }
        public bool IsMouseLeftDown     { get; set; }
        public bool IsMouseRightDown    { get; set; }
        public bool IsMouseLeftPressed  { get; set; }
        public bool IsMouseRightPressed { get; set; }
        public int  MouseScrollDelta    { get; set; }

        // Fönsterfokus
        public bool IsWindowFocused    { get; set; } = true;

        // Idle
        public bool IsIdle             { get; set; }
        public bool ButtonsHasGoneIdle { get; set; }
        public void ResetIdle()        => ButtonsHasGoneIdle = false;

        // Poll gör ingenting i fake – tillståndet sätts manuellt
        public void Poll() { }
    }
}
