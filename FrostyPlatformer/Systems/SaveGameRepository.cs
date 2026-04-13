#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Läser och skriver sparslottar och aktiv spelarsession via Aggregate.Instance.Settings!.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Repository (Adapter)
    ///
    /// MOTIVERING:
    /// Kapslar in Aggregate.Instance.Settings!.SaveSlotsObjs och ActivePlayer bakom
    /// ett interface. Ger SaveLoadSystem ett rent beroende utan att känna till
    /// Aggregate-singletons eller Settings-objektets interna struktur.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.OnCreate() och skickas till SaveLoadSystem-konstruktorn.
    /// Anropas enbart av SaveLoadSystem — inget annat system läser/skriver sparslottar direkt.
    /// </remarks>
    public class SaveGameRepository : ISaveGameRepository
    {
        /// <summary>Läser spardata för slot 1–3. Okänt slot-nummer returnerar slot 1.</summary>
        public SaveSlot ReadSlot(int slot)
        {
            var slots = Aggregate.Instance.Settings!.SaveSlotsObjs;
            if (slot == 3) return slots.SlotThree;
            if (slot == 2) return slots.SlotTwo;
            return slots.SlotOne;
        }

        /// <summary>
        /// Skriver spardata till slot 1–3. Okänt slot-nummer skriver till slot 1.
        /// </summary>
        public void WriteSlot(int slot, SaveSlot data)
        {
            var slots = Aggregate.Instance.Settings!.SaveSlotsObjs;
            if (slot == 3)       slots.SlotThree = data;
            else if (slot == 2)  slots.SlotTwo   = data;
            else                 slots.SlotOne   = data;
        }

        /// <summary>Returnerar den aktiva spelarens spardata från Aggregate.</summary>
        public SaveSlot ReadActivePlayer()
            => Aggregate.Instance.Settings!.ActivePlayer;

        /// <summary>Skriver ny data till den aktiva spelarsessionen i Aggregate.</summary>
        public void WriteActivePlayer(SaveSlot data)
            => Aggregate.Instance.Settings!.ActivePlayer = data;
    }
}
