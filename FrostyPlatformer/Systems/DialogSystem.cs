#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Renderar och hanterar livscykeln för dialogrutor i GameplayState.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Stateful Service
    ///
    /// MOTIVERING:
    /// DialogSystem äger all dialoglogik: vilket innehåll som visas, hur rutan
    /// renderas och när den stängs. GameplayState behöver inte känna till
    /// renderingsdetaljerna — den anropar Show/Dismiss/Render via IDialogSystem.
    ///
    /// Rutan ritas ovanpå spelet i nedre delen av skärmen (y=185). Upp till tre
    /// textrader visas med ett "Press confirm"-tips i nederkanten. Spelaren frigör
    /// skriptkön via IScriptSystem.CompleteCurrentCommand() i GameplayState.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.OnCreate() och injiceras via GameServices.Dialog.
    /// Show() triggas indirekt av skriptsystemet (CommandShowDialog → Program.ShowDialog).
    /// </remarks>
    public class DialogSystem : IDialogSystem
    {
        private IReadOnlyList<string>? _lines;

        private const int BoxX = 2;
        private const int BoxY = 185;
        private const int BoxW = 252;
        private const int BoxH = 52;

        /// <inheritdoc/>
        public bool IsActive => _lines != null;

        /// <inheritdoc/>
        public IReadOnlyList<string>? ActiveLines => _lines;

        /// <inheritdoc/>
        public void Show(IReadOnlyList<string> lines) => _lines = lines;

        /// <inheritdoc/>
        public void Dismiss() => _lines = null;

        /// <inheritdoc/>
        public void Render(IRenderContext rc)
        {
            if (_lines == null) return;

            var darkBlue = new RenderColor(0, 0, 128);

            // Bakgrund
            rc.FillRect(BoxX, BoxY, BoxW, BoxH, darkBlue);

            // Ram
            rc.FillRect(BoxX,            BoxY,            BoxW, 1,    RenderColor.White);
            rc.FillRect(BoxX,            BoxY + BoxH - 1, BoxW, 1,    RenderColor.White);
            rc.FillRect(BoxX,            BoxY,            1,    BoxH, RenderColor.White);
            rc.FillRect(BoxX + BoxW - 1, BoxY,            1,    BoxH, RenderColor.White);

            // Textrader (max 3)
            int maxLines = Math.Min(_lines.Count, 3);
            for (int i = 0; i < maxLines; i++)
                rc.DrawText(_lines[i], BoxX + 4, BoxY + 4 + i * 10);

            // Bekräftelsetips
            rc.DrawText("Press confirm", BoxX + 4, BoxY + BoxH - 10);
        }
    }
}
