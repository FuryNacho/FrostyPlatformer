#nullable enable
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för ISaveLoadSystem. Loggar anrop och spårar slot-argument
    /// utan Aggregate-beroende, fil-I/O eller Clock-kall.
    /// </summary>
    public class FakeSaveLoadSystem : ISaveLoadSystem
    {
        public int LoadCount     { get; private set; }
        public int LastLoadSlot  { get; private set; }

        public int SaveCount     { get; private set; }
        public int LastSaveSlot  { get; private set; }

        public void Load(int slot)
        {
            LoadCount++;
            LastLoadSlot = slot;
        }

        public void Save(int slot)
        {
            SaveCount++;
            LastSaveSlot = slot;
        }
    }
}
