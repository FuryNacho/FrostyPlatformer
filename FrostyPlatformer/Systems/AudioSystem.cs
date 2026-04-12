#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Null-säker wrapper runt Audio.Library.Sound. Delegerar alla ljud-operationer
    /// till det underliggande ljudsystemet om det finns, annars är varje anrop en no-op.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Null Object)
    ///
    /// MOTIVERING:
    /// Audio.Library.Sound kan vara null om hårdvaran saknas eller initialiseringen
    /// misslyckas. Utan den här klassen måste varje anropare skriva if (Sound != null)
    /// före varje ljud-anrop. AudioSystem centraliserar null-hanteringen och låter
    /// states kalla Play/Stop utan att känna till Audio.Library.
    ///
    /// ANVÄNDNING:
    /// new AudioSystem(Aggregate.Instance.Sound) i Program.OnCreate(). Om sound är null
    /// beter sig instansen som ett Null Object — alla metoder är no-ops.
    /// </remarks>
    public class AudioSystem : IAudioSystem
    {
        private readonly Audio.Library.Sound? _sound;

        /// <summary>Skapar ett AudioSystem som delegerar till <paramref name="sound"/>.</summary>
        public AudioSystem(Audio.Library.Sound? sound)
        {
            _sound = sound;
        }

        /// <inheritdoc/>
        public void Play(string soundRef)      => _sound?.play(soundRef);

        /// <inheritdoc/>
        public void Stop(string soundRef)      => _sound?.stop(soundRef);

        /// <inheritdoc/>
        public void StopAll()                  => _sound?.stop();

        /// <inheritdoc/>
        public void Pause(string soundRef)     => _sound?.pause(soundRef);

        /// <inheritdoc/>
        public void PauseAll()                 => _sound?.pause();

        /// <inheritdoc/>
        public bool IsPlaying(string soundRef) => _sound?.isPlaying(soundRef) ?? false;

        /// <inheritdoc/>
        public void CleanUp()                  => _sound?.cleanUp();

        /// <inheritdoc/>
        public void Mute()                     => _sound?.mute();

        /// <inheritdoc/>
        public void UnMute()                   => _sound?.unMute();
    }
}
