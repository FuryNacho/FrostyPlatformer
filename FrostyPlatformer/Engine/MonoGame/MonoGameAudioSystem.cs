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
        private readonly Dictionary<string, SoundEffect>         _effects     = new();
        private readonly Dictionary<string, SoundEffectInstance> _instances   = new();
        // Per-ljud bas-volym (0..1). Editor-musiken registreras lägre än spelmusiken.
        private readonly Dictionary<string, float>               _baseVolume  = new();
        // Ljud som ska vara oberoende av spelets Mute()/UnMute() (level editor-musik).
        private readonly HashSet<string>                         _editorMusic = new();

        // Spelljudet styrs av AudioOn via Mute()/UnMute(). Implementeras per-instans
        // (inte via global MasterVolume) så att editor-musiken kan vara på/av oberoende.
        private bool _gameMuted;

        /// <summary>
        /// Laddar ett WAV-ljud från disk och registrerar det under ett referensnamn.
        /// </summary>
        /// <param name="soundRef">Referensnamn — samma string som SoundRef.*-konstanterna.</param>
        /// <param name="filePath">Absolut sökväg till WAV-filen.</param>
        /// <param name="isLooped">True för bakgrundsmusik som ska loopas automatiskt.</param>
        /// <param name="volume">Bas-volym 0..1. Editor-musiken spelas dämpat (~hälften).</param>
        /// <param name="isEditorMusic">
        /// True för level editor-musik — undantas från spelets Mute() så att
        /// "Game sound off" inte tystar editorn (och tvärtom).
        /// </param>
        public void RegisterSound(string soundRef, string filePath, bool isLooped = false,
                                  float volume = 1f, bool isEditorMusic = false)
        {
            using var stream = File.OpenRead(filePath);
            var effect   = SoundEffect.FromStream(stream);
            var instance = effect.CreateInstance();
            instance.IsLooped = isLooped;
            instance.Volume   = volume;
            _effects[soundRef]    = effect;
            _instances[soundRef]  = instance;
            _baseVolume[soundRef] = volume;
            if (isEditorMusic) _editorMusic.Add(soundRef);
        }

        // Effektiv volym givet nuvarande mute-läge: spelljud tystas av _gameMuted,
        // editor-musik påverkas aldrig.
        private float EffectiveVolume(string soundRef)
            => _gameMuted && !_editorMusic.Contains(soundRef) ? 0f : _baseVolume[soundRef];

        /// <inheritdoc/>
        /// <remarks>
        /// Om ljudet redan spelas (SoundState.Playing) görs ingenting,
        /// i linje med IAudioSystem-kontraktet.
        /// </remarks>
        public void Play(string soundRef)
        {
            if (!_instances.TryGetValue(soundRef, out var inst)) return;
            if (inst.State == SoundState.Playing) return;
            inst.Volume = EffectiveVolume(soundRef);
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
        /// <remarks>
        /// Tystar spelljudet per-instans (inte via global MasterVolume) så att
        /// editor-musiken kan vara på/av oberoende av spelljudet. Editor-musik
        /// undantas helt. Befintliga instanser pausas inte.
        /// </remarks>
        public void Mute()
        {
            _gameMuted = true;
            foreach (var (soundRef, inst) in _instances)
                if (!_editorMusic.Contains(soundRef))
                    inst.Volume = 0f;
        }

        /// <inheritdoc/>
        public void UnMute()
        {
            _gameMuted = false;
            foreach (var (soundRef, inst) in _instances)
                if (!_editorMusic.Contains(soundRef))
                    inst.Volume = _baseVolume[soundRef];
        }

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
