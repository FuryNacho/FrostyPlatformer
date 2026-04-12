#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för IScoreSystem — testar FakeScoreSystem i isolation.
    /// Ingen fil-I/O eller Aggregate krävs.
    /// </summary>
    [TestClass]
    public class ScoreSystemTests
    {
        private static TimeSpan T(int hours, int minutes, int seconds) =>
            new TimeSpan(hours, minutes, seconds);

        // ── PlacesOnHighScore ─────────────────────────────────────────────────

        [TestMethod]
        public void PlacesOnHighScore_BetterTime_ReturnsTrue()
        {
            var score = new FakeScoreSystem();
            score.Seed(T(0, 5, 0), T(0, 10, 0));

            Assert.IsTrue(score.PlacesOnHighScore(T(0, 3, 0)));
        }

        [TestMethod]
        public void PlacesOnHighScore_WorseTime_ReturnsFalse()
        {
            var score = new FakeScoreSystem();
            score.Seed(T(0, 5, 0));

            Assert.IsFalse(score.PlacesOnHighScore(T(0, 10, 0)));
        }

        [TestMethod]
        public void PlacesOnHighScore_EmptyList_ReturnsFalse()
        {
            var score = new FakeScoreSystem();
            Assert.IsFalse(score.PlacesOnHighScore(T(0, 1, 0)));
        }

        // ── IsNewFirstPlace ───────────────────────────────────────────────────

        [TestMethod]
        public void IsNewFirstPlace_BetterThanFirst_ReturnsTrue()
        {
            var score = new FakeScoreSystem();
            score.Seed(T(0, 5, 0), T(0, 10, 0));

            Assert.IsTrue(score.IsNewFirstPlace(T(0, 1, 0)));
        }

        [TestMethod]
        public void IsNewFirstPlace_WorseThanFirst_ReturnsFalse()
        {
            var score = new FakeScoreSystem();
            score.Seed(T(0, 5, 0), T(0, 10, 0));

            Assert.IsFalse(score.IsNewFirstPlace(T(0, 7, 0)));
        }

        [TestMethod]
        public void IsNewFirstPlace_EmptyList_ReturnsFalse()
        {
            var score = new FakeScoreSystem();
            Assert.IsFalse(score.IsNewFirstPlace(T(0, 1, 0)));
        }

        // ── PutOnHighScore ────────────────────────────────────────────────────

        [TestMethod]
        public void PutOnHighScore_AddsEntry_AppearsInList()
        {
            var score = new FakeScoreSystem();
            score.PutOnHighScore(new HighScoreObj { Handle = "AAA", TimeSpan = T(0, 3, 0), DateTime = DateTime.Now });

            var list = score.GetList();
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("AAA", list[0].Handle);
        }

        [TestMethod]
        public void PutOnHighScore_KeepsMaxFiveEntries()
        {
            var score = new FakeScoreSystem();
            for (int i = 1; i <= 7; i++)
                score.PutOnHighScore(new HighScoreObj { Handle = "X", TimeSpan = T(0, i, 0), DateTime = DateTime.Now });

            Assert.AreEqual(5, score.GetList().Count);
        }

        [TestMethod]
        public void PutOnHighScore_ListRemainsSortedByTime()
        {
            var score = new FakeScoreSystem();
            score.PutOnHighScore(new HighScoreObj { Handle = "B", TimeSpan = T(0, 10, 0), DateTime = DateTime.Now });
            score.PutOnHighScore(new HighScoreObj { Handle = "A", TimeSpan = T(0, 3, 0),  DateTime = DateTime.Now });

            var list = score.GetList();
            Assert.IsTrue(list[0].TimeSpan <= list[1].TimeSpan);
        }

        // ── Save / Reset ──────────────────────────────────────────────────────

        [TestMethod]
        public void Save_IncrementsSaveCount()
        {
            var score = new FakeScoreSystem();
            score.Save();
            score.Save();
            Assert.AreEqual(2, score.SaveCount);
        }

        [TestMethod]
        public void Reset_ClearsList()
        {
            var score = new FakeScoreSystem();
            score.Seed(T(0, 5, 0), T(0, 10, 0));
            score.Reset();
            Assert.AreEqual(0, score.GetList().Count);
        }

        [TestMethod]
        public void Reset_IncrementsResetCount()
        {
            var score = new FakeScoreSystem();
            score.Reset();
            Assert.AreEqual(1, score.ResetCount);
        }

        // ── GetList returnerar kopia ──────────────────────────────────────────

        [TestMethod]
        public void GetList_ReturnsCopy_MutationDoesNotAffectInternal()
        {
            var score = new FakeScoreSystem();
            score.Seed(T(0, 5, 0));

            var list = score.GetList();
            list.Clear();

            Assert.AreEqual(1, score.GetList().Count);
        }
    }
}
