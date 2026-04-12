#nullable enable
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
  
    public class DynamicGameObject
    {
        public DynamicGameObject() { }
        public DynamicGameObject(string name)
        {
            this.Name = name;
            px = 0.0f;
            py = 0.0f;
            vx = 0.0f;
            vy = 0.0f;
            SolidVsDynamic = true;
            SolidVsMap = true;
            Friendly = true;
            Redundant = false;
            IsAttackable = false;
            IsTempEnergi = false;
            TurnedTo = Enum.PlayerOrientation.Right;
        }

        /// <summary>
        /// Position X - x:horisontal
        /// </summary>
        public float px { get; set; }
        /// <summary>
        /// Position Y - y:vertikal
        /// </summary>
        public float py { get; set; }
        /// <summary>
        /// Velocity  X - x:horisontal
        /// </summary>
        public float vx { get; set; }
        /// <summary>
        /// Velocity Y - y:vertikal
        /// </summary>
        public float vy { get; set; }

        /// <summary>
        /// Är solid mot kartan
        /// </summary>
        public bool SolidVsMap { get; set; }
        /// <summary>
        /// Är solid mot andra objekt
        /// </summary>
        public bool SolidVsDynamic { get; set; }
        /// <summary>
        /// Är vänlig
        /// </summary>
        public bool Friendly { get; set; }
        /// <summary>
        /// Namnet på dynamiskt objekt
        /// </summary>
        public string? Name { get; set; }
        public int Id { get; set; }

        public int CoinId { get; set; }

        public int Attr { get; set; }

        public bool Redundant { get; set; }

        public bool IsAttackable { get; set; }
        


        //public bool SwitchX { get; set; }

        public bool IsTempEnergi { get; set; }

        public bool IsHero { get; set; } = false;
        public bool detHarBallatUr { get; set; }
        public bool Grounded { get; set; }

        public bool IsIdle { get; set; }
        public int IdleCounter { get; set; }

        /// <summary>
        /// går till och med denna (vänster)
        /// </summary>
        public int FromCor { get; set; }
        /// <summary>
        /// går till och med denna (höger)
        /// </summary>
        public int ToCor { get; set; }

        public int PrevTick { get; set; }
        

        public float SampleOne { get; set; }
        public float SampleTow { get; set; }
        public float SampleThree { get; set; }


        public Enum.PlayerOrientation TurnedTo { get; set; }

        public Enum.Actions Patrol { get; set; }

        public Enum.StageStatus StageStatus { get; set; }

        public bool Controllable { get; set; } = true;
        protected float KnockBackTimer = 0.0f;
        protected float KnockBackDX = 0.0f;
        protected float KnockBackDY = 0.0f;

        public int RemoveCount { get; set; }

        public static Program? Engine { get; set; }

        /// <summary>
        /// Ansvarar själv för hur den ser ut på skärmen.  Tar en instans av olcGameEnigne
        /// ox oy offset som typ betyder kamera
        /// </summary>
        /// <summary>
        /// Sant om objektet inte kan förstöras av hoppspark.
        /// Åsidosätts i subklasser som DynamicCreatureEnemyIcicle.
        /// </summary>
        public virtual bool IsIndestructible => false;

        public virtual void DrawSelf(IRenderContext graphics, float ox, float oy) { }

        /// <summary>
        /// Elapsed time
        /// </summ