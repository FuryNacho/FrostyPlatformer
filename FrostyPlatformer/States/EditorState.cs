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
    /// Arbetar uteslutande mot UserMaps/ — spelets egna kartor rörs aldrig.
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
    internal enum EditorMode { Tiles, Collision, Spawn, Goal, Pickup, Enemy }

    internal sealed class EditorState : IGameState
    {
        // ── Kamerakonstanter ────────────────────────────────────────────────────
        private const float ScrollSpeed = 8.0f;

        // ── Palette-layout ──────────────────────────────────────────────────────
        private const int PalettePadding = 2;
        private const int PaletteWidth   =
            GameConstants.TileSheetColumns * GameConstants.TileSize + PalettePadding * 2;

        // ── Användarkartor — 7 fasta slots ──────────────────────────────────────
        private const int MaxUserSlots = 7;
        private static string SlotId(int n) => $"slot{n}";   // "slot1" .. "slot7"

        // ── Färgkonstanter ──────────────────────────────────────────────────────
        private static readonly RenderColor CollisionOverlayColor =
            new RenderColor(220, 50, 50, 100);
        private static readonly RenderColor SpawnMarkerColor =
            new RenderColor(50, 220, 80, 200);
        private static readonly RenderColor GoalMarkerColor =
            new RenderColor(255, 200, 0, 230);
        private static readonly RenderColor PickupMarkerColor =
            new RenderColor(0, 210, 200, 210);
        private static readonly RenderColor EnemyMarkerColor =
            new RenderColor(220, 60, 60, 210);
        private static readonly RenderColor MapBoundsColor =
            new RenderColor(255, 200, 0, 230);

        private readonly GameServices   _services;
        private readonly IRenderContext _rc;

        private LevelObj?           _levelObj;
        private LevelObjMapAdapter? _mapAdapter;
        private string              _mapId = "";
        private float               _camTargetX;
        private float               _camTargetY;
        private int                 _selectedTileId;
        private EditorMode          _mode;
        private bool                _undoMode;       // toggle via U — LMB raderar istället för målar
        private int                 _selectedPickupSubType;
        private int                 _selectedEnemySubType;

        private static readonly string[] PickupSubTypes = { "Energy" };
        private static readonly string[] EnemySubTypes  = { "Penguin", "Walrus", "Frost" };

        // ── HUD-meddelanden ─────────────────────────────────────────────────────
        private string _hudMessage     = "";
        private float  _hudMessageTimer;
        private const float HudMessageDuration = 2.0f;

        // ── Dirty-flagga ────────────────────────────────────────────────────────
        private bool _isDirty;

        // ── Slot-picker ─────────────────────────────────────────────────────────
        private bool              _showMapPicker;
        private int               _pickerIndex;
        private List<MapPickerEntry>? _pickerEntries;
        private bool              _pickerPendingDirty;

        private const int PickerLineHeight = 10;
        private const int PickerX          = 16;
        private const int PickerStartY     = 24;

        private static readonly RenderColor PickerOverlayColor = new RenderColor(0,   0,   0,  180);
        private static readonly RenderColor PickerSelectColor  = new RenderColor(50,  80, 160, 220);

        private sealed class MapPickerEntry
        {
            public string Label   { get; init; } = "";
            public string SlotId  { get; init; } = "";
            public bool   IsEmpty { get; init; }
        }

        // ── Ny-karta-dialog ──────────────────────────────────────────────────────
        private bool    _showNewMapDialog;
        private int     _newMapWidthIdx    = 3;   // index i WidthPresets  → 32
        private int     _newMapHeightIdx   = 3;   // index i HeightPresets → 24
        private int     _newMapTilesetIdx;         // index i KnownTilesets → tilesheetspring
        private int     _newMapDialogField;        // 0=bredd, 1=höjd, 2=tileset
        private bool    _newMapPendingDirty;
        private string? _pendingNewSlotId;         // vilket slot N-dialogen ska spara till

        private static readonly int[]    WidthPresets  = { 16, 20, 24, 32, 40, 48, 64, 80, 96, 128, 192, 256 };
        private static readonly int[]    HeightPresets = { 14, 16, 20, 24, 32, 40, 48, 64, 80, 96, 128, 192, 256 };
        private static readonly string[] KnownTilesets =
        {
            "tilesheetspring.tsx", "tilesheetsummer.tsx",
            "tilesheetfall.tsx",   "tilesheetwinter.tsx"
        };

        /// <summary>Skapar ett nytt EditorState.</summary>
        /// <param name="services">Gemensamma speltjänster (input, kamera, renderer m.m.).</param>
        public EditorState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        /// <summary>Öppnar slot-pickern direkt vid start — ingen spelkarta laddas automatiskt.</summary>
        public void Enter(GameContext context)
        {
            _selectedTileId = 0;
            _mode           = EditorMode.Tiles;
            OpenMapPicker();
        }

        /// <summary>
        /// Hanterar input, ritar kartan med rutnät, palette och slot-picker.
        /// </summary>
        public void Update(GameContext context, float deltaTime)
        {
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (!_services.Input.IsWindowFocused) return;

            if (_hudMessageTimer > 0f)
                _hudMessageTimer -= deltaTime;

            // Escape: stäng öppen dialog, eller lämna editorn
            if (_services.Input.IsCancelPressed)
            {
                if (_showNewMapDialog)
                {
                    _showNewMapDialog    = false;
                    _newMapPendingDirty  = false;
                    _pendingNewSlotId    = null;
                    OpenMapPicker();      // gå tillbaka till pickern
                    return;
                }
                if (_showMapPicker)
                {
                    // Ingen karta laddad = lämna editorn, annars stäng bara pickern
                    if (_mapAdapter == null)
                    {
                        context.MenuNavigation = Enum.MenuState.StartMenu;
                        _services.StateManager.Transition(new MenuState(_services), context);
                    }
                    else
                    {
                        CloseMapPicker();
                    }
                    return;
                }
                context.MenuNavigation = Enum.MenuState.StartMenu;
                _services.StateManager.Transition(new MenuState(_services), context);
                return;
            }

            bool noDialog = !_showMapPicker && !_showNewMapDialog;

            // L eller N öppnar slot-pickern
            if (noDialog && (_services.Input.IsEditorLoad || _services.Input.IsEditorNew))
                OpenMapPicker();

            int mapAreaWidth = context.ScreenWidth - PaletteWidth;
            int visX = mapAreaWidth / GameConstants.TileSize;
            int visY = context.ScreenHeight / GameConstants.TileSize;

            // Slot-picker — hantera input och rita overlay
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

            // Ny-karta-dialog
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

            if (_services.Input.IsEditorToggleCollision)
                _mode = _mode == EditorMode.Collision ? EditorMode.Tiles : EditorMode.Collision;
            if (_services.Input.IsEditorToggleSpawn)
                _mode = _mode == EditorMode.Spawn ? EditorMode.Tiles : EditorMode.Spawn;
            if (_services.Input.IsEditorToggleGoal)
                _mode = _mode == EditorMode.Goal ? EditorMode.Tiles : EditorMode.Goal;
            if (_services.Input.IsEditorTogglePickup)
                _mode = _mode == EditorMode.Pickup ? EditorMode.Tiles : EditorMode.Pickup;
            if (_services.Input.IsEditorToggleEnemy)
            {
                if (_mode != EditorMode.Enemy)
                {
                    _mode = EditorMode.Enemy;
                    _selectedEnemySubType = 0;
                }
                else
                {
                    _selectedEnemySubType++;
                    if (_selectedEnemySubType >= EnemySubTypes.Length)
                    {
                        _selectedEnemySubType = 0;
                        _mode = EditorMode.Tiles;
                    }
                }
            }

            if (_mode == EditorMode.Pickup && PickupSubTypes.Length > 1)
            {
                if (_services.Input.IsLeftPressed)
                    _selectedPickupSubType = (_selectedPickupSubType - 1 + PickupSubTypes.Length) % PickupSubTypes.Length;
                if (_services.Input.IsRightPressed)
                    _selectedPickupSubType = (_selectedPickupSubType + 1) % PickupSubTypes.Length;
            }
            if (_services.Input.IsEditorUndoPressed)
                _undoMode = !_undoMode;

            bool mouseInMap = _services.Input.MouseX < mapAreaWidth;
            if (mouseInMap)
            {
                if      (_mode == EditorMode.Tiles)     HandleTilePainting(cam);
                else if (_mode == EditorMode.Collision) HandleCollisionPainting(cam);
                else if (_mode == EditorMode.Spawn)     HandleSpawnPlacement(cam);
                else if (_mode == EditorMode.Goal)      HandleGoalPlacement(cam);
                else if (_mode == EditorMode.Pickup)    HandlePickupPlacement(cam);
                else                                    HandleEnemyPlacement(cam);
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
            if (_levelObj.HasGoal)
                DrawGoalMarker(cam, mapAreaWidth);
            DrawPickupMarkers(cam, mapAreaWidth);
            DrawEnemyMarkers(cam, mapAreaWidth);

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

        /// <summary>Rendering sker i Update — Draw är avsiktligt tom.</summary>
        public void Draw(IRenderContext renderContext) { }

        /// <summary>Ingen städning krävs.</summary>
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
        /// Ritar tile-palette som en sidebar till höger.
        /// Vald tile markeras med vit ram, hovrad tile med gul ram.
        /// </summary>
        private void DrawPalette(GameContext context, int mapAreaWidth)
        {
            int ts = GameConstants.TileSize;

            int relX      = _services.Input.MouseX - mapAreaWidth - PalettePadding;
            int relY      = _services.Input.MouseY - PalettePadding;
            int hoverCol  = relX >= 0 ? relX / ts : -1;
            int hoverRow  = relY >= 0 ? relY / ts : -1;
            bool hasHover = hoverCol >= 0 && hoverCol < GameConstants.TileSheetColumns
                         && hoverRow >= 0 && hoverRow < GameConstants.TileSheetRows;
            int hoverTile = hasHover ? hoverRow * GameConstants.TileSheetColumns + hoverCol : -1;

            _rc.FillRect(mapAreaWidth, 0, PaletteWidth, context.ScreenHeight, RenderColor.DarkGrey);

            for (int row = 0; row < GameConstants.TileSheetRows; row++)
            {
                for (int col = 0; col < GameConstants.TileSheetColumns; col++)
                {
                    int tileId  = row * GameConstants.TileSheetColumns + col;
                    int screenX = mapAreaWidth + PalettePadding + col * ts;
                    int screenY = PalettePadding + row * ts;

                    _rc.DrawPartialSprite(SpriteId.MapTileSheet,
                        screenX, screenY, col * ts, row * ts, ts, ts);

                    if (tileId == _selectedTileId)
                        DrawTileBorder(screenX, screenY, ts, RenderColor.White);
                    else if (tileId == hoverTile)
                        DrawTileBorder(screenX, screenY, ts, new RenderColor(255, 220, 0, 200));
                }
            }
        }

        private void DrawTileBorder(int x, int y, int ts, RenderColor c)
        {
            _rc.DrawLine(x,          y,          x + ts - 1, y,          c);
            _rc.DrawLine(x,          y + ts - 1, x + ts - 1, y + ts - 1, c);
            _rc.DrawLine(x,          y,          x,          y + ts - 1, c);
            _rc.DrawLine(x + ts - 1, y,          x + ts - 1, y + ts - 1, c);
        }

        // ── Tile-placering och kollisionsredigering ──────────────────────────────

        /// <summary>
        /// Hanterar tile-målning (vänster) och radering (höger).
        /// Penseldragning fungerar automatiskt medan knappen hålls ned.
        /// </summary>
        private void HandleTilePainting(CameraView cam)
        {
            bool leftDown  = _services.Input.IsMouseLeftDown;
            bool rightDown = _services.Input.IsMouseRightDown;
            bool erase     = rightDown || (_undoMode && leftDown);
            bool place     = leftDown && !_undoMode;
            if (!place && !erase) return;

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            _mapAdapter!.SetTile(tx, ty, place ? _selectedTileId : 0);
            _isDirty = true;
        }

        /// <summary>
        /// Hanterar kollisionsredigering: vänster = solid, höger = öppen.
        /// </summary>
        private void HandleCollisionPainting(CameraView cam)
        {
            bool leftDown    = _services.Input.IsMouseLeftDown;
            bool rightDown   = _services.Input.IsMouseRightDown;
            bool removeSolid = rightDown || (_undoMode && leftDown);
            bool addSolid    = leftDown && !_undoMode;
            if (!addSolid && !removeSolid) return;

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            _mapAdapter!.SetSolid(tx, ty, addSolid);
            _isDirty = true;
        }

        /// <summary>
        /// Sparar kartan till UserMaps/. Kräver att spawn-punkt är satt.
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
            ShowMessage($"Saved: UserMaps/{_mapId}.json");
        }

        private void ShowMessage(string message)
        {
            _hudMessage      = message;
            _hudMessageTimer = HudMessageDuration;
        }

        /// <summary>
        /// Sätter spawn-positionen till klickad tile. Höger musknapp rensar spawn.
        /// </summary>
        private void HandleSpawnPlacement(CameraView cam)
        {
            bool clearPressed = _services.Input.IsMouseRightPressed
                             || (_undoMode && _services.Input.IsMouseLeftPressed);
            bool placePressed = !_undoMode && _services.Input.IsMouseLeftPressed;
            if (!placePressed && !clearPressed) return;

            if (clearPressed)
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

        /// <summary>
        /// Placerar mål/portal-objektet (LMB) eller tar bort det (RMB).
        /// Bara ett mål per karta tillåts — befintligt ersätts vid ny placering.
        /// Portalen leder alltid tillbaka till worldmap.
        /// </summary>
        private void HandleGoalPlacement(CameraView cam)
        {
            bool clearPressed = _services.Input.IsMouseRightPressed
                             || (_undoMode && _services.Input.IsMouseLeftPressed);
            bool placePressed = !_undoMode && _services.Input.IsMouseLeftPressed;
            if (!placePressed && !clearPressed) return;

            if (clearPressed)
            {
                _levelObj!.Objects.RemoveAll(o => o.ObjectType == "Goal");
                _isDirty = true;
                return;
            }

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            if (tx < 0 || tx >= _mapAdapter!.Width || ty < 0 || ty >= _mapAdapter!.Height) return;

            _levelObj!.Objects.RemoveAll(o => o.ObjectType == "Goal");
            _levelObj.Objects.Add(new PlacedObject
            {
                ObjectType = "Goal",
                SubType    = "Goal",
                TileX      = tx,
                TileY      = ty
            });
            _isDirty = true;
        }

        /// <summary>
        /// Placerar en pickup av vald sub-typ (LMB) eller tar bort pickup på tilen (RMB).
        /// Flera pickups per karta tillåts; max en per tile.
        /// </summary>
        private void HandlePickupPlacement(CameraView cam)
        {
            bool clearPressed = _services.Input.IsMouseRightPressed
                             || (_undoMode && _services.Input.IsMouseLeftPressed);
            bool placePressed = !_undoMode && _services.Input.IsMouseLeftPressed;
            if (!placePressed && !clearPressed) return;

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            if (tx < 0 || tx >= _mapAdapter!.Width || ty < 0 || ty >= _mapAdapter!.Height) return;

            if (clearPressed)
            {
                _levelObj!.Objects.RemoveAll(
                    o => o.ObjectType == "Pickup" && o.TileX == tx && o.TileY == ty);
                _isDirty = true;
                return;
            }

            // En pickup per tile — inga staplar
            if (_levelObj!.Objects.Exists(
                    o => o.ObjectType == "Pickup" && o.TileX == tx && o.TileY == ty))
                return;

            _levelObj.Objects.Add(new PlacedObject
            {
                ObjectType = "Pickup",
                SubType    = PickupSubTypes[_selectedPickupSubType],
                TileX      = tx,
                TileY      = ty
            });
            _isDirty = true;
        }

        /// <summary>
        /// Placerar en fiende av vald typ (LMB) eller tar bort fienden på tilen (RMB).
        /// Max en fiende per tile. Typ väljs med vänster/höger piltangent.
        /// </summary>
        private void HandleEnemyPlacement(CameraView cam)
        {
            bool clearPressed = _services.Input.IsMouseRightPressed
                             || (_undoMode && _services.Input.IsMouseLeftPressed);
            bool placePressed = !_undoMode && _services.Input.IsMouseLeftPressed;
            if (!placePressed && !clearPressed) return;

            var (tx, ty) = EditorMath.ScreenToTile(
                _services.Input.MouseX, _services.Input.MouseY, cam);

            if (tx < 0 || tx >= _mapAdapter!.Width || ty < 0 || ty >= _mapAdapter!.Height) return;

            if (clearPressed)
            {
                _levelObj!.Objects.RemoveAll(
                    o => o.ObjectType == "Enemy" && o.TileX == tx && o.TileY == ty);
                _isDirty = true;
                return;
            }

            // En fiende per tile — inga staplar
            if (_levelObj!.Objects.Exists(
                    o => o.ObjectType == "Enemy" && o.TileX == tx && o.TileY == ty))
                return;

            _levelObj.Objects.Add(new PlacedObject
            {
                ObjectType = "Enemy",
                SubType    = EnemySubTypes[_selectedEnemySubType],
                TileX      = tx,
                TileY      = ty
            });
            _isDirty = true;
        }

        // ── Kartladdning ─────────────────────────────────────────────────────────

        /// <summary>
        /// Laddar en karta från UserMaps/ och återställer kameran.
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

        private void RegisterTilesheet(string tilesetSource)
        {
            // TilesetSource är t.ex. "tilesheetspring.tsx" — Aggregate-nyckeln är samma utan ".tsx"
            string assetName = tilesetSource.Replace(".tsx", "");
            string? path = _services.Assets.GetSpritePath(assetName);
            if (path != null)
                _rc.RegisterSprite(SpriteId.MapTileSheet, path);
        }

        // ── Slot-picker ──────────────────────────────────────────────────────────

        /// <summary>
        /// Öppnar slot-pickern med de 7 fasta användarkartorna.
        /// Slottar utan sparad fil visas som tomma.
        /// </summary>
        private void OpenMapPicker()
        {
            _pickerEntries = new List<MapPickerEntry>();
            var existing   = new System.Collections.Generic.HashSet<string>(
                _services.UserMaps.GetAvailableMapIds());

            for (int i = 1; i <= MaxUserSlots; i++)
            {
                string slotId  = SlotId(i);
                bool   isEmpty = !existing.Contains(slotId);
                _pickerEntries.Add(new MapPickerEntry
                {
                    Label   = isEmpty ? $"Slot {i}  [Empty]" : $"Slot {i}  [{slotId}]",
                    SlotId  = slotId,
                    IsEmpty = isEmpty
                });
            }

            _pickerIndex        = 0;
            _pickerPendingDirty = false;
            _showMapPicker      = true;
        }

        private void CloseMapPicker()
        {
            _showMapPicker      = false;
            _pickerPendingDirty = false;
        }

        // ── Ny-karta-dialog ──────────────────────────────────────────────────────

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
            // Om en karta redan är öppen och osparad — varna en gång
            if (_isDirty && _mapAdapter != null && !_newMapPendingDirty)
            {
                ShowMessage("Unsaved changes! Press Enter again to discard.");
                _newMapPendingDirty = true;
                return;
            }

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

            string mapId        = _pendingNewSlotId ?? SlotId(1);
            _levelObj           = level;
            _mapAdapter         = new LevelObjMapAdapter(_levelObj);
            _mapId              = mapId;
            _isDirty            = true;
            _camTargetX         = 0f;
            _camTargetY         = 0f;
            _mode               = EditorMode.Tiles;
            _showNewMapDialog   = false;
            _newMapPendingDirty = false;
            _pendingNewSlotId   = null;

            RegisterTilesheet(level.TilesetSource);
            ShowMessage($"New map '{mapId}' created — Ctrl+S to save.");
        }

        private void DrawNewMapDialogOverlay(GameContext context)
        {
            const int boxX = 100, boxY = 70, boxW = 200, boxH = 90;
            const int labelX = boxX + 8, valueX = boxX + 72;
            const int lineH = 14;

            _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, PickerOverlayColor);
            _rc.FillRect(boxX, boxY, boxW, boxH, new RenderColor(20, 20, 30, 240));

            string title = _pendingNewSlotId != null
                ? $"NEW MAP  ({_pendingNewSlotId})"
                : "NEW MAP";
            _rc.DrawText(title, labelX, boxY + 4);

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
                _rc.DrawText(labels[i], labelX, y);
                _rc.DrawText(values[i], valueX, y);
            }

            _rc.DrawText("Enter=create   Esc=back", labelX, boxY + boxH - 12);
        }

        private void UpdateMapPicker(GameContext context)
        {
            if (_pickerEntries == null) return;

            if (_services.Input.IsUpPressed)
                _pickerIndex = Math.Max(0, _pickerIndex - 1);
            if (_services.Input.IsDownPressed)
                _pickerIndex = Math.Min(_pickerEntries.Count - 1, _pickerIndex + 1);

            if (_services.Input.IsConfirmPressed)
                ConfirmPickerLoad();
        }

        private void ConfirmPickerLoad()
        {
            if (_pickerEntries == null) return;
            var entry = _pickerEntries[_pickerIndex];

            if (entry.IsEmpty)
            {
                // Tomt slot — öppna N-dialogen för att konfigurera den nya kartan
                _pendingNewSlotId   = entry.SlotId;
                _showMapPicker      = false;
                _newMapDialogField  = 0;
                _newMapPendingDirty = false;
                _showNewMapDialog   = true;
                return;
            }

            if (_isDirty && !_pickerPendingDirty)
            {
                ShowMessage("Unsaved changes! Press Enter again to discard.");
                _pickerPendingDirty = true;
                return;
            }

            LoadMap(entry.SlotId, isUserMap: true);
            CloseMapPicker();
            _mode = EditorMode.Tiles;
        }

        private void DrawMapPickerOverlay(GameContext context)
        {
            if (_pickerEntries == null) return;

            _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, PickerOverlayColor);
            _rc.DrawText("YOUR MAPS   Up/Down=navigate   Enter=open/create   Esc=exit", PickerX, 8);

            int y = PickerStartY;
            for (int i = 0; i < _pickerEntries.Count; i++)
            {
                var  entry    = _pickerEntries[i];
                bool selected = i == _pickerIndex;

                if (selected)
                {
                    string action = entry.IsEmpty ? "[Enter = new map]" : "[Enter = load]";
                    _rc.FillRect(PickerX - 2, y - 1, context.ScreenWidth - PickerX * 2,
                                 PickerLineHeight, PickerSelectColor);
                    _rc.DrawText($"> {entry.Label}  {action}", PickerX, y);
                }
                else
                {
                    _rc.DrawText($"  {entry.Label}", PickerX, y);
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
        /// Ritar en gul ram runt kartans faktiska tile-yta.
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
        /// Ritar halvtransparent röd overlay över solida tiles (visas bara i kollisionsläge).
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

                    if (screenX >= mapAreaWidth) continue;

                    _rc.FillRect(screenX, screenY, ts, ts, CollisionOverlayColor);
                }
            }
        }

        /// <summary>
        /// Ritar ett grönt kryss mitt i spawn-tilen.
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

        /// <summary>
        /// Ritar en gul diamant vid mål-/portal-tilen.
        /// </summary>
        private void DrawGoalMarker(CameraView cam, int mapAreaWidth)
        {
            var goal = _levelObj!.Objects.Find(o => o.ObjectType == "Goal");
            if (goal == null) return;

            var (sx, sy) = EditorMath.TileToScreen(goal.TileX, goal.TileY, cam);
            if (sx >= mapAreaWidth) return;

            int ts  = GameConstants.TileSize;
            int cx  = sx + ts / 2;
            int cy  = sy + ts / 2;
            int arm = ts / 2 - 1;
            var c   = GoalMarkerColor;

            _rc.DrawLine(cx,       cy - arm, cx + arm, cy,       c);
            _rc.DrawLine(cx + arm, cy,       cx,       cy + arm, c);
            _rc.DrawLine(cx,       cy + arm, cx - arm, cy,       c);
            _rc.DrawLine(cx - arm, cy,       cx,       cy - arm, c);
        }

        /// <summary>
        /// Ritar en teal fylld ruta med "E"-etikett för varje pickup i kartan.
        /// </summary>
        private void DrawPickupMarkers(CameraView cam, int mapAreaWidth)
        {
            int ts     = GameConstants.TileSize;
            int margin = ts / 4;
            var c      = PickupMarkerColor;

            foreach (var obj in _levelObj!.Objects)
            {
                if (obj.ObjectType != "Pickup") continue;

                var (sx, sy) = EditorMath.TileToScreen(obj.TileX, obj.TileY, cam);
                if (sx >= mapAreaWidth) continue;

                _rc.FillRect(sx + margin, sy + margin, ts - margin * 2, ts - margin * 2, c);
                _rc.DrawText("E", sx + margin + 1, sy + margin + 1);
            }
        }

        /// <summary>
        /// Ritar en röd fylld ruta med initial-bokstav (P/W/F) för varje fiende i kartan.
        /// </summary>
        private void DrawEnemyMarkers(CameraView cam, int mapAreaWidth)
        {
            int ts     = GameConstants.TileSize;
            int margin = 1;
            var c      = EnemyMarkerColor;

            foreach (var obj in _levelObj!.Objects)
            {
                if (obj.ObjectType != "Enemy") continue;

                var (sx, sy) = EditorMath.TileToScreen(obj.TileX, obj.TileY, cam);
                if (sx >= mapAreaWidth) continue;

                _rc.FillRect(sx + margin, sy + margin, ts - margin * 2, ts - margin * 2, c);
                string label = obj.SubType.Length > 0 ? obj.SubType[..1] : "?";
                _rc.DrawText(label, sx + margin + 2, sy + margin + 2);
            }
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

        /// <summary>
        /// Treraders statusfält: läge/karta/brush — tile-info/lägesspecifik info — tangentbord.
        /// Innehållet i rad 2 anpassas till aktivt läge. Alla rader klipps hårt vid
        /// kartområdets kant (mapAreaWidth) för att inte hamna under palette-sidebaren.
        /// </summary>
        private void DrawHud(GameContext context, int hoverTileX, int hoverTileY, int mapAreaWidth)
        {
            if (_mapAdapter == null || _levelObj == null) return;

            // Max tecken som ryms i kartområdet innan palette-sidebaren
            int maxChars = (mapAreaWidth - 2) / GameConstants.FontCharWidth;

            string modeLabel = _mode switch
            {
                EditorMode.Collision => "COL",
                EditorMode.Spawn     => "SPAWN",
                EditorMode.Goal      => "GOAL",
                EditorMode.Pickup    => "ITEM",
                EditorMode.Enemy     => "ENEMY",
                _                    => "TILES"
            };

            string tileInfo = hoverTileX >= 0
                ? $"({hoverTileX},{hoverTileY}) id:{_mapAdapter.GetIndex(hoverTileX, hoverTileY)}"
                  + (_mapAdapter.GetSolid(hoverTileX, hoverTileY) ? "[S]" : "[O]")
                : "[palette]";

            // Rad 2 — visar bara det som är relevant för aktivt läge
            string spawnInfo = _levelObj.HasSpawn
                ? $"sp:({_levelObj.SpawnX},{_levelObj.SpawnY})"
                : "sp:none";
            var    goalObj   = _levelObj.Objects.Find(o => o.ObjectType == "Goal");
            string goalInfo  = goalObj != null
                ? $"g:({goalObj.TileX},{goalObj.TileY})"
                : "g:none";
            int pickups = _levelObj.Objects.FindAll(o => o.ObjectType == "Pickup").Count;
            int enemies = _levelObj.Objects.FindAll(o => o.ObjectType == "Enemy").Count;

            string modeInfo = _mode switch
            {
                EditorMode.Spawn  => spawnInfo,
                EditorMode.Goal   => goalInfo,
                EditorMode.Pickup => $"p:{pickups}",
                EditorMode.Enemy  => $"e:{enemies} {EnemySubTypes[_selectedEnemySubType]}",
                _                 => spawnInfo   // Tiles + Collision
            };

            string mouseCtrl = _mode switch
            {
                EditorMode.Collision => "LMB=solid RMB=clr",
                EditorMode.Spawn     => "LMB=set RMB=clr",
                EditorMode.Goal      => "LMB=set RMB=clr",
                EditorMode.Pickup    => "LMB=add RMB=del",
                EditorMode.Enemy     => "LMB=add RMB=del",
                _                    => "LMB=tile RMB=del"
            };

            bool   undoActive = _undoMode;
            string dirtyMark  = _isDirty ? "*" : "";
            string row1 = $"[{modeLabel}] {_mapId}{dirtyMark} {_mapAdapter.Width}x{_mapAdapter.Height}  b:{_selectedTileId}";
            string row2 = $"{tileInfo}  {modeInfo}  {mouseCtrl}";
            string row3 = "C=col G=spawn T=goal I=item E=enemy U=undo";
            string row4 = undoActive ? "[UNDO MODE - LMB erases]" : "L=maps S=save";

            const int hudHeight = 44;
            _rc.FillRect(0, 0, context.ScreenWidth, hudHeight, new RenderColor(0, 0, 0, 170));
            _rc.DrawText(HudFit(row1, maxChars), 2, 2);
            _rc.DrawText(HudFit(row2, maxChars), 2, 12);
            _rc.DrawText(HudFit(row3, maxChars), 2, 22);
            _rc.DrawText(HudFit(row4, maxChars), 2, 33);

            if (_hudMessageTimer > 0f)
                _rc.DrawText(_hudMessage, 2, hudHeight + 1);
        }

        private static string HudFit(string s, int maxChars)
            => s.Length > maxChars ? s[..maxChars] : s;
    }
}
