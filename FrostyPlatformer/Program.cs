
/***************************************************************************************************************************************
* Developer notes                                                                                                                      *
*                                                                                                                                      *
* This project (Penguin After All) started out as a pure tinker project trying out OLCPixelGameEngine.                                 *
*                                                                                                                                      *
* As the project progressed I started indulging the thought of actually publishing the compiled version to the public.                 *
* The game is available for free on itch.io (https://furynacho.itch.io/penguin-after-all)                                              *
* Later, this project became a tinkering project to test out Claude Code. This gave new life to the project                            *
* and the goal became to refactor and update the project.                                                                              *
*                                                                                                                                      *
* I want to remind you that all creative content belonging to this project is copyright protected.                                     *
*                                                                                                                                      *
* 2026-04-06, Dev.                                                                                                                     *
*                                                                                                                                      *
***************************************************************************************************************************************/

#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrostyPlatformer.Engine.MonoGame;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Commands;
using FrostyPlatformer.Models;
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer
{
    /// <summary>
    /// Composition Root — ärver MonoGame.Framework.Game, skapar och kopplar samman
    /// alla system, hanterar spelets livscykel via Initialize/LoadContent/Update/Draw.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Composition Root
    ///
    /// MOTIVERING:
    /// Program.cs var tidigare en Raylib-spelloop. I Fas 2e ersätts den med MonoGames
    /// Game-klass. All spellogik delegeras till GameStateManager/IGameState — Program
    /// ansvarar enbart för att skapa beroenden, koppla samman systemen och starta
    /// spelloopen.
    ///
    /// MONOGAME-LOOPMÖNSTER:
    /// Logik körs i Update(GameTime) via _stateManager.Update().
    /// Rendering körs i Draw(GameTime) via _stateManager.Draw().
    /// IGameState.Update hanterar input och spellogik; IGameState.Draw hanterar rendering (SRP).
    ///
    /// CLEAR:
    /// GraphicsDevice.Clear() måste anropas FÖRE SpriteBatch.Begin(). Rensningen sker
    /// i Draw() här, en gång per frame, innan batchen öppnas.
    /// IRenderContext.Clear() i states är ett no-op och används inte.
    /// </remarks>
    public class Program : Game
    {
        // ── MonoGame-infrastruktur ────────────────────────────────────────────
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch                    _spriteBatch   = null!;
        private Microsoft.Xna.Framework.Input.KeyboardState _prevKeyboard;

        // ── Spelsystem ────────────────────────────────────────────────────────
        private MonoGameInputProvider   _input         = null!;
        private ICameraSystem           _camera        = null!;
        private ITileMapRenderer        _tileRenderer  = null!;
        private MonoGameRenderContext   _renderContext = null!;
        private MonoGameAudioSystem     _audioSystem   = null!;
        private IDialogSystem           _dialog        = null!;
        private IQuestSystem            _questSystem   = null!;
        private IItemSystem             _itemSystem    = null!;
        private IWorldMapSystem         _worldMapSystem  = null!;
        private ISaveLoadSystem         _saveLoadSystem  = null!;

        // ── Tillståndsmaskin ──────────────────────────────────────────────────
        private States.GameStateManager _stateManager = null!;
        private States.GameServices     _services     = null!;

        // ── Delad spelkontext (Blackboard) ────────────────────────────────────
        private Core.GameContext _context = new Core.GameContext();

        // ── Tidsmätning ───────────────────────────────────────────────────────
        private float    _elapsed     = 0f;
        private TimeSpan _runningTime = TimeSpan.Zero;

        // ── Skärmkonstanter ───────────────────────────────────────────────────
        private const int ScreenW = GameConstants.ScreenWidth;
        private const int ScreenH = GameConstants.ScreenHeight;
        private const int PixW    = GameConstants.PixelWidth;
        private const int PixH    = GameConstants.PixelHeight;

        // ── Egenskaper som delegerar till _context ────────────────────────────
        private DynamicCreatureHero Hero
        {
            get => _context.Player!;
            set => _context.Player = value;
        }
        private List<DynamicGameObject> listDynamics => _context.ActiveObjects;
        private FrostyPlatformer.Models.Map CurrentMap
        {
            get => _context.CurrentLevel!;
            set => _context.CurrentLevel = value;
        }
        private List<Quest> ListQuests
        {
            get => _context.ActiveQuests;
            set => _context.ActiveQuests = value;
        }
        private List<Item> ListItems
        {
            get => _context.CollectedItems;
            set => _context.CollectedItems = value;
        }
        public HashSet<int> EnergiIdLista
        {
            get => _context.CollectedEnergiIds;
            set => _context.CollectedEnergiIds = value;
        }
        private TimeSpan ActualTotalTime
        {
            get => _context.ActualTotalTime;
            set => _context.ActualTotalTime = value;
        }
        private bool RightToAccessPodium
        {
            get => _context.RightToAccessPodium;
            set => _context.RightToAccessPodium = value;
        }

        // ── Konstruktor ───────────────────────────────────────────────────────

        public Program()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth  = ScreenW * PixW;
            _graphics.PreferredBackBufferHeight = ScreenH * PixH;
            IsFixedTimeStep        = true;
            TargetElapsedTime      = TimeSpan.FromSeconds(1.0 / 60.0);
            Window.AllowUserResizing = true;
            Content.RootDirectory  = ".";
            Window.Title           = "Frosty Platformer";
        }

        // ── Entry point ───────────────────────────────────────────────────────
        static void Main()
        {
            try
            {
                using var game = new Program();
                game.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal error: " + ex.ToString());
            }
        }

        // ── MonoGame livscykel ────────────────────────────────────────────────

        /// <summary>
        /// Skapar spelsystem som inte behöver GraphicsDevice. Anropas av MonoGame
        /// innan LoadContent.
        /// </summary>
        protected override void Initialize()
        {
            Window.ClientSizeChanged += OnClientSizeChanged;

            Core.Aggregate.Instance.Load(this);

            _questSystem    = new QuestSystem(_context);
            _itemSystem     = new ItemSystem(_context);
            _worldMapSystem = new WorldMapSystem();
            _saveLoadSystem = new SaveLoadSystem(
                _context,
                new SaveGameRepository(),
                () => _runningTime = TimeSpan.Zero);

            ActualTotalTime = new TimeSpan();
            Hero = new DynamicCreatureHero();
            ChangeMap(MapName.WorldMap, 2, 3, Hero);

            _input        = new MonoGameInputProvider(() => IsActive);
            _camera       = new CameraSystem();
            _tileRenderer = new TileMapRenderer();
            _dialog       = new DialogSystem();

            base.Initialize();
        }

        /// <summary>
        /// Skapar SpriteBatch, RenderContext och AudioSystem. Registrerar sprites och ljud.
        /// Kopplar ihop GameServices och sätter SplashState som startläge.
        /// </summary>
        protected override void LoadContent()
        {
            _spriteBatch   = new SpriteBatch(GraphicsDevice);
            // PixW×PixH (4×4): varje logisk spelpixel ritas som ett 4×4-block direkt
            // mot backbuffer — inget render-target-mellansteg behövs.
            _renderContext = new MonoGameRenderContext(GraphicsDevice, _spriteBatch, scaleX: PixW, scaleY: PixH);
            RegisterSprites();

            _audioSystem = new MonoGameAudioSystem();
            RegisterSounds();

            _stateManager = new States.GameStateManager();

            string mapDataRoot  = System.IO.Path.Combine(
                Core.Aggregate.Instance.ReadWrite.GetRoot,
                "Resources", "Assets", "MapData");
            string gameMapsPath = System.IO.Path.Combine(mapDataRoot, "Tiled");
            string userMapsPath = System.IO.Path.Combine(mapDataRoot, "UserMaps");

            _services = new States.GameServices(
                _input, _camera, _tileRenderer, _renderContext, _stateManager,
                _audioSystem,
                new ScoreSystem(),
                new ScriptSystem(),
                new SettingsService(),
                Core.Aggregate.Instance,
                new Systems.TiledMapRepository(gameMapsPath),
                new Systems.TiledMapRepository(userMapsPath, scanDirectory: true),
                _dialog,
                _questSystem,
                _itemSystem,
                _worldMapSystem,
                _saveLoadSystem,
                new Systems.UserMapScoreRepository(Core.Aggregate.Instance.ReadWrite),
                (mapName, x, y) => ChangeMap(mapName, x, y),
                Reset,
                () => Exit(),
                () => { bool v = Core.Aggregate.Instance.HasSwitchedState; Core.Aggregate.Instance.HasSwitchedState = false; return v; },
                () => Core.Aggregate.Instance.HasSwitchedState = false,
                () => Core.Aggregate.Instance.CheckSwitchX(),
                id  => Core.Aggregate.Instance.GetMyX(id)
            );
            _stateManager.SetInitial(new States.SplashState(_services), _context);
            UpdateScreenDimensions();
        }

        /// <summary>
        /// Spårar förfluten tid, hanterar fönsterkontrollen F11 (helskärm) och
        /// kör spellogik via _stateManager.Update().
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            _elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _runningTime += TimeSpan.FromSeconds(_elapsed);
            _context.GameTotalTime = _runningTime + _context.ActualTotalTime;

            var kb = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            if (kb.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F11) &&
                _prevKeyboard.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.F11))
                _graphics.ToggleFullScreen();
            _prevKeyboard = kb;

            _stateManager.Update(_context, _elapsed);

            base.Update(gameTime);
        }

        /// <summary>
        /// Enkelt-pass-rendering: rensar backbuffern och anropar _stateManager.Draw().
        /// Ingen render target används — koordinater skalas med PixW×PixH (4×4).
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null, null, null, null);
            _stateManager.Draw(_renderContext, _context);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>Frigör GPU-texturer och ljud-resurser vid programavslut.</summary>
        protected override void UnloadContent()
        {
            _audioSystem?.CleanUp();
            _renderContext?.UnloadAll();
            base.UnloadContent();
        }

        // ── Fönsterhantering ──────────────────────────────────────────────────

        /// <summary>
        /// Anropas av MonoGame när fönstrets klientyta ändrar storlek.
        /// Uppdaterar backbuffern och <see cref="Core.GameContext.ScreenWidth/Height"/>
        /// så att alla states alltid arbetar med faktiska skärmdimensioner.
        /// </summary>
        private void OnClientSizeChanged(object? sender, EventArgs e)
        {
            int w = Window.ClientBounds.Width;
            int h = Window.ClientBounds.Height;
            if (w <= 0 || h <= 0) return; // ignorera minimering

            _graphics.PreferredBackBufferWidth  = w;
            _graphics.PreferredBackBufferHeight = h;
            _graphics.ApplyChanges();
            UpdateScreenDimensions();
        }

        /// <summary>
        /// Synkroniserar <see cref="Core.GameContext.ScreenWidth/Height"/> med
        /// den aktuella viewport-storleken i logiska spelpixlar (pixlar / PixW|PixH).
        /// </summary>
        private void UpdateScreenDimensions()
        {
            _context.ScreenWidth  = GraphicsDevice.Viewport.Width  / PixW;
            _context.ScreenHeight = GraphicsDevice.Viewport.Height / PixH;
        }

        // ── Sprite- och ljudregistrering ──────────────────────────────────────

        private void RegisterSprites()
        {
            var agg = Core.Aggregate.Instance;

            void Reg(Rendering.SpriteId id, string? path)
            {
                if (path != null) _renderContext.RegisterSprite(id, path);
            }

            Reg(Rendering.SpriteId.Font,              agg.GetSpritePath("font"));
            Reg(Rendering.SpriteId.Items,             agg.GetSpritePath("items"));
            Reg(Rendering.SpriteId.Hero,              agg.GetSpritePath("hero"));
            Reg(Rendering.SpriteId.EnemyPenguin,      agg.GetSpritePath("enemyone"));
            Reg(Rendering.SpriteId.EnemyWalrus,       agg.GetSpritePath("enemytwo"));
            Reg(Rendering.SpriteId.EnemyFrost,        agg.GetSpritePath("enemythree"));
            Reg(Rendering.SpriteId.EnemyIcicle,       agg.GetSpritePath("enemyzero"));
            Reg(Rendering.SpriteId.EnemyBoss,         agg.GetSpritePath("enemyboss"));
            Reg(Rendering.SpriteId.EnemyWind,         agg.GetSpritePath("enemywind"));
            Reg(Rendering.SpriteId.WorldMapTileSheet,  agg.GetSpritePath("tilesheetwm"));
            Reg(Rendering.SpriteId.SplashStart,        agg.GetSpritePath(SplashScreenRef.Start));
            Reg(Rendering.SpriteId.SplashEnd,          agg.GetSpritePath(SplashScreenRef.End));
            Reg(Rendering.SpriteId.EndArt,             agg.GetSpritePath("endart"));
            Reg(Rendering.SpriteId.MapTileSheet,       CurrentMap.SpritePath);
        }

        private void RegisterSounds()
        {
            var root     = Core.Aggregate.Instance.ReadWrite.GetRoot;
            var soundDir = System.IO.Path.Combine(root, "Resources", "Assets", "Sound");

            void Reg(string soundRef, bool isLooped = false)
                => _audioSystem.RegisterSound(soundRef,
                       System.IO.Path.Combine(soundDir, soundRef),
                       isLooped);

            Reg(SoundRef.Jump);
            Reg(SoundRef.Land);
            Reg(SoundRef.Damage);
            Reg(SoundRef.DamageHero);
            Reg(SoundRef.PickUp);
            Reg(SoundRef.BGSoundWorld,      isLooped: true);
            Reg(SoundRef.BGSoundGame,       isLooped: true);
            Reg(SoundRef.BGSoundFinalStage, isLooped: true);
            Reg(SoundRef.BGSoundEnd,        isLooped: true);
            Reg(SoundRef.BGNearPerfectEnd,  isLooped: true);
            Reg(SoundRef.BGPerfectEnd,      isLooped: true);

            if (Core.Aggregate.Instance.Settings?.AudioOn == true)
                _audioSystem.UnMute();
            else
                _audioSystem.Mute();
        }

        // ── Infrastrukturoperationer ──────────────────────────────────────────

        private void Reset()
        {
            Core.Aggregate.Instance.Settings!.ActivePlayer = new SaveSlot();
            Hero.Health = Core.Aggregate.Instance.Settings.ActivePlayer.HeroEnergi;
            ActualTotalTime = new TimeSpan();
            _runningTime    = TimeSpan.Zero;
            RightToAccessPodium = true;
            Core.Aggregate.Instance.Settings.ActivePlayer.StageCompleted = 0;
            EnergiIdLista = new HashSet<int>();
        }

        public void ChangeMap(string MapName, float x, float y)
            => ChangeMap(MapName, x, y, this.Hero);

        public void ChangeMap(string MapName, float x, float y, DynamicGameObject hero)
        {
            listDynamics.Clear();
            listDynamics.Add(hero);
            var map = Core.Aggregate.Instance.GetMap(MapName);
            if (map == null)
            {
                Core.Aggregate.Instance.ReadWrite.WriteToLog($"ChangeMap: kartan '{MapName}' finns inte.");
                throw new ArgumentException($"Kartan '{MapName}' är inte laddad.", nameof(MapName));
            }
            CurrentMap = map;

            // _renderContext kan vara null under det första ChangeMap-anropet i Initialize()
            // (innan LoadContent körts). Registreringen sker då i RegisterSprites().
            if (_renderContext != null && CurrentMap.SpritePath != null)
                _renderContext.RegisterSprite(Rendering.SpriteId.MapTileSheet, CurrentMap.SpritePath);

            hero.px = x;
            hero.py = y;

            CurrentMap.PopulateDynamics(listDynamics);
            _questSystem.PopulateForMap(listDynamics, CurrentMap.Name);
        }

        public void AddQuest(Quest quest) => _questSystem.Add(quest);

        public bool GiveItem(Item item)
        {
            _itemSystem.Collect(item);
            return true;
        }

        /// <summary>
        /// Anropas av skriptsystemet (CommandShowDialog) för att visa en dialogruta.
        /// </summary>
        public void ShowDialog(List<string> listLines) => _dialog.Show(listLines);

        /// <summary>Stänger spelfönstret och avslutar spelloopen via MonoGames Exit().</summary>
        public void Finish() => Exit();
    }
}
