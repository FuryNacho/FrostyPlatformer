#nullable enable
using System;
using System.Collections.Generic;

namespace FrostyPlatformer.Models
{
    /// <summary>
    /// Representerar ett placerat objekt på kartan (mål, fiende, pickup).
    /// Serialiseras till Tiled JSON Objects-lagret via TiledMapRepository.
    /// </summary>
    public class PlacedObject
    {
        /// <summary>Objektkategori: "Goal", "Pickup" eller "Enemy".</summary>
        public string ObjectType { get; set; } = "";

        /// <summary>Specifik typ inom kategorin, t.ex. "Penguin" för Enemy.</summary>
        public string SubType    { get; set; } = "";

        /// <summary>Horisontell tile-koordinat.</summary>
        public int    TileX      { get; set; }

        /// <summary>Vertikal tile-koordinat.</summary>
        public int    TileY      { get; set; }
    }


    public class CreateObj
    {
        public string   fileData        { get; set; } = "";
        public string?  spritePath      { get; set; }
        public string   name            { get; set; } = "";
        public string   fileDataJsonPath { get; set; } = "";
        public LevelObj? levelObj       { get; set; }
    }

    public class LevelObj
    {
        public int[] TileIndex      { get; set; } = System.Array.Empty<int>();
        public int[] AttributeIndex { get; set; } = System.Array.Empty<int>();
        public int   Width          { get; set; }
        public int   Height         { get; set; }

        /// <summary>Hjältens startposition X i tile-koordinater. -1 = ej satt.</summary>
        public int SpawnX { get; set; } = -1;

        /// <summary>Hjältens startposition Y i tile-koordinater. -1 = ej satt.</summary>
        public int SpawnY { get; set; } = -1;

        /// <summary>True om en spawn-punkt är satt (SpawnX och SpawnY är giltiga).</summary>
        public bool HasSpawn => SpawnX >= 0 && SpawnY >= 0;

        /// <summary>Relativ sökväg till tileset-filen (.tsx) som refereras i Tiled JSON.</summary>
        public string TilesetSource { get; set; } = "tilesheetspring.tsx";

        /// <summary>Placerade objekt (mål, fiender, pickups) i kartan.</summary>
        public List<PlacedObject> Objects { get; set; } = new List<PlacedObject>();

        /// <summary>True om kartan har ett mål-/portal-objekt.</summary>
        public bool HasGoal => Objects.Exists(o => o.ObjectType == "Goal");
    }


    public class SettingsObj
    {
        //public int StageCompleted { get; set; }

        //public int SpawnAtWorldMap { get; set; } = 1;

        public SaveSlot ActivePlayer { get; set; } = new SaveSlot();

        public bool AudioOn { get; set; } = true;

        public SaveSlotMainObj SaveSlotsObjs { get; set; } = new SaveSlotMainObj();

        //public bool ShowEnd { get; set; }
        public bool Log { get; set; } = false;
        public bool Mute { get; set; } = false;
    }

    public class SaveSlotMainObj
    {
        //public SaveSlot SlotOne { get; set; } = new SaveSlot() {Name = "Slot One" };
        //public SaveSlot SlotTwo { get; set; } = new SaveSlot() { Name = "Slot Two" };
        //public SaveSlot SlotThree { get; set; } = new SaveSlot() { Name = "Slot Three" };
        public SaveSlot SlotOne { get; set; } = new SaveSlot();
        public SaveSlot SlotTwo { get; set; } = new SaveSlot();
        public SaveSlot SlotThree { get; set; } = new SaveSlot();
    }
    public class SaveSlot
    {
        public DateTime DateTime { get; set; } = DateTime.Now;
        //public string Name { get; set; }
        public TimeSpan Time { get; set; }
        public bool IsUsed { get; set; } = false;
        public int HeroEnergi { get; set; } = 7;


        public int StageCompleted { get; set; } = 0;
        public int SpawnAtWorldMap { get; set; } = 1;
        public bool ShowEnd { get; set; } = false;

        //public float PosX { get; set; }
        //public float PosY { get; set; }
        public List<int> EnergiCollected { get; set; } = new List<int>();

    }

    /// <summary>Bästa genomspelning av ett user map-slot.</summary>
    public class UserMapScore
    {
        public string   SlotId   { get; set; } = "";
        public string   Handle   { get; set; } = "";
        public TimeSpan BestTime { get; set; }
        public DateTime SetAt    { get; set; }
    }

    public class HighScoreObj
    {
        public string Handle { get; set; } = "";
        public TimeSpan TimeSpan { get; set; }
        public DateTime DateTime { get; set; }


        public int Percent { get; set; }

    }

    public class HighScoreEnterName
    {
        public string letter      = "";
        public string MyProperty  { get; set; } = "";
    }

    public class OptionsObj
    {
        public string Display { get; set; } = "";
        public bool OptionIsBack { get; set; }

        public bool OptionIsSlotOne { get; set; }
        public bool OptionIsSlotTwo { get; set; }
        public bool OptionIsSlotThree { get; set; }

        public bool SlotIsUsed { get; set; }
    }


    public class KonamiObj
    {
        public void nope()
        {
            up = upUp = down = downDown = left = right = leftLeft = rightRight = AB = A = B = false;
        }

        public bool up { get; set; }
        public bool upUp { get; set; }
        public bool down { get; set; }
        public bool downDown { get; set; }
        public bool left { get; set; }
        public bool right { get; set; }
        public bool leftLeft { get; set; }
        public bool rightRight { get; set; }
        public bool AB { get; set; }

        public bool A { get; set; }

        public bool B { get; set; }

    }


    public class EnergiRainObject
    {
        public bool  MakeItRain       { get; set; }
        public int   RemainingToSpawn { get; set; }
        public float StartPosX        { get; set; }
        public float StartPosY        { get; set; }
    }


}
