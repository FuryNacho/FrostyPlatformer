#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för ISaveLoadSystem — testar FakeSaveLoadSystem i isolation.
    /// Ingen Aggregate, fil-I/O eller Clock krävs.
    /// </summary>
    [TestClass]
    public class SaveLoadSystemTests
    {
        // ── Initialvärden ─────────────────────────────────────────────────────

        [TestMethod]
        public void Load_InitialCount_IsZero()
        {
            var sys = new FakeSaveLoadSystem();
            Assert.AreEqual(0, sys.LoadCount);
        }

        [TestMethod]
        public void Save_InitialCount_IsZero()
        {
            var sys = new FakeSaveLoadSystem();
            Assert.AreEqual(0, sys.SaveCount);
        }

        // ── Load — spårar slot ────────────────────────────────────────────────

        [TestMethod]
        public void Load_Slot1_TracksCorrectSlot()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(1);
            Assert.AreEqual(1, sys.LastLoadSlot);
        }

        [TestMethod]
        public void Load_Slot2_TracksCorrectSlot()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(2);
            Assert.AreEqual(2, sys.LastLoadSlot);
        }

        [TestMethod]
        public void Load_Slot3_TracksCorrectSlot()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(3);
            Assert.AreEqual(3, sys.LastLoadSlot);
        }

        [TestMethod]
        public void Load_SingleCall_CountIsOne()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(1);
            Assert.AreEqual(1, sys.LoadCount);
        }

        [TestMethod]
        public void Load_MultipleCalls_CountAccumulates()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(1);
            sys.Load(2);
            sys.Load(3);
            Assert.AreEqual(3, sys.LoadCount);
        }

        [TestMethod]
        public void Load_MultipleCalls_LastSlotIsLatest()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(1);
            sys.Load(3);
            Assert.AreEqual(3, sys.LastLoadSlot);
        }

        // ── Save — spårar slot ────────────────────────────────────────────────

        [TestMethod]
        public void Save_Slot1_TracksCorrectSlot()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(1);
            Assert.AreEqual(1, sys.LastSaveSlot);
        }

        [TestMethod]
        public void Save_Slot2_TracksCorrectSlot()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(2);
            Assert.AreEqual(2, sys.LastSaveSlot);
        }

        [TestMethod]
        public void Save_Slot3_TracksCorrectSlot()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(3);
            Assert.AreEqual(3, sys.LastSaveSlot);
        }

        [TestMethod]
        public void Save_SingleCall_CountIsOne()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(2);
            Assert.AreEqual(1, sys.SaveCount);
        }

        [TestMethod]
        public void Save_MultipleCalls_CountAccumulates()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(1);
            sys.Save(2);
            sys.Save(3);
            Assert.AreEqual(3, sys.SaveCount);
        }

        [TestMethod]
        public void Save_MultipleCalls_LastSlotIsLatest()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(3);
            sys.Save(1);
            Assert.AreEqual(1, sys.LastSaveSlot);
        }

        // ── Load och Save är oberoende räknare ───────────────────────────────

        [TestMethod]
        public void LoadAndSave_CountsAreTrackedIndependently()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(1);
            sys.Load(2);
            sys.Save(3);
            Assert.AreEqual(2, sys.LoadCount);
            Assert.AreEqual(1, sys.SaveCount);
        }

        [TestMethod]
        public void Load_DoesNotIncrementSaveCount()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Load(1);
            Assert.AreEqual(0, sys.SaveCount);
        }

        [TestMethod]
        public void Save_DoesNotIncrementLoadCount()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(1);
            Assert.AreEqual(0, sys.LoadCount);
        }

        [TestMethod]
        public void Load_AfterSave_LoadSlotCorrect()
        {
            var sys = new FakeSaveLoadSystem();
            sys.Save(3);
            sys.Load(2);
            Assert.AreEqual(2, sys.LastLoadSlot);
            Assert.AreEqual(3, sys.LastSaveSlot);
        }
    }

    /// <summary>
    /// Enhetstester för den riktiga SaveLoadSystem med FakeSaveGameRepository.
    /// Verifierar att Load/Save-logiken muterar GameContext och repository korrekt.
    /// </summary>
    [TestClass]
    public class SaveLoadSystemImplTests
    {
        private static GameContext MakeContext()
        {
            var ctx = new GameContext { Player = new DynamicCreatureHero() };
            return ctx;
        }

        private static SaveLoadSystem Make(GameContext ctx, FakeSaveGameRepository repo)
            => new SaveLoadSystem(ctx, repo, () => { });

        // ── Load — GameContext-mutationer ──────────────────────────────────────

        [TestMethod]
        public void Load_SetsActualTotalTimeFromSlot()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();
            repo.SetSlot(1, new SaveSlot { Time = new TimeSpan(1, 2, 3) });

            Make(ctx, repo).Load(1);

            Assert.AreEqual(new TimeSpan(1, 2, 3), ctx.ActualTotalTime);
        }

        [TestMethod]
        public void Load_SetsPlayerHealthFromSlot()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();
            repo.SetSlot(2, new SaveSlot { HeroEnergi = 4 });

            Make(ctx, repo).Load(2);

            Assert.AreEqual(4, ctx.Player!.Health);
        }

        [TestMethod]
        public void Load_SetsCollectedEnergiIdsFromSlot()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();
            repo.SetSlot(1, new SaveSlot { EnergiCollected = new List<int> { 10, 20 } });

            Make(ctx, repo).Load(1);

            CollectionAssert.AreEqual(new[] { 10, 20 }, ctx.CollectedEnergiIds);
        }

        [TestMethod]
        public void Load_WritesActivePlayerToRepository()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();
            repo.SetSlot(1, new SaveSlot { StageCompleted = 3 });

            Make(ctx, repo).Load(1);

            Assert.IsNotNull(repo.LastWrittenActivePlayer);
            Assert.AreEqual(3, repo.LastWrittenActivePlayer!.StageCompleted);
        }

        [TestMethod]
        public void Load_CallsClockHardReset()
        {
            var ctx   = MakeContext();
            var repo  = new FakeSaveGameRepository();
            int resets = 0;
            var sys   = new SaveLoadSystem(ctx, repo, () => resets++);

            sys.Load(1);

            Assert.AreEqual(1, resets);
        }

        [TestMethod]
        public void Load_Slot1_ReadsFromSlot1()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Load(1);

            Assert.AreEqual(1, repo.LastReadSlot);
        }

        [TestMethod]
        public void Load_Slot2_ReadsFromSlot2()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Load(2);

            Assert.AreEqual(2, repo.LastReadSlot);
        }

        [TestMethod]
        public void Load_Slot3_ReadsFromSlot3()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Load(3);

            Assert.AreEqual(3, repo.LastReadSlot);
        }

        // ── Save — repository-skrivningar ─────────────────────────────────────

        [TestMethod]
        public void Save_Slot1_WritesToSlot1()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Save(1);

            Assert.AreEqual(1, repo.LastWriteSlot);
        }

        [TestMethod]
        public void Save_Slot2_WritesToSlot2()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Save(2);

            Assert.AreEqual(2, repo.LastWriteSlot);
        }

        [TestMethod]
        public void Save_Slot3_WritesToSlot3()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Save(3);

            Assert.AreEqual(3, repo.LastWriteSlot);
        }

        [TestMethod]
        public void Save_OverridesHeroEnergiWithPlayerHealth()
        {
            var ctx  = MakeContext();
            ctx.Player!.Health = 6;
            var repo = new FakeSaveGameRepository();
            repo.SetActivePlayer(new SaveSlot { HeroEnergi = 9 });

            Make(ctx, repo).Save(1);

            Assert.AreEqual(6, repo.LastWrittenSlotData!.HeroEnergi);
        }

        [TestMethod]
        public void Save_OverridesTimeWithGameTotalTime()
        {
            var ctx  = MakeContext();
            ctx.GameTotalTime = new TimeSpan(0, 5, 30);
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Save(2);

            Assert.AreEqual(new TimeSpan(0, 5, 30), repo.LastWrittenSlotData!.Time);
        }

        [TestMethod]
        public void Save_SetsIsUsedTrue()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();
            repo.SetActivePlayer(new SaveSlot { IsUsed = false });

            Make(ctx, repo).Save(1);

            Assert.IsTrue(repo.LastWrittenSlotData!.IsUsed);
        }

        [TestMethod]
        public void Save_UsesContextCollectedEnergiIds()
        {
            var ctx  = MakeContext();
            ctx.CollectedEnergiIds = new List<int> { 5, 7, 11 };
            var repo = new FakeSaveGameRepository();

            Make(ctx, repo).Save(1);

            CollectionAssert.AreEqual(new[] { 5, 7, 11 }, repo.LastWrittenSlotData!.EnergiCollected);
        }

        [TestMethod]
        public void Save_ReadsActivePlayerFromRepository()
        {
            var ctx  = MakeContext();
            var repo = new FakeSaveGameRepository();
            repo.SetActivePlayer(new SaveSlot { StageCompleted = 5 });

            Make(ctx, repo).Save(1);

            // StageCompleted kopieras från ActivePlayer och ska inte åsidosättas
            Assert.AreEqual(5, repo.LastWrittenSlotData!.StageCompleted);
        }

        [TestMethod]
        public void Save_DoesNotCallClockHardReset()
        {
            var ctx   = MakeContext();
            var repo  = new FakeSaveGameRepository();
            int resets = 0;
            var sys   = new SaveLoadSystem(ctx, repo, () => resets++);

            sys.Save(1);

            Assert.AreEqual(0, resets);
        }

        [TestMethod]
        public void Save_SetsDateTimeToApproximatelyNow()
        {
            var before = DateTime.Now;
            var ctx    = MakeContext();
            var repo   = new FakeSaveGameRepository();

            Make(ctx, repo).Save(1);

            var after = DateTime.Now;
            Assert.IsTrue(repo.LastWrittenSlotData!.DateTime >= before &&
                          repo.LastWrittenSlotData.DateTime  <= after);
        }
    }
}
