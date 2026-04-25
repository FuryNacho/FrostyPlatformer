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
    /// kamerascrollning, tile-palette och direktredigering av tile-data.
    /// Kollisionsredigering och spawn-punkt tillkommer i E2c–E2d.
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
    /// GameStateManager.Transition(). Konstruktorn tar bara GameServices — samma
    /// mönster som alla andra states. Escape leder tillbaka till MenuState.
    /// </remarks>
    /// <summary>Redigeringsläge i editorn — styr vad musklick gör.</summary>
    internal enum EditorMode { Tiles, Collision, Spawn }

    internal sealed class EditorState : IGameState
    {
        // ── Kamerakonstanter ────────────────────────────────────────────────────
        private const float ScrollSpeed = 8.0f;

        // ── Palette-layout ──────────────────────────────────────────────────────
        private const int PalettePadding = 2;
        private const int PaletteWidth   =
            GameConstants.TileSheetColumns * GameConstants.TileSize + PalettePadding * 2;

        // ── Kollisionsoverlay ───────────────────────────────────────────────────
        private static readonly RenderColor CollisionOverlayColor =
            new RenderColor(220, 50, 50, 100);
        private static readonly RenderColor SpawnMarkerColor =
            new RenderColor(50, 220, 80, 200);

        private readonly GameServices   _services;
        private readonly IRenderContext _rc;

        private LevelObj?           _levelObj;
        private LevelObjMapAdapter? _mapAdapter;
        private string              _mapId = "mapone";
        private float               _camTargetX;
        private float               _camTargetY;
        private int                 _selectedTileId;   // 0-baserat sprite-sheet-index
        private EditorMode          _mode;

        // ── HUD-meddelanden ─────────────────────────────────────────────────────
        private string _hudMessage     = "";
        private float  _hudMessageTimer;
        private const float HudMessageDuration = 2.0f;

        /// <summary>Skapar ett nytt EditorState.</summary>
        /// <param name="services">Gemensamma speltjänster (input, kamera, renderer m.m.).</param>
        public EditorState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        /// <summary>Laddar standardkartan och placerar kameran i kartans övre vänstra hörn.</summary>
        public void Enter(GameContext context)
        {
            _levelObj       = _services.Assets.GetMapData("mapone");
            _mapAdapter     = _levelObj != null ? new LevelObjMapAdapter(_levelObj) : null;
            _camTargetX     = 0f;
            _camTargetY     = 0f;
            _selectedTileId = 0;
            _mode           = EditorMode.Tiles;
        }

        /// <summary>
        /// Hanterar input, ritar kartan med rutnät och palette, och tar emot redigeringsinput.
        /// </summary>
        public void Update(GameContext context, float deltaTime)
        {
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (!_services.Input.IsWindowFocused) return;

            int mapAreaWidth = context.ScreenWidth - PaletteWidth;

            HandleCameraScroll(deltaTime);

            if (_services.Input.IsCancelPressed)
            {
                context.MenuNavigation = Enum.MenuState.StartMenu;
                _services.StateManager.Transition(new MenuState(_services), context);
                return;
            }

            if (_hudMessageTimer > 0f)
                _hudMessageTimer -= deltaTime;

            if (_services.Input.IsEditorSave)
                HandleSave();

            if (_mapAdapter == null || _levelObj == null) return;

            var cam = _services.Camera.Calculate(
                _camTargetX, _camTargetY,
                _mapAdapter.Width, _mapAdapter.Height,
                mapAreaWidth, context.ScreenHeight);

            // Lägestoggle (C = kollision, G = spawn)
            if (_services.Input.IsEditorToggleCollision)
                _mode = _mode == EditorMode.Collision ? EditorMode.Tiles : EditorMode.Collision;
            if (_services.Input.IsEditorToggleSpawn)
                _mode = _mode == EditorMode.Spawn ? EditorMode.Tiles : EditorMode.Spawn;

            bool mouseInMap = _services.Input.MouseX < mapAreaWidth;

            if (mouseInMap)
            {
                if (_mode == EditorMode.Tiles)
                    HandleTilePainting(cam);
                else if (_mode == EditorMode.Collision)
                    HandleCollisionPainting(cam);
                else
                    HandleSpawnPlacement(cam);
            }
            else
            {
                if (_mode == EditorMode.Tiles)
                    HandlePaletteClick(context, mapAreaWidth);
            }

            // Rita
            DrawTiles(cam);
            if (_mode == EditorMode.Collision)
                DrawCollisionOverlay(cam, mapAreaWidth, context.ScreenHeight);
            DrawGrid(cam, mapAreaWidth, context.ScreenHeight);

            if (_levelObj!.HasSpawn)
                DrawSpawnMarker(cam, mapAreaWidth);

            if (mouseInMap)
            {
                var (hx, hy) = EditorMath.ScreenToTile(_services.Input.MouseX, _services.Input.MouseY, cam);
                DrawCursor(hx, hy, cam);
                DrawHud(context, hx, hy, mapAreaWidth);
            }
            else
            {
                DrawHud(context, -1, -1, mapAreaWidth);
            }

            DrawPalette(context, mapAreaWidth);
        }

        /// <summary>Rendering sker i Update — Draw är avsiktligt tom (se GameplayState).</summary>
        public void Draw(IRenderContext renderContext) { }

        /// <summary>Ingen städning krävs i v1.</summary>
        public void Exit(GameContext context) { }

        // ── Kameranavigering ─────────────────────────────────────────────────────

        private void HandleCameraScroll(float deltaTime)
        {
            float d = ScrollSpeed * deltaTime;
            if (_services.Input.IsRightDown) _camTargetX += d;
            if (_services.Input.IsLeftDown)  _camTargetX -= d;
            if (_services.Input.IsDownDown)  _camTargetY += d;
            if (_services.Input.IsUpDown)    _camTargetY -= d;
            if (_camTargetX < 0) _camTargetX = 0;
            if (_camTargetY < 0) _camTargetY = 0;
        }

        // ── Tile-palette ─────────────────────────────────────────────────────────

        /// <summary>
        /// Väljer tile när användaren klickar i palette-sidebaren.
        /// Tile-ID är 0-baserat (0 = lufttile, 1 = första sprite-sheet-tilen).
        /// </summary>
        private void HandlePaletteClick(GameContext context, int mapAreaWidth)
        {
            if (!_services.Input.IsMouseLeftPressed) return;

            int relX = _services.Input.MouseX - mapAreaWidth - PalettePadding;
            int relY = _services.Input.MouseY - PalettePadding;
            int col  = relX / GameConstants.TileSize;
            int row  = relY / GameConstants.TileSize;

            if (col < 0 || col >= GameConstants.TileSheetColumns) return;
            if (row < 0 || row >= GameConstants.TileSheetRows)    return;

            _selectedTileId = row * GameConstants.TileSheetColumns + col;
        }

        /// <summary>
        /// Ritar tile-palette som en sidebar till höger. Markerar vald tile med vit ram.
        /// </summary>
        private void DrawPalette(GameContext context, int mapAreaWidth)
        {
            int ts = GameConstants.TileSize;

            // Mörkgrå bakgrund för tydlig visuell separation
            _rc.FillRect(mapAreaWidth, 0, PaletteWidth, context.ScreenHeight, RenderColor.DarkGrey);

            // Rita alla tiles i paletten
            for (int row = 0; row < GameConstants.TileSheetRows; row++)
            {
                for (int col = 0; col < GameConstants.TileSheetColumns; col++)
                {
                    int tileId  = row * GameConstants.TileSheetColumns + col;
                    int screenX = mapAreaWidth + PalettePadding + col * ts;
                    int screenY = PalettePadding + row * ts;
                    int spriteX = col * ts;
                    int spriteY = row * ts;

                    _rc.DrawPartialSprite(SpriteId.MapTileSheet,
                        screenX, screenY, spriteX, spriteY, ts, ts);

                    // Vit ram runt vald tile
                    if (tileId == _selectedTileId)
                    {
                        var w = RenderColor.White;
                        _rc.DrawLine(screenX,        screenY,        screenX + ts - 1, screenY,        w);
                        _rc.DrawLine(screenX,        screenY + ts - 1, screenX + ts - 1, screenY + ts - 1, w);
                        _rc.DrawLine(screenX,        screenY,        screenX,        screenY + ts - 1, w);
                        _rc.DrawLine(screenX + ts - 1, screenY,        screenX + ts - 1, screenY + ts - 1, w);
                    }
                }
            }
        }

        // ── Tile-placering och kollisionsredigering ──────────────────────────────

        /// <summary>
        /// Hanterar tile-målning (vänster musknapp) och radering (höger musknapp).
        /// Aktiv så länge knappen hålls ned — "penseldragning" fungerar automatiskt.
        /// </summary>
        private void HandleTilePainting(CameraView cam)
        {
            bool leftDown  = _services.Input.IsMouseLeftDown;
            bool rightDown = _services.Input.IsMouseRightDown;
            if (!leftDown && !rightDown) return;

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            if (leftDown)
                _mapAdapter!.SetTile(tx, ty, _selectedTileId);
            else
                _mapAdapter!.SetTile(tx, ty, 0);
        }

        /// <summary>
        /// Hanterar kollisionsredigering: vänster = solid, höger = icke-solid.
        /// Penseldragning stöds som i tile-läget.
        /// </summary>
        private void HandleCollisionPainting(CameraView cam)
        {
            bool leftDown  = _services.Input.IsMouseLeftDown;
            bool rightDown = _services.Input.IsMouseRightDown;
            if (!leftDown && !rightDown) return;

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            _mapAdapter!.SetSolid(tx, ty, leftDown);
        }

        /// <summary>
        /// Validerar och sparar kartan till UserMaps/ via services.UserMaps.
        /// Visar felmeddelande om spawn-punkt saknas. Visar bekräftelse i HUD vid lyckat sparande.
        /// </summary>
        private void HandleSave()
        {
            if (_levelObj == null) return;

            if (!_levelObj.HasSpawn)
            {
                ShowMessage("No spawn set — place spawn (G) before saving!");
                return;
            }

            _services.UserMaps.Save(_mapId, _levelObj);
            ShowMessage($"Saved to UserMaps/{_mapId}.json");
        }

        private void ShowMessage(string message)
        {
            _hudMessage      = message;
            _hudMessageTimer = HudMessageDuration;
        }

        /// <summary>
        /// Sätter spawn-positionen till den tile som vänster musknapp klickar på.
        /// Höger musknapp rensar spawn-positionen (SpawnX/Y = -1).
        /// </summary>
        private void HandleSpawnPlacement(CameraView cam)
        {
            if (!_services.Input.IsMouseLeftPressed && !_services.Input.IsMouseRightPressed) return;

            if (_services.Input.IsMouseRightPressed)
            {
                _levelObj!.SpawnX = -1;
                _levelObj!.SpawnY = -1;
                return;
            }

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            if (tx < 0 || tx >= _mapAdapter!.Width || ty < 0 || ty >= _mapAdapter!.Height) return;

            _levelObj!.SpawnX = tx;
            _levelObj!.SpawnY = ty;
        }

        // ── Rendering ────────────────────────────────────────────────────────────

        private void DrawTiles(CameraView cam)
        {
            foreach (var call in _services.TileRenderer.GetDrawCalls(cam, _mapAdapter!))
                _rc.DrawPartialSprite(SpriteId.MapTileSheet,
                    call.ScreenX, call.ScreenY,
                    call.SpriteX, call.SpriteY,
                    call.TileWidth, call.TileHeight);
        }

        /// <summary>
        /// Ritar ett rutnät inom kartans vy. Linjer förskjuts med kamerans sub-tile
        /// offset så att de följer kartscrollen pixel för pixel.
        /// </summary>
        private void DrawGrid(CameraView cam, int mapAreaWidth, int screenHeight)
        {
            var color = RenderColor.DarkGrey;
            int ts    = GameConstants.TileSize;

            float startX = -(int)cam.TileOffsetX % ts;
            for (float x = startX; x <= mapAreaWidth; x += ts)
                _rc.DrawLine((int)x, 0, (int)x, screenHeight, color);

            float startY = -(int)cam.TileOffsetY % ts;
            for (float y = startY; y <= screenHeight; y += ts)
                _rc.DrawLine(0, (int)y, mapAreaWidth, (int)y, color);
        }

        /// <summary>
        /// Ritar en halvtransparent röd overlay över alla solida tiles.
        /// Visas bara i kollisionsläge. Ritas ovanpå tiles men under rutnätet.
        /// </summary>
        private void DrawCollisionOverlay(CameraView cam, int mapAreaWidth, int screenHeight)
        {
            int ts = GameConstants.TileSize;

            for (int x = -1; x < cam.VisibleTilesX + 1; x++)
            {
                for (int y = -1; y < cam.VisibleTilesY + 2; y++)
                {
                    int mapX = (int)(x + cam.OffsetX);
                    int mapY = (int)(y + cam.OffsetY);
                    if (!_mapAdapter!.GetSolid(mapX, mapY)) continue;

                    int screenX = (int)(x * ts - cam.TileOffsetX);
                    int screenY = (int)(y * ts - cam.TileOffsetY);

                    // Klipp mot kartvy för att inte rita in i paletten
                    if (screenX >= mapAreaWidth) continue;

                    _rc.FillRect(screenX, screenY, ts, ts, CollisionOverlayColor);
                }
            }
        }

        /// <summary>
        /// Ritar ett grönt kryss (+) mitt i spawn-tilen. Visas alltid när ett spawn är satt,
        /// oavsett vilket redigeringsläge som är aktivt.
        /// </summary>
        private void DrawSpawnMarker(CameraView cam, int mapAreaWidth)
        {
            var (sx, sy) = EditorMath.TileToScreen(_levelObj!.SpawnX, _levelObj!.SpawnY, cam);
            if (sx >= mapAreaWidth) return;

            int ts  = GameConstants.TileSize;
            int cx  = sx + ts / 2;
            int cy  = sy + ts / 2;
            int arm = ts / 2 - 1;
            var c   = SpawnMarkerColor;

            _rc.DrawLine(cx - arm, cy,       cx + arm, cy,       c);
            _rc.DrawLine(cx,       cy - arm, cx,       cy + arm, c);
        }

        private void DrawCursor(int tileX, int tileY, CameraView cam)
        {
            var (cx, cy) = EditorMath.TileToScreen(tileX, tileY, cam);
            int ts = GameConstants.TileSize;
            var c  = RenderColor.White;
            _rc.DrawLine(cx,          cy,          cx + ts - 1, cy,          c);
            _rc.DrawLine(cx,          cy + ts - 1, cx + ts - 1, cy + ts - 1, c);
            _rc.DrawLine(cx,          cy,          cx,          cy + ts - 1, c);
            _rc.DrawLine(cx + ts - 1, cy,          cx + ts - 1, cy + ts - 1, c);
        }

        /// <summary>Statusrad: kartnamn, storlek och info om hovered tile (eller "palette"-läge).</summary>
        private void DrawHud(GameContext context, int hoverTileX, int hoverTileY, int mapAreaWidth)
        {
            if (_mapAdapter == null || _levelObj == null) return;

            string modeLabel = _mode switch
            {
                EditorMode.Collision => "COLLISION",
                EditorMode.Spawn     => "SPAWN",
                _                   => "TILES"
            };
            string tileInfo  = hoverTileX >= 0
                ? $"({hoverTileX},{hoverTileY}) id:{_mapAdapter.GetIndex(hoverTileX, hoverTileY)} "
                  + (_mapAdapter.GetSolid(hoverTileX, hoverTileY) ? "[solid]" : "[open]")
                : "[palette]";

            string spawnInfo = _levelObj.HasSpawn
                ? $"spawn:({_levelObj.SpawnX},{_levelObj.SpawnY})"
                : "spawn:none";

            string controls = _mode switch
            {
                EditorMode.Collision => "LMB=solid  RMB=open",
                EditorMode.Spawn     => "LMB=set  RMB=clear",
                _                   => "LMB=paint  RMB=erase"
            };

            string line = $"[{modeLabel}]  {_mapId} {_mapAdapter.Width}x{_mapAdapter.Height}  "
                        + $"brush:{_selectedTileId}  {tileInfo}  {spawnInfo}  "
                        + $"{controls}  C=collision  G=spawn  Ctrl+S=save  Arrows=scroll  Esc=exit";

            _rc.DrawText(line, 2, 2);

            if (_hudMessageTimer > 0f)
                _rc.DrawText(_hudMessage, 2, 12);
        }
    }
}
