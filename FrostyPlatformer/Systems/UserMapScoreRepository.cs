#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Sparar och läser bästa genomspelningstider för user map-slots i
    /// Resources/Assets/MapData/UserMaps/scores.json.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Repository
    ///
    /// MOTIVERING:
    /// Håller fil-I/O för user map-rekord isolerat från states. Samma
    /// ReadWrite-infrastruktur som resten av spelet (HighScore, Settings).
    /// Ett rekord per slot — enklare än spelets top-5-lista.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() och injiceras via GameServices.UserMapScores.
    /// Läser/skriver lazily per anrop — ingen in-memory cache (filen är liten).
    /// </remarks>
    public class UserMapScoreRepository : IUserMapScoreRepository
    {
        private const string FilePath      = @"\Resources\Assets\MapData\UserMaps";
        private const string FileName      = @"\scores";
        private const string FileExtension = ".json";

        private readonly ReadWrite _rw;

        /// <summary>Skapar en ny repository som använder angiven ReadWrite-instans.</summary>
        public UserMapScoreRepository(ReadWrite readWrite)
        {
            _rw = readWrite;
        }

        /// <inheritdoc/>
        public UserMapScore? GetRecord(string slotId)
        {
            var list = Load();
            return list?.Find(s => s.SlotId == slotId);
        }

        /// <inheritdoc/>
        public void SaveRecord(string slotId, string handle, TimeSpan time)
        {
            var list = Load() ?? new List<UserMapScore>();

            int idx = list.FindIndex(s => s.SlotId == slotId);
            var record = new UserMapScore
            {
                SlotId   = slotId,
                Handle   = handle,
                BestTime = time,
                SetAt    = DateTime.Now,
            };

            if (idx >= 0)
                list[idx] = record;
            else
                list.Add(record);

            _rw.WriteJson(FilePath, FileName, FileExtension, list);
        }

        private List<UserMapScore>? Load()
            => _rw.ReadJson<List<UserMapScore>>(FilePath, FileName, FileExtension);
    }
}
