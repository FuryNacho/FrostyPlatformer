#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Abstraherar laddning och sparning av speldata till/från namngivna sparslottar.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion (Facade)
    ///
    /// MOTIVERING:
    /// States (SettingsState) anropade tidigare Action&lt;int&gt;-delegates i GameServices
    /// för att ladda och spara spel. ISaveLoadSystem ersätter dessa råa delegates
    /// med ett namngivet, testbart interface som döljer att data lagras i
    /// Aggregate.Instance.Settings och att Clock.HardReset() anropas vid laddning.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new SaveLoadSystem(context) och injiceras
    /// via GameServices.SaveLoad. SettingsState anropar _services.SaveLoad.Load(slot)
    /// och _services.SaveLoad.Save(slot) utan kännedom om Aggregate eller Clock.
    /// </remarks>
    public interface ISaveLoadSystem
    {
        /// <summary>
        /// Laddar sparslott 1–3 till aktiv spelarsession.
        /// Återställer spelarens hälsa, insamlade föremål och spelklockan.
        /// </summary>
        void Load(int slot);

        /// <summary>
        /// Sparar nuvarande spelstatus till sparslott 1–3.
        /// Stämplar slotten med aktuell hälsa, tid och datum.
        /// </summary>
        void Save(int slot);
    }
}
