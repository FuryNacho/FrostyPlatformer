#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// In-memory teststubb för IUserMapScoreRepository. Ingen fil-I/O.
    /// </summary>
    public class FakeUserMapScoreRepository : IUserMapScoreRepository
    {
        private readonly Dictionary<string, UserMapScore> _records = new Dictionary<string, UserMapScore>();

        public UserMapScore? GetRecord(string slotId)
            => _records.TryGetValue(slotId, out var r) ? r : null;

        public void SaveRecord(string slotId, string handle, TimeSpan time)
        {
            _records[slotId] = new UserMapScore
            {
                SlotId   = slotId,
                Handle   = handle,
                BestTime = time,
                SetAt    = DateTime.Now,
            };
        }

        public void DeleteRecord(string slotId) => _records.Remove(slotId);
    }
}
