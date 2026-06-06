#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Cyklar mönstret [main, main, middle] och loopar: huvudspåret två gånger,
    /// mellanspåret en gång, sedan om från början.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Ren domänlogik bakom interface (se IEditorMusicSequencer)
    ///
    /// MOTIVERING:
    /// Inga motorberoenden — hela tillståndet är ett index i ett fast mönster.
    /// Det gör sekvenseringen trivialt enhetstestbar.
    ///
    /// ANVÄNDNING:
    /// new EditorMusicSequencer(SoundRef.EditorMusicMain, SoundRef.EditorMusicMiddle).
    /// </remarks>
    public sealed class EditorMusicSequencer : IEditorMusicSequencer
    {
        private readonly string[] _pattern;
        private int _index;

        /// <param name="mainTrack">Huvudspåret som spelas två gånger i rad.</param>
        /// <param name="middleTrack">Mellanspåret som spelas en gång efter de två.</param>
        public EditorMusicSequencer(string mainTrack, string middleTrack)
        {
            _pattern = new[] { mainTrack, mainTrack, middleTrack };
        }

        /// <inheritdoc/>
        public string Current => _pattern[_index];

        /// <inheritdoc/>
        public void Reset() => _index = 0;

        /// <inheritdoc/>
        public string Advance()
        {
            _index = (_index + 1) % _pattern.Length;
            return _pattern[_index];
        }
    }
}
