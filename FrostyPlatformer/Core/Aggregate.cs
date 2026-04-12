#nullable enable
using PixelEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Commands;
using Audio.Library;

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
        private Dictionary<string, Sprite> MapSprites { get; set; } = new Dictionary<string, Sprite>();
        private Dictionary<string, string> MapSpritePaths { get; set; } = new Dictionary<string, string>();
        private Dictionary<string, Map> MapMaps { get; set; } = new Dictionary<string, Map>();
        private Dictionary<string, Item> MapItems { get; set; } = new Dictionary<string, Item>();
        private FrostyPlatformer.Systems.IMapRepository MapRepository { get; set; } = null!;
        private string PathSprites => @"\Resources\Assets\Sprites";
        private string PathSettings => @"\Resources\Settings";
        private string PathSound  => @"\Resources\Assets\Sound";
        /// <summary>Sätts i Load() — null! före dess (garanterat initierat via Load).</summary>
        public ScriptProcessor Script { get; set; } = null!;
        private Random Random { get; set; } = new Random();

        /// <summary>null om inställningsfilen saknas vid start — kontrollera före användning.</summary>
        public SettingsObj? Settings { get; set; } = new SettingsObj();
        private List<HighScoreObj> HighScoreList { get; set; } = null!;

        /// <summary>null om ljudsystemet inte kunde laddas — alla anropare kontrollerar redan null.</summary>
        public Audio.Library.Sound? Sound { get; private set; }

        internal void Load(Program game)
        {
            ThisGame = game;
            ReadWrite = new ReadWrite(EnableWriteToLog);
            LoadSettings();

            MapRepository = new FrostyPlatformer.Systems.MapRepository(ReadWrite);
            LoadSprites();
            LoadMaps();
            LoadItems();
            Script = new ScriptProcessor();


            LoadHighScore();

            if (Settings != null && !Settings.Mute)
                LoadSound();

        }

        public void LoadSound()
        {
            try
            {
                Sound = Audio.Library.Sound.Instance;
                //var initSuccess = Sound.init();
                //Sound.loadSound("uno.wav");

                //Sound.loopSound("uno.wav");
                //Sound.loadSound("Click.wav");

                //Sound.loadSound("puttekong.wav");
                //Sound.loopSound("Click.wav");
                //Sound.play("Piano.wav");

                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.Jump); 
                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.Land); 
                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.Damage);
                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.DamageHero);
                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.PickUp);

                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundWorld);
                Sound.loopSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundWorld);

                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundGame);
                Sound.loopSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundGame);

                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
                Sound.loopSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundFinalStage);

                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundEnd);
                Sound.loopSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGSoundEnd);

                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGNearPerfectEnd);
                Sound.loopSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGNearPerfectEnd); 

                Sound.loadSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGPerfectEnd);
                Sound.loopSound(FrostyPlatformer.Global.GlobalNamespace.SoundRef.BGPerfectEnd);
                // pefect end?
                // just done?


                if (Settings != null)
                    if (Settings.AudioOn)
                    {
                       Sound.unMute();
                    }
                    else if(!Settings.AudioOn)
                    {
                        Sound.mute();
                    }

            }
            catch (Exception ex)
            {

                ReadWrite.WriteToLog(String.Format("LoadSound - Could not load sound. {0}", ex.ToString()));
                Sound = null;
            }
        }

        private void LoadSprites()
        {
            LoadSprite("tilesheetspring", PathSprites, @"\tilesheetspring", ".png");
            LoadSprite("tilesheetsummer", PathSprites, @"\tilesheetsummer", ".png");
            LoadSprite("tilesheetfall", PathSprites, @"\tilesheetfall", ".png");
            LoadSprite("tilesheetwinter", PathSprites, @"\tilesheetwinter", ".png");


            LoadSprite("tilesheetwm", PathSprites, @"\tilesheetwm", ".png"); // tile sheet
            //LoadSprite("tilesheetone", PathSprites, @"\tilesheetone", ".png"); // tile sheet
            //LoadSprite("tilesheettwo", PathSprites, @"\tilesheettwo", ".png"); // tile sheet
            LoadSprite("font", PathSprites, @"\font", ".png"); // font
            LoadSprite("hero", PathSprites, @"\hero", ".png"); // hero
            /*LoadSprite("energi", PathSprites, @"\energi", ".png");*/ // energi
            LoadSprite("items", PathSprites, @"\items", ".png");

            // enemy
            LoadSprite("enemyzero", PathSprites, @"\enemyzero", ".png");
            LoadSprite("enemyone", PathSprites, @"\enemyone", ".png");
            LoadSprite("enemytwo", PathSprites, @"\enemytwo", ".png");
            LoadSprite("enemythree", PathSprites, @"\enemythree", ".png");
            LoadSprite("enemyboss", PathSprites, @"\enemyboss", ".png");

            //LoadSprite("cord", PathSprites, @"\cord", ".bmp"); // tile sheet that is coordinates


            LoadSprite("splashstart", PathSprites, @"\splashstart", ".png");
            LoadSprite("splashend", PathSprites, @"\splashend", ".png");
            //LoadSprite("splashaltend", PathSprites, @"\splashaltend", ".png");
            //LoadSprite("splashsuperaltend", PathSprites, @"\splashsuperaltend", ".png");
            LoadSprite("endart", PathSprites, @"\endart", ".png");

        }

        private void LoadSprite(string FriendlyName, string FilePath, string FileName, string FileExtension)
        {
            // make sure resource exists
            var fullDirectory = ReadWrite.CreateIfNotExists(FilePath, FileName, FileExtension, false);
            if (!string.IsNullOrEmpty(fullDirectory))
            {
                var sprite = Sprite.Load(fullDirectory);
                MapSprites.Add(FriendlyName, sprite);
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
            MapMaps.Add("worldmap", wm);

            var lvl1 = new MapOne(this, enemyFactory, itemFactory);
            MapMaps.Add("mapone", lvl1);
            var lvl2 = new MapTwo(this, enemyFactory, itemFactory);
            MapMaps.Add("maptwo", lvl2);
            var lvl3 = new MapThree(this, enemyFactory, itemFactory);
            MapMaps.Add("mapthree", lvl3);
            var lvl4 = new MapFour(this, enemyFactory, itemFactory);
            MapMaps.Add("mapfour", lvl4);
            var lvl5 = new MapFive(this, enemyFactory, itemFactory);
            MapMaps.Add("mapfive", lvl5);
            var lvl6 = new MapSix(this, enemyFactory, itemFactory);
            MapMaps.Add("mapsix", lvl6);
            var lvl7 = new MapSeven(this, enemyFactory, itemFactory);
            MapMaps.Add("mapseven", lvl7);
            var lvl8 = new MapEight(this, enemyFactory, itemFactory);
            MapMaps.Add("mapeight", lvl8);

            var lvl9 = new MapNine(this, enemyFactory, itemFactory);
            MapMaps.Add("mapnine", lvl9);
        }

        private void LoadItems()
        {
            var e = new ItemEnergi();
            MapItems.Add("energi", e);
        }

        private void LoadSettings()
        {
            Settings = ReadWrite.ReadJson<SettingsObj>(PathSettings, @"\settings", ".json")
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
            return ReadWrite.WriteJson<SettingsObj>(PathSettings, @"\settings", ".json", Settings);
        }


        #region High Score
        private void LoadHighScore()
        {
            HighScoreList = ReadWrite.ReadJson <List<HighScoreObj>> (PathSettings, @"\highscore", ".json");

            //High Score
            if (HighScoreList == null)
                HighScoreList = new List<HighScoreObj>();

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
            if (HighScoreList.FirstOrDefault().TimeSpan > TS)
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
            return ReadWrite.WriteJson<List<HighScoreObj>>(PathSettings, @"\highscore", ".json", HighScoreList);
        }
        #endregion

        public LevelObj? GetMapData(string name) => MapRepository.Load(name);

        /// <summary>
        /// Returnerar den fullständiga filsökvägen till en laddad sprite.
        /// Används av PixelEngineRenderContext.RegisterSprite för att ladda sprites motor-agnostiskt.
        /// </summary>
        public string? GetSpritePath(string name) =>
            MapSpritePaths.TryGetValue(name, out var path) ? path : null;

        /// <summary>
        /// Hämtar en redan laddad PixelEngine.Sprite vid namn.
        /// Används internt av Items.cs — anropas via Aggregate.Instance direkt.
        /// </summary>
        public Sprite? GetSprite(string name) =>
            MapSprites.TryGetValue(name, out var sprite) ? sprite : null;

        public Item? GetItem(string name) =>
            MapItems.TryGetValue(name, out var item) ? item : null;

        public Map? GetMap(string name) =>
            MapMaps.TryGetValue(name, out var map) ? map : null;

        public SettingsObj? GetSettings() => Settings;

        public bool SaveSettings(SettingsObj settingsObj)
        {

            this.Settings = settingsObj;
            return true;

            // TODO; spara settings
            // return ReadWrite.WriteJson<SettingsObj>(PathSettings, @"\settings", ".json", settingsObj);


        }

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
            // är under jorden
            if (IsUnderGround && HasBeenOnTheTop && !IsMoving)
            {
                IsMoving = false;
                HasBeenOnTheTop = false;
                HasBeenOnTheBottom = true;

                ChangeX();
            }

            //nollställ för att kunna switcha igen
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

            //var d = 0;
            //var e = 0;

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
