#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;

[TestClass]
public class WorldMapSystemTests
{
    private static WorldMapSystem Make() => new WorldMapSystem();

    // ── GetStageEntry ─────────────────────────────────────────────────────────

    [TestMethod]
    public void GetStageEntry_Stage1_ReturnsMapOne()
    {
        var entry = Make().GetStageEntry(1);
        Assert.IsNotNull(entry);
        Assert.AreEqual("mapone", entry!.Value.MapName);
    }

    [TestMethod]
    public void GetStageEntry_Stage8_ReturnsMapEight()
    {
        var entry = Make().GetStageEntry(8);
        Assert.IsNotNull(entry);
        Assert.AreEqual("mapeight", entry!.Value.MapName);
    }

    [TestMethod]
    public void GetStageEntry_Stage9_ReturnsMapNine()
    {
        var entry = Make().GetStageEntry(9);
        Assert.IsNotNull(entry);
        Assert.AreEqual("mapnine", entry!.Value.MapName);
    }

    [TestMethod]
    public void GetStageEntry_Stage0_ReturnsNull()
        => Assert.IsNull(Make().GetStageEntry(0));

    [TestMethod]
    public void GetStageEntry_Stage10_ReturnsNull()
        => Assert.IsNull(Make().GetStageEntry(10));

    [TestMethod]
    public void GetStageEntry_NegativeStage_ReturnsNull()
        => Assert.IsNull(Make().GetStageEntry(-1));

    [TestMethod]
    public void GetStageEntry_AllStages_ReturnNonNullAndDistinctMaps()
    {
        var sys = Make();
        var mapNames = new System.Collections.Generic.HashSet<string>();
        for (int s = 1; s <= 9; s++)
        {
            var entry = sys.GetStageEntry(s);
            Assert.IsNotNull(entry, $"Stage {s} ska returnera ett entry");
            Assert.IsTrue(mapNames.Add(entry!.Value.MapName),
                $"Stage {s} kartnamn ({entry.Value.MapName}) ska vara unikt");
        }
    }

    [TestMethod]
    public void GetStageEntry_Stage3_ReturnsCorrectCoordinates()
    {
        var entry = Make().GetStageEntry(3);
        Assert.AreEqual("mapthree", entry!.Value.MapName);
        Assert.AreEqual(2f,  entry.Value.X, 0.001f);
        Assert.AreEqual(20f, entry.Value.Y, 0.001f);
    }

    [TestMethod]
    public void GetStageEntry_Stage7_ReturnsCorrectCoordinates()
    {
        var entry = Make().GetStageEntry(7);
        Assert.AreEqual("mapseven", entry!.Value.MapName);
        Assert.AreEqual(3f,  entry.Value.X, 0.001f);
        Assert.AreEqual(18f, entry.Value.Y, 0.001f);
    }

    // ── GetSpawnPosition ──────────────────────────────────────────────────────

    [TestMethod]
    public void GetSpawnPosition_Zero_ReturnsDefaultSpawn()
    {
        var (x, y) = Make().GetSpawnPosition(0);
        Assert.AreEqual(3f, x, 0.001f);
        Assert.AreEqual(8f, y, 0.001f);
    }

    [TestMethod]
    public void GetSpawnPosition_One_ReturnsSameAsDefault()
    {
        var (x, y) = Make().GetSpawnPosition(1);
        Assert.AreEqual(3f, x, 0.001f);
        Assert.AreEqual(8f, y, 0.001f);
    }

    [TestMethod]
    public void GetSpawnPosition_Two_ReturnsThreeStepsRight()
    {
        var (x, y) = Make().GetSpawnPosition(2);
        Assert.AreEqual(6f, x, 0.001f);
        Assert.AreEqual(8f, y, 0.001f);
    }

    [TestMethod]
    public void GetSpawnPosition_Eight_ReturnsCorrectPosition()
    {
        var (x, y) = Make().GetSpawnPosition(8);
        Assert.AreEqual(3f + 7 * 3f, x, 0.001f);
        Assert.AreEqual(8f, y, 0.001f);
    }

    [TestMethod]
    public void GetSpawnPosition_Nine_ReturnsCorrectPosition()
    {
        var (x, y) = Make().GetSpawnPosition(9);
        Assert.AreEqual(3f + 8 * 3f, x, 0.001f);
        Assert.AreEqual(8f, y, 0.001f);
    }

    [TestMethod]
    public void GetSpawnPosition_OutOfRange_ReturnsDefaultSpawn()
    {
        var sys = Make();
        var (x10, _) = sys.GetSpawnPosition(10);
        var (xN,  _) = sys.GetSpawnPosition(-1);
        Assert.AreEqual(3f, x10, 0.001f);
        Assert.AreEqual(3f, xN,  0.001f);
    }

    [TestMethod]
    public void GetSpawnPosition_AllNineStages_YIsAlwaysEight()
    {
        var sys = Make();
        for (int s = 1; s <= 9; s++)
        {
            var (_, y) = sys.GetSpawnPosition(s);
            Assert.AreEqual(8f, y, 0.001f, $"Stage {s} Y ska vara 8");
        }
    }

    [TestMethod]
    public void GetSpawnPosition_EachStep_IncreasesXByThree()
    {
        var sys = Make();
        for (int s = 2; s <= 9; s++)
        {
            var (xPrev, _) = sys.GetSpawnPosition(s - 1);
            var (xCurr, _) = sys.GetSpawnPosition(s);
            Assert.AreEqual(3f, xCurr - xPrev, 0.001f,
                $"X-skillnad mellan stage {s-1} och {s} ska vara 3");
        }
    }
}
