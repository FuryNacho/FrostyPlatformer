#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Spelläge för den inbyggda level editorn. Visar en karta med fri
    /// kamerascrollning och ett rutnät. Grundläggande redigeringsfunktioner
    /// läggs till i Fas E2 och framåt.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Editorn behöver exakt samma livscykel som övriga spellägen (Enter/Update/
    /// Draw/Exit) och samma beroenden via GameServices. Att bygga den som ett
    /// IGameState ger fri övergång till och från editorn utan att GameStateManager
    /// behöver veta något om editorn specifikt (OCP). Det håller editorn isolerad
    /// från övrig spellogik och gör det enkelt att lägga till redigeringsfunktioner
    /// fas för fas utan att röra andra states.
    ///
    /// ANVÄNDNING:
    /// Skapas av MenuState.HandleSelection("Level Editor") och tas emot av
    /// GameStateManager.Transition(). Konstruktorn tar GameServices (gemensamt
    /// för alla states) och IMapRepository (för att ladda och, senare, spara kartor).
    /// Escape leder tillbaka till MenuState.
    /// </remarks>
    internal sealed class EditorState : IGameState
    {
        private const float ScrollSpeed = 8.0f;   // tile/sekund vid kameranavigering

        private readonly GameServices   _services;
        private readonly IRenderContext _rc;

        private Map?  _map;
        private float _camTargetX;
        private float _camTargetY;

        /// <summary>
        /// Skapar ett nytt EditorState.
        /// </summary>
        /// <param name="services">Gemensamma speltjänster (input, kamera, renderer m.m.).</param>
        public EditorState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        /// <summary>Laddar standardkartan och placerar kameran i kartans övre vänstra hörn.</summary>
        public void Enter(GameContext context)
        {
            _map        = LoadMap("mapone");
            _camTargetX = 0f;
            _camTargetY = 0f;
        }

        /// <summary>
        /// Hanterar kameranavigering, ritar kartan med rutnät och lyssnar på Escape.
        /// </summary>
        public void Update(GameContext context, float deltaTime)
        {
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (!_services.Input.IsWindowFocused) return;

            HandleCameraScroll(deltaTime);

            if (_services.Input.IsCancelPressed)
            {
                context.MenuNavigation = Enum.MenuState.StartMenu;
                _services.StateManager.Transition(new MenuState(_services), context);
                return;
            }

            if (_map == null) return;

            var cam = _services.Camera.Calculate(
                _camTargetX, _camTargetY,
                _map.Width, _map.Height,
                context.ScreenWidth, context.ScreenHeight);

            var (hoverTileX, hoverTileY) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            DrawTiles(cam);
            DrawGrid(cam, context);
            DrawCursor(hoverTileX, hoverTileY, cam);
            DrawHud(context, hoverTileX, hoverTileY);
        }

        /// <summary>Rendering sker i Update — Draw är avsiktligt tom (se GameplayState).</summary>
        public void Draw(IRenderContext renderContext) { }

        /// <summary>Ingen städning krävs i v1.</summary>
        public void Exit(GameContext context) { }

        // ── Privata hjälpmetoder ─────────────────────────────────────────────────

        /// <summary>
        /// Laddar en LevelObj via Assets och konverterar den till en Map
        /// så att TileMapRenderer kan arbeta mot IMapData-kontraktet.
        /// Returnerar null om kartan inte hittas.
        /// </summary>
        private Map? LoadMap(string mapId)
        {
            var levelObj = _services.Assets.GetMapData(mapId);
            if (levelObj == null) return null;

            var map = new Map();
            map.Create(new CreateObj
            {
                name        = mapId,
                levelObj    = levelObj,
                spritePath  = _services.Assets.GetSpritePath("tilesheetspring") ?? ""
            });
            return map;
        }

        /// <summary>
        /// Förflyttar kameramålet baserat på riktningsinput. Kläms mot kartgränser
        /// av CameraSystem.Calculate() — vi behöver inte klämma manuellt här.
        /// </summary>
        private void HandleCameraScroll(float deltaTime)
        {
            float delta = ScrollSpeed * deltaTime;

            if (_services.Input.IsRightDown) _camTargetX += delta;
            if (_services.Input.IsLeftDown)  _camTargetX -= delta;
            if (_services.Input.IsDownDown)  _camTargetY += delta;
            if (_services.Input.IsUpDown)    _camTargetY -= delta;

            if (_camTargetX < 0) _camTargetX = 0;
            if (_camTargetY < 0) _camTargetY = 0;
        }

        /// <summary>Ritar alla synliga tiles via TileMapRenderer.</summary>
        private void DrawTiles(CameraView cam)
        {
            foreach (var call in _services.TileRenderer.GetDrawCalls(cam, _map!))
                _rc.DrawPartialSprite(SpriteId.MapTileSheet,
                    call.ScreenX, call.ScreenY,
                    call.SpriteX, call.SpriteY,
                    call.TileWidth, call.TileHeight);
        }

        /// <summary>
        /// Ritar ett rutnät över hela skärmen för att visa tile-gränser.
        /// Rutnätets linjer förskjuts med kamerans sub-tile offset så att
        /// de följer kartscrollen pixel för pixel.
        /// </summary>
        private void DrawGrid(CameraView cam, GameContext context)
        {
            var color = RenderColor.DarkGrey;
            int ts    = GameConstants.TileSize;

            // Vertikala linjer
            float startX = -(int)cam.TileOffsetX % ts;
            for (float x = startX; x <= context.ScreenWidth; x += ts)
                _rc.DrawLine((int)x, 0, (int)x, context.ScreenHeight, color);

            // Horisontella linjer
            float startY = -(int)cam.TileOffsetY % ts;
            for (float y = startY; y <= context.ScreenHeight; y += ts)
                _rc.DrawLine(0, (int)y, context.ScreenWidth, (int)y, color);
        }

        /// <summary>
        /// Ritar en vit konturmarkör runt den tile som musen pekar på.
        /// Ritas sist för att synas ovanpå tiles och rutnät.
        /// </summary>
        private void DrawCursor(int tileX, int tileY, CameraView cam)
        {
            var (cx, cy) = EditorMath.TileToScreen(tileX, tileY, cam);
            int ts = GameConstants.TileSize;
            var c  = RenderColor.White;

            _rc.DrawLine(cx,          cy,          cx + ts - 1, cy,          c);  // övre kant
            _rc.DrawLine(cx,          cy + ts - 1, cx + ts - 1, cy + ts - 1, c);  // nedre kant
            _rc.DrawLine(cx,          cy,          cx,          cy + ts - 1, c);  // vänster kant
            _rc.DrawLine(cx + ts - 1, cy,          cx + ts - 1, cy + ts - 1, c);  // höger kant
        }

        /// <summary>Visar kartnamn, storlek och info om den tile musen pekar på.</summary>
        private void DrawHud(GameContext context, int hoverTileX, int hoverTileY)
        {
            if (_map == null)
            {
                _rc.DrawText("No map loaded", 4, 4);
                return;
            }

            int   tileId = _map.GetIndex(hoverTileX, hoverTileY);
            bool  solid  = _map.GetSolid(hoverTileX, hoverTileY);
            string info  = $"mapone ({_map.Width}x{_map.Height})  "
                         + $"Tile ({hoverTileX},{hoverTileY}) id:{tileId} "
                         + (solid ? "[solid]" : "[open]")
                         + "  [Arrows=scroll  Esc=exit]";

            _rc.DrawText(info, 4, 4);
        }
    }
}
