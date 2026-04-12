#nullable enable
using System.Collections.Generic;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Hanterar dialogrutor som triggas av skriptsystemet under gameplay.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion
    ///
    /// MOTIVERING:
    /// Dialogen var tidigare en stub i GameContext (PendingDialog) utan rendering
    /// eller avslutningslogik. IDialogSystem samlar hela livscykeln — visa, rendera
    /// och avsluta — bakom ett testbart interface. GameplayState och Program känner
    /// bara till interfacet, inte implementationen.
    ///
    /// ANVÄNDNING:
    /// Show() anropas från Program.ShowDialog() (via CommandShowDialog.Start()).
    /// Render() anropas av GameplayState varje frame när IsActive är true.
    /// Dismiss() + IScriptSystem.CompleteCurrentCommand() anropas av GameplayState
    /// när spelaren trycker confirm — det frigör skriptköns nästa kommando.
    /// </remarks>
    public interface IDialogSystem
    {
        /// <summary>True om en dialogruta visas just nu.</summary>
        bool IsActive { get; }

        /// <summary>De textrader som visas i aktiv dialogruta. Null om ingen aktiv dialog.</summary>
        IReadOnlyList<string>? ActiveLines { get; }

        /// <summary>Visar en dialogruta med angivna textrader.</summary>
        void Show(IReadOnlyList<string> lines);

        /// <summary>Stänger dialogrutan och rensar aktiva textrader.</summary>
        void Dismiss();

        /// <summary>Ritar dialogrutan på skärmen. No-op om ingen aktiv dialog.</summary>
        void Render(IRenderContext rc);
    }
}
