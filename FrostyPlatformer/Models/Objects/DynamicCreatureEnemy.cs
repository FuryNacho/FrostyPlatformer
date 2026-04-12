#nullable enable
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;
using System;

namespace FrostyPlatformer.Models.Objects
{

    public class DynamicCreatureEnemyPenguin : Creature
    {

        public DynamicCreatureEnemyPenguin() : base("enemyone", SpriteId.EnemyPenguin)
        {
            Friendly = false;
            Health = 2;
            MaxHealth = 2;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 4;
        }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Health <= 0)
            {
                vx = 0;
                vy = 0;
                SolidVsDynamic = false;
                IsAttackable = false;
                return;
            }

            if (player != null)
            {
                // För att jaga player. 

                // no default behaviour
                // Check if player is nearby
                float fTargetX = player.px - px;
                float fTargetY = player.py - py;
                float fDistance = (float)Math.Sqrt(fTargetX * fTargetX + fTargetY * fTargetY);

                StateTick -= fElapsedTime;

                if (StateTick <= 0.0f) // för att inte göra beslut så ofta. 
                {
                    if (fDistance < 6.0f)
                    {
                        Ticker = 0;

                        vx = (fTargetX / fDistance) * 2.0f;
                        // vy = (fTargetY / fDistance) * 2.0f;

                        //if (fDistance < 1.5f) // för att attakera med projektil
                        //    PerformAttack();
                    }
                    else
                    {
                        vx = 0;
                        vy = 0;
                        Ticker++;
                    }

                    StateTick += 1.0f;


                    // enkel hantera om idle
                    if (Ticker > 7)
                    {
                        var vändåt = GetFacingDirection();
                        //FacingDirection = Enum.Direction.EAST;

                        if (vändåt == 1)
                        {
                            //vänster Enum.Direction.WEST
                            vx = 0.12f;
                        }
                        else if (vändåt == 3)
                        {
                            // höger Enum.Direction.EAST
                            vx = -0.12f;
                        }

                        Ticker = 0;
                    }

                }
            }

        }
    }


    public class DynamicCreatureEnemyWalrus : Creature
    {

        public DynamicCreatureEnemyWalrus() : base("enemytwo", SpriteId.EnemyWalrus)
        {
            Friendly = false;
            Health = 2;
            MaxHealth = 2;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 4;
        }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Health <= 0)
            {
                vx = 0;
                vy = 0;
                SolidVsDynamic = false;
                IsAttackable = false;
                Patrol = Enum.Actions.Left;
                return;
            }

            if (player != null)
            {
                // patrol (and fear of heights)

                StateTick -= fElapsedTime;

                if (StateTick <= 0.0f) // för att inte göra beslut så ofta. 
                {
                    if (Patrol == Enum.Actions.Left)
                    {
                        vx = -2;
                    }
                    else if (Patrol == Enum.Actions.Right)
                    {
                        vx = 2;
                    }
                    else
                    {
                        vy = 0;
                        vx = 0;

                    }

                    StateTick += 1.0f;
                }
            }

        }

        public override void OnWallCollision(ref float newX, bool turnPatrol, bool movingLeft, IMapData map, float fBorder)
        {
            if (movingLeft)
            {
                bool solid1 = map.GetSolid((int)(newX + 0f), (int)(py + 0f) + 1);
                bool solid2 = map.GetSolid((int)(newX + 0f), (int)(py + 0.9f) + 1);
                if (!solid1 || !solid2 || turnPatrol) { newX += 0.1f; vx = 2; Patrol = Enum.Actions.Right; }
                else Patrol = Enum.Actions.Left;
            }
            else
            {
                bool solid1 = map.GetSolid((int)(newX + (1f - fBorder)), (int)(py + fBorder) + 1);
                bool solid2 = map.GetSolid((int)(newX + (1f - fBorder)), (int)(py + (1f - fBorder) + 1));
                if (!solid1 || !solid2 || turnPatrol) { newX = (int)newX; vx = -2; Patrol = Enum.Actions.Left; }
                else Patrol = Enum.Actions.Right;
            }
        }
    }

    public class DynamicCreatureEnemyFrost : Creature
    {

        public DynamicCreatureEnemyFrost() : base("enemythree", SpriteId.EnemyFrost)
        {
            Friendly = false;
            Health = 2;
            MaxHealth = 2;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 4;



        }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Health <= 0)
            {
                vx = 0;
                vy = 0;
                SolidVsDynamic = false;
                IsAttackable = false;
                Patrol = Enum.Actions.Left;
                return;
            }

            if (player != null)
            {
                // patrol (and fear of heights)

                StateTick -= fElapsedTime;

                if (StateTick <= 0.0f) // för att inte göra beslut så ofta. 
                {
                    if (Patrol == Enum.Actions.Left)
                    {
                        vx = -1;
                    }
                    else if (Patrol == Enum.Actions.Right)
                    {
                        vx = 1;
                    }
                    else
                    {
                        vy = 0;
                        vx = 0;

                    }


                    StateTick += 1.0f;
                }
            }

        }

        public override void OnWallCollision(ref float newX, bool turnPatrol, bool movingLeft, IMapData map, float fBorder)
        {
            Ticker++;

            if (DoSpecialAction)
            {
                bool solid = map.GetSolid((int)(px + 0f), (int)(py + 0f) + 1);
                if (solid && !HasJumped) { vy = GameConstants.EnemyJumpVelocity; HasJumped = true; }
                else if (solid && HasJumped)
                {
                    if (DoneSpecialAction == 1) { Ticker = 0; DoSpecialAction = false; DoneSpecialAction = 0; HasJumped = false; }
                    else DoneSpecialAction = 1;
                }
            }
            if (!DoSpecialAction)
            {
                if (movingLeft)
                {
                    if (turnPatrol) { newX += 0.1f; vx = 1; Patrol = Enum.Actions.Right; }
                    else { Patrol = Enum.Actions.Left; if (Ticker > 8) DoSpecialAction = true; }
                }
                else
                {
                    if (turnPatrol) { newX = (int)newX; vx = -1; Patrol = Enum.Actions.Left; }
                    else { Patrol = Enum.Actions.Right; if (Ticker > 8) DoSpecialAction = true; }
                }
            }
            if (movingLeft)
            {
                if ((int)(newX + 1f) <= FromCor) { newX += 0.1f; vx = 1; Patrol = Enum.Actions.Right; }
            }
            else
            {
                if ((int)newX >= ToCor) { newX = (int)newX; vx = -1; Patrol = Enum.Actions.Left; }
            }
        }

        public override void OnStuckCheck()
        {
            if (PrevTick == 10)      SampleOne   = px;
            else if (PrevTick == 30) SampleTow   = px;
            else if (PrevTick == 50) SampleThree = px;

            if (PrevTick >= 50)
            {
                float c1 = Math.Abs(SampleOne - SampleTow);
                if (c1 <= 0.3f)
                {
                    float c2 = Math.Abs(SampleOne - SampleThree);
                    if (c2 <= 0.3f)
                    {
                        float curr = px;
                        if (Math.Abs(curr - SampleOne) < 5 && Math.Abs(curr - SampleTow) < 5 && Math.Abs(curr - SampleThree) < 5)
                            vy = -2;
                    }
                }
            }
            if (PrevTick >= 60) PrevTick = 0;
            PrevTick++;
        }
    }


    public class DynamicCreatureEnemyIcicle : Creature
    {
        /// <summary>
        /// Isbossen kan inte förstöras av hoppspark — alltid oförstörbar.
        /// </summary>
        public override bool IsIndestructible => true;

        public DynamicCreatureEnemyIcicle() : base("enemyzero", SpriteId.EnemyIcicle)
        {
            Friendly = false;
            Health = 20;
            MaxHealth = 2;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 4;
        }

        public Enum.LastStage State { get; set; }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Health <= 0)
            {
                vx = 0;
                vy = 0;
                SolidVsDynamic = false;
                IsAttackable = false;
                return;
            }

            //Temp, för test i demo endast. Byt ut när boss är bra först

            if (player != null)
            {
                Ticker++;

                StateTick -= fElapsedTime;

                if (StateTick <= 0.0f) // för att inte göra beslut så ofta. 
                {
                    StateTick += 1.0f;

                }


                


                if (Ticker < 60)
                {
                    State = Enum.LastStage.MovingDown;
                    
                }
                else if (Ticker < 120)
                {
                    State = Enum.LastStage.MovingUp;

                    

                }
                else if (Ticker >= 180)
                {
                    Ticker = 0;

                }




                if (py < 12 || py > 13)
                {
                    if (py < 12)
                    {
                        py = 12;
                    }
                    if (py > 13)
                    {
                        py = 13;
                    }
                }

                if (State == Enum.LastStage.MovingDown)
                {
                    if (vy <= 0)
                    {
                    }
                    vy = 2;

                    //if (py != 13)
                    //{

                    //    py = 13;
                    //}

                }
                else if (State == Enum.LastStage.MovingUp)
                {
                    if (vy >= 0)
                    {
                    }
                    vy = -2;
                }



                vx = 0;
                if (py <= FromCor)
                {
                    vy = 0;
                    py = 12;
                }
                if (py >= ToCor)
                {
                    vy = 0;
                    py = 13;
                }

            }
            //end temp

        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            int screenX = (int)((px - ox) * 16.0f);
            int screenY = (int)((py - oy) * 16.0f);
            gfx.DrawPartialSprite(SpriteId, screenX, screenY, 4 * 16, 1 * 16, 16, 16);
        }
    }




    public class DynamicCreatureEnemyBoss : Creature
    {

        public DynamicCreatureEnemyBoss() : base("enemyboss", SpriteId.EnemyBoss)
        //public DynamicCreatureEnemyBoss() : base("enemyboss", Core.Aggregate.Instance.GetSprite("enemyzero"))
        {
            Friendly = false;
            Health = 30;
            MaxHealth = 50;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 10;
        }

        public Enum.LastStage State { get; set; }


        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Health <= 0)
            {
                vx = 0;
                vy = 0;
                SolidVsDynamic = false;
                IsAttackable = false;
                // Patrol = Enum.Actions.Left;
                return;
            }



            if (player != null)
            {
                Ticker++;

                StateTick -= fElapsedTime;





                if (StateTick <= 0.0f) // för att inte göra beslut så ofta. 
                {
                    StateTick += 1.0f;

                }



                //if (!IsAttackable)
                //{
                //    Ticker = 0;
                //}

                //
                //if (State == Enum.LastStage.StayUp && Ticker < 120)
                //{
                //    State = Enum.LastStage.MovingDown;
                //}
                //else if (State == Enum.LastStage.MovingDown && Ticker > 120)
                //{
                //    State = Enum.LastStage.StayDown;
                //}
                //else if (State == Enum.LastStage.StayDown && Ticker > 150)
                //{
                //    State = Enum.LastStage.MovingUp;
                //}
                //else if (State == Enum.LastStage.MovingUp && Ticker > 270)
                //{
                //    State = Enum.LastStage.StayUp;
                //}
                //else if (Ticker >= 300)
                //{
                //    Ticker = 0;
                //}
                if (Ticker < 60)
                {
                    State = Enum.LastStage.MovingDown;
                }
                else if (Ticker < 120)
                {
                    State = Enum.LastStage.MovingUp;

                    if (Ticker < 118)
                    {
                        TurnedTo = Enum.PlayerOrientation.Left;
                    }

                }else if (Ticker < 160)
                {
                    TurnedTo = Enum.PlayerOrientation.Right;
                }
                else if (Ticker >= 180)
                {
                    Ticker = 0;
                }



               
                //



                if (py < 12 || py > 13)
                {
                    if (py < 12)
                    {
                        py = 12;
                    }
                    if (py > 13)
                    {
                        py = 13;
                    }
                }

                if (State == Enum.LastStage.MovingDown)
                {
                    if (vy <= 0)
                    {
                    }
                    vy = 2;

                    //if (py != 13)
                    //{
                    //    //py = py + 0.20f;
                    //    py = 13;
                    //}

                }
                else if (State == Enum.LastStage.MovingUp)
                {
                    if (vy >= 0)
                    {
                    }
                    vy = -2;
                }



                vx = 0;
                if (py <= FromCor)
                {
                    vy = 0;
                    py = 12;
                }
                if (py >= ToCor)
                {
                    vy = 0;
                    py = 13;
                }


                //reset IsAttackable
                if (!IsAttackable && py == 13)
                {
                    IsAttackable = true;
                }


                // Switch X
                if (py == 13.0f)
                {
                    Core.Aggregate.Instance.IsUnderGround = true;
                    Core.Aggregate.Instance.IsMoving = false;
                }
                else if (py == 12.0f)
                {
                    Core.Aggregate.Instance.IsAboveGround = true;
                    Core.Aggregate.Instance.IsMoving = false;
                }
                else
                {
                    Core.Aggregate.Instance.IsMoving = true;
                    Core.Aggregate.Instance.IsUnderGround = false;
                    Core.Aggregate.Instance.IsAboveGround = false;
                }
               



                //if (State == Enum.LastStage.StayUp)
                //{
                //    py = 12.0f;
                //}
                //else if (State == Enum.LastStage.MovingUp)
                //{
                //    if (py > 12.0f)
                //    {
                //        py = py - 0.025f;
                //    }
                //    else
                //    {
                //        py = 12.0f;
                //    }
                //}
                //else if (State == Enum.LastStage.StayDown)
                //{
                //    py = 13.0f;
                //}
                //else if (State == Enum.LastStage.MovingDown)
                //{
                //    if (py < 13.0f)
                //    {
                //        py = py + 0.025f;
                //    }
                //    else
                //    {
                //        py = 13.0f;
                //    }
                //}






            }

        }

        #region DrawSelf



        //public override void DrawSelf(Program gfx, float ox, float oy)  // gfx  = graphics //  olcConscoleGameEngineOOP
        //{
        //    int spriteSize = 32;
        //    // Måste draw rätt sprite som passar state som creature är i, in this point in time

        //    // Mosvarar vart på spriten som ska ritas. 

        //    int SheetOffsetX = 0; //Uppe till vänster är sheet offset 0. (noll index)
        //    int SheetOffsetY = 0;// Om y är 1 så är det en rad ner (noll index)

        //    switch (sprGraphicsState)
        //    {
        //        /*
        //         * längst ner i högra hörnet (5x5)
        //           SheetOffsetY = 4 * 16; (Vertikalen)
        //           SheetOffsetX = 4 * 16; (Horizontalen)
        //         */
        //        /*
        //         Right = 0,
        //         Left = 1
        //         */

        //        case Enum.GraphicsState.Standing:

        //            if (IsIdle)
        //            {

        //                //var asdf = this is DynamicCreatureHero;
        //                //if (asdf) // IsHero
        //                //{
        //                //    var qwer = true;
        //                //}

        //                int oscillera = (int)TurnedTo == 0 ? 0 : 2;
        //                if (GraphicCounter % 2 == 0)
        //                {
        //                    SheetOffsetY = 4 * spriteSize;
        //                    SheetOffsetX = (0 + oscillera) * spriteSize;
        //                }
        //                else
        //                {
        //                    SheetOffsetY = 4 * spriteSize;
        //                    SheetOffsetX = (1 + oscillera) * spriteSize;
        //                }

        //                break;
        //            }

        //            //if (IsHero) // IsHero
        //            //{
        //            //    var asdf = this as DynamicCreatureHero;
        //            //    if (asdf.LookDown)
        //            //    {
        //            //        // rad tre för höger
        //            //        // rad fyra för vänster
        //            //        SheetOffsetY = (int)(TurnedTo + 2) * 16;
        //            //        // kolumn fyra för titta ner
        //            //        SheetOffsetX = 48;
        //            //        break;
        //            //    }

        //            //    if (asdf.LookUp)
        //            //    {
        //            //        // rad tre för höger
        //            //        // rad fyra för vänster
        //            //        SheetOffsetY = (int)(TurnedTo + 2) * 16;
        //            //        // tredje komlumn för titta upp
        //            //        SheetOffsetX = 32;
        //            //        break;
        //            //    }
        //            //}


        //            SheetOffsetX = 0;
        //            SheetOffsetY = (int)TurnedTo * 16;
        //            break;

        //        case Enum.GraphicsState.Walking:
        //            //SheetOffsetX = (int)FacingDirection * 16; // så typ den övre raden är åt vilket håll, sen switcha mellan övre raden och undre raden i hans sprite.
        //            //SheetOffsetY = (int)FacingDirection * 16;
        //            SheetOffsetX = GraphicCounter * 16;
        //            SheetOffsetY = (int)TurnedTo * 16;
        //            break;

        //        case Enum.GraphicsState.Jumping:

        //            SheetOffsetX = 0 * 16;
        //            SheetOffsetY = ((int)TurnedTo + 2) * 16;

        //            break;

        //        case Enum.GraphicsState.Falling:

        //            SheetOffsetX = 1 * 16;
        //            SheetOffsetY = ((int)TurnedTo + 2) * 16;

        //            break;

        //        case Enum.GraphicsState.Celebrating:
        //            SheetOffsetX = 4 * 16;
        //            SheetOffsetY = 4 * 16;
        //            break;

        //        case Enum.GraphicsState.Dead:

        //            SheetOffsetX = GraphicCounter * 16;
        //            SheetOffsetY = 4 * 16;
        //            break;

        //        case Enum.GraphicsState.TakingDamage:

        //            SheetOffsetY = (int)(TurnedTo + 2) * 16;
        //            SheetOffsetX = 16 * 4;

        //            break;
        //    }


        //    //Sen är det dags att rita ut spriten
        //    // dynamiska objektet finns i world space, men måste rita den i screen space. 1 - 1 translation eftersom alla enheter är en / en enheter.
        //    //Vi måste bara ta reda på vart kameran titar i world space.
        //    var firstMagicalPlayerParam = new Point();
        //    if (IsHero)
        //    {
        //        var notGoOfTheWorldLeft = 0.0f;
        //        notGoOfTheWorldLeft = (px - ox) * 16.0f;
        //        if (notGoOfTheWorldLeft <= 1.0f)
        //        {
        //            notGoOfTheWorldLeft = 1.0f;
        //        }
        //        firstMagicalPlayerParam = new Point((int)notGoOfTheWorldLeft, (int)((py - oy) * 16.0f));
        //    }
        //    else
        //    {
        //        //  firstMagicalPlayerParam = new Point((int)((px - ox) * 16.0f), (int)((py - oy) * 16.0f)); // Vart tilen ska ritas.
        //        firstMagicalPlayerParam = new Point((int)((px - ox) * 16.0f), (int)((py - oy) * 16.0f));
        //    }

        //    // SheetOffsetX och SheetOffsetY ger top left in en sprite
        //    var secondMagicalPlayerParam = new Point(SheetOffsetX, SheetOffsetY); // Vilken tile i spritesheeten som ska ritas.

        //    // 16 är för närvarande en full enhet 
        //    gfx.DrawPartialSprite(firstMagicalPlayerParam, Sprite!, secondMagicalPlayerParam, spriteSize, spriteSize);

        //}
        #endregion

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            int sheetOffsetX = 0;
            int sheetOffsetY = 0;

            if (!IsAttackable)
            {
                sheetOffsetY = 1 * 16;
                sheetOffsetX = 0 * 16;
            }
            else if (State == Enum.LastStage.MovingUp)
            {
                if (py <= 12)
                {
                    // "idle" — vrid på huvudet
                    if (TurnedTo == Enum.PlayerOrientation.Left)
                    {
                        sheetOffsetY = 0 * 16;
                        sheetOffsetX = 4 * 16;
                    }
                    else // Right
                    {
                   