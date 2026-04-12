#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IDialogSystem. Spårar anrop utan att rendera något.
    /// </summary>
    public class FakeDialogSystem : IDialogSystem
    {
        public bool IsActive { get; private set; }
        public IReadOnlyList<string>? ActiveLines { get; private set; }

        /// <summary>Antal gånger Render() anropades.</summary>
        public int RenderCount { get; private set; }

        public void Show(IReadOnlyList<string> lines)
        {
            ActiveLines = lines;
            IsActive    = true;
        }

        public void Dismiss()
        {
            ActiveLines = null;
            IsActive    = false;
        }

        public void Render(IRenderContext rc) => RenderCount++;
    }
}
