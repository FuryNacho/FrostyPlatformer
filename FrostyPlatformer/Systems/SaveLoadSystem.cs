#nullable enable
using System;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Hanterar laddning och sparning av speldata via ett injicerat ISaveGameRepository.
    /// Synkroniserar GameContext med sparad data vid laddning.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion (Facade)
    ///
    /// MOTIVERING:
    /// Load/Save-metoderna låg tidigare i Program.cs och anropade Aggregate.Instance
    /// direkt, vilket omöjliggjorde enhetstestning. SaveLoadSystem tar emot ett
    /// ISaveGameRepository och en Action för klockreset, vilket gör hela klassen
    /// testbar utan hårdvara, Aggregate eller Clock.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() med en SaveGameRepository och en lambda
    /// som anropar Clock.HardReset() (Clock är instansegenskap på Game,
    /// inte åtkomlig utanför Program). Injiceras via GameServices.SaveLoad.
    /// </remarks>
    public class SaveLoadSystem : ISaveLoadSystem
    {
        private readonly GameContext         _context;
        private readonly ISaveGameRepository _repo;
        private readonly Action              _clockHardReset;

        public SaveLoadSystem(GameContext context, ISaveGameRepository repo, Action clockHardReset)
        {
            _context        = context;
            _repo           = repo;
            _clockHardReset = clockHardReset;
        }

        /// <summary>
        /// Kopierar sparslottens data till aktiv spelsession och återställer klockan.
        /// </summary>
        public void Load(int slot)
        {
            var src = _repo.ReadSlot(slot);
            var copyObj = new SaveSlot
            {
                DateTime        = src.DateTime,
                Time            = src.Time,
                IsUsed          = src.IsUsed,
                HeroEnergi      = src.HeroEnergi,
                StageCompleted  = src.StageCompleted,
                SpawnAtWorldMap = src.SpawnAtWorldMap,
                ShowEnd         = src.ShowEnd,
                EnergiCollected = src.EnergiCollected
            };

            _context.ActualTotalTime = copyObj.Time;
            _repo.WriteActivePlayer(copyObj);
            _context.Player!.Health  = copyObj.HeroEnergi;
            _context.CollectedEnergiIds = copyObj.EnergiCollected;
            _clockHardReset();
        }

        /// <summary>
        /// Kopierar nuvarande spelstatus till angiven sparslott.
        /// Åsidosätter hälsa, tid och datum med aktuella runtime-värden.
        /// </summary>
        public void Save(int slot)
        {
            var active = _repo.ReadActivePlayer();
            var snapshot = new SaveSlot
            {
                DateTime        = active.DateTime,
                Time            = active.Time,
                IsUsed          = active.IsUsed,
                HeroEnergi      = active.HeroEnergi,
                StageCompleted  = active.StageCompleted,
                SpawnAtWorldMap = active.SpawnAtWorldMap,
                ShowEnd         = active.ShowEnd,
                EnergiCollected = _context.CollectedEnergiIds
            };

            // Åsidosätt med aktuella runtime-värden — ActivePlayer kan ha inaktuell data
            snapshot.HeroEnergi = _context.Player!.Health;
            snapshot.Time       = _context.GameTotalTime;
            snapshot.DateTime   = DateTime.Now;
            snapshot.IsUsed     = true;

            _repo.WriteSlot(slot, snapshot);
        }
    }
}
