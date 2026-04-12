
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
using Raylib_cs;
using FrostyPlatformer.Engine.Raylib;
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
    /// Composition Root — skapar och kopplar samman alla system, startar spelloop.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Composition Root
    ///
    /// MOTIVERING:
    /// Program.cs var tidigare en 5 000-raders God Object med all spellogik inbäddad.
    /// Efter Fas 4b Steg 5 delegeras all spellogik till GameStateManager och dess
    /// IGameState-implementationer. Program.cs ansvarar nu enbart för att skapa
    /// beroenden, koppla samman systemen och starta spelloopen.
    ///
    /// ANVÄNDNING:
    /// Initialize() sätter upp alla system och registrerar SplashState som startläge.
    /// Run() innehåller Raylib-spelloopen som delegerar till GameStateManager varje frame.
    /// Infrastrukturmetoder (ChangeMap, Reset, Load, Save) injiceras som delegates
    /// i GameServices och anropas av states via _services.
    /// </remarks>
    public class Program
    {
        // ── Infrastruktur ─────────────────────────────────────────────────────
        private RaylibInputProvider         _input        = null!;
        private ICameraSystem               _camera       = null!;
        private ITileMapRenderer            _tileRenderer = null!;
        private RaylibRenderContext         _renderContext = null!;
        private RaylibAudioSystem           _audioSystem  = null!;
        private IDialogSystem               _dialog       = null!;
        private IQuestSystem                _questSystem  = null!;
        private IItemSystem                 _itemSystem   = null!;
        private IWorldMapSystem             _worldMapSystem = null!;
        private ISaveLoadSystem             _saveLoadSystem = null!;

        // ── Ny tillståndsmaskin (Fas 4b Steg 5) ──────────────────────────────
        private States.GameStateManager _stateManager = null!;
        private States.GameServices     _services     = null!;

        // ── Delad spelkontext (Blackboard) ────────────────────────────────────
        private Core.GameContext _context = new Core.GameContext();

        // ── Tidsmätning (ersätter PixelEngine Clock) ──────────────────────────
        private TimeSpan _runningTime = TimeSpan.Zero;

        // ── Avstängningsflagga (ersätter PixelEngine.Game.Finish) ─────────────
        private bool _shouldQuit = false;

        // ── Skärmkonstanter ───────────────────────────────────────────────────
        const int ScreenW = GameConstants.ScreenWidth;
        const int ScreenH = GameConstants.ScreenHeight;
        const int PixW    = GameConstants.PixelWidth;
        const int PixH    = GameConstants.PixelHeight;

        // ── Egenskaper som delegerar till _context ────────────────────────────
        private DynamicCreatureHero Hero
        {
            get => _context.Player;
            set => _context.Player = value;
        }
        private List<DynamicGameObject> listDynamics => _context.ActiveObjects;

        private FrostyPlatformer.Models.Map CurrentMap
        {
            get => _context.CurrentLevel;
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
        public List<int> EnergiIdLista
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

        // ── Entry point ───────────────────────────────────────────────────────
        static void Main(string[] args)
        {
            try
            {
                new Program().Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal error: " + ex.ToString());
            }
        }

        // ── Spelloop ──────────────────────────────────────────────────────────

        /// <summary>
        /// Initierar Raylib-fönstret, laddar resurser och kör spelloopen tills spelaren stänger fönstret.
        /// </summary>
        public void Run()
        {
            Raylib.InitWindow(ScreenW * PixW, ScreenH * PixH, "Frosty Platformer");
            Raylib.InitAudioDevice();
            Raylib.SetTargetFPS(GameConstants.FrameRate);

            Initialize();

            while (!Raylib.WindowShouldClose() && !_shouldQuit)
            {
                float elapsed = Raylib.GetFrameTime();
                _runningTime += TimeSpan.FromSeconds(elapsed);
                _context.GameTotalTime = _runningTime + _context.ActualTotalTime;

                Raylib.BeginDrawing();
                _stateManager.Update(_context, elapsed);
                Raylib.EndDrawing();
            }

            _audioSystem.CleanUp();
            _renderContext.UnloadAll();
            Raylib.CloseWindow();
        }

        // ── Initiering ────────────────────────────────────────────────────────

        private void Initialize()
        {
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
            ChangeMap("worldmap", 2, 3, Hero);

            _input        = new RaylibInputProvider();
            _camera       = new CameraSystem();
            _tileRenderer = new TileMapRenderer();
            _dialog       = new DialogSystem();

            _renderContext = new RaylibRenderContext();
            RegisterSprites();

            _audioSystem = new RaylibAudioSystem();
            RegisterSounds();

            _stateManager = new States.GameStateManager();
            _services = new States.GameServices(
                _input, _camera, _tileRenderer, _renderContext, _stateManager,
                _audioSystem,
                new ScoreSystem(),
                new ScriptSystem(),
                new SettingsService(),
                Core.Aggregate.Instance,
                _dialog,
                _questSystem,
                _itemSystem,
                _worldMapSystem,
                _saveLoadSystem,
                (mapName, x, y) => ChangeMap(mapName, x, y),
                Reset,
                () => _shouldQuit = true,
                () => { bool v = Core.Aggregate.Instance.HasSwitchedState; Core.Aggregate.Instance.HasSwitchedState = false; return v; },
                () => Core.Aggregate.Instance.HasSwitchedState = false,
                () => Core.Aggregate.Instance.CheckSwitchX(),
                id  => Core.Aggregate.Instance.GetMyX(id)
            );
            _stateManager.SetInitial(new States.SplashState(_services), _context);
        }

        private void RegisterSprites()
        {
            var agg = Core.Aggregate.Instance;

            void Reg(Rendering.SpriteId id, string? path)
            {
                if (path != null) _renderContext.RegisterSprite(id, path);
            }

            Reg(Rendering.SpriteId.Font,             agg.GetSpritePath("font"));
            Reg(Rendering.SpriteId.Items,            agg.GetSpritePath("items"));
            Reg(Rendering.SpriteId.Hero,             agg.GetSpritePath("hero"));
            Reg(Rendering.SpriteId.EnemyPenguin,     agg.GetSpritePath("enemyone"));
            Reg(Rendering.SpriteId.EnemyWalrus,      agg.GetSpritePath("enemytwo"));
            Reg(Rendering.SpriteId.EnemyFrost,       agg.GetSpritePath("enemythree"));
            Reg(Rendering.SpriteId.EnemyIcicle,      agg.GetSpritePath("enemyzero"));
            Reg(Rendering.SpriteId.EnemyBoss,        agg.GetSpritePath("enemyboss"));
            Reg(Rendering.SpriteId.EnemyWind,        agg.GetSpritePath("enemywind"));
            Reg(Rendering.SpriteId.WorldMapTileSheet, agg.GetSpritePath("tilesheetwm"));
            Reg(Rendering.SpriteId.SplashStart,       agg.GetSpritePath(SplashScreenRef.Start));
            Reg(Rendering.SpriteId.SplashEnd,         agg.GetSpritePath(SplashScreenRef.End));
            Reg(Rendering.SpriteId.EndArt,            agg.GetSpritePath("endart"));
            Reg(Rendering.SpriteId.MapTileSheet,      CurrentMap.SpritePath);
        }

        private void RegisterSounds()
        {
            if (Core.Aggregate.Instance.Settings?.Mute == true) return;

            var root     = Core.Aggregate.Instance.ReadWrite.GetRoot;
            var soundDir = System.IO.Path.Combine(root, "Resources", "Assets", "Sound");

            void Reg(string soundRef)
                => _audioSystem.RegisterSound(soundRef, System.IO.Path.Combine(soundDir, soundRef));

            Reg(SoundRef.Jump);
            Reg(SoundRef.Land);
            Reg(SoundRef.Damage);
            Reg(SoundRef.DamageHero);
            Reg(SoundRef.PickUp);
            Reg(SoundRef.BGSoundWorld);
            Reg(SoundRef.BGSoundGame);
            Reg(SoundRef.BGSoundFinalStage);
            Reg(SoundRef.BGSoundEnd);
            Reg(SoundRef.BGNearPerfectEnd);
            Reg(SoundRef.BGPerfectEnd);

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
            EnergiIdLista = new List<int>();
        }

        public void ChangeMap(string MapName, float x, float y)
        {
            ChangeMap(MapName, x, y, this.Hero);
        }

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

            // Uppdatera MapTileSheet i renderContexten när kartan byts.
            // _renderContext kan vara null under det första ChangeMap-anropet i Initialize()
            // (innan _renderContext skapats) — i det fallet sker registreringen i RegisterSprites().
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
        /// DialogSystem äger renderingen; GameplayState hanterar avfärdning och
        /// frigör skriptkön via IScriptSystem.CompleteCurrentCommand().
        /// </summary>
        public void ShowDialog(List<string> listLines) => _dialog.Show(listLines);

        /// <summary>Stänger spelfönstret och avslutar spelloopen.</summary>
        public void Finish() => _shouldQuit = true;
    }
}
