#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Abstraherar ljud-uppspelning och tillåter states att spela ljud utan att
    /// känna till Audio.Library eller Aggregate.Instance.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion (Interface Segregation)
    ///
    /// MOTIVERING:
    /// States anropade tidigare Aggregate.Instance.Sound direkt — ett singleton-beroende
    /// som omöjliggör testning och kopplar states hårt till Audio.Library-implementationen.
    /// IAudioSystem bryter beroendet: states beror på abstraktionen, AudioSystem wrapprar
    /// den konkreta Audio.Library.Sound med null-säkert delegering.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new AudioSystem(Aggregate.Instance.Sound) och
    /// injiceras via GameServices.Audio. States anropar _services.Audio.Play(SoundRef.Jump)
    /// utan null-check — AudioSystem hanterar null-fallet internt.
    /// </remarks>
    public interface IAudioSystem
    {
        /// <summary>Startar uppspelning av ett ljud. Gör ingenting om ljudet redan spelas.</summary>
        void Play(string soundRef);

        /// <summary>Stoppar ett specifikt ljud.</summary>
        void Stop(string soundRef);

        /// <summary>Stoppar alla ljud.</summary>
        void StopAll();

        /// <summary>Pausar ett specifikt ljud.</summary>
        void Pause(string soundRef);

        /// <summary>Pausar alla ljud.</summary>
        void PauseAll();

        /// <summary>Returnerar true om ett specifikt ljud spelas just nu.</summary>
        bool IsPlaying(string soundRef);

        /// <summary>Frigör alla ljud-resurser. Anropas när applikationen avslutas.</summary>
        void CleanUp();

        /// <summary>Dämpar alla ljud. Påverkar inte uppspelningsläget — ljuden återupptas vid UnMute.</summary>
        void Mute();

        /// <summary>Återaktiverar ljudet efter Mute.</summary>
        void UnMute();
    }
}
