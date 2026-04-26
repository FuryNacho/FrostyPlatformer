#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för IUserMapScoreRepository — testar FakeUserMapScoreRepository i isolation.
    /// Ingen fil-I/O krävs.
    /// </summary>
    [TestClass]
    public class UserMapScoreRepositoryTests
    {
        private static TimeSpan T(int minutes, int seconds) =>
            new TimeSpan(0, minutes, seconds);

        // ── GetRecord ─────────────────────────────────────────────────────────

        [TestMethod]
        public void GetRecord_NoRecordExists_ReturnsNull()
        {
            var repo = new FakeUserMapScoreRepository();

            var result = repo.GetRecord("slot1");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetRecord_AfterSave_ReturnsRecord()
        {
            var repo = new FakeUserMapScoreRepository();
            repo.SaveRecord("slot1", "ACE", T(1, 30));

            var result = repo.GetRecord("slot1");

            Assert.IsNotNull(result);
            Assert.AreEqual("slot1",    result.SlotId);
            Assert.AreEqual("ACE",      result.Handle);
            Assert.AreEqual(T(1, 30),   result.BestTime);
        }

        [TestMethod]
        public void GetRecord_WrongSlot_ReturnsNull()
        {
            var repo = new FakeUserMapScoreRepository();
            repo.SaveRecord("slot1", "ACE", T(1, 30));

            var result = repo.GetRecord("slot2");

            Assert.IsNull(result);
        }

        // ── SaveRecord ────────────────────────────────────────────────────────

        [TestMethod]
        public void SaveRecord_SaveTwiceSameSlot_UpdatesNotDuplicates()
        {
            var repo = new FakeUserMapScoreRepository();
            repo.SaveRecord("slot1", "OLD", T(2, 0));
            repo.SaveRecord("slot1", "NEW", T(1, 0));

            var result = repo.GetRecord("slot1");

            Assert.IsNotNull(result);
            Assert.AreEqual("NEW",    result.Handle);
            Assert.AreEqual(T(1, 0), result.BestTime);
        }

        [TestMethod]
        public void SaveRecord_DifferentSlots_BothPresent()
        {
            var repo = new FakeUserMapScoreRepository();
            repo.SaveRecord("slot1", "AAA", T(1, 0));
            repo.SaveRecord("slot2", "BBB", T(2, 0));

            var r1 = repo.GetRecord("slot1");
            var r2 = repo.GetRecord("slot2");

            Assert.IsNotNull(r1);
            Assert.IsNotNull(r2);
            Assert.AreEqual("AAA",    r1.Handle);
            Assert.AreEqual("BBB",    r2.Handle);
            Assert.AreEqual(T(1, 0), r1.BestTime);
            Assert.AreEqual(T(2, 0), r2.BestTime);
        }

        [TestMethod]
        public void SaveRecord_SetsSlotId()
        {
            var repo = new FakeUserMapScoreRepository();
            repo.SaveRecord("slot3", "XYZ", T(0, 45));

            var result = repo.GetRecord("slot3");

            Assert.IsNotNull(result);
            Assert.AreEqual("slot3", result.SlotId);
        }
    }
}
