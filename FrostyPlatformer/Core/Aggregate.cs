#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Commands;

namespace FrostyPlatformer.Core
{
    public class Aggregate : IAssets
    {
        private static readonly object padlock = new object();
        private static Aggregate? instance = null;
        /// <summary>
        /// Toggle error logging on or off
        /// </summary>
        private bool EnableWriteToLog { get; set; } = false;
        Aggregate()
        {
        }
        public static Aggregate Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Aggregate();
                    }
                    return instance;
                }
            }
        }
        public bool HasSwitchedState { get; set; } = false;
        /// <summary>Sätts i Load() — null före dess.</summary>
        public Program? ThisGame { get; set; }
        /// <summary>Sätts i Load() — null! före dess (garanterat initierat via Load).</summary>
        public ReadWrite ReadWrite { get; set; } = null!;
        private Dictionary<string, string> MapSpritePaths { get; set; } = new Dictionary<string, string>();
        private Dictionary<string, Map> MapMaps { get; set; } = new Dictionary<string, Map>();
        private Dictionary<string, Item> MapItems { get; set; } = new Dictionary<string, Item>();
        private FrostyPlatformer.Systems.IMapRepository MapRepository { get; set; } = null!;
        private string PathSprites  => $@"\{MapPath.Resources}\{MapPath.Assets}\{MapPath.Sprites}";
        private string PathSettings => $@"\{MapPath.Resources}\{MapPath.Settings}";
        private string PathSound    => $@"\{MapPath.Resources}\{MapPath.Assets}\{MapPath.Sound}";
        /// <summary>Sätts i Load() — null! före dess (garanterat initierat via Load).</summary>
        public ScriptProcessor Script { get; set; } = null!;
        private Random Random { get; set; } = new Random();

        /// <summary>null om inställningsfilen saknas vid start — kontrollera före användning.</summary>
        public SettingsObj? Settings { get; set; } = new SettingsObj();
        private List<HighScoreObj> HighScoreList { get; set; } = null!;

        internal void Load(Program game)
        {
            ThisGame = game;
            ReadWrite = new ReadWrite(EnableWriteToLog);
            LoadSettings();

            string tiledMapPath = Path.Combine(ReadWrite.GetRoot, MapPath.Resources, MapPath.Assets, MapPath.MapData, MapPath.Tiled);
            MapRepository = new FrostyPlatformer.Systems.TiledMapRepository(tiledMapPath);
            LoadSprites();
            LoadMaps();
            LoadItems();
            Script = new ScriptProcessor();

            LoadHighScore();
        }

        private void LoadSprites()
        {
            // Sprite-filnamnet matchar alltid nyckeln — lokal helper eliminerar upprepningen.
            void Spr(string key) => LoadSprite(key, PathSprites, @"\" + key, FileExt.Png);

            Spr(SpriteRef.TileSheetSpring);
            Spr(SpriteRef.TileSheetSummer);
            Spr(SpriteRef.TileSheetFall);
            Spr(SpriteRef.TileSheetWinter);
            Spr(SpriteRef.TileSheetBoss);
            Spr(SpriteRef.TileSheetCustom);
            Spr(SpriteRef.TileSheetWorldMap);
            Spr(SpriteRef.Font);
            Spr(SpriteRef.Hero);
            Spr(SpriteRef.Items);
            Spr(SpriteRef.EnemyIcicle);
            Spr(SpriteRef.EnemyPenguin);
            Spr(SpriteRef.EnemyWalrus);
            Spr(SpriteRef.EnemyFrost);
            Spr(SpriteRef.EnemyBoss);
            Spr(SpriteRef.MirrorScarlet);
            Spr(SpriteRef.SwarmCopy);
            Spr(SplashScreenRef.Start);
            Spr(SplashScreenRef.End);
            Spr(SpriteRef.EndArt);
        }

        private void LoadSprite(string FriendlyName, string FilePath, string FileName, string FileExtension)
        {
            var fullDirectory = ReadWrite.CreateIfNotExists(FilePath, FileName, FileExtension, false);
            if (!string.IsNullOrEmpty(fullDirectory))
            {
                MapSpritePaths.Add(FriendlyName, fullDirectory);
            }
            else
            {
                ReadWrite.WriteToLog(String.Format("LoadSprite - Could not load resource. FriendlyName: {0}. Root: {1}. Path: {2}. FileName: {3}. FileExtension: {4}",
                    FriendlyName, ReadWrite.GetRoot, FilePath, FileName, FileExtension));
                throw new FileLoadException("Could not Load resource");
            }
        }

        private void LoadMaps()
        {
            var enemyFactory = new FrostyPlatformer.Systems.EnemyFactory();
            var itemFactory  = new FrostyPlatformer.Systems.ItemFactory();

            var wm = new WorldMap(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.WorldMap, wm);

            var lvl1 = new MapOne(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapOne, lvl1);
            var lvl2 = new MapTwo(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapTwo, lvl2);
            var lvl3 = new MapThree(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapThree, lvl3);
            var lvl4 = new MapFour(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapFour, lvl4);
            var lvl5 = new MapFive(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapFive, lvl5);
            var lvl6 = new MapSix(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapSix, lvl6);
            var lvl7 = new MapSeven(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapSeven, lvl7);
            var lvl8 = new MapEight(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapEight, lvl8);

            var lvl9 = new MapNine(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapNine, lvl9);

            var lvl10 = new MapTen(this, enemyFactory, itemFactory);
            MapMaps.Add(MapName.MapTen, lvl10);
        }

        private void LoadItems()
        {
            var e = new ItemEnergi();
            MapItems.Add(ItemRef.Energi, e);
        }

        private void LoadSettings()
        {
            Settings = ReadWrite.ReadJson<SettingsObj>(PathSettings, DataFile.Settings, FileExt.Json)
                       ?? new SettingsObj();

            EnableWriteToLog = Settings.Log;
            if (Settings.Log)
            {
                ReadWrite = new ReadWrite(EnableWriteToLog);
            }
        }

        public bool SaveSettings()
        {
            if (Settings == null) return false;
            return ReadWrite.WriteJson<SettingsObj>(PathSettings, DataFile.Settings, FileExt.Json, Settings);
        }


        #region High Score
        private void LoadHighScore()
        {
            HighScoreList = ReadWrite.ReadJson<List<HighScoreObj>>(PathSettings, DataFile.HighScore, FileExt.Json)
                            ?? new List<HighScoreObj>();

            if (HighScoreList.Count < 6)
            {
                int addToFive = 5 - HighScoreList.Count;
                for (int i = 0; i < addToFive; i++)
                {
                    HighScoreList.Add(new HighScoreObj { DateTime = DateTime.Now, Handle = "Empty", TimeSpan = new TimeSpan(7, 23, 59, 59) });
                }
            }
            HighScoreList = HighScoreList.OrderBy(x => x.TimeSpan).ThenBy(y => y.DateTime).ToList();

          

        }
        public bool PlacesOnHighScore(TimeSpan TS)
        {
            return HighScoreList.Any(x => x.TimeSpan > TS);
        }
        public void PutOnHighScore(HighScoreObj HSO)
        {
            HighScoreList.Add(HSO);
            HighScoreList = HighScoreList.OrderBy(x => x.TimeSpan).ThenBy(y => y.DateTime).Take(5).ToList();
        }
        public void ResetHighScore()
        {
            HighScoreList = new List<HighScoreObj>();
            for (int i = 0; i < 5; i++)
            {
                HighScoreList.Add(new HighScoreObj { DateTime =new DateTime(2020,7,28), Handle = "Empty", TimeSpan = new TimeSpan(7, 23, 59, 59) });
            }
            SaveHighScoreList();
        }
        public bool IsNewFirstPlaceHS(TimeSpan TS)
        {
            if (HighScoreList.FirstOrDefault()!.TimeSpan > TS)
            {
                return true;
            }
            return false;
        }
        public List<HighScoreObj> GetHighScoreList()
        {
            return HighScoreList;
        }
        public bool SaveHighScoreList()
        {
            return ReadWrite.WriteJson<List<HighScoreObj>>(PathSettings, DataFile.HighScore, FileExt.Json, HighScoreList);
        }
        #endregion

        public LevelObj? GetMapData(string name) => MapRepository.Load(name);

        /// <summary>
        /// Returnerar den fullständiga filsökvägen till en laddad sprite.
        /// Används av RaylibRenderContext.RegisterSprite för att ladda sprites motor-agnostiskt.
        /// </summary>
        public string? GetSpritePath(string name) =>
            MapSpritePaths.TryGetValue(name, out var path) ? path : null;

        public Item? GetItem(string name) =>
            MapItems.TryGetValue(name, out var item) ? item : null;

        public Map? GetMap(string name) =>
            MapMaps.TryGetValue(name, out var map) ? map : null;

        public SettingsObj? GetSettings() => Settings;

        public int RNG(int SmallNumber, int BigNumber)
        {
            if (SmallNumber > BigNumber)
                return Random.Next(BigNumber, SmallNumber);

            return Random.Next(SmallNumber, BigNumber);
        }


        #region SwitchX
        public bool IsUnderGround { get; set; } = false;
        public bool IsAboveGround { get; set; } = false;
        public bool IsMoving { get; set; } = false;
        public bool HasBeenOnTheTop { get; set; } = true;

        public bool HasBeenOnTheBottom { get; set; } = false;


        public void CheckSwitchX()
        {
            if (IsUnderGround && HasBeenOnTheTop && !IsMoving)
            {
                IsMoving = false;
                HasBeenOnTheTop = false;
                HasBeenOnTheBottom = true;

                ChangeX();
            }

            if (IsAboveGround && HasBeenOnTheBottom && !IsMoving)
            {
                IsMoving = false;
                HasBeenOnTheTop = true;
                HasBeenOnTheBottom = false;
            }

        }


        public void ChangeX()
        {
            var a = 0;
            var b = 0;
            var c = 0;

            var idx = 0;
            Random rand = new Random();
            do
            {
                idx++;
                a = rand.Next(0, 7);
                b = rand.Next(0, 7);
                c = rand.Next(0, 7);
            } while ((a == b) || (b == c) || (a == c) || (idx > 25));

            if (idx > 25)
            {
                ValueXArray = new int[] { 2, 4, 6 };
            }
            else
            {
                ValueXArray = new int[] { PossibleValueForX[a], PossibleValueForX[b], PossibleValueForX[c] };
            }


        }

        int[] PossibleValueForX = new int[] { 2, 4, 6, 8, 10, 12, 14 };


        int[] ValueXArray = new int[] { 2, 4, 6 };

        public int GetMyX(int id)
        {
            return ValueXArray[id-1];
        }


        #endregion

      

        #region Snow
        public class MakeItSnow
        {
            public List<int[]> arrayList = new List<int[]>();

            public MakeItSnow(int from=1, int to=100, int hight = 224)
            {
                for (int i = 0; i < hight; i++)
                {
                    var rowToAdd = new List<int>();

                    for (int j = 0; j < 254; j++)
                    {
                        var randomNo = Core.Aggregate.Instance.RNG(from, to);
                        if (randomNo < 10)
                        {
                            rowToAdd.Add(j);
                        }
                    }

                    arrayList.Add(rowToAdd.ToArray());
                }
            }
        }
        #endregion

    }


}
