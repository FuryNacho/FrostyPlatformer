#nullable enable
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

[TestClass]
public class DialogSystemTests
{
    // ── Grundtillstånd ────────────────────────────────────────────────────────

    [TestMethod]
    public void Initially_not_active()
    {
        var sys = new DialogSystem();
        Assert.IsFalse(sys.IsActive);
    }

    [TestMethod]
    public void Initially_active_lines_null()
    {
        var sys = new DialogSystem();
        Assert.IsNull(sys.ActiveLines);
    }

    // ── Show ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Show_makes_active()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Hej" });
        Assert.IsTrue(sys.IsActive);
    }

    [TestMethod]
    public void Show_stores_lines()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Rad 1", "Rad 2" });
        Assert.AreEqual(2, sys.ActiveLines!.Count);
        Assert.AreEqual("Rad 1", sys.ActiveLines[0]);
        Assert.AreEqual("Rad 2", sys.ActiveLines[1]);
    }

    [TestMethod]
    public void Show_replaces_previous_dialog()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Gammal" });
        sys.Show(new List<string> { "Ny rad 1", "Ny rad 2" });
        Assert.AreEqual(2, sys.ActiveLines!.Count);
        Assert.AreEqual("Ny rad 1", sys.ActiveLines[0]);
    }

    // ── Dismiss ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Dismiss_deactivates()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Hej" });
        sys.Dismiss();
        Assert.IsFalse(sys.IsActive);
    }

    [TestMethod]
    public void Dismiss_clears_active_lines()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Hej" });
        sys.Dismiss();
        Assert.IsNull(sys.ActiveLines);
    }

    [TestMethod]
    public void Dismiss_when_not_active_is_noop()
    {
        var sys = new DialogSystem();
        sys.Dismiss(); // ska inte kasta
        Assert.IsFalse(sys.IsActive);
    }

    // ── Render ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Render_noop_when_not_active()
    {
        var sys = new DialogSystem();
        var rc  = new FakeRenderContext();
        sys.Render(rc);
        Assert.AreEqual(0, rc.DrawnTexts.Count);
    }

    [TestMethod]
    public void Render_draws_text_when_active()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Rad 1" });
        var rc = new FakeRenderContext();
        sys.Render(rc);
        Assert.IsTrue(rc.DrawnTexts.Count > 0);
    }

    [TestMethod]
    public void Render_includes_confirm_hint()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Hej" });
        var rc = new FakeRenderContext();
        sys.Render(rc);
        Assert.IsTrue(rc.DrawnTexts.Exists(t => t.Text.Contains("confirm")));
    }

    [TestMethod]
    public void Render_draws_up_to_three_lines()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "A", "B", "C", "D" }); // 4 rader — max 3 ska ritas
        var rc = new FakeRenderContext();
        sys.Render(rc);
        // 3 textrader + 1 bekräftelsetips = 4 DrawText-anrop totalt
        int textLines = rc.DrawnTexts.FindAll(t => !t.Text.Contains("confirm")).Count;
        Assert.AreEqual(3, textLines);
    }

    [TestMethod]
    public void Render_noop_after_dismiss()
    {
        var sys = new DialogSystem();
        sys.Show(new List<string> { "Hej" });
        sys.Dismiss();
        var rc = new FakeRenderContext();
        sys.Render(rc);
        Assert.AreEqual(0, rc.DrawnTexts.Count);
    }
}
