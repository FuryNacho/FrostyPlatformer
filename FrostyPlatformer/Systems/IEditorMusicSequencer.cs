#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Bestämmer ordningen på level editor-musiken: huvudspåret loopas, men efter
    /// två spelningar smyger ett mellanspår in en gång, sedan börjar mönstret om.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Ren domänlogik bakom interface (Dependency Inversion)
    ///
    /// MOTIVERING:
    /// Sekvenseringen (main, main, middle, repeat) är ren logik utan motorberoenden.
    /// Den bryts ut från EditorState så att den kan enhetstestas utan ljudhårdvara —
    /// EditorState behöver bara veta "vilket spår nu" och "vad blir nästa när det tog slut".
    ///
    /// ANVÄNDNING:
    /// EditorState äger en instans. Vid start anropas Reset() följt av att Current
    /// spelas. När det spelande spåret tagit slut anropas Advance() och returvärdet
    /// spelas. Sekvenseraren känner inte till uppspelning, volym eller mute.
    /// </remarks>
    public interface IEditorMusicSequencer
    {
        /// <summary>Spåret som ska spelas just nu.</summary>
        string Current { get; }

        /// <summary>Återställer sekvensen till början (huvudspårets första spelning).</summary>
        void Reset();

        /// <summary>Stegar fram till nästa spår i mönstret och returnerar det.</summary>
        string Advance();
    }
}
