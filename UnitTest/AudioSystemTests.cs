#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för IAudioSystem — FakeAudioSystem och AudioSystem(null).
    /// Testar att FakeAudioSystem loggar anrop korrekt och att AudioSystem med
    /// null-sound är en no-op utan undantag (Null Object-beteende).
    /// </summary>
    [TestClass]
    public class AudioSystemTests
    {
        // ── FakeAudioSystem ────────────────────────────────────────────────────────

        [TestMethod]
        public void FakeAudio_Play_LogsCall()
        {
            var audio = new FakeAudioSystem();
            audio.Play("bgmusic");
            CollectionAssert.Contains(audio.PlayCalls, "bgmusic");
        }

        [TestMethod]
        public void FakeAudio_Stop_LogsCall()
        {
            var audio = new FakeAudioSystem();
            audio.Stop("bgmusic");
            CollectionAssert.Contains(audio.StopCalls, "bgmusic");
        }

        [TestMethod]
        public void FakeAudio_Pause_LogsCall()
        {
            var audio = new FakeAudioSystem();
            audio.Pause("bgmusic");
            CollectionAssert.Contains(audio.PauseCalls, "bgmusic");
        }

        [TestMethod]
        public void FakeAudio_StopAll_IncrementsCounter()
        {
            var audio = new FakeAudioSystem();
            audio.StopAll();
            audio.StopAll();
            Assert.AreEqual(2, audio.StopAllCount);
        }

        [TestMethod]
        public void FakeAudio_PauseAll_IncrementsCounter()
        {
            var audio = new FakeAudioSystem();
            audio.PauseAll();
            Assert.AreEqual(1, audio.PauseAllCount);
        }

        [TestMethod]
        public void FakeAudio_CleanUp_IncrementsCounter()
        {
            var audio = new FakeAudioSystem();
            audio.CleanUp();
            Assert.AreEqual(1, audio.CleanUpCount);
        }

        [TestMethod]
        public void FakeAudio_IsPlaying_TrueAfterPlay()
        {
            var audio = new FakeAudioSystem();
            audio.Play("bgmusic");
            Assert.IsTrue(audio.IsPlaying("bgmusic"));
        }

        [TestMethod]
        public void FakeAudio_IsPlaying_FalseAfterStop()
        {
            var audio = new FakeAudioSystem();
            audio.Play("bgmusic");
            audio.Stop("bgmusic");
            Assert.IsFalse(audio.IsPlaying("bgmusic"));
        }

        [TestMethod]
        public void FakeAudio_IsPlaying_FalseAfterPause()
        {
            var audio = new FakeAudioSystem();
            audio.Play("bgmusic");
            audio.Pause("bgmusic");
            Assert.IsFalse(audio.IsPlaying("bgmusic"));
        }

        [TestMethod]
        public void FakeAudio_IsPlaying_FalseAfterStopAll()
        {
            var audio = new FakeAudioSystem();
            audio.Play("bgmusic");
            audio.Play("sfx");
            audio.StopAll();
            Assert.IsFalse(audio.IsPlaying("bgmusic"));
            Assert.IsFalse(audio.IsPlaying("sfx"));
        }

        [TestMethod]
        public void FakeAudio_IsPlaying_FalseForUnknownRef()
        {
            var audio = new FakeAudioSystem();
            Assert.IsFalse(audio.IsPlaying("unknown"));
        }

        // ── AudioSystem(null) — Null Object-beteende ──────────────────────────────

        [TestMethod]
        public void AudioSystem_NullSound_Play_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.Play("bgmusic"); // ska vara no-op
        }

        [TestMethod]
        public void AudioSystem_NullSound_Stop_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.Stop("bgmusic");
        }

        [TestMethod]
        public void AudioSystem_NullSound_StopAll_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.StopAll();
        }

        [TestMethod]
        public void AudioSystem_NullSound_Pause_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.Pause("bgmusic");
        }

        [TestMethod]
        public void AudioSystem_NullSound_PauseAll_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.PauseAll();
        }

        [TestMethod]
        public void AudioSystem_NullSound_IsPlaying_ReturnsFalse()
        {
            var audio = new AudioSystem(null);
            Assert.IsFalse(audio.IsPlaying("bgmusic"));
        }

        [TestMethod]
        public void AudioSystem_NullSound_CleanUp_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.CleanUp();
        }

        [TestMethod]
        public void AudioSystem_NullSound_Mute_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.Mute();
        }

        [TestMethod]
        public void AudioSystem_NullSound_UnMute_DoesNotThrow()
        {
            var audio = new AudioSystem(null);
            audio.UnMute();
        }

        // ── FakeAudioSystem – Mute/UnMute ─────────────────────────────────────────

        [TestMethod]
        public void FakeAudio_Mute_IncrementsMuteCount()
        {
            var audio = new FakeAudioSystem();
            audio.Mute();
            audio.Mute();
            Assert.AreEqual(2, audio.MuteCount);
        }

        [TestMethod]
        public void FakeAudio_Mute_SetsMutedFlag()
        {
            var audio = new FakeAudioSystem();
            audio.Mute();
            Assert.IsTrue(audio.IsMuted);
        }

        [TestMethod]
        public void FakeAudio_UnMute_IncrementsUnMuteCount()
        {
            var audio = new FakeAudioSystem();
            audio.UnMute();
            Assert.AreEqual(1, audio.UnMuteCount);
        }

        [TestMethod]
        public void FakeAudio_UnMute_ClearsMutedFlag()
        {
            var audio = new FakeAudioSystem();
            audio.Mute();
            audio.UnMute();
            Assert.IsFalse(audio.IsMuted);
        }
    }
}
