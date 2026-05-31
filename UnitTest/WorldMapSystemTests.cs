#nullable enable
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Systems;

[TestClass]
public class WorldMapSystemTests
{
    private static WorldMapSystem Make() => new WorldMapSystem();

    // ── TiledWorldMapSystem — hjälpare ────────────────────────────────────────

    private static PlacedObject Node(string subType, int tileX, int tileY) => new PlacedObject
    {
        ObjectType = "StopPoint",
        SubType    = subType,
        TileX      = tileX,
        TileY      = tileY
    };

    /// <summary>Enkel linjär bana: junction → mapone → junction → maptwo.</summary>
    private static TiledWorldMapSystem MakeLinear() => new TiledWorldMapSystem(new List<PlacedObject>
    {
        Node("",       2, 8),
        Node("mapone", 5, 8),
        Node("",       8, 8),
        Node("maptwo", 11, 8),
    });

    // ── GetCurrentNode ─────────────────────────────────────────────────────────

    [TestMethod]
    public void GetCurrentNode_ExactTileMatch_ReturnsNode()
    {
        var sys  = MakeLinear();
        var node = sys.GetCurrentNode(5, 8);
        Assert.IsNotNull(node);
        Assert.AreEqual("mapone", node!.SubType);
    }

    [TestMethod]
    public void GetCurrentNode_BetweenNodes_ReturnsNull()
        => Assert.IsNull(MakeLinear().GetCurrentNode(6, 8));

    [TestMethod]
    public void GetCurrentNode_WrongTile_ReturnsNull()
        => Assert.IsNull(MakeLinear().GetCurrentNode(5, 9));

    [TestMethod]
    public void GetCurrentNode_EmptyNodeList_ReturnsNull()
        => Assert.IsNull(new TiledWorldMapSystem(new List<PlacedObject>()).GetCurrentNode(5, 8));

    [TestMethod]
    public void GetCurrentNode_Junction_ReturnsJunctionNode()
    {
        var node = MakeLinear().GetCurrentNode(2, 8);
        Assert.IsNotNull(node);
        Assert.AreEqual("", node!.SubType);
    }

    // ── GetNextNode ────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetNextNode_Right_ReturnsNearestNodeToTheRight()
    {
        var sys     = MakeLinear();
        var current = Node("", 2, 8);
        var next    = sys.GetNextNode(current, 1, 0);
        Assert.IsNotNull(next);
        Assert.AreEqual(5, next!.TileX);
        Assert.AreEqual("mapone", next.SubType);
    }

    [TestMethod]
    public void GetNextNode_Left_ReturnsNearestNodeToTheLeft()
    {
        var sys     = MakeLinear();
        var current = Node("maptwo", 11, 8);
        var next    = sys.GetNextNode(current, -1, 0);
        Assert.IsNotNull(next);
        Assert.AreEqual(8, next!.TileX);
    }

    [TestMethod]
    public void GetNextNode_NoNodeInDirection_ReturnsNull()
    {
        var sys     = MakeLinear();
        var current = Node("maptwo", 11, 8);
        Assert.IsNull(sys.GetNextNode(current, 1, 0));   // längst till höger
    }

    [TestMethod]
    public void GetNextNode_Down_ReturnsNodeBelow()
    {
        var sys = new TiledWorldMapSystem(new List<PlacedObject>
        {
            Node("mapone", 5, 4),
            Node("",       5, 8),
        });
        var next = sys.GetNextNode(Node("mapone", 5, 4), 0, 1);
        Assert.IsNotNull(next);
        Assert.AreEqual(8, next!.TileY);
    }

    [TestMethod]
    public void GetNextNode_Up_ReturnsNodeAbove()
    {
        var sys = new TiledWorldMapSystem(new List<PlacedObject>
        {
            Node("mapone", 5, 4),
            Node("",       5, 8),
        });
        var next = sys.GetNextNode(Node("", 5, 8), 0, -1);
        Assert.IsNotNull(next);
        Assert.AreEqual(4, next!.TileY);
    }

    [TestMethod]
    public void GetNextNode_WithinCrossAxisTolerance_FindsNode()
    {
        // Nod är en tile förskjuten i Y — ska ändå hittas vid horisontell sökning
        var sys = new TiledWorldMapSystem(new List<PlacedObject>
        {
            Node("mapone", 5, 8),
            Node("maptwo", 10, 9),    // Y-förskjutning = 1 (inom tolerans)
        });
        var next = sys.GetNextNode(Node("mapone", 5, 8), 1, 0);
        Assert.IsNotNull(next);
        Assert.AreEqual("maptwo", next!.SubType);
    }

