#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar startsplash-skärmen — den animerade snöscenen som visas när spelet startar.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplaySplashScreen (ca 115 rader). Flytten isolerar
    /// snöeffektens tillstånd (MakeItSnow-objekt, räknare) från resten av spelet.
    ///
    /// ANVÄNDNING:
    /// Aktiveras av GameStateManager som initial state. Övergår till MenuState
    /// när spelaren trycker en knapp.
    /// </remarks>
    internal sealed class SplashState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        // Nedräkning innan "Press any button" visas
        private int _countDown = 60;

        // Snöeffekt-tillstånd
        private Aggregate.MakeItSnow? _snowSlow;
        private Aggregate.MakeItSnow? _snowFast;
        private int  _counterSlow = 1;
        private int  _counterFast = 1;
        private float _incSlow;
        private float _incFast;

        public SplashState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            _countDown  = 60;
            _snowSlow   = null;
            _snowFast   = null;
            _counterSlow = 1;
            _counterFast = 1;
            _incSlow    = 0f;
            _incFast    = 0f;
        }

        public void Update(GameContext context, float elapsed)
        {
            if (_countDown > 0) _countDown--;

            _services.Input.Poll();

            // Input — tryck valfri knapp för att gå till menyn
            if (_services.Input.IsWindowFocused)
            {
                if (_services.Input.IsAnyKeyPressed || !_services.Input.IsIdle)
                {
                    _services.Input.ButtonsHasGoneIdle = false;
                    context.MenuNavigation = Enum.MenuState.StartMenu;
                    _services.StateManager.Transition(new MenuState(_services), context);
                    _snowSlow = null;
                    _snowFast = null;
                    return;
                }
            }

            AdvanceSnow(elapsed, 59);
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            // Rita splash-bakgrunden centrerad (256×224 sprite på dynamisk skärm)
            int cx = (context.ScreenWidth  - GameConstants.ScreenWidth)  / 2;
            int cy = (context.ScreenHeight - GameConstants.ScreenHeight) / 2;
            _rc.DrawSprite(SpriteId.SplashStart, cx, cy);

            // "Remastered Edition"-sigillet (88×88) — stämplat i nedre högra kvadranten av splash-rutan.
            const int SealSize        = 88;
            const int SealMarginRight = 6;
            const int SealMarginBottom = 24;
            _rc.DrawSprite(SpriteId.SplashRemastered,
                           cx + GameConstants.ScreenWidth  - SealSize - SealMarginRight,
                           cy + GameConstants.ScreenHeight - SealSize - SealMarginBottom);

            if (_countDown <= 0)
            {
                const int TextLeftMargin     = 4;   // Vänstermarginal relativt splash-sprites vänsterkant
                const int PressAnyButtonRelY = 100; // Y-position relativt splash-sprites överkant
                _rc.DrawText("Press any button", cx + TextLeftMargin, cy + PressAnyButtonRelY);
            }

            DrawSnow(59, cx, cy);
        }

        public void Exit(GameContext context) { }

        // ── Snöeffekt ───────────────────────────────────────────────────────────
        private void AdvanceSnow(float elapsed, int height)
        {
            if (_snowSlow == null || _snowFast == null ||
                _snowSlow.arrayList.Count <= height - 1)
            {
                _snowSlow = new Aggregate.MakeItSnow(1, 400, height);
                _snowFast = new Aggregate.MakeItSnow(1, 400, height);
            }

            _incSlow += elapsed;
            if (_incSlow >= 0.5f)
            {
                _incSlow = 0f;
                _counterSlow = _counterSlow < height ? _counterSlow + 1 : 1;
            }

            _incFast += elapsed;
            if (_incFast >= 0.2f)
            {
                _incFast = 0f;
                _counterFast = _counterFast < height ? _counterFast + 1 : 1;
            }
        }

        private void DrawSnow(int height, int offsetX, int offsetY)
        {
            if (_snowSlow == null || _snowFast == null) return;

            for (int i = height; i > 0; i--)
            {
                int absI = System.Math.Abs(_counterSlow - i);
                if (absI < 1) absI = 1;
                if (absI > height - 1) absI = height - 1;
                foreach (var x in _snowSlow.arrayList[absI])
                    _rc.DrawPixel(offsetX + x, offsetY + i, RenderColor.White);
            }
            for (int i = height; i > 0; i--)
            {
                int absI = System.Math.Abs(_counterFast - i);
                if (absI < 1) absI = 1;
                if (absI > height - 1) absI = height - 1;
                foreach (var x in _snowFast.arrayList[absI])
                    _rc.DrawPixel(offsetX + x, offsetY + i, RenderColor.White);
            }
        }
    }
}
