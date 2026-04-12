#nullable enable
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// In-memory-implementering av ISaveGameRepository för testning.
    /// Låter tester kontrollera vad som finns i slottarna och verifiera vad som skrivs,
    /// utan Aggregate-beroende eller fil-I/O.
    /// </summary>
    public class FakeSaveGameRepository : ISaveGameRepository
    {
        private SaveSlot _slotOne   = new SaveSlot();
        private SaveSlot _slotTwo   = new SaveSlot();
        private SaveSlot _slotThree = new SaveSlot();
        private SaveSlot _active    = new SaveSlot();

        // ── Statistik för verifiering ────────────────────────────────────────

        public int ReadSlotCallCount  { get; private set; }
        public int LastReadSlot       { get; private set; }

        public int WriteSlotCallCount { get; private set; }
        public int LastWriteSlot      { get; private set; }
        public SaveSlot? LastWrittenSlotData { get; private set; }

        public int WriteActivePlayerCallCount { get; private set; }
        public SaveSlot? LastWrittenActivePlayer { get; private set; }

        // ── Testhjälpare: sätt initialdata ───────────────────────────────────

        /// <summary>Fyller en sparslott med testdata före testet kör.</summary>
        public void SetSlot(int slot, SaveSlot data)
        {
            if (slot == 3)       _slotThree = data;
            else if (slot == 2)  _slotTwo   = data;
            else                 _slotOne   = data;
        }

        /// <summary>Sätter ActivePlayer-data innan testet kör.</summary>
        public void SetActivePlayer(SaveSlot data) => _active = data;

        // ── ISaveGameRepository ───────────────────────────────────────────────

        public SaveSlot ReadSlot(int slot)
        {
            ReadSlotCallCount++;
            LastReadSlot = slot;
            if (slot == 3) return _slotThree;
            if (slot == 2) return _slotTwo;
            return _slotOne;
        }

        public void WriteSlot(int slot, SaveSlot data)
        {
            WriteSlotCallCount++;
            LastWriteSlot      = slot;
            LastWrittenSlotData = data;
            if (slot == 3)      _slotThree = data;
            else if (slot == 2) _slotTwo   = data;
            else                _slotOne   = data;
        }

        public SaveSlot ReadActivePlayer() => _active;

        public void WriteActivePlayer(SaveSlot data)
        {
            WriteActivePlayerCallCount++;
            LastWrittenActivePlayer = data;
            _active = data;
        }

        /// <summary>Returnerar den sparslott som senast skrevs, oberoende av slot-nummer.</summary>
        public SaveSlot ReadWrittenSlot(int slot)
        {
            if (slot == 3) return _slotThree;
            if (slot == 2) return _slotTwo;
            return _slotOne;
        }
    }
}
