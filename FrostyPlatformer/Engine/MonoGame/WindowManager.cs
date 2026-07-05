#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Engine.MonoGame
{
    /// <summary>
    /// Äger fönster- och helskärmshanteringen. Enda platsen som rör
    /// <see cref="GraphicsDeviceManager"/>/<see cref="GameWindow"/> för att byta läge.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Structural)
    ///
    /// MOTIVERING:
    /// Helskärmslogik låg tidigare inline i Program.Update (F11 → _graphics.ToggleFullScreen),
    /// vilket gjorde en hardware mode switch och glömde re-synka den dynamiska upplösningen
    /// → svart skärm och vy inzoomad i hörnet. WindowManager kapslar in lägesbytet och
    /// använder borderless fullscreen (HardwareModeSwitch = false) för en ren övergång.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program (Composition Root) med spelets GraphicsDeviceManager och GameWindow.
    /// Program anropar Apply() vid uppstart (med sparat/standardläge) och vid lägesbyte,
    /// och kör därefter sin UpdateScreenDimensions() för att synka ScreenWidth/Height.
    /// </remarks>
    public sealed class WindowManager
    {
        /// <summary>
        /// Läget spelet startar i när inget val sparats i settings. Ändra denna rad för
        /// att flippa standardläge (t.ex. <see cref="ScreenMode.BorderlessFullscreen"/> för konsolbygget).
        /// </summary>
        public const ScreenMode DefaultScreenMode = ScreenMode.BorderlessFullscreen;

        private readonly GraphicsDeviceManager _graphics;

        /// <summary>Det läge som senast applicerats.</summary>
        public ScreenMode Current { get; private set; } = DefaultScreenMode;

        public WindowManager(GraphicsDeviceManager graphics)
        {
            _graphics = graphics;
        }

        /// <summary>
        /// Applicerar ett fönsterläge och kör <see cref="GraphicsDeviceManager.ApplyChanges"/>.
        /// Anroparen ansvarar för att efteråt re-synka spelets logiska skärmdimensioner.
        /// </summary>
        public void Apply(ScreenMode mode)
        {
            switch (mode)
            {
                case ScreenMode.BorderlessFullscreen:
                    // HardwareModeSwitch = false → borderless helskärm utan mode switch
                    // (ingen svart skärm/monitor-resync). Ramen sköts av IsFullScreen —
                    // rör INTE Window.IsBorderless, då fastnar den vid återgång till windowed.
                    DisplayMode dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                    _graphics.HardwareModeSwitch        = false;
                    _graphics.PreferredBackBufferWidth  = dm.Width;
                    _graphics.PreferredBackBufferHeight = dm.Height;
                    _graphics.IsFullScreen              = true;
                    break;

                case ScreenMode.Windowed:
                default:
                    _graphics.IsFullScreen              = false;
                    _graphics.PreferredBackBufferWidth  = GameConstants.WindowedWidth;
                    _graphics.PreferredBackBufferHeight = GameConstants.WindowedHeight;
                    break;
            }

            _graphics.ApplyChanges();
            Current = mode;
        }

        /// <summary>Returnerar det läge en toggle (F11) ska byta till från nuvarande läge.</summary>
        public ScreenMode NextToggleMode()
            => Current == ScreenMode.BorderlessFullscreen
                ? ScreenMode.Windowed
                : ScreenMode.BorderlessFullscreen;
    }
}
