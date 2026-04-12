#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Delegerar inställningsoperationer till Aggregate. Bryter states-beroendet
    /// på Aggregate.Instance.Settings utan att flytta eller duplicera lagringlogiken.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Facade)
    ///
    /// MOTIVERING:
    /// Settings ägs och persisteras av Aggregate. Att flytta den hade inneburit
    /// omskrivning av fil-I/O och laddningslogik. SettingsService är ett tunt lager
    /// som exponerar enbart de operationer states behöver via ISettingsService.
    ///
    /// ANVÄNDNING:
    /// new SettingsService() i Program.OnCreate(). Kräver att Aggregate.Instance
    /// är initialiserat. Injiceras via GameServices.Settings.
    /// </remarks>
    public class SettingsService : ISettingsService
    {
        /// <inheritdoc/>
        public bool AudioOn
        {
            get => Aggregate.Instance.Settings!.AudioOn;
            set => Aggregate.Instance.Settings!.AudioOn = value;
        }

        /// <inheritdoc/>
        public SaveSlot ActivePlayer => Aggregate.Instance.Settings!.ActivePlayer;

        /// <inheritdoc/>
        public SaveSlotMainObj SaveSlots => Aggregate.Instance.Settings!.SaveSlotsObjs;

        /// <inheritdoc/>
        public void ClearSaveSlot(int slot)
        {
            var slots = Aggregate.Instance.Settings!.SaveSlotsObjs;
            if (slot == 1)      slots.SlotOne   = new SaveSlot();
            else if (slot == 2) slots.SlotTwo   = new SaveSlot();
            else if (slot == 3) slots.SlotThree = new SaveSlot();
        }

        /// <inheritdoc/>
        public void Save() => Aggregate.Instance.SaveSettings();
    }
}
