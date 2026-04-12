#nullable enable
using System.Collections.Generic;
using System.Text;
using Raylib_cs;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.Engine.Raylib
{
    /// <summary>
    /// Raylib-implementationen av IAudioSystem — laddar och spelar WAV-filer
    /// via Raylib.LoadSound / PlaySound.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Null Object för saknade filer)
    ///
    /// MOTIVERING:
    /// Ersätter AudioSystem (Audio.Library.Sound). Spelkoden beror på IAudioSystem
    /// och känner inte till Raylib. Alla ljud registreras vid uppstart via
    /// RegisterSound() med en filsökväg — states spelar sedan ljud via IAudioSystem.Play(ref).
    /// Ljud som inte registrerats hanteras som no-ops för att undvika kraschar.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.cs (Composition Root). Raylib.InitAudioDevice() måste anropas
    /// innan RaylibAudioSystem skapas. Alla WAV-filer registreras med RegisterSound()
    /// direkt efter skapandet. CleanUp() anropas vid programavslut.
    /// </remarks>
    public class RaylibAudioSystem : IAudioSystem
    {
        private readonly Dictionary<string, Sound> _sounds  = new();
        private bool _muted;

        /// <summary>
        /// Laddar ett WAV-ljud från disk och registrerar det under ett referensnamn.
        /// Anropas från Program.cs (Composition Root) under initialisering.
        /// </summary>
        public void RegisterSound(string soundRef, string filePath)
            => _sounds[soundRef] = LoadSoundFromPath(filePath);

        /// <inheritdoc/>
        public void Play(string soundRef)
        {
            if (_muted || !_sounds.TryGetValue(soundRef, out var sound)) return;
            Raylib_cs.Raylib.PlaySound(sound);
        }

        /// <inheritdoc/>
        public void Stop(string soundRef)
        {
            if (_sounds.TryGetValue(soundRef, out var sound))
                Raylib_cs.Raylib.StopSound(sound);
        }

        /// <inheritdoc/>
        public void StopAll()
        {
            foreach (var sound in _sounds.Values)
                Raylib_cs.Raylib.StopSound(sound);
        }

        /// <inheritdoc/>
        public void Pause(string soundRef)
        {
            if (_sounds.TryGetValue(soundRef, out var sound))
                Raylib_cs.Raylib.PauseSound(sound);
        }

        /// <inheritdoc/>
        public void PauseAll()
        {
            foreach (var sound in _sounds.Values)
                Raylib_cs.Raylib.PauseSound(sound);
        }

        /// <inheritdoc/>
        public bool IsPlaying(string soundRef)
            => _sounds.TryGetValue(soundRef, out var sound) && Raylib_cs.Raylib.IsSoundPlaying(sound);

        /// <inheritdoc/>
        public void Mute()   => _muted = true;

        /// <inheritdoc/>
        public void UnMute() => _muted = false;

        /// <inheritdoc/>
        public void CleanUp()
        {
            foreach (var sound in _sounds.Values)
                Raylib_cs.Raylib.UnloadSound(sound);
            _sounds.Clear();
            Raylib_cs.Raylib.CloseAudioDevice();
        }

        private static unsafe Sound LoadSoundFromPath(string filePath)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(filePath + '\0');
            fixed (byte* ptr = bytes)
                return Raylib_cs.Raylib.LoadSound((sbyte*)ptr);
        }
    }
}
