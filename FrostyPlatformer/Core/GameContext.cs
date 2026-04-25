#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Commands;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Models.Objects;

namespace FrostyPlatformer.Core
{
    /// <summary>
    /// Delat dataobjekt som håller det aktiva speltillståndet och injiceras
    /// i alla system och spellägen.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Blackboard (Parameter Object)
    ///
    /// MOTIVERING:
    /// Spelstatens data var utspridd i statiska fält på Aggregate.Instance och som
    /// instansfält direkt på Program. Det gjorde det omöjligt att isolera eller
    /// testa system utan att starta hela applikationen. GameContext samlar all
    /// levande speldata på ett ställe och skickas explicit som parameter — inga
    /// dolda beroenden via singletons.
    ///
    /// Alternativet (fortsätta med Aggregate.Instance) innebär att varje system
    /// håller en implicit global koppling som varken går att ersätta i tester
    /// eller byta ut utan att röra all spelkod.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.OnCreate() (Composition Root) och hålls som
    /// privat fält. Delegeras till via property-wrappers i Program.cs för att
    /// befintlig kod fortsätter kompilera utan ändringar. Framtida system och
    /// spellägen tar emot GameContext som konstruktor- eller Update-parameter.
    ///
    /// GameContext är ett dataobjekt — ingen logik läggs här. Logik hör hemma
    /// i system (IGameSystem) eller tillstånd (IGameState).
    /// </remarks>
    public class GameContext
    {
        // ─────────────────────────────────────────────
        // Kärnentiteter
        // ─────────────────────────────────────────────

        /// <summary>Spelaren (hjälten). Sätts i OnCreate och hålls uppdaterad via Hero-wrappern.</summary>
        public DynamicCreatureHero? Player { get; set; }

        /// <summary>Aktiv karta/nivå. Byts ut vid ChangeMap.</summary>
        public Map? CurrentLevel { get; set; }

        /// <summary>
        /// Alla aktiva dynamiska objekt (spelare, fiender, items, teleportar).
        /// Listan töms och fylls om vid varje kartbyte (ChangeMap).
        /// Använd RemoveAll/Add direkt på listan — tilldela inte om referensen.
        /// </summary>
        public List<DynamicGameObject> ActiveObjects { get; } = new List<DynamicGameObject>();

        // ─────────────────────────────────────────────
        // Spelarens rörelsefysik
        // ─────────────────────────────────────────────

        /// <summary>
        /// 0 = hjälten på väg ned. 1–3 = hur länge hjälten varit i luften.
        /// Hanteras av fysik/input-logiken i DisplayStage.
        /// </summary>
        public int HeroAirBornState { get; set; }

        /// <summary>
        /// 0 = hjälten är inte på marken. 1–3 = hur länge hjälten varit på marken.
        /// Hanteras av fysik/input-logiken i DisplayStage.
        /// </summary>
        public int HeroLandedState { get; set; }

        // ─────────────────────────────────────────────
        // Quest, inventory och energi
        // ─────────────────────────────────────────────

        /// <summary>Aktiva quests. Kan ersättas med ny lista av AddQuest (prepend-logik).</summary>
        public List<Quest> ActiveQuests { get; set; } = new List<Quest>();

        /// <summary>Items i spelarens inventarier.</summary>
        public List<Item> CollectedItems { get; set; } = new List<Item>();

        /// <summary>Id:n för insamlade energi-objekt. Används för att markera dem som borttagna.</summary>
        public List<int> CollectedEnergiIds { get; set; } = new List<int>();

        // ─────────────────────────────────────────────
        // Spelmätning och timing
        // ─────────────────────────────────────────────

        /// <summary>Accumulerad speltid från senaste laddning/start.</summary>
        public TimeSpan ActualTotalTime { get; set; }

        /// <summary>Speltid exklusive paus och menylägen — det som visas på high score.</summary>
        public TimeSpan GameTotalTime { get; set; }

        /// <summary>Speltid vid avslutsskärmen — fryst när spelaren når slutet.</summary>
        public TimeSpan EndTotalTime { get; set; }

        /// <summary>Räknare för inaktivitet i idle-detektion (normaliserat till 60 fps-ekvivalenta enheter).</summary>
        public float IdleCounter { get; set; }

        // ─────────────────────────────────────────────
        // Navigationsläge för menyer (delas mellan MenuState, SettingsState, PauseState)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Vilket under-menyläge som är aktivt — styr vilken lista MenuState och
        /// SettingsState visar. Sätts av avsändarstaten innan övergången sker.
        /// </summary>
        public Enum.MenuState MenuNavigation { get; set; } = Enum.MenuState.StartMenu;

        // ─────────────────────────────────────────────
        // Tillstånd som delas mellan End- och HighScore-states
        // ─────────────────────────────────────────────

        /// <summary>
        /// True om spelaren kom till HighScore via slutskärmen — då ska vi
        /// gå tillbaka till slutskärmen efter HighScore istället för menyn.
        /// </summary>
        public bool ReturnToEndAfterHighScore { get; set; }

        /// <summary>
        /// True så länge spelaren inte lagt in ett high score i det här spelsessionen.
        /// Nollställs i Reset(). Förhindrar dubbel high score-registrering.
        /// </summary>
        public bool RightToAccessPodium { get; set; } = true;

        // ─────────────────────────────────────────────
        // Skärmdimensioner (dynamiska — uppdateras vid fönsterändring)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Skärmens bredd i logiska spelpixlar. Uppdateras av Program.cs vid
        /// fönsterändring. States använder denna istället för GameConstants.ScreenWidth
        /// för att stödja dynamisk upplösning (Fas 4b+).
        /// </summary>
        public int ScreenWidth  { get; set; } = Global.GameConstants.ScreenWidth;

        /// <summary>
        /// Skärmens höjd i logiska spelpixlar. Uppdateras av Program.cs vid
        /// fönsterändring. States använder denna istället för GameConstants.ScreenHeight
        /// för att stödja dynamisk upplösning (Fas 4b+).
        /// </summary>
        public int ScreenHeight { get; set; } = Global.GameConstants.ScreenHeight;

    }
}
