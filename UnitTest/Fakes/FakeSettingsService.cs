#nullable enable
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för ISettingsService. Hanterar en in-memory SettingsObj och
    /// loggar anrop så att tester kan verifiera beteende utan fil-I/O eller Aggregate.
    /// </summary>
    public class FakeSettingsService : ISettingsService
    {
        private readonly SettingsObj _settings = new SettingsObj { ActivePlayer = new SaveSlot() };

        public int SaveCount        { get; private set; }
        public int ClearSlotCount   { get; private set; }
        public int LastClearedSlot  { get; private set; }

        public bool AudioOn
        {
            get => _settings.AudioOn;
            set => _settings.AudioOn = value;
        }

        public bool EditorAudioOn
        {
            get => _settings.EditorAudioOn;
            set => _settings.EditorAudioOn = value;
        }

        public ScreenMode? ScreenMode
        {
            get => _settings.ScreenMode;
            set => _settings.ScreenMode = value;
        }

        public SaveSlot ActivePlayer => _settings.ActivePlayer;

        public SaveSlotMainObj SaveSlots => _settings.SaveSlotsObjs;

        public void ClearSaveSlot(int slot)
        {
            ClearSlotCount++;
            LastClearedSlot = slot;
            if (slot == 1)      _settings.SaveSlotsObjs.SlotOne   = new SaveSlot();
            else if (slot == 2) _settings.SaveSlotsObjs.SlotTwo   = new SaveSlot();
            else if (slot == 3) _settings.SaveSlotsObjs.SlotThree = new SaveSlot();
        }

        public void Save() => SaveCount++;
    }
}
