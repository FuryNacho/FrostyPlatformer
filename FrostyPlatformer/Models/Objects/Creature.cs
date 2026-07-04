#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    public abstract class Creature : DynamicGameObject
    {
        public Creature(string name, SpriteId spriteId)
            : base(name)
        {
            SpriteId = spriteId;
            Health = 5;
            MaxHealth = 10;
            FacingDirection = Enum.Direction.EAST;
            sprGraphicsState = Enum.GraphicsState.Falling;
            Timer = 0.0f;
            GraphicCounter = 0;
            IsAttackable = true;
        }

        //States
        private Enum.Direction FacingDirection { get; set; }
        protected Enum.GraphicsState sprGraphicsState { get; set; }
        protected int GraphicCounter { get; set; }
        private float Timer { get; set; }
        protected SpriteId SpriteId { get; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int DamageGiven { get; set; } = 0;

        public int Ticker { get; set; } = 0;
        public bool DoSpecialAction { get; set; } = false;
        public bool HasJumped { get; set; } = false;
        public int DoneSpecialAction { get; set; } = 0;

       

        public override void Update(float elapsedTime, DynamicGameObject? player = null)
        {
            if (KnockBackTimer > 0.0f)
            {
                // Flyttas till vänster eller höger, sätt hjälten till motsatt riktning

                if (vx < 0)
                {
                    TurnedTo = Enum.PlayerOrientation.Right;
                }
                else
                {
                    TurnedTo = Enum.PlayerOrientation.Left;

                }

                vx = KnockBackDX * 3.0f;
                vy = KnockBackDY * 2.0f;
                IsAttackable = false;
                KnockBackTimer -= elapsedTime;
                sprGraphicsState = Enum.GraphicsState.TakingDamage;

                if (KnockBackTimer <= 0.0f)
                {
                    StateTick = 0.0f;
                    Controllable = true;
                    IsAttackable = true;
                    // KnockBack() nollar SolidVsDynamic; återställ den här tillsammans med de andra
                    // knockback-flaggorna. Utan detta blev knuffade fiender permanenta "spöken" som
                    // hjälten går rakt igenom (märkbart i akt 2-svärmen). Health > 0 → döda förblir
                    // icke-solida (deras egen Behaviour/borttagning styr då).
                    SolidVsDynamic = Health > 0;
                }
            }
            else
            {
                //if (IsHero)
                //{


                // En bubbla av tid som förflyttar sig oberoende av resten av spelets state. Skapar ett slags pendel, gissar jag på.
                Timer += elapsedTime;
                if (Timer <= 0.1f)
                {
                    //GraphicCounter = 0;
                    GraphicCounter = 1;

                }
                else if (Timer <= 0.2f)
                {
                    //GraphicCounter = 1;
                    GraphicCounter = 2;

                }
                else if (Timer <= 0.3f)
                {
                    //GraphicCounter = 2;
                    GraphicCounter = 3;

                }
                //else if (Timer <= 0.4f)
                //{
                //    //GraphicCounter = 3;
                //    GraphicCounter = 4;

                //}
                //else if (Timer <= 0.5f)
                //{
                //    GraphicCounter = 4;
                //    Timer = 0.0f;
                //}
                else
                {
                    GraphicCounter = 4;
                    Timer = 0.0f;

                }
                //Timer += elapsedTime;
                //if (Timer >= 0.2f)
                //{
                //    Timer -= 0.2f;

                    //    // tänkt att "ocilera" (vad fan det nu är på svenska). ticka mellan 0 och 1
                    //    GraphicCounter++;
                    //    GraphicCounter %= 2;
                    //}


                    //   }


                if (Math.Abs(vy) > 0 && !IsIdle)
                {
                    if (vy < 0)
                    {
                        sprGraphicsState = Enum.GraphicsState.Jumping;
                    }
                    else
                    {
                        sprGraphicsState = Enum.GraphicsState.Falling;
                    }

                    
                }
                else if (Math.Abs(vx) > 1.0f )
                {
                    sprGraphicsState = Enum.GraphicsState.Walking;
                }
                else
                {
                    sprGraphicsState = Enum.GraphicsState.Standing;
                }

                if (Health <= 0)
                {
                    sprGraphicsState = Enum.GraphicsState.Dead;
                }


                if (vx < -0.1f)
                {
                    FacingDirection = Enum.Direction.WEST;
                    TurnedTo = Enum.PlayerOrientation.Left;
                }
                if (vx > 0.1f)
                {
                    FacingDirection = Enum.Direction.EAST;
                    TurnedTo = Enum.PlayerOrientation.Right;
                }
                if (vy < -0.1f)
                {
                    FacingDirection = Enum.Direction.NORTH;
                }
                if (vy > 0.1f)
                {
                    FacingDirection = Enum.Direction.SOUTH;
                }

                Behaviour(elapsedTime, player);

            }
        }


        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            int SheetOffsetX = 0;
            int SheetOffsetY = 0;

            switch (sprGraphicsState)
                {
                    /*
                     * längst ner i högra hörnet (5x5)
                       SheetOffsetY = 4 * 16; (Vertikalen)
                       SheetOffsetX = 4 * 16; (Horizontalen)
                     */
                    /*
                     Right = 0,
                     Left = 1
                     */

                    case Enum.GraphicsState.Standing:

                        if (IsIdle)
                        {

                            //var asdf = this is DynamicCreatureHero;
                            //if (asdf) // IsHero
                            //{
                            //    var qwer = true;
                            //}

                            // Idle-flapp: Timer cyklar 0→0.4s. Dela halvt/halvt → 2.5 flap/sek.
                            // Tidigare GraphicCounter % 2 gav 10 Hz (var 0.1s) — för snabbt och glitchigt.
                            int oscillera = (int)TurnedTo == 0 ? 0 : 2;
                            if (Timer < 0.2f)
                            {
                                SheetOffsetY = 4 * 16;
                                SheetOffsetX = (0 + oscillera) * 16;
                            }
                            else
                            {
                                SheetOffsetY = 4 * 16;
                                SheetOffsetX = (1 + oscillera) * 16;
                            }

                            break;
                        }

                        if (IsHero) // IsHero
                        {
                            var asdf = (this as DynamicCreatureHero)!; // guarded av IsHero
                            if (asdf.LookDown)
                            {
                                // rad tre för höger
                                // rad fyra för vänster
                                SheetOffsetY = (int)(TurnedTo + 2) * 16;
                                // kolumn fyra för titta ner
                                SheetOffsetX = 48;
                                break;
                            }

                            if (asdf.LookUp)
                            {
                                // rad tre för höger
                                // rad fyra för vänster
                                SheetOffsetY = (int)(TurnedTo + 2) * 16;
                                // tredje komlumn för titta upp
                                SheetOffsetX = 32;
                                break;
                            }
                        }


                        SheetOffsetX = 0;
                        SheetOffsetY = (int)TurnedTo * 16;
                        break;

                    case Enum.GraphicsState.Walking:
                        //SheetOffsetX = (int)FacingDirection * 16; // så typ den övre raden är åt vilket håll, sen switcha mellan övre raden och undre raden i hans sprite.
                        //SheetOffsetY = (int)FacingDirection * 16;
                        SheetOffsetX = GraphicCounter * 16;
                        SheetOffsetY = (int)TurnedTo * 16;
                        break;

                    case Enum.GraphicsState.Jumping:

                        SheetOffsetX = 0 * 16;
                        SheetOffsetY = ((int)TurnedTo + 2) * 16;

                        break;

                    case Enum.GraphicsState.Falling:

                        SheetOffsetX = 1 * 16;
                        SheetOffsetY = ((int)TurnedTo + 2) * 16;

                        break;

                    case Enum.GraphicsState.Celebrating:
                        SheetOffsetX = 4 * 16;
                        SheetOffsetY = 4 * 16;
                        break;

                    case Enum.GraphicsState.Dead:

                        SheetOffsetX = GraphicCounter * 16;
                        SheetOffsetY = 4 * 16;
                        break;

                    case Enum.GraphicsState.TakingDamage:

                        SheetOffsetY = (int)(TurnedTo + 2) * 16;
                        SheetOffsetX = 16 * 4;

                        break;
            }

            int screenX;
            int screenY = ToPixel(py, oy);

            if (IsHero)
            {
                // Kläm till minimum 1 px — förhindrar vänsterkants-clipping av hjälte-spriten.
                float rawX = (px - ox) * GameConstants.TileSize;
                screenX = (int)Math.Round(MathF.Max(1.0f, rawX), MidpointRounding.AwayFromZero);
            }
            else
            {
                screenX = ToPixel(px, ox);
            }

            gfx.DrawPartialSprite(SpriteId, screenX, screenY, SheetOffsetX, SheetOffsetY, 16, 16);

        }

        /// <summary>
        /// Definierar kreaturets autonoma beteende varje tick.
        /// Varje konkret subklass måste implementera detta — tom implementation
        /// är tillåten för passiva objekt (overlay, hero-styrning via Program.cs).
        /// </summary>
        public abstract void Behaviour(float fElapsedTime, DynamicGameObject? player = null);

        public virtual void PerformAttack() { }

        public virtual void KnockBack(float dx, float dy, float dist)
        {
            KnockBackDX = dx;
            KnockBackDY = dy;
            KnockBackTimer = dist;
            SolidVsDynamic = false;
            Controllable = false;
            IsAttackable = false;
        }

        public virtual void ChangeStageKnockBackReset()
        {
            KnockBackDX = 0;
            KnockBackDY = 0;
            KnockBackTimer = 0;
            SolidVsDynamic = true;
            Controllable = true;
            IsAttackable = true;
        }

        public int GetFacingDirection() => (int)FacingDirection;

        public float StateTick { get; set; }

    }

}
