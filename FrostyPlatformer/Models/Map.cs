#nullable enable
using FrostyPlatformer.Commands;
using FrostyPlatformer.Core;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyPlatformer.Models
{


    public class Map : IMapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }
        public string? SpritePath { get; set; }
        /// <summary>
        /// Plural for index
        /// </summary>
        private int[]? Indices { get; set; }
        private bool[]? Solids { get; set; }

        protected IAssets? Assets { get; set; }
        protected IEnemyFactory? EnemyFactory { get; set; }
        protected IItemFactory? ItemFactory { get; set; }

        public static ScriptProcessor Script { get { return Core.Aggregate.Instance.Script;} }

        public Map()
        {
            Width = 0;
            Height = 0;
            Name = "";
            SpritePath = null;
        }

        public virtual bool PopulateDynamics(List<DynamicGameObject> listDynamicObjs)
        {
            return false;
        }
        public virtual bool OnInteraction(List<DynamicGameObject> listDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {
            return false;
        }

        public int GetIndex(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                return Indices![y * Width + x];
            else
                return 0;
        }


        public bool GetSolid(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return Solids![y * Width + x];
            }
            else
            {
                 return true;
            }
        }


        public bool Create(CreateObj createObj)
        {
            Name = createObj.name;
            SpritePath = createObj.spritePath;
            Height = createObj.levelObj.Height;
            Width = createObj.levelObj.Width;
            Indices = createObj.levelObj.TileIndex;

            // Only use solid or not solid for now. Might want other states in the future. 
            Solids = new bool[Width * Height];
            var length = createObj.levelObj.AttributeIndex.Length;
            for (int i = 0; i < length; i++)
            {
                if (createObj.levelObj.AttributeIndex[i].Equals(0))
                {
                    Solids[i] = false;
                }
                else
                {
                    Solids[i] = true;
                }
            }

            return false;
        }

    };




    public class WorldMap : Map
    {
        public CreateObj CreateObj { get; set; }

        public WorldMap(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;

            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("worldmap"),
                spritePath = assets.GetSpritePath("tilesheetwm"), // tilesheetwm //tilesheetone
                name = "worldmap",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }

        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {

            //Overlay
            #region overlay
            DynamicGameObject overlay1 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay1);
            overlay1.px = 3;
            overlay1.py = 8;
            overlay1.Name = "overlayworldmap";
            overlay1.Id = 1;
            overlay1.StageStatus = Enum.StageStatus.NotPassed;

            DynamicGameObject overlay2 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay2);
            overlay2.px = 6;
            overlay2.py = 8;
            overlay2.Name = "overlayworldmap";
            overlay2.Id = 2;
            overlay2.StageStatus = Enum.StageStatus.NotPassed;

            DynamicGameObject overlay3 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay3);
            overlay3.px = 9;
            overlay3.py = 8;
            overlay3.Name = "overlayworldmap";
            overlay3.Id = 3;
            overlay3.StageStatus = Enum.StageStatus.NotPassed;

            DynamicGameObject overlay4 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay4);
            overlay4.px = 12;
            overlay4.py = 8;
            overlay4.Name = "overlayworldmap";
            overlay4.Id = 4;
            overlay4.StageStatus = Enum.StageStatus.NotPassed;

            DynamicGameObject overlay5 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay5);
            overlay5.px = 15;
            overlay5.py = 8;
            overlay5.Name = "overlayworldmap";
            overlay5.Id = 5;
            overlay5.StageStatus = Enum.StageStatus.NotPassed;

            DynamicGameObject overlay6 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay6);
            overlay6.px = 18;
            overlay6.py = 8;
            overlay6.Name = "overlayworldmap";
            overlay6.Id = 6;
            overlay6.StageStatus = Enum.StageStatus.NotPassed;

            DynamicGameObject overlay7 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay7);
            overlay7.px = 21;
            overlay7.py = 8;
            overlay7.Name = "overlayworldmap";
            overlay7.Id = 7;
            overlay7.StageStatus = Enum.StageStatus.NotPassed;

            DynamicGameObject overlay8 = new DynamicCreatureOverlayWorldMap();
            ListDynamicObjs.Add(overlay8);
            overlay8.px = 24;
            overlay8.py = 8;
            overlay8.Name = "overlayworldmap";
            overlay8.Id = 8;
            overlay8.StageStatus = Enum.StageStatus.NotPassed;

            #endregion


            return true;
        }

        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (target.Name == "Teleport")
            {
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }

    }


    public class MapOne : Map
    {
        public CreateObj CreateObj { get; set; }

        public MapOne(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;

            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapone"),
                spritePath = assets.GetSpritePath("tilesheetspring"),
                name = "mapone",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }




        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {

            //Placering
            float placeraPortalX = 57.5f;
            float placeraPortalY = 20;
            float skickaTillX = 2;
            float skickaTillY = 3;
            // ListDynamicObjs.Add(new Teleport(placeraPortalX, placeraPortalY, "maptwo", skickaTillX, skickaTillY)); // Placering hamna, plcering visa
            ListDynamicObjs.Add(new Teleport(placeraPortalX, placeraPortalY, "worldmap", skickaTillX, skickaTillY));


            // Placera ut fiender
            //DynamicGameObject g1 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            //ListDynamicObjs.Add(g1);
            //g1.px = 11;
            //g1.py = 5;
            //g1.Name = "BadPeng";

            //DynamicGameObject g2 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            //ListDynamicObjs.Add(g2);
            //g2.px = 14;
            //g2.py = 5;
            //g2.Name = "NoTwo";


            //Valross
            DynamicGameObject g3 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g3);
            g3.px = 37;
            g3.py = 21;
            g3.Name = "walrus";


            //Istapp
            //DynamicGameObject g4 = EnemyFactory!.Create(EnemyType.Icicle, Assets!);
            //ListDynamicObjs.Add(g4);
            //g4.px = 60;
            //g4.py = 7;
            //g4.Name = "icicle";


            // 3 18
            //DynamicGameObject g5 = EnemyFactory!.Create(EnemyType.Boss, Assets!);
            //ListDynamicObjs.Add(g5);
            //g5.px = 3;
            //g5.py = 18;
            //g5.Name = "frostboss";


            // Add items
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 20, 23, Assets!, 0, 1));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 25, 23, Assets!, 0, 2));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 43, 22, Assets!, 0, 3));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 47, 22, Assets!, 0, 4));


            return true;
        }

        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 1)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 1;
                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 1;
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }


    }


    public class MapTwo : Map
    {

        public CreateObj CreateObj { get; set; }

        public MapTwo(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("maptwo"),
                spritePath = assets.GetSpritePath("tilesheetspring"),
                name = "maptwo",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }


        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {


            //ListDynamicObjs.Add(new Teleport(50.0f, 3.0f, "mapone", 2.0f, 2.0f)); // placering portal
            ListDynamicObjs.Add(new Teleport(125.5f, 23.0f, "worldmap", 2.0f, 3.0f));


            //Valross
            DynamicGameObject g2 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g2);
            g2.px = 8;
            g2.py = 22;
            g2.Name = "walrus";

            DynamicGameObject g3 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g3);
            g3.px = 20;
            g3.py = 13;
            g3.Name = "walrus";

            DynamicGameObject g4 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g4);
            g4.px = 87;
            g4.py = 21;
            g4.Name = "walrus";

            DynamicGameObject g5 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g5);
            g5.px = 93;
            g5.py = 22;
            g5.Name = "walrus";

            DynamicGameObject g6 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g6);
            g6.px = 100;
            g6.py = 22;
            g6.Name = "walrus";

            DynamicGameObject g7 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g7);
            g7.px = 111;
            g7.py = 18;
            g7.Name = "walrus";

            DynamicGameObject g8 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(g8);
            g8.px = 47;
            g8.py = 22;
            g8.Name = "walrus";

            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 29, 23, Assets!, 0, 5));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 30, 23, Assets!, 0, 6));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 31, 23, Assets!, 0, 7));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 32, 23, Assets!, 0, 8));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 65, 18, Assets!, 0, 9));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 97, 23, Assets!, 0, 10));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 98, 23, Assets!, 0, 11));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 40, 18, Assets!, 0, 12));


            return true;
        }

        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 2)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 2;
                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 2;
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }
    }

    public class MapThree : Map
    {

        public CreateObj CreateObj { get; set; }

        public MapThree(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapthree"),
                spritePath = assets.GetSpritePath("tilesheetsummer"),
                name = "mapthree",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }


        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {

            //ListDynamicObjs.Add(new Teleport(50.0f, 3.0f, "mapone", 2.0f, 2.0f)); // placering portal
            ListDynamicObjs.Add(new Teleport(187.5f, 3.0f, "worldmap", 2.0f, 5.0f));


            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 31, 23, Assets!, 0, 13));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 32, 23, Assets!, 0, 14));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 33, 23, Assets!, 0, 15));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 34, 23, Assets!, 0, 16));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 189, 17, Assets!, 0, 17));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 172, 19, Assets!, 0, 18));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 155, 22, Assets!, 0, 19));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 83, 8, Assets!, 0, 20));


            DynamicGameObject g1 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g1);
            g1.px = 20;
            g1.py =22;
            g1.Name = "BadPeng"; 

            DynamicGameObject g2 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g2);
            g2.px = 45;
            g2.py = 18;
            g2.Name = "BadPeng";

            DynamicGameObject g3 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g3);
            g3.px = 53;
            g3.py = 18;
            g3.Name = "BadPeng";

            DynamicGameObject g4 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g4);
            g4.px = 86;
            g4.py = 9;
            g4.Name = "BadPeng";

            DynamicGameObject g5 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g5);
            g5.px = 105;
            g5.py = 13;
            g5.Name = "BadPeng";

            DynamicGameObject g6 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g6);
            g6.px = 110;
            g6.py = 12;
            g6.Name = "BadPeng";

            DynamicGameObject g7 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g7);
            g7.px = 162;
            g7.py = 8;
            g7.Name = "BadPeng";

            DynamicGameObject g8 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g8);
            g8.px = 176;
            g8.py = 7;
            g8.Name = "BadPeng";

            return true;
        }

        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 3)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 3;

                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 3;
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }
    }

    public class MapFour : Map
    {

        public CreateObj CreateObj { get; set; }

        public MapFour(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapfour"),
                spritePath = assets.GetSpritePath("tilesheetsummer"),
                name = "mapfour",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }


        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {
            #region items
            // Placera ut fiender
            DynamicGameObject g1 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g1);
            g1.px = 17;
            g1.py = 4;
            g1.Name = "BadPeng";

            DynamicGameObject g2 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g2);
            g2.px = 30;
            g2.py = 10;
            g2.Name = "BadPeng";

            DynamicGameObject g3 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g3);
            g3.px = 37;
            g3.py = 7;
            g3.Name = "BadPeng";

            DynamicGameObject g4 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g4);
            g4.px = 48;
            g4.py = 9;
            g4.Name = "BadPeng";

            DynamicGameObject g5 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g5);
            g5.px = 70;
            g5.py = 7;
            g5.Name = "BadPeng";

            DynamicGameObject g6 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g6);
            g6.px = 86;
            g6.py = 9;
            g6.Name = "BadPeng";

            DynamicGameObject g7 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g7);
            g7.px = 98;
            g7.py = 10;
            g7.Name = "BadPeng";

            DynamicGameObject g8 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g8);
            g8.px = 121;
            g8.py = 8;
            g8.Name = "BadPeng";

            DynamicGameObject g9 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g9);
            g9.px = 131;
            g9.py = 4;
            g9.Name = "BadPeng";

            DynamicGameObject g10 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g10);
            g10.px = 168;
            g10.py = 6;
            g10.Name = "BadPeng";

            DynamicGameObject g11 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g11);
            g11.px = 35;
            g11.py = 15;
            g11.Name = "BadPeng";

            DynamicGameObject g12 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g12);
            g12.px = 36;
            g12.py = 20;
            g12.Name = "BadPeng";

            DynamicGameObject g13 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g13);
            g13.px = 46;
            g13.py = 19;
            g13.Name = "BadPeng";

            DynamicGameObject g14 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g14);
            g14.px = 66;
            g14.py = 14;
            g14.Name = "BadPeng";

            DynamicGameObject g15 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g15);
            g15.px = 58;
            g15.py = 14;
            g15.Name = "BadPeng";

            DynamicGameObject g16 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g16);
            g16.px = 47;
            g16.py = 13;
            g16.Name = "BadPeng";

            DynamicGameObject g17 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g17);
            g17.px = 185;
            g17.py = 13;
            g17.Name = "BadPeng";

            DynamicGameObject g18 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g18);
            g18.px = 176;
            g18.py = 15;
            g18.Name = "BadPeng";

            DynamicGameObject g19 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g19);
            g19.px = 168;
            g19.py = 17;
            g19.Name = "BadPeng";

            DynamicGameObject g20 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g20);
            g20.px = 139;
            g20.py = 20;
            g20.Name = "BadPeng";

            DynamicGameObject g21 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g21);
            g21.px = 124;
            g21.py = 18;
            g21.Name = "BadPeng";

            DynamicGameObject g22 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g22);
            g22.px = 106;
            g22.py = 18;
            g22.Name = "BadPeng";

            DynamicGameObject g23 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(g23);
            g23.px = 96;
            g23.py = 16;
            g23.Name = "BadPeng";


          



            // Add items
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 16, 21, Assets!, 0, 21));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 17, 21, Assets!, 0, 22));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 18, 21, Assets!, 0, 23));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 19, 21, Assets!, 0, 24));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 74, 15, Assets!, 0, 25));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 103, 11, Assets!, 0, 26));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 121, 13, Assets!, 0, 27));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 122, 13, Assets!, 0, 28));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 127, 3, Assets!, 0, 29));
            //ListDynamicObjs.Add(new DynamicItem(128, 3, Assets!.GetItem("energi")));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 190, 3, Assets!, 0, 30));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 157, 18, Assets!, 0, 31));
            
            #endregion


            //ListDynamicObjs.Add(new Teleport(50.0f, 3.0f, "mapone", 2.0f, 2.0f)); // placering portal
            ListDynamicObjs.Add(new Teleport(183.5f, 4.0f, "worldmap", 2.0f, 5.0f));


            return true;
        }

        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 4)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 4;

                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 4;
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }
    }


    public class MapFive : Map
    {
        //TODO

        public CreateObj CreateObj { get; set; }

        public MapFive(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            //TODO
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapfive"),
                spritePath = assets.GetSpritePath("tilesheetfall"),
                name = "mapfive",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }

        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {
            DynamicGameObject p1 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p1);
            p1.px = 152;
            p1.py = 35;
            p1.Name = "BadPeng";

            DynamicGameObject p2 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p2);
            p2.px = 84;
            p2.py = 30;
            p2.Name = "BadPeng";

            DynamicGameObject p3 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p3);
            p3.px = 109;
            p3.py = 27;
            p3.Name = "BadPeng";

            DynamicGameObject p4 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p4);
            p4.px = 96;
            p4.py = 18;
            p4.Name = "BadPeng";

            DynamicGameObject p5 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p5);
            p5.px = 104;
            p5.py = 17;
            p5.Name = "BadPeng";

            DynamicGameObject p6 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p6);
            p6.px = 142;
            p6.py = 16;
            p6.Name = "BadPeng";


            DynamicGameObject w1 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w1);
            w1.px = 26;
            w1.py = 35;
            w1.Name = "walrus";

            DynamicGameObject w2 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w2);
            w2.px = 49;
            w2.py = 39;
            w2.Name = "walrus";

            DynamicGameObject w3 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w3);
            w3.px = 90;
            w3.py = 43;
            w3.Name = "walrus";

            DynamicGameObject w4 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w4);
            w4.px = 120;
            w4.py = 46;
            w4.Name = "walrus";

            DynamicGameObject w5 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w5);
            w5.px = 157;
            w5.py = 47;
            w5.Name = "walrus";

            DynamicGameObject w6 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w6);
            w6.px = 131;
            w6.py = 38;
            w6.Name = "walrus";

            DynamicGameObject w7 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w7);
            w7.px = 116;
            w7.py = 23;
            w7.Name = "walrus";

            DynamicGameObject w8 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w8);
            w8.px = 116;
            w8.py = 16;
            w8.Name = "walrus";

            DynamicGameObject w9 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w9);
            w9.px = 164;
            w9.py = 19;
            w9.Name = "walrus";

            DynamicGameObject w10 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w10);
            w10.px = 181;
            w10.py = 18;
            w10.Name = "walrus";


            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 56, 40, Assets!, 0, 32));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 141, 48, Assets!, 0, 33));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 143, 36, Assets!, 0, 34));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 115, 36, Assets!, 0, 35));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 86, 30, Assets!, 0, 36));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 99, 18, Assets!, 0, 37));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 140, 15, Assets!, 0, 38));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 183, 17, Assets!, 0, 39));

            ListDynamicObjs.Add(new Teleport(188.5f, 17.0f, "worldmap", 2.0f, 5.0f));

            return true;
        }
        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            //TODO
            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 5)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 5;

                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 5;
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }

    }
    public class MapSix : Map
    {
        //TODO
        public CreateObj CreateObj { get; set; }

        public MapSix(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapsix"),
                spritePath = assets.GetSpritePath("tilesheetfall"),
                name = "mapsix",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }

        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {

            DynamicGameObject p1 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p1);
            p1.px = 20;
            p1.py = 21;
            p1.Name = "BadPeng";

            DynamicGameObject p2 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p2);
            p2.px = 28;
            p2.py = 20;
            p2.Name = "BadPeng";

            DynamicGameObject p3 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p3);
            p3.px = 86;
            p3.py = 20;
            p3.Name = "BadPeng";

            DynamicGameObject p4 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p4);
            p4.px = 92;
            p4.py = 18;
            p4.Name = "BadPeng";

            DynamicGameObject p5 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p5);
            p5.px = 174;
            p5.py = 16;
            p5.Name = "BadPeng";

            DynamicGameObject p6 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p6);
            p6.px = 234;
            p6.py = 20;
            p6.Name = "BadPeng";


            DynamicGameObject w1 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w1);
            w1.px = 34;
            w1.py = 20;
            w1.Name = "walrus";

            DynamicGameObject w2 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w2);
            w2.px = 58;
            w2.py = 20;
            w2.Name = "walrus";

            DynamicGameObject w3 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w3);
            w3.px = 66;
            w3.py = 20;
            w3.Name = "walrus";

            DynamicGameObject w4 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w4);
            w4.px = 104;
            w4.py = 17;
            w4.Name = "walrus";

            DynamicGameObject w5 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w5);
            w5.px = 126;
            w5.py = 14;
            w5.Name = "walrus";

            DynamicGameObject w6 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w6);
            w6.px = 147;
            w6.py = 17;
            w6.Name = "walrus";

            DynamicGameObject w7 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w7);
            w7.px = 161;
            w7.py = 16;
            w7.Name = "walrus";

            DynamicGameObject w8 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w8);
            w8.px = 166;
            w8.py = 16;
            w8.Name = "walrus";

            DynamicGameObject w9 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w9);
            w9.px = 192;
            w9.py = 19;
            w9.Name = "walrus";

            DynamicGameObject w10 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w10);
            w10.px = 208;
            w10.py = 19;
            w10.Name = "walrus";

            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 19, 21, Assets!, 0, 40));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 21, 21, Assets!, 0, 41));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 29, 20, Assets!, 0, 42));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 30, 30, Assets!, 0, 43));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 33, 20, Assets!, 0, 44));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 41, 21, Assets!, 0, 45));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 42, 21, Assets!, 0, 46));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 51, 20, Assets!, 0, 47));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 68, 20, Assets!, 0, 48));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 102, 17, Assets!, 0, 49));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 152, 18, Assets!, 0, 50));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 174, 16, Assets!, 0, 51));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 197, 18, Assets!, 0, 52));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 198, 18, Assets!, 0, 53));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 199, 18, Assets!, 0, 54));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 245, 19, Assets!, 0, 55));


            ListDynamicObjs.Add(new Teleport(253.5f, 18.0f, "worldmap", 2.0f, 5.0f));

            return true;
        }
        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            //TODO
            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 6)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 6;

                    // Flag this is final stage

                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 6;

                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }
    }
    public class MapSeven : Map
    {
        //TODO
        public CreateObj CreateObj { get; set; }

        public MapSeven(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            //TODO
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapseven"),
                spritePath = assets.GetSpritePath("tilesheetwinter"),
                name = "mapseven",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }

        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {


            DynamicGameObject p1 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p1);
            p1.px = 49;
            p1.py = 18;
            p1.Name = "BadPeng";

            DynamicGameObject p2 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p2);
            p2.px = 93;
            p2.py = 16;
            p2.Name = "BadPeng";

            DynamicGameObject p3 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p3);
            p3.px = 187;
            p3.py = 19;
            p3.Name = "BadPeng";

            DynamicGameObject p4 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p4);
            p4.px = 204;
            p4.py = 14;
            p4.Name = "BadPeng";

            DynamicGameObject p5 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p5);
            p5.px = 220;
            p5.py = 11;
            p5.Name = "BadPeng";

            DynamicGameObject p6 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p6);
            p6.px = 223;
            p6.py = 13;
            p6.Name = "BadPeng";

            DynamicGameObject p7 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p7);
            p7.px = 239;
            p7.py = 12;
            p7.Name = "BadPeng";

            DynamicGameObject p8 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p8);
            p8.px = 268;
            p8.py = 18;
            p8.Name = "BadPeng";

            DynamicGameObject p9 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p9);
            p9.px = 274;
            p9.py = 20;
            p9.Name = "BadPeng";

            DynamicGameObject p10 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p10);
            p10.px = 299;
            p10.py = 20;
            p10.Name = "BadPeng";

            DynamicGameObject p11 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p11);
            p11.px = 315;
            p11.py = 20;
            p11.Name = "BadPeng";

            DynamicGameObject p12 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p12);
            p12.px = 231;
            p12.py = 6;
            p12.Name = "BadPeng";

            DynamicGameObject p13 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p13);
            p13.px = 254;
            p13.py = 6;
            p13.Name = "BadPeng";

            DynamicGameObject p14 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p14);
            p14.px = 237;
            p14.py = 3;
            p14.Name = "BadPeng";

            DynamicGameObject p15 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p15);
            p15.px = 37;
            p15.py = 7;
            p15.Name = "BadPeng";

            DynamicGameObject p16 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p16);
            p16.px = 62;
            p16.py = 7;
            p16.Name = "BadPeng";


            DynamicGameObject f01 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f01);
            f01.px = 22;
            f01.py = 19;
            //f01.py = 20;
            f01.FromCor = 18; 
            f01.ToCor = 28;
            f01.Name = "frost";
            f01.Id = 1;


            DynamicGameObject f02 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f02);
            f02.px = 65;
            f02.py = 19;
            f02.FromCor = 65; 
            f02.ToCor = 67; 
            f02.Name = "frost";
            f02.Id = 2;

            ///
            DynamicGameObject f03 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f03);
            f03.px = 40;
            f03.py = 17;
            f03.FromCor = 36; 
            f03.ToCor = 46; 
            f03.Name = "frost";
            f03.Id = 3;

            DynamicGameObject f04 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f04);
            f04.px = 98;
            f04.py = 18;
            f04.FromCor = 96;
            f04.ToCor = 105;
            f04.Name = "frost";
            f04.Id = 4;
           


            DynamicGameObject f05 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f05);
            f05.px = 157;
            f05.py = 17;
            f05.FromCor = 151;
            f05.ToCor = 162;
            f05.Name = "frost";
            f05.Id = 5;

            DynamicGameObject f06 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f06);
            f06.px = 165;
            f06.py = 18;
            f06.FromCor = 163;
            f06.ToCor = 169;
            f06.Name = "frost";
            f06.Id = 6;


            DynamicGameObject f07 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f07);
            f07.px = 104;
            f07.py = 7;
            f07.FromCor = 104;
            f07.ToCor = 106;
            f07.Name = "frost";
            f07.Id = 7;

            DynamicGameObject f08 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f08);
            f08.px = 89;
            f08.py = 8;
            f08.FromCor = 89;
            f08.ToCor = 93;
            f08.Name = "frost";
            f08.Id = 8;


            DynamicGameObject f09 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f09);
            f09.px = 72;
            f09.py = 9;
            f09.FromCor = 71;
            f09.ToCor = 74;
            f09.Name = "frost";
            f09.Id = 9;

            DynamicGameObject f10 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f10);
            f10.px = 74;
            f10.py = 9;
            f10.FromCor = 74;
            f10.ToCor = 78;
            f10.Name = "frost";
            f10.Id = 10;


            DynamicGameObject f11 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f11);
            f11.px = 65;
            f11.py = 9;
            f11.FromCor = 65;
            f11.ToCor = 67;
            f11.Name = "frost";
            f11.Id = 11;

            DynamicGameObject f12 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f12);
            f12.px = 44;
            f12.py = 6;
            f12.FromCor = 43;
            f12.ToCor = 50;
            f12.Name = "frost";
            f12.Id = 12;


            DynamicGameObject f13 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f13);
            f13.px = 19;
            f13.py = 7;
            f13.FromCor = 17;
            f13.ToCor = 20;
            f13.Name = "frost";
            f13.Id = 13;

            DynamicGameObject f14 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f14);
            f14.px = 149;
            f14.py = 9;
            f14.FromCor = 149;
            f14.ToCor = 161;
            f14.Name = "frost";
            f14.Id = 14;


            DynamicGameObject f15 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f15);
            f15.px = 179;
            f15.py = 5;
            f15.FromCor = 177;
            f15.ToCor = 181;
            f15.Name = "frost";
            f15.Id = 15;

            DynamicGameObject f16 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f16);
            f16.px = 234;
            f16.py = 11;
            f16.FromCor = 234;
            f16.ToCor = 236;
            f16.Name = "frost";
            f16.Id = 16;


            DynamicGameObject f17 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f17);
            f17.px = 298;
            f17.py = 4;
            f17.FromCor = 298;
            f17.ToCor = 302;
            f17.Name = "frost";
            f17.Id = 17;



            DynamicGameObject w1 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w1);
            w1.px = 32;
            w1.py = 19;
            w1.Name = "walrus";


            DynamicGameObject w2 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w2);
            w2.px = 101;
            w2.py = 14;
            w2.Name = "walrus";

            DynamicGameObject w3 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w3);
            w3.px = 110;
            w3.py = 21;
            w3.Name = "walrus";

            DynamicGameObject w4 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w4);
            w4.px = 112;
            w4.py = 21;
            w4.Name = "walrus";

            DynamicGameObject w5 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w5);
            w5.px = 127;
            w5.py = 20;
            w5.Name = "walrus";

            DynamicGameObject w6 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w6);
            w6.px = 143;
            w6.py = 19;
            w6.Name = "walrus";

            DynamicGameObject w7 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w7);
            w7.px = 160;
            w7.py = 17;
            w7.Name = "walrus";

            DynamicGameObject w8 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w8);
            w8.px = 176;
            w8.py = 19;
            w8.Name = "walrus";

            DynamicGameObject w9 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w9);
            w9.px = 121;
            w9.py = 9;
            w9.Name = "walrus";

            DynamicGameObject w10 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w10);
            w10.px = 138;
            w10.py = 8;
            w10.Name = "walrus";

            DynamicGameObject w11 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w11);
            w11.px = 172;
            w11.py = 7;
            w11.Name = "walrus";

            DynamicGameObject w12 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w12);
            w12.px = 186;
            w12.py = 4;
            w12.Name = "walrus";

            DynamicGameObject w13 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w13);
            w13.px = 202;
            w13.py = 6;
            w13.Name = "walrus";

            DynamicGameObject w14 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w14);
            w14.px = 214;
            w14.py = 7;
            w14.Name = "walrus";

            DynamicGameObject w15 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w15);
            w15.px = 223;
            w15.py = 19;
            w15.Name = "walrus";

            DynamicGameObject w16 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w16);
            w16.px = 247;
            w16.py = 14;
            w16.Name = "walrus";

            DynamicGameObject w17 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w17);
            w17.px = 260;
            w17.py = 15;
            w17.Name = "walrus";

            DynamicGameObject w18 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w18);
            w18.px = 278;
            w18.py = 21;
            w18.Name = "walrus";

            DynamicGameObject w19 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w19);
            w19.px = 294;
            w19.py = 21;
            w19.Name = "walrus";

            DynamicGameObject w20 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w20);
            w20.px = 245;
            w20.py = 3;
            w20.Name = "walrus";

            DynamicGameObject w21 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w21);
            w21.px = 291;
            w21.py = 4;
            w21.Name = "walrus";

            DynamicGameObject w22 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w22);
            w22.px = 126;
            w22.py = 3;
            w22.Name = "walrus";

            DynamicGameObject w23 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w23);
            w23.px = 97;
            w23.py = 7;
            w23.Name = "walrus";

            DynamicGameObject w24 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w24);
            w24.px = 55;
            w24.py = 6;
            w24.Name = "walrus";

            //DynamicGameObject w25 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            //ListDynamicObjs.Add(w25);
            //w25.px = 126;
            //w25.py = 14;
            //w25.Name = "walrus";

            DynamicGameObject w26 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w26);
            w26.px = 7;
            w26.py = 10;
            w26.Name = "walrus";


            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 205, 14, Assets!, 0, 56));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 182, 16, Assets!, 0, 57));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 174, 19, Assets!, 0, 58));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 159, 17, Assets!, 0, 59));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 158, 17, Assets!, 0, 60));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 147, 20, Assets!, 0, 61));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 131, 23, Assets!, 0, 62));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 132, 23, Assets!, 0, 63));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 110, 21, Assets!, 0, 64));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 94, 15, Assets!, 0, 65));


            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 83, 20, Assets!, 0, 66));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 49, 18, Assets!, 0, 67));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 118, 11, Assets!, 0, 68));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 119, 11, Assets!, 0, 69));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 158, 7, Assets!, 0, 70));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 192, 7, Assets!, 0, 71));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 226, 15, Assets!, 0, 72));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 250, 15, Assets!, 0, 73));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 317, 20, Assets!, 0, 74));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 233, 6, Assets!, 0, 75));

            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 256, 6, Assets!, 0, 76));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 1, 7, Assets!, 0, 77));

            ListDynamicObjs.Add(new Teleport(315.5f, 7.0f, "worldmap", 2.0f, 5.0f));

            return true;
        }


        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 7)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 7;

                    // Flag this is final stage
                   
                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 7;
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }
    }

    public class MapEight : Map
    {
        //TODO
        public CreateObj CreateObj { get; set; }

        public MapEight(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            //TODO
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapeight"),
                spritePath = assets.GetSpritePath("tilesheetwinter"),
                name = "mapeight",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }

        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {
            #region W
            DynamicGameObject w1 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w1);
            w1.px = 64;
            w1.py = 41;
            w1.Name = "walrus";

            DynamicGameObject w2 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w2);
            w2.px = 80;
            w2.py = 43;
            w2.Name = "walrus";

            DynamicGameObject w3 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w3);
            w3.px = 69;
            w3.py = 47;
            w3.Name = "walrus";

            DynamicGameObject w4 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w4);
            w4.px = 43;
            w4.py = 47;
            w4.Name = "walrus";

            DynamicGameObject w5 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w5);
            w5.px = 22;
            w5.py = 47;
            w5.Name = "walrus";

            DynamicGameObject w6 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w6);
            w6.px = 106;
            w6.py = 47;
            w6.Name = "walrus";

            DynamicGameObject w7 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w7);
            w7.px = 130;
            w7.py = 43;
            w7.Name = "walrus";

            DynamicGameObject w8 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w8);
            w8.px = 364;
            w8.py = 47;
            w8.Name = "walrus";

            DynamicGameObject w9 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w9);
            w9.px = 171;
            w9.py = 43;
            w9.Name = "walrus";

            DynamicGameObject w10 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w10);
            w10.px = 184;
            w10.py = 47;
            w10.Name = "walrus";

            DynamicGameObject w11 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w11);
            w11.px = 200;
            w11.py = 46;
            w11.Name = "walrus";

            DynamicGameObject w12 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w12);
            w12.px = 245;
            w12.py = 45;
            w12.Name = "walrus";

            DynamicGameObject w13 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w13);
            w13.px = 229;
            w13.py = 45;
            w13.Name = "walrus";





            DynamicGameObject w14 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w14);
            w14.px = 67;
            w14.py = 36;
            w14.Name = "walrus";


            DynamicGameObject w15 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w15);
            w15.px = 118;
            w15.py = 29;
            w15.Name = "walrus";

            DynamicGameObject w16 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w16);
            w16.px = 161;
            w16.py = 27;
            w16.Name = "walrus";

            DynamicGameObject w17 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w17);
            w17.px = 174;
            w17.py = 34;
            w17.Name = "walrus";

            DynamicGameObject w18 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w18);
            w18.px = 205;
            w18.py = 29;
            w18.Name = "walrus";

            DynamicGameObject w19 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w19);
            w19.px = 343;
            w19.py = 24;
            w19.Name = "walrus";

            DynamicGameObject w20 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w20);
            w20.px = 312;
            w20.py = 21;
            w20.Name = "walrus";

            DynamicGameObject w21 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w21);
            w21.px = 212;
            w21.py = 22;
            w21.Name = "walrus";

            DynamicGameObject w22 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w22);
            w22.px = 189;
            w22.py = 14;
            w22.Name = "walrus";

            DynamicGameObject w23 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w23);
            w23.px = 205;
            w23.py = 2;
            w23.Name = "walrus";

            //DynamicGameObject w24 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            //ListDynamicObjs.Add(w24);
            //w24.px = 255;
            //w24.py = 7;
            //w24.Name = "walrus";

            DynamicGameObject w25 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w25);
            w25.px = 247;
            w25.py = 4;
            w25.Name = "walrus";

            DynamicGameObject w26 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w26);
            w26.px = 297;
            w26.py = 4;
            w26.Name = "walrus";

            DynamicGameObject w27 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w27);
            w27.px = 306;
            w27.py = 4;
            w27.Name = "walrus";

            DynamicGameObject w28 = EnemyFactory!.Create(EnemyType.Walrus, Assets!);
            ListDynamicObjs.Add(w28);
            w28.px = 341;
            w28.py = 6;
            w28.Name = "walrus";

            #endregion

            #region P
            DynamicGameObject p1 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p1);
            p1.px = 94;
            p1.py = 46;
            p1.Name = "BadPeng";

            DynamicGameObject p2 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p2);
            p2.px = 100;
            p2.py = 46;
            p2.Name = "BadPeng";

            DynamicGameObject p3 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p3);
            p3.px = 114;
            p3.py = 44;
            p3.Name = "BadPeng";

            DynamicGameObject p4 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p4);
            p4.px = 145;
            p4.py = 39;
            p4.Name = "BadPeng";

            DynamicGameObject p5 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p5);
            p5.px = 147;
            p5.py = 42;
            p5.Name = "BadPeng";

            DynamicGameObject p6 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p6);
            p6.px = 219;
            p6.py = 44;
            p6.Name = "BadPeng";

            DynamicGameObject p7 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p7);
            p7.px = 295;
            p7.py = 43;
            p7.Name = "BadPeng";


            //
            DynamicGameObject p8 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p8);
            p8.px = 89;
            p8.py = 32;
            p8.Name = "BadPeng";

            DynamicGameObject p9 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p9);
            p9.px = 108;
            p9.py = 30;
            p9.Name = "BadPeng";

            DynamicGameObject p10 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p10);
            p10.px = 147;
            p10.py = 26;
            p10.Name = "BadPeng";

            DynamicGameObject p11 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p11);
            p11.px = 286;
            p11.py = 23;
            p11.Name = "BadPeng";

            DynamicGameObject p12 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p12);
            p12.px = 256;
            p12.py = 23;
            p12.Name = "BadPeng";

            DynamicGameObject p13 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p13);
            p13.px = 203;
            p13.py = 9;
            p13.Name = "BadPeng";

            DynamicGameObject p14 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p14);
            p14.px = 231;
            p14.py = 8;
            p14.Name = "BadPeng";

            DynamicGameObject p15 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p15);
            p15.px = 257;
            p15.py = 2;
            p15.Name = "BadPeng";

            DynamicGameObject p16 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p16);
            p16.px = 270;
            p16.py = 9;
            p16.Name = "BadPeng";

            //
            DynamicGameObject p17 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p17);
            p17.px = 286;
            p17.py = 4;
            p17.Name = "BadPeng";

            DynamicGameObject p18 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p18);
            p18.px = 328;
            p18.py = 2;
            p18.Name = "BadPeng";

            DynamicGameObject p19 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p19);
            p19.px = 332;
            p19.py = 2;
            p19.Name = "BadPeng";

            DynamicGameObject p20 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p20);
            p20.px = 341;
            p20.py = 14;
            p20.Name = "BadPeng";

            DynamicGameObject p21 = EnemyFactory!.Create(EnemyType.Penguin, Assets!);
            ListDynamicObjs.Add(p21);
            p21.px = 341;
            p21.py = 14;
            p21.Name = "BadPeng";

            #endregion

            #region F
            DynamicGameObject f01 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f01);
            f01.px = 26;
            f01.py = 42;
            //f01.py = 20;
            f01.FromCor = 26;
            f01.ToCor = 28;
            f01.Name = "frost";
            f01.Id = 1;


            DynamicGameObject f02 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f02);
            f02.px = 44;
            f02.py = 42;
            f02.FromCor = 44;
            f02.ToCor = 46;
            f02.Name = "frost";
            f02.Id = 2;

            DynamicGameObject f03 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f03);
            f03.px = 266;
            f03.py = 44;
            f03.FromCor = 266;
            f03.ToCor = 271;
            f03.Name = "frost";
            f03.Id = 3;

            DynamicGameObject f04 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f04);
            f04.px = 314;
            f04.py = 43;
            f04.FromCor = 314;
            f04.ToCor = 317;
            f04.Name = "frost";
            f04.Id = 4;


            DynamicGameObject f05 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f05);
            f05.px = 357;
            f05.py = 23;
            f05.FromCor = 356;
            f05.ToCor = 360;
            f05.Name = "frost";
            f05.Id = 5;

            DynamicGameObject f06 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f06);
            f06.px = 220;
            f06.py = 2;
            f06.FromCor = 220;
            f06.ToCor = 225;
            f06.Name = "frost";
            f06.Id = 6;


            DynamicGameObject f07 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f07);
            f07.px = 256;
            f07.py = 7;
            f07.FromCor = 256;
            f07.ToCor = 261;
            f07.Name = "frost";
            f07.Id = 7;

            DynamicGameObject f08 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f08);
            f08.px = 362;
            f08.py = 6;
            f08.FromCor = 362;
            f08.ToCor = 366;
            f08.Name = "frost";
            f08.Id = 8;

            DynamicGameObject f09 = EnemyFactory!.Create(EnemyType.Frost, Assets!);
            ListDynamicObjs.Add(f09);
            f09.px = 353;
            f09.py = 7;
            f09.FromCor = 351;
            f09.ToCor = 355;
            f09.Name = "frost";
            f09.Id = 9;

            #endregion

            #region Energie


            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 36, 42, Assets!, 0, 78));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 45, 42, Assets!, 0, 79));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 66, 36, Assets!, 0, 80));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 79, 32, Assets!, 0, 81));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 119, 29, Assets!, 0, 82));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 126, 27, Assets!, 0, 83));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 154, 26, Assets!, 0, 84));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 180, 31, Assets!, 0, 85));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 192, 28, Assets!, 0, 86));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 201, 24, Assets!, 0, 87));

            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 195, 20, Assets!, 0, 88));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 185, 18, Assets!, 0, 89));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 189, 14, Assets!, 0, 90));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 200, 10, Assets!, 0, 91));

            //ListDynamicObjs.Add(new DynamicItem(222, 10, Assets!.GetItem("energi")));

            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 274, 4, Assets!, 0, 92));
            ListDynamicObjs.Add(ItemFactory!.Create(ItemType.Energi, 371, 14, Assets!, 0, 93));

            #endregion

           ListDynamicObjs.Add(new Teleport(377.5f, 8.0f, "mapnine", 2.0f, 5.0f));

            // f�r test att snabbt komma till sista banan
            //ListDynamicObjs.Add(new Teleport(3f, 41f, "mapnine", 1.0f, 5.0f));

            return true;
        }


        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (target.Name == "Teleport")
            {
                if (Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted < 8)
                {
                    Core.Aggregate.Instance.Settings!.ActivePlayer.StageCompleted = 8;

                    // Flag this is final stage
                    //Core.Aggregate.Instance.Settings!.ActivePlayer.ShowEnd = true;
                    Core.Aggregate.Instance.HasSwitchedState = true;

                }
                Core.Aggregate.Instance.Settings!.ActivePlayer.SpawnAtWorldMap = 8;
                Script.AddCommand(new CommandChangeMap((target as Teleport)!.MapName, (target as Teleport)!.MapPosX, (target as Teleport)!.MapPosY));
            }

            return false;
        }
    }

    public class MapNine : Map
    {
        //TODO
        public CreateObj CreateObj { get; set; }

        public MapNine(IAssets assets, IEnemyFactory enemyFactory, IItemFactory itemFactory)
        {
            Assets = assets;
            EnemyFactory = enemyFactory;
            ItemFactory = itemFactory;
            //TODO
            this.CreateObj = new CreateObj()
            {
                levelObj = assets.GetMapData("mapnine"),
                spritePath = assets.GetSpritePath("tilesheetwinter"),
                name = "mapnine",
            };

            CrateFromChild();
        }

        public bool CrateFromChild()
        {
            return this.Create(CreateObj);
        }

        public override bool PopulateDynamics(List<DynamicGameObject> ListDynamicObjs)
        {

            //TODO
            //ice
            DynamicGameObject ice1 = EnemyFactory!.Create(EnemyType.Icicle, Assets!);
            ListDynamicObjs.Add(ice1);
            ice1.px = 3;
            ice1.py = 12;
            ice1.Name = "ice";
            ice1.FromCor = 11;
            ice1.ToCor = 14;
            ice1.Attr = 3;
            ice1.Id = 3;

            //ice
            DynamicGameObject ice2 = EnemyFactory!.Create(EnemyType.Icicle, Assets!);
            ListDynamicObjs.Add(ice2);
            ice2.px = 7;
            ice2.py = 12;
            ice2.Name = "ice";
            ice2.FromCor = 11;
            ice2.ToCor = 14;
            ice2.Attr = 2;
            ice2.Id = 2;

            //boss
            DynamicGameObject boss = EnemyFactory!.Create(EnemyType.Boss, Assets!);
            ListDynamicObjs.Add(boss);
            boss.px = 11;
            boss.py = 12;
            boss.Name = "boss";
            boss.FromCor = 11;
            boss.ToCor = 14;
            boss.Attr = 1;
            boss.Id = 1;


            //Overlay
            DynamicGameObject overlay1ice = new DynamicCreatureOverlayIce();
            ListDynamicObjs.Add(overlay1ice);
            overlay1ice.px = 1;
            overlay1ice.py = 13;

            DynamicGameObject overlay2ice = new DynamicCreatureOverlayIce();
            ListDynamicObjs.Add(overlay2ice);
            overlay2ice.px = 3;
            overlay2ice.py = 13;

            DynamicGameObject overlay3ice = new DynamicCreatureOverlayIce();
            ListDynamicObjs.Add(overlay3ice);
            overlay3ice.px = 5;
            overlay3ice.py = 13;

            DynamicGameObject overlay4ice = new DynamicCreatureOverlayIce();
            ListDynamicObjs.Add(overlay4ice);
            overlay4ice.px = 7;
            overlay4ice.py = 13;

            DynamicGameObject overlay5ice = new DynamicCreatureOverlayIce();
            ListDynamicObjs.Add(overlay5ice);
            overlay5ice.px = 9;
            overlay5ice.py = 13;

            DynamicGameObject overlay6ice = new DynamicCreatureOverlayIce();
            ListDynamicObjs.Add(overlay6ice);
            overlay6ice.px = 11;
            overlay6ice.py = 13;

            DynamicGameObject overlay7ice = new DynamicCreatureOverlayIce();
            ListDynamicObjs.Add(overlay7ice);
            overlay7ice.px = 13;
            overlay7ice.py = 13;
            //

            DynamicGameObject overlay1 = new DynamicCreatureOverlay();
            ListDynamicObjs.Add(overlay1);
            overlay1.px = 2;
            overlay1.py = 13;
            overlay1.Name = "overlay";

            DynamicGameObject overlay2 = new DynamicCreatureOverlay();
            ListDynamicObjs.Add(overlay2);
            overlay2.px = 4;
            overlay2.py = 13;
            overlay2.Name = "overlay";

            DynamicGameObject overlay3 = new DynamicCreatureOverlay();
            ListDynamicObjs.Add(overlay3);
            overlay3.px = 6;
            overlay3.py = 13;
            overlay3.Name = "overlay";

            DynamicGameObject overlay4 = new DynamicCreatureOverlay();
            ListDynamicObjs.Add(overlay4);
            overlay4.px = 8;
            overlay4.py = 13;
            overlay4.Name = "overlay";

            DynamicGameObject overlay5 = new DynamicCreatureOverlay();
            ListDynamicObjs.Add(overlay5);
            overlay5.px = 10;
            overlay5.py = 13;
            overlay5.Name = "overlay";

            DynamicGameObject overlay6 = new DynamicCreatureOverlay();
            ListDynamicObjs.Add(overlay6);
            overlay6.px = 12;
            overlay6.py = 13;
            overlay6.Name = "overlay";



            DynamicGameObject overlay7 = new DynamicCreatureOverlay();
            ListDynamicObjs.Add(overlay7);
            overlay7.px = 14;
            overlay7.py = 13;
            overlay7.Name = "overlay";


            //ListDynamicObjs.Add(new Teleport(12f, 12.0f, "worldmap", 2.0f, 5.0f));
            ListDynamicObjs.Add(new Teleport(0.0f, 0.0f, "worldmap", 2.0f, 5.0f));

            return true;
        }


        public override bool OnInteraction(List<DynamicGameObject> ListDynamicObjs, DynamicGameObject target, Enum.NATURE nature)
        {

            if (tar