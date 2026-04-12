#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IScoreSystem. Hanterar en in-memory highscore-lista och
    /// loggar anrop så att tester kan verifiera rätt beteende utan fil-I/O.
    /// </summary>
    public class FakeScoreSystem : IScoreSystem
    {
        private readonly List<HighScoreObj> _list = new List<HighScoreObj>();

        public int SaveCount  { get; private set; }
        public int ResetCount { get; private set; }

        public bool PlacesOnHighScore(TimeSpan time) => _list.Any(x => x.TimeSpan > time);

        public bool IsNewFirstPlace(TimeSpan time)
        {
            var first = _list.OrderBy(x => x.TimeSpan).FirstOrDefault();
            return first != null && first.TimeSpan > time;
        }

        public void PutOnHighScore(HighScoreObj entry)
        {
            _list.Add(entry);
            var sorted = _list.OrderBy(x => x.TimeSpan).ThenBy(y => y.DateTime).Take(5).ToList();
            _list.Clear();
            _list.AddRange(sorted);
        }

        public void Save() => SaveCount++;

        public List<HighScoreObj> GetList() => new List<HighScoreObj>(_list);

        public void Reset()
        {
            ResetCount++;
            _list.Clear();
        }

        /// <summary>Hjälpmetod för tester — fyller listan med poster med givna tider.</summary>
        public void Seed(params TimeSpan[] times)
        {
            _list.Clear();
            foreach (var t in times)
                _list.Add(new HighScoreObj { TimeSpan = t, Handle = "Test", DateTime = DateTime.Now });
        }
    }
}
