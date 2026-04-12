#nullable enable
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Systems;

[TestClass]
public class ItemSystemTests
{
    // ── Hjälpklass för testning ──────────────────────────────────────────────

    /// <summary>
    /// Minimal Item-subklass för tester. Sprite sätts till null eftersom
    /// testerna inte kör PixelEngine-rendering.
    /// </summary>
    private class StubItem : Item
    {
        public StubItem(string name = "stub") : base(name, null!, "test") { }
    }

    private static GameContext MakeContext() => new GameContext();

    // ── CollectedItems ───────────────────────────────────────────────────────

    [TestMethod]
    public void CollectedItems_InitiallyEmpty()
    {
        var sys = new ItemSystem(MakeContext());
        Assert.AreEqual(0, sys.CollectedItems.Count);
    }

    [TestMethod]
    public void CollectedItems_ReflectsContext()
    {
        var ctx = MakeContext();
        ctx.CollectedItems.Add(new StubItem());
        var sys = new ItemSystem(ctx);
        Assert.AreEqual(1, sys.CollectedItems.Count);
    }

    // ── Collect ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Collect_AddsItemToList()
    {
        var ctx = MakeContext();
        var sys = new ItemSystem(ctx);
        var item = new StubItem("sword");

        sys.Collect(item);

        Assert.AreEqual(1, sys.CollectedItems.Count);
        Assert.AreSame(item, sys.CollectedItems[0]);
    }

    [TestMethod]
    public void Collect_MultipleItems_AllPresent()
    {
        var ctx = MakeContext();
        var sys = new ItemSystem(ctx);

        sys.Collect(new StubItem("a"));
        sys.Collect(new StubItem("b"));
        sys.Collect(new StubItem("c"));

        Assert.AreEqual(3, sys.CollectedItems.Count);
    }

    [TestMethod]
    public void Collect_UpdatesContextCollectedItems()
    {
        var ctx = MakeContext();
        var sys = new ItemSystem(ctx);

        sys.Collect(new StubItem("key"));

        Assert.AreEqual(1, ctx.CollectedItems.Count);
    }

    [TestMethod]
    public void Collect_PreservesItemOrder()
    {
        var ctx = MakeContext();
        var sys = new ItemSystem(ctx);
        var first  = new StubItem("first");
        var second = new StubItem("second");

        sys.Collect(first);
        sys.Collect(second);

        Assert.AreSame(first,  sys.CollectedItems[0]);
        Assert.AreSame(second, sys.CollectedItems[1]);
    }

    // ── CollectedItems är IReadOnlyList ───────────────────────────────────────

    [TestMethod]
    public void CollectedItems_IsReadOnly_CannotMutateViaInterface()
    {
        var sys = new ItemSystem(MakeContext());
        Assert.IsInstanceOfType(sys.CollectedItems, typeof(IReadOnlyList<Item>));
    }
}
