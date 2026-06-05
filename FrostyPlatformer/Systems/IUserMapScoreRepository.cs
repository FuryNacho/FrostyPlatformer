#nullable enable
using System;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Läser och skriver bästa genomspelningstid per user map-slot.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Repository
    ///
    /// MOTIVERING:
    /// Isolerar fil-I/O för user map-rekord från states och GameServices.
    /// States kan testa mot FakeUserMapScoreRepository utan disk-access.
    ///
    /// ANVÄNDNING:
    /// Injiceras via GameServices.UserMapScores. I produktion används
    /// UserMapScoreRepository som skriver till UserMaps/scores.json.
    /// I tester används FakeUserMapScoreRepository.
    /// </remarks>
    public interface IUserMapScoreRepository
    {
        /// <summary>
        /// Returnerar bästa rekord för angiven slot, eller null om inget rekord finns.
        /// </summary>
        UserMapScore? GetRecord(string slotId);

        /// <summary>
        /// Sparar ett nytt rekord för angiven slot. Ersätter eventuellt befintligt rekord.
        /// </summary>
        void SaveRecord(string slotId, string handle, TimeSpan time);

        /// <summary>
        /// Tar bort rekordet för angiven slot. Gör inget om inget rekord finns.
        /// </summary>
        void DeleteRecord(string slotId);
    }
}
