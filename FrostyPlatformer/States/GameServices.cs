#nullable enable
using System;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Samlar alla infrastrukturtjänster som ett spelläge behöver och injiceras
    /// i varje state-konstruktor för att undvika att skicka sju parametrar separat.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Service Locator (Parameter Object)
    ///
    /// MOTIVERING:
    /// Utan den här klassen skulle varje state-konstruktor behöva 7+ parametrar
    /// (IInputProvider, ICameraSystem, IRenderContext, GameStateManager, tre callbacks).
    /// GameServices paketerar dem och gör konstruktorsignaturer läsbara.
    /// Den är ett rent databärande objekt — ingen logik läggs här.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.cs (Composition Root). Skickas till varje state
    /// via konstruktorn. Callbacks (ChangeMap, Reset) pekar på metoder
    /// i Program.cs och fångar dess privata fält via closure.
    /// </remarks>
    public class GameServices
    {
        /// <summary>Abstraherat tangentbords- och gamepad-input.</summary>
        public IInputProvider Input { get; }

        /// <summary>Kameraberäkning för scroll-kartor.</summary>
        public ICameraSystem Camera { get; }

        /// <summary>Tile-renderare för kartvisning.</summary>
        public ITileMapRenderer TileRenderer { get; }

        /// <summary>Motor-agnostisk renderingskontekst för all grafik.</summary>
        public IRenderContext RenderContext { get; }

        /// <summary>Statemaskinen — används av states för att göra övergångar.</summary>
        public GameStateManager StateManager { get; }

        /// <summary>Null-säkert ljudsystem. Anrop är no-ops om hårdvara saknas.</summary>
        public IAudioSystem Audio { get; }

        /// <summary>Highscore-operationer utan direkt Aggregate-beroende.</summary>
        public IScoreSystem Score { get; }

        /// <summary>Tickar spelets kommandokö (ScriptProcessor) en gång per frame.</summary>
        public IScriptSystem Script { get; }

        /// <summary>Inställningar och sparslottar utan direkt Aggregate-beroende.</summary>
        public ISettingsService Settings { get; }

        /// <summary>Sprites, items och kartdata via Aggregates IAssets-implementation.</summary>
        public Core.IAssets Assets { get; }

        /// <summary>Repository för inbyggda spelkartor (Tiled/). Editorn läser härifrån, sparar aldrig hit.</summary>
        public Systems.IMapRepository GameMaps { get; }

        /// <summary>Repository för editorskapade kartor (UserMaps/). Editorn sparar alltid hit.</summary>
        public Systems.IMapRepository UserMaps { get; }

        /// <summary>Dialogrutor triggas av skriptsystemet och renderas av GameplayState.</summary>
        public IDialogSystem Dialog { get; }

        /// <summary>Quest-lista och map-populering för aktiva quests.</summary>
        public IQuestSystem Quests { get; }

        /// <summary>Spelarens itemlager — insamlade items.</summary>
        public IItemSystem Items { get; }

        /// <summary>Världskartans stage-ingångspunkter och spawn-positioner.</summary>
        public IWorldMapSystem WorldMap { get; }

        /// <summary>
        /// Laddar och aktiverar en karta. Kallar Program.ChangeMap(name, x, y, hero)
        /// med spelarens nuvarande hero-objekt från GameContext.
        /// </summary>
        public Action<string, float, float> ChangeMap { get; }

        /// <summary>
        /// Nollställer spelet (nytt spel). Kallar Program.Reset() som anropar
        /// Clock.HardReset() och återställer GameContext-fält.
        /// </summary>
        public Action Reset { get; }

        /// <summary>Laddar och sparar spel till/från namngivna sparslottar.</summary>
        public ISaveLoadSystem SaveLoad { get; }

        /// <summary>Avslutar spelloopen. Kallar Program.Finish() via Aggregate.</summary>
        public Action ExitGame { get; }

        /// <summary>
        /// Kontrollerar om ett skript triggat en kartwitch denna frame och nollställer flaggan.
        /// Returnerar true om en switch är väntande (GameplayState ska rensa aktiva objekt).
        /// </summary>
        public Func<bool> CheckAndClearSwitchedState { get; }

        /// <summary>Nollställer ScriptSwitchedState-flaggan (anropas i Enter).</summary>
        public Action ClearSwitchedState { get; }

        /// <summary>
        /// Uppdaterar bossbanans X-switch-mekanik (CheckSwitchX i Aggregate).
        /// Anropas varje frame för Boss-objektet på mapnine.
        /// </summary>
        public Action TriggerBossCheck { get; }

        /// <summary>Returnerar X-positionen för ett boss-objekt med givet id (GetMyX).</summary>
        public Func<int, int> GetBossObjectX { get; }

        /// <summary>
        /// Skapar ett nytt GameServices med alla beroenden och callbacks.
        /// </summary>
        public GameServices(
            IInputProvider input,
            ICameraSystem camera,
            ITileMapRenderer tileRenderer,
            IRenderContext renderContext,
            GameStateManager stateManager,
            IAudioSystem audio,
            IScoreSystem score,
            IScriptSystem script,
            ISettingsService settings,
            Core.IAssets assets,
            Systems.IMapRepository gameMaps,
            Systems.IMapRepository userMaps,
            IDialogSystem dialog,
            IQuestSystem quests,
            IItemSystem items,
            IWorldMapSystem worldMap,
            ISaveLoadSystem saveLoad,
            Action<string, float, float> changeMap,
            Action reset,
            Action exitGame,
            Func<bool> checkAndClearSwitchedState,
            Action clearSwitchedState,
            Action triggerBossCheck,
            Func<int, int> getBossObjectX)
        {
            Input                    = input;
            Camera                   = camera;
            TileRenderer             = tileRenderer;
            RenderContext            = renderContext;
            StateManager             = stateManager;
            Audio                    = audio;
            Score                    = score;
            Script                   = script;
            Settings                 = settings;
            Assets                   = assets;
            GameMaps                 = gameMaps;
            UserMaps                 = userMaps;
            Dialog                   = dialog;
            Quests                   = quests;
            Items                    = items;
            WorldMap                 = worldMap;
            SaveLoad                 = saveLoad;
            ChangeMap                = changeMap;
            Reset                    = reset;
            ExitGame                 = exitGame;
            CheckAndClearSwitchedState = checkAndClearSwitchedState;
            ClearSwitchedState       = clearSwitchedState;
            TriggerBossCheck         = triggerBossCheck;
            GetBossObjectX           = getBossObjectX;
        }
    }
}
