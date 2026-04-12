#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IAudioSystem. Spelar inget ljud men loggar alla anrop
    /// så att tester kan verifiera att rätt ljud spelades vid rätt tillfälle.
    /// </summary>
    public class FakeAudioSystem : IAudioSystem
    {
        public List<string> PlayCalls   { get; } = new List<string>();
        public List<string> StopCalls   { get; } = new List<string>();
        public List<string> PauseCalls  { get; } = new List<string>();
        public int          StopAllCount  { get; private set; }
        public int          PauseAllCount { get; private set; }
        public int          CleanUpCount  { get; private set; }
        public int          MuteCount     { get; private set; }
        public int          UnMuteCount   { get; private set; }
        public bool         IsMuted       { get; private set; }

        private readonly HashSet<string> _playing = new HashSet<string>();

        public void Play(string soundRef)
        {
            PlayCalls.Add(soundRef);
            _playing.Add(soundRef);
        }

        public void Stop(string soundRef)
        {
            StopCalls.Add(soundRef);
            _playing.Remove(soundRef);
        }

        public void StopAll()
        {
            StopAllCount++;
            _playing.Clear();
        }

        public void Pause(string soundRef)
        {
            PauseCalls.Add(soundRef);
            _playing.Remove(soundRef);
        }

        public void PauseAll()
        {
            PauseAllCount++;
            _playing.Clear();
        }

        public bool IsPlaying(string soundRef) => _playing.Contains(soundRef);

        public void CleanUp() => CleanUpCount++;

        public void Mute()   { MuteCount++;   IsMuted = true; }
        public void UnMute() { UnMuteCount++; IsMuted = false; }
    }
}
