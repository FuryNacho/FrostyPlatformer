#nullable enable
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.Engine.MonoGame
{
    /// <summary>
    /// MonoGame-implementationen av IAudioSystem — laddar WAV-filer runtime via
    /// SoundEffect.FromStream och spelar dem med SoundEffectInstance.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Null Object för saknade ljud-refs)
    ///
    /// MOTIVERING:
    /// Ersätter RaylibAudioSystem. Spelkoden beror på IAudioSystem och känner inte
    /// till MonoGame. Alla ljud registreras vid uppstart via RegisterSound() med
    /// en filsökväg — states spelar sedan ljud via IAudioSystem.Play(ref).
    /// Saknade refs hanteras som no-ops för att undvika kraschar.
    ///
    /// LOOPHANTERING:
    /// Bakgrundsmusik (BGSoundWorld, BGSoundGame, m.fl.) registreras med isLooped=true.
    /// Raylib-implementationen saknar loopning — states kompenserar med ett
    /// "kolla IsPlaying och spela igen"-mönster. Med MonoGame loopas dessa
    /// ljudinstanser naturligt via SoundEffectInstance.IsLooped.
    ///
    /// MUTE:
    /// Implementeras via SoundEffect.MasterVolume (statisk, global).
    /// Fördel: redan spelande ljud fortsätter sin position och återupptas när
    /// UnMute anropas, utan att states behöver trigga dem igen (till skillnad
    /// från Raylib-implementationen som blockerar Play när muted).
    ///
    /// WAV-RISK:
    /// MonoGame kräver PCM WAV (standard 8 eller 16 bit). ADPCM-komprimerade
    /// WAV-filer eller ovanliga samplingshastigheter kan orsaka InvalidOperationException.
    /// Verifiera alla 11 filer vid integration (steg 2f).
    ///
    /// ANVÄNDNING:
    /// Skapas i FrostyGame.LoadContent() (Composition Root). Alla WAV-filer
    /// registreras med RegisterSound() direkt efter skapandet. Anroparen
    /// är ansvarig för att sätta isLooped=true för bakgrundsmusik.
    /// CleanUp() anropas vid programavslut.
    /// </remarks>
    public class MonoGameAudioSystem : IAudioSystem
    {
        // SoundEffect-instansen måste hållas vid liv så länge SoundEffectInstance används.
        private readonly Dictionary<string, SoundEffect>         _effects   = new();
        private readonly Dictionary<string, SoundEffectInstance> _instances = new();

        /// <summary>
        /// Laddar ett WAV-ljud från disk och registrerar det under ett referensnamn.
        /// </summary>
        /// <param name="soundRef">Referensnamn — samma string som SoundRef.*-konstanterna.</param>
        /// <param name="filePath">Absolut sökväg till WAV-filen.</param>
        /// <param name="isLooped">True för bakgrundsmusik som ska loopas automatiskt.</param>
        public void RegisterSound(string soundRef, string filePath, bool isLooped = false)
        {
            using var stream = File.OpenRead(filePath);
            var effect   = SoundEffect.FromStream(stream);
            var instance = effect.CreateInstance();
            instance.IsLooped = isLooped;
            _effects[soundRef]   = effect;
            _instances[soundRef] = instance;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Om ljudet redan spelas (SoundState.Playing) görs ingenting,
        /// i linje med IAudioSystem-kontraktet.
        /// </remarks>
        public void Play(string soundRef)
        {
            if (!_instances.TryGetValue(soundRef, out var inst)) return;
            if (inst.State == SoundState.Playing) return;
            inst.Play();
        }

        /// <inheritdoc/>
        public void Stop(string soundRef)
        {
            if (_instances.TryGetValue(soundRef, out var inst))
                inst.Stop();
        }

        /// <inheritdoc/>
        public void StopAll()
        {
            foreach (var inst in _instances.Values)
                inst.Stop();
        }

        /// <inheritdoc/>
        public void Pause(string soundRef)
        {
            if (_instances.TryGetValue(soundRef, out var inst))
                inst.Pause();
        }

        /// <inheritdoc/>
        public void PauseAll()
        {
            foreach (var inst in _instances.Values)
                inst.Pause();
        }

        /// <inheritdoc/>
        public bool IsPlaying(string soundRef)
            => _instances.TryGetValue(soundRef, out var inst) &&
               inst.State == SoundState.Playing;

        /// <inheritdoc/>
        /// <remarks>Tystar via global MasterVolume — befintliga instanser pausas inte.</remarks>
        public void Mute()   => SoundEffect.MasterVolume = 0f;

        /// <inheritdoc/>
        public void UnMute() => SoundEffect.MasterVolume = 1f;

        /// <inheritdoc/>
        public void CleanUp()
        {
            foreach (var inst in _instances.Values) inst.Dispose();
            foreach (var eff  in _effects.Values)   eff.Dispose();
            _instances.Clear();
            _effects.Clear();
        }
    }
}
