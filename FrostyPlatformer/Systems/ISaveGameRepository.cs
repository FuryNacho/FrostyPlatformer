#nullable enable
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Dataåtkomst för sparslottar och aktiv spelarsession.
    /// Döljer att data lagras i Aggregate.Instance.Settings.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Repository
    ///
    /// MOTIVERING:
    /// SaveLoadSystem anropade tidigare Aggregate.Instance.Settings direkt, vilket
    /// hindrade enhetstestning av Load/Save-logiken. ISaveGameRepository isolerar
    /// fil-I/O-lagret (Aggregate) från affärslogiken (SaveLoadSystem) och gör det
    /// möjligt att ersätta datalagringen med en in-memory-fake i tester.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new SaveGameRepository() och skickas till
    /// SaveLoadSystem-konstruktorn. I tester används FakeSaveGameRepository som
    /// låter testerna styra vad som finns i slottarna och verifiera vad som skrivs.
    /// </remarks>
    public interface ISaveGameRepository
    {
        /// <summary>Läser spardata för slot 1–3.</summary>
        SaveSlot ReadSlot(int slot);

        /// <summary>Skriver spardata till slot 1–3.</summary>
        void WriteSlot(int slot, SaveSlot data);

        /// <summary>Returnerar den aktiva spelarens spardata.</summary>
        SaveSlot ReadActivePlayer();

        /// <summary>Skriver ny data till den aktiva spelarsessionen.</summary>
        void WriteActivePlayer(SaveSlot data);
    }
}
