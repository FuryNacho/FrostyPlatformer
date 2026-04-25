#nullable enable
using System;
using System.Collections.Generic;
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
        private static readonly RenderColor MapBoundsColor =
            new RenderColor(255, 200, 0, 230);

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

        // ── Dirty-flagga ────────────────────────────────────────────────────────
        private bool _isDirty;

        // ── Kartväljare ─────────────────────────────────────────────────────────
        private bool              _showMapPicker;
        private int               _pickerIndex;
        private List<MapPickerEntry>? _pickerEntries;
        private bool              _pickerPendingDirty;

        private const int PickerLineHeight = 10;
        private const int PickerX          = 16;
        private const int PickerStartY     = 24;

        private static readonly RenderColor PickerOverlayColor  = new RenderColor(0,   0,   0,  180);
        private static readonly RenderColor PickerSelectColor   = new RenderColor(50,  80, 160, 220);
        private static readonly RenderColor PickerHeaderBgColor = new RenderColor(30,  30,  30, 200);

        private sealed class MapPickerEntry
        {
            public string  Label    { get; init; } = "";
            public string? MapId    { get; init; }   // null = avdelningsrubrik (ej valbar)
            public bool    IsUserMap { get; init; }
        }

        // ── Ny-karta-dialog ──────────────────────────────────────────────────────
        private bool _showNewMapDialog;
        private int  _newMapWidthIdx    = 3;   // index i WidthPresets  → 32
        private int  _newMapHeightIdx   = 3;   // index i HeightPresets → 24
        private int  _newMapTilesetIdx;        // index i KnownTilesets → spring.tsx
        private int  _newMapDialogField;       // 0=bredd, 1=höjd, 2=tileset
        private bool _newMapPendingDirty;

        private static readonly int[]    WidthPresets  = { 16, 20, 24, 32, 40, 48, 64, 80, 96, 128, 192, 256 };
        private static readonly int[]    HeightPresets = { 14, 16, 20, 24, 32, 40, 48, 64, 80, 96, 128, 192, 256 };
        private static readonly string[] KnownTilesets = { "spring.tsx", "summer.tsx", "fall.tsx", "winter.tsx" };

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
            LoadMap("mapone", isUserMap: false);
            _selectedTileId = 0;
            _mode           = EditorMode.Tiles;
            _showMapPicker  = false;
        }

        /// <summary>
        /// Hanterar input, ritar kartan med rutnät, palette och kartväljare.
        /// </summary>
        public void Update(GameContext context, float deltaTime)
        {
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (!_services.Input.IsWindowFocused) return;

            if (_hudMessageTimer > 0f)
                _hudMessageTimer -= deltaTime;

            // Escape: stäng öppna dialoger, annars lämna editorn
            if (_services.Input.IsCancelPressed)
            {
                if (_showMapPicker)    { CloseMapPicker(); return; }
                if (_showNewMapDialog) { _showNewMapDialog = false; _newMapPendingDirty = false; return; }
                context.MenuNavigation = Enum.MenuState.StartMenu;
                _services.StateManager.Transition(new MenuState(_services), context);
                return;
            }

            bool noDialog = !_showMapPicker && !_showNewMapDialog;

            // Öppna kartväljaren (L) eller ny-karta-dialog (N)
            if (noDialog && _services.Input.IsEditorLoad) OpenMapPicker();
            if (noDialog && _services.Input.IsEditorNew)  HandleNewMapRequest();

            int mapAreaWidth = context.ScreenWidth - PaletteWidth;
            int visX = mapAreaWidth / GameConstants.TileSize;
            int visY = context.ScreenHeight / GameConstants.TileSize;

            // Kartväljare — hantera input och rita overlay, sedan avsluta framen
            if (_showMapPicker)
            {
                UpdateMapPicker(context);
                if (_mapAdapter != null && _levelObj != null)
                {
                    var bgCam = _services.Camera.Calculate(
                        _camTargetX + visX / 2f, _camTargetY + visY / 2f,
                        _mapAdapter.Width, _mapAdapter.Height,
                        mapAreaWidth, context.ScreenHeight);
                    DrawTiles(bgCam);
                    DrawMapBounds(bgCam, mapAreaWidth, context.ScreenHeight);
                    DrawGrid(bgCam, mapAreaWidth, context.ScreenHeight);
                    DrawPalette(context, mapAreaWidth);
                }
                DrawMapPickerOverlay(context);
                return;
            }

            // Ny-karta-dialog — hantera input och rita overlay
            if (_showNewMapDialog)
            {
                UpdateNewMapDialog();
                if (_mapAdapter != null && _levelObj != null)
                {
                    var bgCam = _services.Camera.Calculate(
                        _camTargetX + visX / 2f, _camTargetY + visY / 2f,
                        _mapAdapter.Width, _mapAdapter.Height,
                        mapAreaWidth, context.ScreenHeight);
                    DrawTiles(bgCam);
                    DrawMapBounds(bgCam, mapAreaWidth, context.ScreenHeight);
                    DrawGrid(bgCam, mapAreaWidth, context.ScreenHeight);
                    DrawPalette(context, mapAreaWidth);
                }
                DrawNewMapDialogOverlay(context);
                return;
            }

            if (_mapAdapter == null || _levelObj == null) return;

            var cam = _services.Camera.Calculate(
                _camTargetX + visX / 2f, _camTargetY + visY / 2f,
                _mapAdapter.Width, _mapAdapter.Height,
                mapAreaWidth, context.ScreenHeight);

            HandleCameraScroll(deltaTime, _mapAdapter.Width, _mapAdapter.Height, visX, visY);

            if (_services.Input.IsEditorSave) HandleSave();

            // Lägestoggle (C = kollision, G = spawn)
            if (_services.Input.IsEditorToggleCollision)
                _mode = _mode == EditorMode.Collision ? EditorMode.Tiles : EditorMode.Collision;
            if (_services.Input.IsEditorToggleSpawn)
                _mode = _mode == EditorMode.Spawn ? EditorMode.Tiles : EditorMode.Spawn;

            bool mouseInMap = _services.Input.MouseX < mapAreaWidth;
            if (mouseInMap)
            {
                if (_mode == EditorMode.Tiles)          HandleTilePainting(cam);
                else if (_mode == EditorMode.Collision) HandleCollisionPainting(cam);
                else                                    HandleSpawnPlacement(cam);
            }
            else if (_mode == EditorMode.Tiles)
            {
                HandlePaletteClick(context, mapAreaWidth);
            }

            DrawTiles(cam);
            DrawMapBounds(cam, mapAreaWidth, context.ScreenHeight);
            if (_mode == EditorMode.Collision)
                DrawCollisionOverlay(cam, mapAreaWidth, context.ScreenHeight);
            DrawGrid(cam, mapAreaWidth, context.ScreenHeight);

            if (_levelObj.HasSpawn)
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

        private void HandleCameraScroll(float deltaTime, int mapWidth, int mapHeight, int visX, int visY)
        {
            float d = ScrollSpeed * deltaTime;
            if (_services.Input.IsRightDown) _camTargetX += d;
            if (_services.Input.IsLeftDown)  _camTargetX -= d;
            if (_services.Input.IsDownDown)  _camTargetY += d;
            if (_services.Input.IsUpDown)    _camTargetY -= d;

            float maxX = Math.Max(0f, mapWidth  - visX);
            float maxY = Math.Max(0f, mapHeight - visY);
            _camTargetX = Math.Clamp(_camTargetX, 0f, maxX);
            _camTargetY = Math.Clamp(_camTargetY, 0f, maxY);
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
            _isDirty = true;
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
            _isDirty = true;
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
            _isDirty = false;
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
            _isDirty = true;
        }

        // ── Kartladdning ─────────────────────────────────────────────────────────

        /// <summary>
        /// Laddar en karta från rätt repository och återställer kameran.
        /// Nollställer dirty-flaggan — kartan är i synk med disk.
        /// </summary>
        private void LoadMap(string mapId, bool isUserMap)
        {
            var level = isUserMap
                ? _services.UserMaps.Load(mapId)
                : _services.GameMaps.Load(mapId);

            if (level == null)
            {
                ShowMessage($"Could not load '{mapId}'");
                return;
            }

            _levelObj   = level;
            _mapAdapter = new LevelObjMapAdapter(_levelObj);
            _mapId      = mapId;
            _isDirty    = false;
            _camTargetX = 0f;
            _camTargetY = 0f;

            RegisterTilesheet(_levelObj.TilesetSource);
        }

        /// <summary>
        /// Registrerar rätt tilesheet-sprite för MapTileSheet-ID:t.
        /// Deriverar Aggregate-sprite-namnet från TilesetSource ("spring.tsx" → "tilesheetspring").
        /// </summary>
        private void RegisterTilesheet(string tilesetSource)
        {
            string stem = tilesetSource.Replace(".tsx", "");
            string? path = _services.Assets.GetSpritePath("tilesheet" + stem);
            if (path != null)
                _rc.RegisterSprite(SpriteId.MapTileSheet, path);
        }

        // ── Kartväljare ──────────────────────────────────────────────────────────

        private void OpenMapPicker()
        {
            _pickerEntries = new List<MapPickerEntry>();

            _pickerEntries.Add(new MapPickerEntry { Label = "-- GAME MAPS --" });
            foreach (var id in _services.GameMaps.GetAvailableMapIds())
                _pickerEntries.Add(new MapPickerEntry { Label = id, MapId = id, IsUserMap = false });

            var userIds = new List<string>(_services.UserMaps.GetAvailableMapIds());
            if (userIds.Count > 0)
            {
                _pickerEntries.Add(new MapPickerEntry { Label = "-- USER MAPS --" });
                foreach (var id in userIds)
                    _pickerEntries.Add(new MapPickerEntry { Label = id, MapId = id, IsUserMap = true });
            }

            _pickerIndex        = _pickerEntries.FindIndex(e => e.MapId != null);
            _pickerPendingDirty = false;
            _showMapPicker      = true;
        }

        private void CloseMapPicker()
        {
            _showMapPicker      = false;
            _pickerPendingDirty = false;
        }

        // ── Ny-karta-dialog ──────────────────────────────────────────────────────

        private void HandleNewMapRequest()
        {
            if (_isDirty && !_newMapPendingDirty)
            {
                ShowMessage("Unsaved changes! Press N again to discard and create new.");
                _newMapPendingDirty = true;
                return;
            }
            _newMapPendingDirty  = false;
            _newMapDialogField   = 0;
            _showNewMapDialog    = true;
        }

        private void UpdateNewMapDialog()
        {
            if (_services.Input.IsDownPressed)
                _newMapDialogField = (_newMapDialogField + 1) % 3;
            if (_services.Input.IsUpPressed)
                _newMapDialogField = (_newMapDialogField + 2) % 3;

            if (_services.Input.IsRightPressed) ChangeNewMapField(+1);
            if (_services.Input.IsLeftPressed)  ChangeNewMapField(-1);

            if (_services.Input.IsConfirmPressed) ConfirmNewMap();
        }

        private void ChangeNewMapField(int delta)
        {
            switch (_newMapDialogField)
            {
                case 0:
                    _newMapWidthIdx   = Math.Clamp(_newMapWidthIdx   + delta, 0, WidthPresets.Length  - 1);
                    break;
                case 1:
                    _newMapHeightIdx  = Math.Clamp(_newMapHeightIdx  + delta, 0, HeightPresets.Length - 1);
                    break;
                case 2:
                    _newMapTilesetIdx = (_newMapTilesetIdx + delta + KnownTilesets.Length) % KnownTilesets.Length;
                    break;
            }
        }

        private void ConfirmNewMap()
        {
            int w = WidthPresets[_newMapWidthIdx];
            int h = HeightPresets[_newMapHeightIdx];

            var level = new LevelObj
            {
                Width          = w,
                Height         = h,
                TileIndex      = new int[w * h],
                AttributeIndex = new int[w * h],
                TilesetSource  = KnownTilesets[_newMapTilesetIdx],
                SpawnX         = 1,
                SpawnY         = 1
            };

            string mapId    = GenerateNewMapId();
            _levelObj       = level;
            _mapAdapter     = new LevelObjMapAdapter(_levelObj);
            _mapId          = mapId;
            _isDirty        = true;
            _camTargetX     = 0f;
            _camTargetY     = 0f;
            _mode           = EditorMode.Tiles;
            _showNewMapDialog = false;

            RegisterTilesheet(level.TilesetSource);
            ShowMessage($"New map '{mapId}' created — Ctrl+S to save.");
        }

        private string GenerateNewMapId()
        {
            var existing = new System.Collections.Generic.HashSet<string>(
                _services.UserMaps.GetAvailableMapIds());
            if (!existing.Contains("newmap")) return "newmap";
            int n = 2;
            while (existing.Contains($"newmap{n}")) n++;
            return $"newmap{n}";
        }

        private void DrawNewMapDialogOverlay(GameContext context)
        {
            const int boxX = 100, boxY = 70, boxW = 200, boxH = 90;
            const int labelX = boxX + 8, valueX = boxX + 72;
            const int lineH = 14;

            _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, PickerOverlayColor);
            _rc.FillRect(boxX, boxY, boxW, boxH, new RenderColor(20, 20, 30, 240));

            _rc.DrawText("NEW MAP", labelX, boxY + 4);

            string[] labels = { "Width:", "Height:", "Tileset:" };
            string[] values =
            {
                $"< {WidthPresets[_newMapWidthIdx]} >",
                $"< {HeightPresets[_newMapHeightIdx]} >",
                $"< {KnownTilesets[_newMapTilesetIdx]} >"
            };

            for (int i = 0; i < 3; i++)
            {
                int y = boxY + 20 + i * lineH;
                if (i == _newMapDialogField)
                    _rc.FillRect(labelX - 2, y - 1, boxW - 12, lineH, PickerSelectColor);
                _rc.DrawText(labels[i], labelX,  y);
                _rc.DrawText(values[i], valueX,  y);
            }

            _rc.DrawText("Enter=create   Esc=cancel", labelX, boxY + boxH - 12);
        }

        private void UpdateMapPicker(GameContext context)
        {
            if (_pickerEntries == null) return;

            if (_services.Input.IsUpPressed)   MovePicker(-1);
            if (_services.Input.IsDownPressed) MovePicker(+1);

            if (_services.Input.IsConfirmPressed)
                ConfirmPickerLoad();
        }

        private void MovePicker(int delta)
        {
            if (_pickerEntries == null) return;
            int count = _pickerEntries.Count;
            int next  = _pickerIndex + delta;
            while (next >= 0 && next < count && _pickerEntries[next].MapId == null)
                next += delta;
            if (next >= 0 && next < count)
                _pickerIndex = next;
        }

        private void ConfirmPickerLoad()
        {
            if (_pickerEntries == null) return;
            var entry = _pickerEntries[_pickerIndex];
            if (entry.MapId == null) return;

            if (_isDirty && !_pickerPendingDirty)
            {
                ShowMessage("Unsaved changes! Press Enter again to discard.");
                _pickerPendingDirty = true;
                return;
            }

            LoadMap(entry.MapId, entry.IsUserMap);
            CloseMapPicker();
            _mode = EditorMode.Tiles;
        }

        private void DrawMapPickerOverlay(GameContext context)
        {
            if (_pickerEntries == null) return;

            _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, PickerOverlayColor);
            _rc.DrawText("SELECT MAP   Up/Down=navigate   Enter=load   Esc=cancel", PickerX, 8);

            int y = PickerStartY;
            for (int i = 0; i < _pickerEntries.Count; i++)
            {
                var entry = _pickerEntries[i];

                if (entry.MapId == null)
                {
                    // Avdelningsrubrik
                    _rc.FillRect(PickerX - 2, y - 1, 160, PickerLineHeight, PickerHeaderBgColor);
                    _rc.DrawText(entry.Label, PickerX, y);
                }
                else if (i == _pickerIndex)
                {
                    // Valt alternativ — markerad bakgrund
                    _rc.FillRect(PickerX - 2, y - 1, 160, PickerLineHeight, PickerSelectColor);
                    _rc.DrawText("> " + entry.Label, PickerX, y);
                }
                else
                {
                    _rc.DrawText("  " + entry.Label, PickerX, y);
                }

                y += PickerLineHeight;
            }

            if (_hudMessageTimer > 0f)
                _rc.DrawText(_hudMessage, PickerX, y + PickerLineHeight);
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
        /// Ritar ett rutnät inom kartans vy. Använder samma formel som TileMapRenderer
        /// för pixel-perfekt justering mot tile-kanterna vid alla scroll-positioner.
        /// </summary>
        private void DrawGrid(CameraView cam, int mapAreaWidth, int screenHeight)
        {
            var color = RenderColor.DarkGrey;
            int ts    = GameConstants.TileSize;

            for (int col = -1; col <= cam.VisibleTilesX + 1; col++)
            {
                int x = (int)(col * ts - cam.TileOffsetX);
                if (x < 0 || x > mapAreaWidth) continue;
                _rc.DrawLine(x, 0, x, screenHeight, color);
            }

            for (int row = -1; row <= cam.VisibleTilesY + 2; row++)
            {
                int y = (int)(row * ts - cam.TileOffsetY);
                if (y < 0 || y > screenHeight) continue;
                _rc.DrawLine(0, y, mapAreaWidth, y, color);
            }
        }

        /// <summary>
        /// Ritar en gul ram runt kartans faktiska tile-yta så att användaren
        /// tydligt ser var kartan slutar och tomt utrymme börjar.
        /// </summary>
        private void DrawMapBounds(CameraView cam, int mapAreaWidth, int screenHeight)
        {
            if (_mapAdapter == null) return;

            var (left, top)     = EditorMath.TileToScreen(0,                0,                cam);
            var (right, bottom) = EditorMath.TileToScreen(_mapAdapter.Width, _mapAdapter.Height, cam);

            int l = Math.Max(left,   0);
            int t = Math.Max(top,    0);
            int r = Math.Min(right,  mapAreaWidth - 1);
            int b = Math.Min(bottom, screenHeight - 1);

            if (l > r || t > b) return;

            var c = MapBoundsColor;
            _rc.DrawLine(l, t, r, t, c);
            _rc.DrawLine(l, b, r, b, c);
            _rc.DrawLine(l, t, l, b, c);
            _rc.DrawLine(r, t, r, b, c);
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

            string dirtyMark = _isDirty ? "*" : "";
            string line = $"[{modeLabel}]  {_mapId}{dirtyMark} {_mapAdapter.Width}x{_mapAdapter.Height}  "
                        + $"brush:{_selectedTileId}  {tileInfo}  {spawnInfo}  "
                        + $"{controls}  C=col  G=spawn  N=new  L=load  Ctrl+S=save  Arrows=scroll  Esc=exit";

            _rc.DrawText(line, 2, 2);

            if (_hudMessageTimer > 0f)
                _rc.DrawText(_hudMessage, 2, 12);
        }
    }
}
