#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för EditorMusicSequencer — verifierar mönstret
    /// main, main, middle och att det loopar.
    /// </summary>
    [TestClass]
    public class EditorMusicSequencerTests
    {
        private const string Main   = "main.wav";
        private const string Middle = "middle.wav";

        private static EditorMusicSequencer New() => new EditorMusicSequencer(Main, Middle);

        [TestMethod]
        public void Current_StartsOnMainTrack()
        {
            var seq = New();
            Assert.AreEqual(Main, seq.Current);
        }

        [TestMethod]
        public void Advance_PlaysMainTwiceThenMiddle()
        {
            var seq = New();
            Assert.AreEqual(Main,   seq.Current);   // 1:a main
            Assert.AreEqual(Main,   seq.Advance()); // 2:a main
            Assert.AreEqual(Middle, seq.Advance()); // middle
        }

        [TestMethod]
        public void Advance_LoopsBackToMainAfterMiddle()
        {
            var seq = New();
            seq.Advance();              // main (2:a)
            seq.Advance();              // middle
            Assert.AreEqual(Main, seq.Advance()); // tillbaka till main
        }

        [TestMethod]
        public void Advance_FullPatternRepeats()
        {
            var seq = New();
            var expected = new[] { Main, Middle, Main, Main, Middle, Main, Main, Middle };
            foreach (var track in expected)
                Assert.AreEqual(track, seq.Advance());
        }

        [TestMethod]
        public void Reset_ReturnsToFirstMainTrack()
        {
            var seq = New();
            seq.Advance();      // main
            seq.Advance();      // middle
            seq.Reset();
            Assert.AreEqual(Main, seq.Current);
            Assert.AreEqual(Main, seq.Advance()); // andra main igen
        }
    }
}
