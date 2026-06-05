#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Abstraktion över spelets input-källor (tangentbord + gamepad).
    /// Exponerar semantiska spelåtgärder istället för råa knapptillstånd.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (interface-sida)
    ///
    /// MOTIVERING:
    /// Högnivåkod (states, spellogik) beror på detta interface — inte på konkreta
    /// SlimDX- eller PixelEngine-klasser. Det gör det möjligt att testa all
    /// inputberoende logik utan hårdvara och möjliggör framtida byte av input-bibliotek
    /// utan att röra spelkoden (DIP).
    ///
    /// ANVÄNDNING:
    /// Implementeras av InputManager (produktion) och FakeInputProvider (tester).
    /// Injiceras i GameServices och sprids till alla states som behöver input.
    /// </remarks>
    public interface IInputProvider
    {
        // ─────────────────────────────────────────────
        // Rörelseknappar
        // ─────────────────────────────────────────────
        bool IsRightDown    { get; }
        bool IsLeftDown     { get; }
        bool IsUpDown       { get; }
        bool IsDownDown     { get; }

        bool IsRightPressed { get; }
        bool IsLeftPressed  { get; }
        bool IsUpPressed    { get; }
        bool IsDownPressed  { get; }

        bool IsRightReleased { get; }
        bool IsLeftReleased  { get; }
        bool IsUpReleased    { get; }
        bool IsDownReleased  { get; }

        // ─────────────────────────────────────────────
        // Actionknappar
        // ─────────────────────────────────────────────
        bool IsJumpDown     { get; }
        bool IsJumpPressed  { get; }
        bool IsJumpReleased { get; }

        bool IsConfirmPressed { get; }
        bool IsCancelPressed  { get; }
        bool IsPausePressed   { get; }
        bool IsRunDown        { get; }
        bool IsSelectDown     { get; }
        bool IsAnyKeyPressed  { get; }

        // ─────────────────────────────────────────────
        // Hoppknappens tillstånd (komplex logik)
        // ─────────────────────────────────────────────
        int  JumpButtonState           { get; set; }   // Program.cs hanterar dessa tillsammans med InputManager
        bool JumpButtonPressRelease    { get; set; }
        bool JumpButtonDownRelease     { get; set; }
        bool JumpButtonDownReleaseOnce { get; set; }
        int  JumpButtonCounter         { get; set; }

        // ─────────────────────────────────────────────
        // Idle-tillstånd
        // ─────────────────────────────────────────────
        bool IsIdle             { get; }
        bool ButtonsHasGoneIdle { get; set; }
        void ResetIdle();

        // ─────────────────────────────────────────────
        // Editorn
        // ─────────────────────────────────────────────

        /// <summary>
        /// True den frame som C-tangenten trycks ned. Används av EditorState
        /// för att växla mellan tile- och kollisionsredigeringsläge.
        /// </summary>
        bool IsEditorToggleCollision { get; }

        /// <summary>
        /// True den frame som G-tangenten trycks ned. Används av EditorState
        /// för att växla till spawn-redigeringsläge.
        /// </summary>
        bool IsEditorToggleSpawn { get; }

        /// <summary>
        /// True den frame som T-tangenten trycks ned. Används av EditorState
        /// för att växla till mål/portal-redigeringsläge.
        /// </summary>
        bool IsEditorToggleGoal { get; }

        /// <summary>
        /// True den frame som I-tangenten trycks ned. Används av EditorState
        /// för att växla till pickup/item-redigeringsläge.
        /// </summary>
        bool IsEditorTogglePickup { get; }

        /// <summary>
        /// True den frame som E-tangenten trycks ned. Används av EditorState
        /// för att växla till fiende-redigeringsläge.
        /// </summary>
        bool IsEditorToggleEnemy { get; }

        /// <summary>
        /// True den frame som W-tangenten trycks ned. Används av EditorState (DevMode)
        /// för att växla till stoppunkt-redigeringsläge för världskartan.
        /// </summary>
        bool IsEditorToggleStopPoint { get; }

        /// <summary>
        /// True den frame som Ctrl+S trycks ned. Används av EditorState för att spara kartan.
        /// </summary>
        bool IsEditorSave { get; }

        /// <summary>
        /// True den frame som L-tangenten trycks ned. Öppnar kartväljaren i EditorState.
        /// </summary>
        bool IsEditorLoad { get; }

        /// <summary>
        /// True den frame som N-tangenten trycks ned. Öppnar ny-karta-dialogen i EditorState.
        /// </summary>
        bool IsEditorNew { get; }

        /// <summary>
        /// True så länge U-tangenten hålls ned. Används för att radera/ångra i editorn
        /// på samma sätt som höger musknapp — alternativt kommando till RMB.
        /// </summary>
        bool IsEditorUndoDown { get; }

        /// <summary>
        /// True den frame U-tangenten trycks ned. Används för spawn-rensning (single press).
        /// </summary>
        bool IsEditorUndoPressed { get; }

        /// <summary>
        /// True den frame F5 trycks ned. Startar testspelning (preview) direkt från editorn.
        /// </summary>
        bool IsEditorPreviewPlay { get; }

        // ─────────────────────────────────────────────
        // Editorns gamepad-styrning
        // ─────────────────────────────────────────────

        /// <summary>
        /// Vänster analogspaks X-led, dödzonad (−1..1). Driver editorns virtuella
        /// markör (analogspaken fungerar som musen). 0 inom dödzonen.
        /// </summary>
        float LeftStickX { get; }

        /// <summary>
        /// Vänster analogspaks Y-led, dödzonad (−1..1, positiv = uppåt). Driver
        /// editorns virtuella markör. 0 inom dödzonen.
        /// </summary>
        float LeftStickY { get; }

        /// <summary>True medan kameran ska scrolla vänster (piltangent eller d-pad — ej analog).</summary>
        bool IsEditorScrollLeft { get; }

        /// <summary>True medan kameran ska scrolla höger (piltangent eller d-pad — ej analog).</summary>
        bool IsEditorScrollRight { get; }

        /// <summary>True medan kameran ska scrolla uppåt (piltangent eller d-pad — ej analog).</summary>
        bool IsEditorScrollUp { get; }

        /// <summary>True medan kameran ska scrolla nedåt (piltangent eller d-pad — ej analog).</summary>
        bool IsEditorScrollDown { get; }

        /// <summary>True medan gamepadens primärknapp (A) hålls ned — editorns "vänsterklick".</summary>
        bool IsEditorPrimaryDown { get; }

        /// <summary>True den frame gamepadens primärknapp (A) trycks ned — editorns "vänsterklick".</summary>
        bool IsEditorPrimaryPressed { get; }

        /// <summary>True medan gamepadens sekundärknapp (X) hålls ned — editorns "högerklick" (radera).</summary>
        bool IsEditorSecondaryDown { get; }

        /// <summary>True den frame gamepadens sekundärknapp (X) trycks ned — editorns "högerklick" (radera).</summary>
        bool IsEditorSecondaryPressed { get; }

        // ─────────────────────────────────────────────
        // Mus-input
        // ─────────────────────────────────────────────

        /// <summary>Muspekarens X-koordinat i skärmpixlar.</summary>
        int MouseX { get; }

        /// <summary>Muspelarens Y-koordinat i skärmpixlar.</summary>
        int MouseY { get; }

        /// <summary>True medan vänster musknapp är nedtryckt.</summary>
        bool IsMouseLeftDown { get; }

        /// <summary>True medan höger musknapp är nedtryckt.</summary>
        bool IsMouseRightDown { get; }

        /// <summary>True den frame vänster musknapp precis trycktes ned.</summary>
        bool IsMouseLeftPressed { get; }

        /// <summary>True den frame höger musknapp precis trycktes ned.</summary>
        bool IsMouseRightPressed { get; }

        /// <summary>
        /// Scrollhjulets rörelse sedan förra framen (positiv = uppåt, negativ = nedåt).
        /// Noll om scrollhjulet inte rördes.
        /// </summary>
        int MouseScrollDelta { get; }

        // ─────────────────────────────────────────────
        // Fönsterfokus
        // ─────────────────────────────────────────────

        /// <summary>
        /// True om spelfönstret är aktivt och har fokus.
        /// Används för att undvika att hantera input när fönstret är minimerat.
        /// </summary>
        bool IsWindowFocused { get; }

        // ─────────────────────────────────────────────
        // Uppdatering
        // ─────────────────────────────────────────────

        /// <summary>
        /// Uppdaterar gamepad-tillståndet. Kallas en gång per frame.
        /// </summary>
        void Poll();
    }
}
