#nullable enable
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Abstraherar läsning och skrivning av spelinställningar och sparade spel.
    /// Tillåter states att komma åt inställningar utan att känna till Aggregate.Instance.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion (Facade)
    ///
    /// MOTIVERING:
    /// States läste och skrev tidigare direkt mot Aggregate.Instance.Settings — ett
    /// singleton-beroende som sprids i SettingsState, WorldMapState och EndState.
    /// ISettingsService exponerar enbart de operationer states behöver och döljer
    /// att data lagras i Aggregate och persisteras via ReadWrite.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new SettingsService() och injiceras via
    /// GameServices.Settings. States anropar _services.Settings.AudioOn,
    /// _services.Settings.ActivePlayer m.fl. utan Aggregate-referens.
    /// </remarks>
    public interface ISettingsService
    {
        /// <summary>Anger om spelljudet är på. Ändring sparas inte automatiskt — anropa Save() efteråt.</summary>
        bool AudioOn { get; set; }

        /// <summary>Anger om level editor-musiken är på. Ändring sparas inte automatiskt — anropa Save() efteråt.</summary>
        bool EditorAudioOn { get; set; }

        /// <summary>Den aktiva spelarens spardata (stage, spawn, flaggor m.m.).</summary>
        SaveSlot ActivePlayer { get; }

        /// <summary>Alla tre sparslottar.</summary>
        SaveSlotMainObj SaveSlots { get; }

        /// <summary>Nollställer en sparslot (1–3) till ett tomt tillstånd.</summary>
        void ClearSaveSlot(int slot);

        /// <summary>Sparar inställningar och sparslottar till disk.</summary>
        void Save();
    }
}
