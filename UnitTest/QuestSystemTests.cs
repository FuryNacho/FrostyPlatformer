#nullable enable
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Commands;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;

[TestClass]
public class QuestSystemTests
{
    // ── Hjälpklass för testning ──────────────────────────────────────────────

    /// <summary>Quest-stubb som räknar PopulateDynamics-anrop.</summary>
    private class SpyQuest : Quest
    {
        public int PopulateCallCount { get; private set; }
        public string? LastMapName   { get; private set; }

        public override bool PopulateDynamics(List<DynamicGameObject> vecDyns, string sMap)
        {
            PopulateCallCount++;
            LastMapName = sMap;
            return true;
        }
    }

    private static GameContext MakeContext() => new GameContext();

    // ── ActiveQuests ─────────────────────────────────────────────────────────

    [TestMethod]
    public void ActiveQuests_InitiallyEmpty()
    {
        var sys = new QuestSystem(MakeContext());
        Assert.AreEqual(0, sys.ActiveQuests.Count);
    }

    // ── Add ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Add_SingleQuest_AppearsInActiveQuests()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        var q   = new SpyQuest { Name = "killallwalrus" };

        sys.Add(q);

        Assert.AreEqual(1, sys.ActiveQuests.Count);
        Assert.AreSame(q, sys.ActiveQuests[0]);
    }

    [TestMethod]
    public void Add_SecondQuest_PrependedBeforeFirst()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        var first  = new SpyQuest { Name = "first" };
        var second = new SpyQuest { Name = "second" };

        sys.Add(first);
        sys.Add(second);

        Assert.AreSame(second, sys.ActiveQuests[0], "Nyast quest ska ligga först");
        Assert.AreSame(first,  sys.ActiveQuests[1]);
    }

    [TestMethod]
    public void Add_ThreeQuests_OrderIsNewestFirst()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        var a = new SpyQuest { Name = "a" };
        var b = new SpyQuest { Name = "b" };
        var c = new SpyQuest { Name = "c" };

        sys.Add(a);
        sys.Add(b);
        sys.Add(c);

        Assert.AreEqual("c", sys.ActiveQuests[0].Name);
        Assert.AreEqual("b", sys.ActiveQuests[1].Name);
        Assert.AreEqual("a", sys.ActiveQuests[2].Name);
    }

    [TestMethod]
    public void Add_UpdatesContextActiveQuests()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        var q   = new SpyQuest { Name = "test" };

        sys.Add(q);

        Assert.AreEqual(1, ctx.ActiveQuests.Count);
    }

    // ── PopulateForMap ───────────────────────────────────────────────────────

    [TestMethod]
    public void PopulateForMap_NoQuests_NoCallsMade()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);

        sys.PopulateForMap(new List<DynamicGameObject>(), "mapone");
        // Ska inte kasta
    }

    [TestMethod]
    public void PopulateForMap_OneQuest_CallsPopulateDynamics()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        var spy = new SpyQuest { Name = "patrol" };
        sys.Add(spy);

        sys.PopulateForMap(new List<DynamicGameObject>(), "maptwo");

        Assert.AreEqual(1, spy.PopulateCallCount);
    }

    [TestMethod]
    public void PopulateForMap_PassesMapNameToQuest()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        var spy = new SpyQuest { Name = "patrol" };
        sys.Add(spy);

        sys.PopulateForMap(new List<DynamicGameObject>(), "mapthree");

        Assert.AreEqual("mapthree", spy.LastMapName);
    }

    [TestMethod]
    public void PopulateForMap_TwoQuests_BothReceiveCall()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        var spy1 = new SpyQuest { Name = "q1" };
        var spy2 = new SpyQuest { Name = "q2" };
        sys.Add(spy1);
        sys.Add(spy2);

        sys.PopulateForMap(new List<DynamicGameObject>(), "mapone");

        Assert.AreEqual(1, spy1.PopulateCallCount);
        Assert.AreEqual(1, spy2.PopulateCallCount);
    }

    // ── ActiveQuests är IReadOnlyList ─────────────────────────────────────────

    [TestMethod]
    public void ActiveQuests_IsReadOnly_CannotMutateViaInterface()
    {
        var ctx = MakeContext();
        var sys = new QuestSystem(ctx);
        sys.Add(new SpyQuest { Name = "q" });

        // IReadOnlyList avslöjar inte metoder som Add/Remove — kompilerar inte.
        // Räcker med att verifiera att det är rätt typ.
        Assert.IsInstanceOfType(sys.ActiveQuests, typeof(IReadOnlyList<Quest>));
    }
}
