#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
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
    internal enum EditorMode { Tiles, Collision, Spawn, Goal, Pickup, Enemy, StopPoint }

    internal sealed class EditorState : IGameState
    {
        // ── Developer-gate ───────────────────────────────────────────────────────
        // Sätt till true för att aktivera världskarte-editering (StopPoint-läge +
        // tilesheetwm). Dolt för vanliga spelare — editorn beter sig normalt när false.
        // static readonly (inte const) förhindrar CS0162-varning för gatad kod.
        private static readonly bool DevMode = false;

        // ── Kamerakonstanter ────────────────────────────────────────────────────
        private const float ScrollSpeed = 8.0f;

        // ── Markörkonstanter ────────────────────────────────────────────────────
        // Analogspakens markörfart i skärmpixlar/sekund vid fullt utslag.
        private const float CursorSpeed = 160.0f;

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
        private static readonly RenderColor StopPointMarkerColor =
            new RenderColor(180, 80, 220, 220);

        private readonly GameServices   _services;
        private readonly IRenderContext _rc;

        private LevelObj?           _levelObj;
        private LevelObjMapAdapter? _mapAdapter;
        private string              _mapId = "";
        private float               _camTargetX;
        private float               _camTargetY;
        private CameraView          _editorCam;
        private int                 _selectedTileId;
        private EditorMode          _mode;
        private bool                _undoMode;       // toggle via U — LMB raderar istället för målar
        private int                 _selectedPickupSubType;
        private int                 _selectedEnemySubType;
        private int                 _selectedStopPointSubType;

        // ── Delad markör (mus + analogspak) ──────────────────────────────────────
        // Editorn har en enda markör som styrs av antingen musen (absolut) eller
        // vänster analogspak (inkrementellt). Senast rörda enhet vinner — båda är
        // alltid aktiva. Placering och palettval läser markören, inte musen direkt.
        private float               _cursorX;
        private float               _cursorY;
        private int                 _prevMouseX;
        private int                 _prevMouseY;

        private static readonly string[] PickupSubTypes = { "Energy" };
        private static readonly string[] EnemySubTypes  = { "Penguin", "Walrus", "Frost" };

        // ── Verktygspanel — lägesknappar ─────────────────────────────────────────
        // Klickbara lägesval under tile-paletten. Korta etiketter (≤4 tecken) så de
        // ryms i sidofältets två kolumner. StopPoint läggs till i DevMode.
        private static readonly (EditorMode Mode, string Label)[] ModeButtons =
        {
            (EditorMode.Tiles,     "Tile"),
            (EditorMode.Collision, "Coll"),
            (EditorMode.Spawn,     "Spwn"),
            (EditorMode.Goal,      "Goal"),
            (EditorMode.Pickup,    "Item"),
            (EditorMode.Enemy,     "Enmy"),
        };
        // Tom sträng = korsning (ingen bana). Ordningen matchar den naturliga banlistan.
        private static readonly string[] StopPointSubTypes =
        {
            "",
            MapName.MapOne, MapName.MapTwo,   MapName.MapThree,
            MapName.MapFour, MapName.MapFive,  MapName.MapSix,
            MapName.MapSeven, MapName.MapEight, MapName.MapNine
        };

        // ── HUD-meddelanden ─────────────────────────────────────────────────────
        private string _hudMessage     = "";
        private float  _hudMessageTimer;
        private const float HudMessageDuration = 2.0f;

        // ── Dirty-flagga ────────────────────────────────────────────────────────
        private bool _isDirty;
        private bool _escapePendingDirty;

        // ── Slot-picker ─────────────────────────────────────────────────────────
        private bool              _showMapPicker;
        private int               _pickerIndex;
        private List<MapPickerEntry>? _pickerEntries;
        private bool              _pickerPendingDirty;

        private const int PickerLineHeight = 10;
        private const int PickerX          = 16;
        private const int PickerStartY     = 24;

        private static readonly RenderColor PickerOverlayColor = new RenderColor(0,   0,   0,  180);

        private sealed class MapPickerEntry
        {
            public string Label   { get; init; } = "";
            public string SlotId  { get; init; } = "";
            public bool   IsEmpty { get; init; }
            public bool   IsBack  { get; init; }
        }

        // ── Ny-karta-dialog ──────────────────────────────────────────────────────
        private bool    _showNewMapDialog;
        private int     _newMapWidthIdx    = 3;   // index i WidthPresets  → 32
        private int     _newMapHeightIdx   = 3;   // index i HeightPresets → 24
        private int     _newMapTilesetIdx;         // index i KnownTilesets → tilesheetspring
        private int     _newMapDialogField;        // 0=bredd, 1=höjd, 2=tileset, 3=create, 4=back
        private bool    _newMapPendingDirty;
        private string? _pendingNewSlotId;         // vilket slot N-dialogen ska spara till

        // Dialogen är en vertikal meny: tre värderader följt av två handlingsrader.
        private const int NewMapFieldCount  = 5;
        private const int NewMapCreateField = 3;
        private const int NewMapBackField   = 4;

        private static readonly int[]    WidthPresets  = { 16, 20, 24, 32, 40, 48, 64, 80, 96, 128, 192, 256 };
        private static readonly int[]    HeightPresets = { 14, 16, 20, 24, 32, 40, 48, 64, 80, 96, 128, 192, 256 };
        private static readonly string[] KnownTilesets =
        {
            "tilesheetspring.tsx", "tilesheetsummer.tsx",
            "tilesheetfall.tsx",   "tilesheetwinter.tsx",
            "tilesheetcustom.tsx"
        };

        // I DevMode går även världskartans egen tilesheet att välja.
        private static readonly string[] KnownTilesetsDevMode =
            KnownTilesets.Concat(new[] { "tilesheetwm.tsx" }).ToArray();

        private static string[] AvailableTilesets => DevMode ? KnownTilesetsDevMode : KnownTilesets;

        /// <summary>Skapar ett nytt EditorState.</summary>
        /// <param name="services">Gemensamma speltjänster (input, kamera, renderer m.m.).</param>
        public EditorState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        /// <summary>
        /// Öppnar slot-pickern vid normal start.
        /// Vid återkomst från preview (karta redan laddad) återregistreras tilesheet utan reset.
        /// </summary>
        public void Enter(GameContext context)
        {
            // Initiera den delade markören till musens nuvarande position.
            _cursorX    = _services.Input.MouseX;
            _cursorY    = _services.Input.MouseY;
            _prevMouseX = _services.Input.MouseX;
            _prevMouseY = _services.Input.MouseY;

            // Returning from preview — keep current map and editor state intact
            if (_mapAdapter != null)
            {
                RegisterTilesheet(_levelObj!.TilesetSource);
                return;
            }

            _selectedTileId = 0;
            _mode           = EditorMode.Tiles;
            OpenMapPicker();
        }

        /// <summary>
        /// Hanterar input och beräknar kamera för aktuell frame.
        /// </summary>
        public void Update(GameContext context, float deltaTime)
        {
            _services.Input.Poll();

            if (!_services.Input.IsWindowFocused) return;

            // Markör-bokföring varje frame (även under dialoger) så att växling
            // mellan mus och analogspak förblir korrekt. mouseMoved fångas innan
            // _prevMouse uppdateras; själva markörflytten sker i huvudgrenen nedan.
            int  curMouseX  = _services.Input.MouseX;
            int  curMouseY  = _services.Input.MouseY;
            bool mouseMoved = curMouseX != _prevMouseX || curMouseY != _prevMouseY
                           || _services.Input.IsMouseLeftDown || _services.Input.IsMouseRightDown;
            _prevMouseX = curMouseX;
            _prevMouseY = curMouseY;

            if (_hudMessageTimer > 0f)
                _hudMessageTimer -= deltaTime;

            // Escape: stäng öppen dialog, eller lämna editorn
            if (_services.Input.IsCancelPressed)
            {
                if (_showNewMapDialog)
                {
                    CancelNewMapDialog();
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
                RequestExit(context);
                return;
            }

            bool noDialog = !_showMapPicker && !_showNewMapDialog;

            // L eller N öppnar slot-pickern
            if (noDialog && (_services.Input.IsEditorLoad || _services.Input.IsEditorNew))
                OpenMapPicker();

            int mapAreaWidth = context.ScreenWidth - PaletteWidth;
            int visX = mapAreaWidth / GameConstants.TileSize;
            int visY = context.ScreenHeight / GameConstants.TileSize;

            // Slot-picker — beräkna kamera för bakgrunden och hantera input
            if (_showMapPicker)
            {
                if (_mapAdapter != null && _levelObj != null)
                    _editorCam = _services.Camera.Calculate(
                        _camTargetX + visX / 2f, _camTargetY + visY / 2f,
                        _mapAdapter.Width, _mapAdapter.Height,
                        mapAreaWidth, context.ScreenHeight);
                UpdateMapPicker(context);
                return;
            }

            // Ny-karta-dialog
            if (_showNewMapDialog)
            {
                if (_mapAdapter != null && _levelObj != null)
                    _editorCam = _services.Camera.Calculate(
                        _camTargetX + visX / 2f, _camTargetY + visY / 2f,
                        _mapAdapter.Width, _mapAdapter.Height,
                        mapAreaWidth, context.ScreenHeight);
                UpdateNewMapDialog();
                return;
            }

            if (_mapAdapter == null || _levelObj == null) return;

            _editorCam = _services.Camera.Calculate(
                _camTargetX + visX / 2f, _camTargetY + visY / 2f,
                _mapAdapter.Width, _mapAdapter.Height,
                mapAreaWidth, context.ScreenHeight);

            // Flytta den delade markören (mus ELLER analogspak; senast rörda vinner).
            (_cursorX, _cursorY) = EditorMath.UpdateCursor(
                _cursorX, _cursorY, mouseMoved, curMouseX, curMouseY,
                _services.Input.LeftStickX, _services.Input.LeftStickY,
                CursorSpeed, deltaTime,
                context.ScreenWidth - 1, context.ScreenHeight - 1);

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
            if (DevMode && _services.Input.IsEditorToggleStopPoint)
            {
                if (_mode != EditorMode.StopPoint)
                {
                    _mode = EditorMode.StopPoint;
                    _selectedStopPointSubType = 0;
                }
                else
                {
                    _mode = EditorMode.Tiles;
                }
            }

            if (_mode == EditorMode.Pickup && PickupSubTypes.Length > 1)
            {
                if (_services.Input.IsLeftPressed)
                    _selectedPickupSubType = (_selectedPickupSubType - 1 + PickupSubTypes.Length) % PickupSubTypes.Length;
                if (_services.Input.IsRightPressed)
                    _selectedPickupSubType = (_selectedPickupSubType + 1) % PickupSubTypes.Length;
            }
            if (_mode == EditorMode.StopPoint)
            {
                if (_services.Input.IsLeftPressed)
                    _selectedStopPointSubType = (_selectedStopPointSubType - 1 + StopPointSubTypes.Length) % StopPointSubTypes.Length;
                if (_services.Input.IsRightPressed)
                    _selectedStopPointSubType = (_selectedStopPointSubType + 1) % StopPointSubTypes.Length;
            }
            if (_services.Input.IsEditorUndoPressed)
                _undoMode = !_undoMode;

            if (_services.Input.IsEditorPreviewPlay)
            {
                HandlePreviewPlay(context);
                return;
            }

            bool cursorInMap = CursorX < mapAreaWidth;
            if (cursorInMap)
            {
                if      (_mode == EditorMode.Tiles)     HandleTilePainting(_editorCam);
                else if (_mode == EditorMode.Collision) HandleCollisionPainting(_editorCam);
                else if (_mode == EditorMode.Spawn)     HandleSpawnPlacement(_editorCam);
                else if (_mode == EditorMode.Goal)      HandleGoalPlacement(_editorCam);
                else if (_mode == EditorMode.Pickup)    HandlePickupPlacement(_editorCam);
                else if (_mode == EditorMode.StopPoint) HandleStopPointPlacement(_editorCam);
                else                                    HandleEnemyPlacement(_editorCam);
            }
            else
            {
                if (_mode == EditorMode.Tiles)
                    HandlePaletteClick(context, mapAreaWidth);
                HandleToolPanelClick(context, mapAreaWidth);
            }
        }

        /// <summary>
        /// Ritar kartan med rutnät, palette, kursor och HUD — eller den aktiva dialogen.
        /// </summary>
        public void Draw(IRenderContext renderContext, GameContext context)
        {
            int mapAreaWidth = context.ScreenWidth - PaletteWidth;

            if (_showMapPicker)
            {
                if (_mapAdapter != null && _levelObj != null)
                {
                    DrawTiles(_editorCam);
                    DrawMapBounds(_editorCam, mapAreaWidth, context.ScreenHeight);
                    DrawGrid(_editorCam, mapAreaWidth, context.ScreenHeight);
                    DrawPalette(context, mapAreaWidth);
                }
                DrawMapPickerOverlay(context);
                return;
            }

            if (_showNewMapDialog)
            {
                if (_mapAdapter != null && _levelObj != null)
                {
                    DrawTiles(_editorCam);
                    DrawMapBounds(_editorCam, mapAreaWidth, context.ScreenHeight);
                    DrawGrid(_editorCam, mapAreaWidth, context.ScreenHeight);
                    DrawPalette(context, mapAreaWidth);
                }
                DrawNewMapDialogOverlay(context);
                return;
            }

            if (_mapAdapter == null || _levelObj == null) return;

            DrawTiles(_editorCam);
            DrawMapBounds(_editorCam, mapAreaWidth, context.ScreenHeight);
            if (_mode == EditorMode.Collision)
                DrawCollisionOverlay(_editorCam, mapAreaWidth, context.ScreenHeight);
            DrawGrid(_editorCam, mapAreaWidth, context.ScreenHeight);

            if (_levelObj.HasSpawn)
                DrawSpawnMarker(_editorCam, mapAreaWidth);
            if (_levelObj.HasGoal)
                DrawGoalMarker(_editorCam, mapAreaWidth);
            DrawPickupMarkers(_editorCam, mapAreaWidth);
            DrawEnemyMarkers(_editorCam, mapAreaWidth);
            if (DevMode) DrawStopPointMarkers(_editorCam, mapAreaWidth);

            bool cursorInMap = CursorX < mapAreaWidth;
            if (cursorInMap)
            {
                var (hx, hy) = EditorMath.ScreenToTile(CursorX, CursorY, _editorCam);
                DrawCursor(hx, hy, _editorCam);
                DrawHud(context, hx, hy, mapAreaWidth);
            }
            else
            {
                DrawHud(context, -1, -1, mapAreaWidth);
            }

            DrawPalette(context, mapAreaWidth);
            DrawToolPanel(context, mapAreaWidth);
        }

        /// <summary>Ingen städning krävs.</summary>
        public void Exit(GameContext context) { }

        // ── Delad markör — mus eller gamepad ─────────────────────────────────────
        // "Vänsterklick"/"högerklick" är musknapp ELLER gamepad A/X. Markörens
        // tile-koordinat tas från den delade markörpositionen, inte musen direkt.

        private int CursorX => (int)_cursorX;
        private int CursorY => (int)_cursorY;

        private bool PrimaryDown      => _services.Input.IsMouseLeftDown    || _services.Input.IsEditorPrimaryDown;
        private bool SecondaryDown    => _services.Input.IsMouseRightDown   || _services.Input.IsEditorSecondaryDown;
        private bool PrimaryPressed   => _services.Input.IsMouseLeftPressed  || _services.Input.IsEditorPrimaryPressed;
        private bool SecondaryPressed => _services.Input.IsMouseRightPressed || _services.Input.IsEditorSecondaryPressed;

        // ── Verktygspanel — klickbara kontroller i sidofältet ────────────────────
        // En klickbar kontroll med skärmrektangel, etikett, aktiv-flagga och
        // handling. Samma kodväg träffas av mus och stick-markör eftersom markören
        // är delad. Byggs varje frame av BuildToolPanel och delas av rit + klick.

        private readonly struct PanelButton
        {
            public readonly int X, Y, W, H;
            public readonly string Label;
            public readonly bool Active;
            public readonly Action OnClick;

            public PanelButton(int x, int y, int w, int h, string label, bool active, Action onClick)
            {
                X = x; Y = y; W = w; H = h; Label = label; Active = active; OnClick = onClick;
            }

            public bool Contains(int px, int py) => px >= X && px < X + W && py >= Y && py < Y + H;
        }

        /// <summary>Aktiva läget har en subtyp att cykla (fiendetyp, item, stoppunkt).</summary>
        private bool ModeHasSubType =>
               _mode == EditorMode.Enemy
            || (_mode == EditorMode.Pickup && PickupSubTypes.Length > 1)
            || _mode == EditorMode.StopPoint;

        /// <summary>Etikett för aktiva lägets nuvarande subtyp (tom om läget saknar subtyp).</summary>
        private string CurrentSubTypeLabel => _mode switch
        {
            EditorMode.Enemy     => EnemySubTypes[_selectedEnemySubType],
            EditorMode.Pickup    => PickupSubTypes[_selectedPickupSubType],
            EditorMode.StopPoint => StopPointSubTypes[_selectedStopPointSubType].Length > 0
                                        ? StopPointSubTypes[_selectedStopPointSubType] : "junction",
            _                    => ""
        };

        /// <summary>Byter redigeringsläge och nollställer subtyp vid byte (som tangentbordet).</summary>
        private void SetMode(EditorMode mode)
        {
            if (_mode == mode) return;
            _mode = mode;
            if (mode == EditorMode.Enemy)     _selectedEnemySubType     = 0;
            if (mode == EditorMode.StopPoint) _selectedStopPointSubType = 0;
        }

        /// <summary>Cyklar aktiva lägets subtyp i given riktning (+1/−1).</summary>
        private void CycleSubType(int dir)
        {
            if (_mode == EditorMode.Enemy)
                _selectedEnemySubType = (_selectedEnemySubType + dir + EnemySubTypes.Length) % EnemySubTypes.Length;
            else if (_mode == EditorMode.Pickup && PickupSubTypes.Length > 1)
                _selectedPickupSubType = (_selectedPickupSubType + dir + PickupSubTypes.Length) % PickupSubTypes.Length;
            else if (_mode == EditorMode.StopPoint)
                _selectedStopPointSubType = (_selectedStopPointSubType + dir + StopPointSubTypes.Length) % StopPointSubTypes.Length;
        }

        /// <summary>
        /// Lämnar editorn till menyn — med samma osparade-ändringar-varning som Escape.
        /// Delas av Escape/B-knappen och panelens Back-knapp.
        /// </summary>
        private void RequestExit(GameContext context)
        {
            // Osparade ändringar: första försöket varnar. Ett nytt försök *medan
            // varningen fortfarande syns* bekräftar och lämnar. Hinner varningen
            // försvinna får man frågan igen — så varje färskt försök (Esc/B/Back)
            // är konsekvent, oavsett vilket man pressade sist.
            if (_isDirty && !(_escapePendingDirty && _hudMessageTimer > 0f))
            {
                ShowMessage("Unsaved changes! Press again to exit.");
                _escapePendingDirty = true;
                return;
            }
            _escapePendingDirty = false;
            context.MenuNavigation = Enum.MenuState.StartMenu;
            _services.StateManager.Transition(new MenuState(_services), context);
        }

        /// <summary>
        /// Bygger sidofältets verktygspanel: lägesknappar (2 kolumner), en subtyp-
        /// knapp för lägen med subtyp, samt Save/Load/Play/Back. Geometrin delas av
        /// både ritning och klick-hittest så de aldrig kan glida isär.
        /// </summary>
        private List<PanelButton> BuildToolPanel(GameContext context, int mapAreaWidth)
        {
            var list = new List<PanelButton>();

            int innerX = mapAreaWidth + PalettePadding;
            int innerW = PaletteWidth - PalettePadding * 2;
            int colW   = innerW / 2;
            int rowH   = 11;
            int y      = PalettePadding + GameConstants.TileSheetRows * GameConstants.TileSize + 2;

            var modes = DevMode
                ? ModeButtons.Append((Mode: EditorMode.StopPoint, Label: "Stop")).ToArray()
                : ModeButtons;

            for (int i = 0; i < modes.Length; i++)
            {
                int col = i % 2, row = i / 2;
                var m = modes[i];
                list.Add(new PanelButton(
                    innerX + col * colW, y + row * rowH, colW - 1, rowH - 1,
                    m.Label, _mode == m.Mode, () => SetMode(m.Mode)));
            }
            y += ((modes.Length + 1) / 2) * rowH + 3;

            if (ModeHasSubType)
            {
                list.Add(new PanelButton(
                    innerX, y, innerW, rowH - 1,
                    HudFit(CurrentSubTypeLabel, innerW / GameConstants.FontCharWidth),
                    false, () => CycleSubType(1)));
                y += rowH + 3;
            }

            var actions = new (string Label, Action OnClick)[]
            {
                ("Save", HandleSave),
                ("Load", OpenMapPicker),
                ("Play", () => HandlePreviewPlay(context)),
                ("Back", () => RequestExit(context)),
            };
            for (int i = 0; i < actions.Length; i++)
            {
                int col = i % 2, row = i / 2;
                list.Add(new PanelButton(
                    innerX + col * colW, y + row * rowH, colW - 1, rowH - 1,
                    actions[i].Label, false, actions[i].OnClick));
            }

            return list;
        }

        /// <summary>Hanterar primärklick på en verktygspanel-kontroll vid markörens position.</summary>
        private void HandleToolPanelClick(GameContext context, int mapAreaWidth)
        {
            if (!PrimaryPressed) return;
            foreach (var b in BuildToolPanel(context, mapAreaWidth))
                if (b.Contains(CursorX, CursorY)) { b.OnClick(); return; }
        }

        /// <summary>Ritar verktygspanelen: aktivt läge fylls, markörhovrad kontroll får gul ram.</summary>
        private void DrawToolPanel(GameContext context, int mapAreaWidth)
        {
            foreach (var b in BuildToolPanel(context, mapAreaWidth))
            {
                var fill = b.Active
                    ? new RenderColor(70, 110, 170, 255)
                    : new RenderColor(40, 40, 40, 255);
                _rc.FillRect(b.X, b.Y, b.W, b.H, fill);

                var border = b.Active ? RenderColor.White : new RenderColor(110, 110, 110, 255);
                if (b.Contains(CursorX, CursorY))
                    border = new RenderColor(255, 220, 0, 200);
                DrawRectBorder(b.X, b.Y, b.W, b.H, border);

                _rc.DrawText(b.Label, b.X + 2, b.Y + 2);
            }
        }

        private void DrawRectBorder(int x, int y, int w, int h, RenderColor c)
        {
            _rc.DrawLine(x,         y,         x + w - 1, y,         c);
            _rc.DrawLine(x,         y + h - 1, x + w - 1, y + h - 1, c);
            _rc.DrawLine(x,         y,         x,         y + h - 1, c);
            _rc.DrawLine(x + w - 1, y,         x + w - 1, y + h - 1, c);
        }

        // ── Kameranavigering ─────────────────────────────────────────────────────

        private void HandleCameraScroll(float deltaTime, int mapWidth, int mapHeight, int visX, int visY)
        {
            float d = ScrollSpeed * deltaTime;
            if (_services.Input.IsEditorScrollRight) _camTargetX += d;
            if (_services.Input.IsEditorScrollLeft)  _camTargetX -= d;
            if (_services.Input.IsEditorScrollDown)  _camTargetY += d;
            if (_services.Input.IsEditorScrollUp)    _camTargetY -= d;

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
            if (!PrimaryPressed) return;

            int relX = CursorX - mapAreaWidth - PalettePadding;
            int relY = CursorY - PalettePadding;
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

            int relX      = CursorX - mapAreaWidth - PalettePadding;
            int relY      = CursorY - PalettePadding;
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
            bool leftDown  = PrimaryDown;
            bool rightDown = SecondaryDown;
            bool erase     = rightDown || (_undoMode && leftDown);
            bool place     = leftDown && !_undoMode;
            if (!place && !erase) return;

            var (tx, ty) = EditorMath.ScreenToTile(CursorX, CursorY, cam);

            _mapAdapter!.SetTile(tx, ty, place ? _selectedTileId : 0);
            _isDirty = true;
        }

        /// <summary>
        /// Hanterar kollisionsredigering: vänster = solid, höger = öppen.
        /// </summary>
        private void HandleCollisionPainting(CameraView cam)
        {
            bool leftDown    = PrimaryDown;
            bool rightDown   = SecondaryDown;
            bool removeSolid = rightDown || (_undoMode && leftDown);
            bool addSolid    = leftDown && !_undoMode;
            if (!addSolid && !removeSolid) return;

            var (tx, ty) = EditorMath.ScreenToTile(CursorX, CursorY, cam);

            _mapAdapter!.SetSolid(tx, ty, addSolid);
            _isDirty = true;
        }

        /// <summary>
        /// Startar en testspelning (preview) av aktuell karta.
        /// Kräver spawn-punkt och mål. Esc under preview återvänder hit.
        /// </summary>
        private void HandlePreviewPlay(GameContext context)
        {
            if (_levelObj == null || _mapAdapter == null) return;

            if (!_levelObj.HasSpawn)
            {
                ShowMessage("Preview: no spawn — place spawn (G)");
                return;
            }
            if (!_levelObj.HasGoal)
            {
                ShowMessage("Preview: no goal — place goal (T)");
                return;
            }

            var userMap = new UserMap(_levelObj, _mapId, _services.Assets);

            // Förbered context för preview
            var hero = context.Player!;
            hero.px     = _levelObj.SpawnX;
            hero.py     = _levelObj.SpawnY;
            hero.vx     = 0f;
            hero.vy     = 0f;
            hero.Health = 9;

            // UserMap items start at ID 1000. Clear any IDs from a previous
            // preview run so GameplayState.Enter doesn't remove them again.
            context.CollectedEnergiIds.RemoveWhere(id => id >= 1000);

            context.ActiveObjects.Clear();
            context.ActiveObjects.Add(hero);
            userMap.PopulateDynamics(context.ActiveObjects);

            context.CurrentLevel         = userMap;
            context.IsPreviewMode        = true;
            context.PreviewReturnState   = this;
            context.UserMapRunStartTime  = context.GameTotalTime;

            // Registrera tilesheet för GameplayState-renderingen
            RegisterTilesheet(_levelObj.TilesetSource);

            _services.StateManager.Transition(new GameplayState(_services), context);
        }

        /// <summary>
        /// Sparar kartan till UserMaps/. Kräver att spawn-punkt är satt.
        /// </summary>
        private void HandleSave()
        {
            if (_levelObj == null) return;

            // Världskartan har ingen traditionell spawn — positionen styrs av SpawnAtWorldMap
            // i save-datan och WorldMapSystem.GetSpawnPosition(). Spawn-kravet gäller bara
            // vanliga banor där hjälten faktiskt spawnar på en specifik tile.
            bool isWorldMap = _mapId == MapName.WorldMap;
            if (!isWorldMap && !_levelObj.HasSpawn)
            {
                ShowMessage("No spawn set — place spawn (G) before saving!");
                return;
            }

            _services.UserMaps.Save(_mapId, _levelObj);
            _isDirty            = false;
            _escapePendingDirty = false;
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
            bool clearPressed = SecondaryPressed || (_undoMode && PrimaryPressed);
            bool placePressed = !_undoMode && PrimaryPressed;
            if (!placePressed && !clearPressed) return;

            if (clearPressed)
            {
                _levelObj!.SpawnX = -1;
                _levelObj!.SpawnY = -1;
                return;
            }

            var (tx, ty) = EditorMath.ScreenToTile(CursorX, CursorY, cam);

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
            bool clearPressed = SecondaryPressed || (_undoMode && PrimaryPressed);
            bool placePressed = !_undoMode && PrimaryPressed;
            if (!placePressed && !clearPressed) return;

            if (clearPressed)
            {
                _levelObj!.Objects.RemoveAll(o => o.ObjectType == "Goal");
                _isDirty = true;
                return;
            }

            var (tx, ty) = EditorMath.ScreenToTile(CursorX, CursorY, cam);

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
            bool clearPressed = SecondaryPressed || (_undoMode && PrimaryPressed);
            bool placePressed = !_undoMode && PrimaryPressed;
            if (!placePressed && !clearPressed) return;

            var (tx, ty) = EditorMath.ScreenToTile(CursorX, CursorY, cam);

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
            bool clearPressed = SecondaryPressed || (_undoMode && PrimaryPressed);
            bool placePressed = !_undoMode && PrimaryPressed;
            if (!placePressed && !clearPressed) return;

            var (tx, ty) = EditorMath.ScreenToTile(CursorX, CursorY, cam);

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

        /// <summary>
        /// Placerar en stoppunkt (LMB) eller tar bort den på tilen (RMB).
        /// En stoppunkt per tile — befintlig ersätts vid ny placering.
        /// SubType väljs med vänster/höger piltangent; tom sträng = korsning.
        /// </summary>
        private void HandleStopPointPlacement(CameraView cam)
        {
            bool clearPressed = SecondaryPressed || (_undoMode && PrimaryPressed);
            bool placePressed = !_undoMode && PrimaryPressed;
            if (!placePressed && !clearPressed) return;

            var (tx, ty) = EditorMath.ScreenToTile(CursorX, CursorY, cam);

            if (tx < 0 || tx >= _mapAdapter!.Width || ty < 0 || ty >= _mapAdapter!.Height) return;

            _levelObj!.Objects.RemoveAll(
                o => o.ObjectType == "StopPoint" && o.TileX == tx && o.TileY == ty);

            if (placePressed)
            {
                _levelObj.Objects.Add(new PlacedObject
                {
                    ObjectType = "StopPoint",
                    SubType    = StopPointSubTypes[_selectedStopPointSubType],
                    TileX      = tx,
                    TileY      = ty
                });
            }
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
            // Använd den tilesheet som matchar kartans valda TilesetSource. För vanliga
            // banor är det en transparent säsongs-sheet (transparensen krävs för att
            // parallax-lagret ska synas bakom tiles vid spel); för världskartan i DevMode
            // väljs tilesheetwm — men styrt av kartdatan, inte av ett hårdkodat undantag.
            string? path = _services.Assets.GetSpritePath(SpriteRef.TileSheetForTileset(tilesetSource));
            if (path != null)
                _rc.RegisterSprite(SpriteId.MapTileSheet, path);
        }

        // ── Slot-picker ──────────────────────────────────────────────────────────

        /// <summary>
        /// Öppnar slot-pickern med de 7 fasta användarkartorna.
        /// Slottar utan sparad fil visas som tomma.
        /// I DevMode visas världskartan som en extra rad överst.
        /// </summary>
        private void OpenMapPicker()
        {
            _pickerEntries = new List<MapPickerEntry>();
            var existing   = new System.Collections.Generic.HashSet<string>(
                _services.UserMaps.GetAvailableMapIds());

            // DevMode: världskarta-rad överst — särskiljer sig tydligt från slot-filerna
            if (DevMode)
            {
                bool wmExists = existing.Contains(MapName.WorldMap);
                _pickerEntries.Add(new MapPickerEntry
                {
                    Label   = wmExists ? $"World Map  [{MapName.WorldMap}]" : "World Map  [Empty]",
                    SlotId  = MapName.WorldMap,
                    IsEmpty = !wmExists
                });
            }

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

            // Tillbaka-rad sist — samma "aktiva back" som övriga menyer har, så man
            // inte måste känna till Esc-genvägen.
            _pickerEntries.Add(new MapPickerEntry { Label = "Back", IsBack = true });

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
                _newMapDialogField = (_newMapDialogField + 1) % NewMapFieldCount;
            if (_services.Input.IsUpPressed)
                _newMapDialogField = (_newMapDialogField + NewMapFieldCount - 1) % NewMapFieldCount;

            // Vänster/höger justerar bara värderaderna (0–2); handlingsraderna ignorerar dem.
            if (_services.Input.IsRightPressed) ChangeNewMapField(+1);
            if (_services.Input.IsLeftPressed)  ChangeNewMapField(-1);

            // Confirm aktiverar markerad handlingsrad — Esc backar fortfarande direkt.
            if (_services.Input.IsConfirmPressed && !_services.Input.IsEditorSave)
            {
                if (_newMapDialogField == NewMapCreateField)    ConfirmNewMap();
                else if (_newMapDialogField == NewMapBackField) CancelNewMapDialog();
            }
        }

        /// <summary>Stänger ny-karta-dialogen och återgår till slot-pickern.</summary>
        private void CancelNewMapDialog()
        {
            _showNewMapDialog   = false;
            _newMapPendingDirty = false;
            _pendingNewSlotId   = null;
            OpenMapPicker();
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
                    _newMapTilesetIdx = (_newMapTilesetIdx + delta + AvailableTilesets.Length) % AvailableTilesets.Length;
                    break;
            }
        }

        private void ConfirmNewMap()
        {
            // Om en karta redan är öppen och osparad — varna en gång
            if (_isDirty && _mapAdapter != null && !_newMapPendingDirty)
            {
                ShowMessage("Unsaved changes! Press again to discard.");
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
                TilesetSource  = AvailableTilesets[_newMapTilesetIdx],
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
            ShowMessage($"New map '{mapId}' created — save to keep it.");
        }

        private void DrawNewMapDialogOverlay(GameContext context)
        {
            const int FontW = GameConstants.FontCharWidth;
            const int LineH = 14;

            _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, PickerOverlayColor);

            int cx = context.ScreenWidth / 2;

            string title = _pendingNewSlotId != null
                ? $"NEW MAP  ({_pendingNewSlotId})"
                : "NEW MAP";
            _rc.DrawText(title, cx - (title.Length * FontW) / 2, 24);

            // Värderaderna har vänsterjusterade etiketter (padding 9) så att < >-värdena
            // ligger i linje; handlingsraderna är rena menyval.
            string[] rows =
            {
                $"{"Width:",-9}< {WidthPresets[_newMapWidthIdx]} >",
                $"{"Height:",-9}< {HeightPresets[_newMapHeightIdx]} >",
                $"{"Tileset:",-9}< {ShortTilesetName(AvailableTilesets[_newMapTilesetIdx])} >",
                "Create",
                "Back"
            };

            // Centrera hela blocket på den bredaste raden (inkl. "> "-markörens 2 tecken).
            int maxLen = 0;
            foreach (var r in rows) if (r.Length > maxLen) maxLen = r.Length;
            int blockX = cx - ((maxLen + 2) * FontW) / 2;

            const int startY = 48;
            for (int i = 0; i < rows.Length; i++)
            {
                int  y        = startY + i * LineH;
                bool selected = i == _newMapDialogField;
                _rc.DrawText(selected ? $"> {rows[i]}" : $"  {rows[i]}", blockX, y);
            }

            if (_hudMessageTimer > 0f)
                _rc.DrawText(_hudMessage,
                    cx - (_hudMessage.Length * FontW) / 2,
                    startY + rows.Length * LineH + 6);
        }

        /// <summary>
        /// Kortar ner ett tileset-filnamn för visning: "tilesheetspring.tsx" → "spring".
        /// </summary>
        private static string ShortTilesetName(string tileset)
        {
            string s = tileset;
            if (s.EndsWith(".tsx")) s = s.Substring(0, s.Length - 4);
            if (s.StartsWith("tilesheet")) s = s.Substring("tilesheet".Length);
            return s.Length == 0 ? tileset : s;
        }

        private void UpdateMapPicker(GameContext context)
        {
            if (_pickerEntries == null) return;

            if (_services.Input.IsUpPressed)
                _pickerIndex = Math.Max(0, _pickerIndex - 1);
            if (_services.Input.IsDownPressed)
                _pickerIndex = Math.Min(_pickerEntries.Count - 1, _pickerIndex + 1);

            // Exkludera Ctrl+S — IsConfirmPressed inkluderar S vilket annars
            // triggar en oavsiktlig kartladdning när användaren sparar med Ctrl+S.
            if (_services.Input.IsConfirmPressed && !_services.Input.IsEditorSave)
                ConfirmPickerLoad(context);
        }

        private void ConfirmPickerLoad(GameContext context)
        {
            if (_pickerEntries == null) return;
            var entry = _pickerEntries[_pickerIndex];

            if (entry.IsBack)
            {
                // Samma beteende som Esc i pickern: ingen karta laddad → lämna
                // editorn, annars stäng bara pickern.
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
                ShowMessage("Unsaved changes! Press again to discard.");
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

            string header = "Select map to edit";
            int hx = (context.ScreenWidth / 2) - ((header.Length * 8) / 2);
            _rc.DrawText(header, hx, 8);

            int y = PickerStartY;
            for (int i = 0; i < _pickerEntries.Count; i++)
            {
                var  entry    = _pickerEntries[i];
                bool selected = i == _pickerIndex;

                // Markeras med "> "-prefix som övriga menyer — ingen avvikande ram.
                _rc.DrawText(selected ? $"> {entry.Label}" : $"  {entry.Label}", PickerX, y);

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

        /// <summary>
        /// Ritar en lila fylld ruta med kort label för varje stoppunkt på världskartan.
        /// Visas bara i DevMode. Label: "JX" för korsning, siffra för bana (1–9).
        /// </summary>
        private void DrawStopPointMarkers(CameraView cam, int mapAreaWidth)
        {
            int ts     = GameConstants.TileSize;
            int margin = 1;
            var c      = StopPointMarkerColor;

            foreach (var obj in _levelObj!.Objects)
            {
                if (obj.ObjectType != "StopPoint") continue;

                var (sx, sy) = EditorMath.TileToScreen(obj.TileX, obj.TileY, cam);
                if (sx >= mapAreaWidth) continue;

                _rc.FillRect(sx + margin, sy + margin, ts - margin * 2, ts - margin * 2, c);
                _rc.DrawText(StopPointLabel(obj.SubType), sx + margin + 2, sy + margin + 2);
            }
        }

        private static string StopPointLabel(string subType) => subType switch
        {
            ""                => "JX",
            MapName.MapOne    => "1",
            MapName.MapTwo    => "2",
            MapName.MapThree  => "3",
            MapName.MapFour   => "4",
            MapName.MapFive   => "5",
            MapName.MapSix    => "6",
            MapName.MapSeven  => "7",
            MapName.MapEight  => "8",
            MapName.MapNine   => "9",
            _                 => "?"
        };

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
        /// Fyrraders statusfält: läge/karta/brush — tile-/lägesinfo — primär/sekundär-
        /// betydelse (A/X) — global navigering (B/d-pad). Tangentbordscentrerade
        /// instruktioner är borttagna; verktygspanelen visar alla åtgärder som
        /// klickbara knappar. Alla rader klipps hårt vid kartområdets kant
        /// (mapAreaWidth) för att inte hamna under palette-sidebaren.
        /// </summary>
        private void DrawHud(GameContext context, int hoverTileX, int hoverTileY, int mapAreaWidth)
        {
            if (_mapAdapter == null || _levelObj == null) return;

            // Max tecken som ryms i kartområdet innan palette-sidebaren
            int maxChars = (mapAreaWidth - 2) / GameConstants.FontCharWidth;

            string modeLabel = _mode switch
            {
                EditorMode.Collision  => "COL",
                EditorMode.Spawn      => "SPAWN",
                EditorMode.Goal       => "GOAL",
                EditorMode.Pickup     => "ITEM",
                EditorMode.Enemy      => "ENEMY",
                EditorMode.StopPoint  => "STOP",
                _                     => "TILES"
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

            int stopPoints = _levelObj.Objects.FindAll(o => o.ObjectType == "StopPoint").Count;
            string stopSubType = StopPointSubTypes[_selectedStopPointSubType];
            string stopLabel   = stopSubType.Length > 0 ? stopSubType : "junction";

            string modeInfo = _mode switch
            {
                EditorMode.Spawn      => spawnInfo,
                EditorMode.Goal       => goalInfo,
                EditorMode.Pickup     => $"p:{pickups}",
                EditorMode.Enemy      => $"e:{enemies} {EnemySubTypes[_selectedEnemySubType]}",
                EditorMode.StopPoint  => $"sp:{stopPoints} [{stopLabel}]",
                _                     => spawnInfo   // Tiles + Collision
            };

            // Lägesspecifik betydelse för primär/sekundär. Etiketterna är input-agnostiska:
            // A (gamepad) och LMB (mus) gör samma sak, X och RMB likaså. Det får inte plats
            // på en rad (kartområdet rymmer ~21 tecken), så primär och sekundär står på var
            // sin rad. Back (Esc/B) och scroll (Dpad/piltangenter) listas inte — de upptäcks
            // via Back-knappen i panelen respektive bara genom att prova.
            (string primary, string secondary) = _mode switch
            {
                EditorMode.Collision  => ("solid", "clear"),
                EditorMode.Spawn      => ("set",   "clear"),
                EditorMode.Goal       => ("set",   "clear"),
                EditorMode.Pickup     => ("add",   "del"),
                EditorMode.Enemy      => ("add",   "del"),
                EditorMode.StopPoint  => ("add",   "del"),
                _                     => ("tile",  "erase")
            };

            bool   undoActive = _undoMode;
            string dirtyMark  = _isDirty ? "*" : "";
            string row1 = $"[{modeLabel}] {_mapId}{dirtyMark} {_mapAdapter.Width}x{_mapAdapter.Height} b:{_selectedTileId}";
            string row2 = $"{tileInfo}  {modeInfo}";
            // I undo-läge raderar primär istället för att placera.
            string row3 = undoActive ? $"A/LMB = {secondary} [UNDO]" : $"A/LMB = {primary}";
            string row4 = $"X/RMB = {secondary}";

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