    [TestMethod]
    public void GetNextNode_OutsideCrossAxisTolerance_ReturnsNull()
    {
        // Nod är två tiles förskjuten i Y — utanför tolerans, ska inte hittas
        var sys = new TiledWorldMapSystem(new List<PlacedObject>
        {
            Node("mapone", 5, 8),
            Node("maptwo", 10, 11),   // Y-förskjutning = 3 (utanför tolerans)
        });
        Assert.IsNull(sys.GetNextNode(Node("mapone", 5, 8), 1, 0));
    }

    // ── GetNodeState ───────────────────────────────────────────────────────────

    [TestMethod]
    public void GetNodeState_EmptySubType_IsJunction()
        => Assert.AreEqual(NodeState.Junction, MakeLinear().GetNodeState(Node("", 2, 8), 0));

    [TestMethod]
    public void GetNodeState_CompletedStage_IsCompleted()
        => Assert.AreEqual(NodeState.Completed, MakeLinear().GetNodeState(Node("mapone", 5, 8), stageCompleted: 1));

    [TestMethod]
    public void GetNodeState_NextUnbeatenStage_IsCurrent()
        => Assert.AreEqual(NodeState.Current, MakeLinear().GetNodeState(Node("mapone", 5, 8), stageCompleted: 0));

    [TestMethod]
    public void GetNodeState_StageNotYetReachable_IsLocked()
        => Assert.AreEqual(NodeState.Locked, MakeLinear().GetNodeState(Node("maptwo", 11, 8), stageCompleted: 0));

    [TestMethod]
    public void GetNodeState_Junction_IsAlwaysJunctionRegardlessOfProgress()
    {
        var sys  = MakeLinear();
        var node = Node("", 2, 8);
        Assert.AreEqual(NodeState.Junction, sys.GetNodeState(node, 0));
        Assert.AreEqual(NodeState.Junction, sys.GetNodeState(node, 9));
    }

    // ── GetSpawnTile ───────────────────────────────────────────────────────────

    [TestMethod]
    public void GetSpawnTile_KnownStage_ReturnsTileOfMatchingNode()
    {
        var (tX, tY) = MakeLinear().GetSpawnTile(spawnAtWorldMap: 1, 2, 8);
        Assert.AreEqual(5, tX);
        Assert.AreEqual(8, tY);
    }

    [TestMethod]
    public void GetSpawnTile_ZeroStage_ReturnsMapeoneNode()
    {
        // spawnAtWorldMap=0 (nytt spel) behandlas som bana 1 — returnerar mapone-nodens position.
        var (tX, tY) = MakeLinear().GetSpawnTile(spawnAtWorldMap: 0, 2, 8);
        Assert.AreEqual(5, tX);
        Assert.AreEqual(8, tY);
    }

    [TestMethod]
    public void GetSpawnTile_ZeroStage_NoNodes_ReturnsDefault()
    {
        // Inga noder alls → faller tillbaka på default-koordinaterna.
        var sys = new TiledWorldMapSystem(new List<PlacedObject>());
        var (tX, tY) = sys.GetSpawnTile(spawnAtWorldMap: 0, 2, 8);
        Assert.AreEqual(2, tX);
        Assert.AreEqual(8, tY);
    }

    [TestMethod]
    public void GetSpawnTile_StageNotInNodeList_ReturnsDefault()
    {
        // maptwo (stage 2) finns inte i listan med bara mapone
        var sys = new TiledWorldMapSystem(new List<PlacedObject> { Node("mapone", 5, 8) });
        var (tX, tY) = sys.GetSpawnTile(2, 3, 9);
        Assert.AreEqual(3, tX);
        Assert.AreEqual(9, tY);
    }

    // ── GetStageIndex ──────────────────────────────────────────────────────────

    [TestMethod]
    public void GetStageIndex_AllMapNames_ReturnCorrectIndex()
    {
        var sys = new TiledWorldMapSystem(new List<PlacedObject>());
        string[] subTypes = { "mapone","maptwo","mapthree","mapfour","mapfive",
                               "mapsix","mapseven","mapeight","mapnine" };
        for (int i = 0; i < subTypes.Length; i++)
            Assert.AreEqual(i + 1, sys.GetStageIndex(subTypes[i]), $"SubType: {subTypes[i]}");
    }

    [TestMethod]
    public void GetStageIndex_EmptyString_ReturnsMinusOne()
        => Assert.AreEqual(-1, new TiledWorldMapSystem(new List<PlacedObject>()).GetStageIndex(""));

    [TestMethod]
    public void GetStageIndex_UnknownSubType_ReturnsMinusOne()
        => Assert.AreEqual(-1, new TiledWorldMapSystem(new List<PlacedObject>()).GetStageIndex("unknown"));

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
