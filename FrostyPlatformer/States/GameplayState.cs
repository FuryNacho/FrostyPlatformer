#nullable enable
using System;
using System.Linq;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Items;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar gameplay-fasen — fysik, kollision, fiende-AI, kamerarendering och HUD
    /// för alla spel-banor (mapone–mapnine).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayStage (ca 1120 rader). Isolerar alla gameplay-
    /// specifika fält (BPower, jumpMemory, EnergiRain, enemyJump m.fl.) som tidigare
    /// låg som lösa public-fält i Program.cs.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från WorldMapState när spelaren väljer en bana. Övergår till
    /// GameOverState om Hero.Health &lt; 1 eller PauseState vid Escape/P.
    /// Ljud och energi-lista initieras i Enter().
    /// </remarks>
    internal sealed class GameplayState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        // Parallax-bakgrundssystem — skapas en gång, säsongen sätts i Enter().
        private readonly IParallaxSystem _parallax;

        // Fysik-tillstånd
        // _rememberJumpCollision, _jumpMemory, _fallCounter och _enemyJump är tidsbaserade
        // "grace"-fönster i sekunder (räknas ned/upp med elapsed), inte frame-räknare —
        // se GameConstants.*Seconds. Det gör spelkänslan frame-rate-oberoende.
        private bool  _bPower;
        private float _rememberJumpCollision;
        private float _jumpMemory;
        private float _fallCounter;
        private bool  _allowCoyoteTime;
        private int   _tempMemJumpCounter;
        private int   _tempMemCoyoteCounter;
        private bool  _detHarBallatUrLog;
        private float _maxR, _maxL;

        // Fiende-AI
        private float _enemyJump;

        // Energi-regn vid träff
        private readonly EnergiRainObject _energiRain = new EnergiRainObject();
        private readonly Random _rng = new Random();

        public GameplayState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
            _parallax = services.Parallax;
        }

        public void Enter(GameContext context)
        {
            _services.ClearSwitchedState();

            // Välj parallax-bakgrund för den aktuella kartans årstid. Användarbanor
            // bär sin årstid explicit (utifrån vald tileset); inbyggda banor härleds
            // ur kartnamnet. Världskartan och okända kartor → null → inga lager ritas.
            var season = context.CurrentLevel?.MapSeason
                         ?? SeasonHelper.FromMapName(context.CurrentLevel?.Name);
            if (season.HasValue)
                _parallax.SetSeason(season.Value);

            context.ActiveObjects.RemoveAll(x =>
                context.CollectedEnergiIds.Any(id => id == x.CoinId));

            _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundWorld);
            if (context.CurrentLevel?.IsBossArena != true)
            {
                if (!_services.Audio.IsPlaying(Global.GlobalNamespace.SoundRef.BGSoundGame))
                    _services.Audio.Play(Global.GlobalNamespace.SoundRef.BGSoundGame);
            }
            else
            {
                _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
                if (!_services.Audio.IsPlaying(Global.GlobalNamespace.SoundRef.BGSoundFinalStage))
                    _services.Audio.Play(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
            }

            context.Player!.vx = 0;
            _enemyJump = 0f;

            // Slutboss-arena: skapa fas-controllern EN gång per fight. Vid pause→resume
            // körs Enter om (ny GameplayState) — då finns controllern redan och får INTE
            // återskapas (annars nollställs värmen = gratis-hack). Rensas på världskartan.
            if (context.CurrentLevel?.IsBossArena == true)
            {
                if (context.BossPhase == null)
                {
                    context.BossPhase = new BossPhaseController();
                    // Dräneringstakten sätts av insamlad energi (mer → långsammare; även 0 klarbart).
                    context.BossPhase.WarmthDrainPerSecond = BossPhaseController.ComputeWarmthDrain(
                        context.CollectedEnergiIds.Count, maxEnergy: 100,
                        drainAtZeroEnergy: 1.2f, drainAtFullEnergy: 0.3f);
                }
            }
            else
            {
                context.BossPhase = null;
            }
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Script.Tick(elapsed);

            if (_services.CheckAndClearSwitchedState())
            {
                context.ActiveObjects.RemoveAll(x =>
                    context.CollectedEnergiIds.Any(id => id == x.CoinId));
            }

            if (context.CurrentLevel?.Name == MapName.WorldMap)
            {
                if (context.IsPreviewMode)
                {
                    var runTime = context.GameTotalTime - context.UserMapRunStartTime;
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
                    _services.StateManager.Transition(
                        new UserMapResultState(_services, runTime, context.PreviewReturnState!),
                        context);
                }
                else if (context.UserMapSlotId != null)
                {
                    var runTime = context.GameTotalTime - context.UserMapRunStartTime;
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
                    _services.StateManager.Transition(
                        new UserMapResultState(_services, runTime,
                            new UserMapsState(_services), context.UserMapSlotId),
                        context);
                }
                else
                {
                    _services.StateManager.Transition(new WorldMapState(_services), context);
                }
                return;
            }

            if (_enemyJump > 0f) _enemyJump -= elapsed;

            if (context.Player!.Health < 1)
            {
                // Striden är slut (även om värmen slocknade) — rensa fas-controllern
                // så en ny boss-omgång alltid startar färskt.
                context.BossPhase = null;
                _services.Input.ButtonsHasGoneIdle = false;
                if (context.IsPreviewMode)
                {
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
                    ReturnToEditor(context);
                }
                else if (context.UserMapSlotId != null)
                {
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
                    _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
                    ReturnToUserMaps(context);
                }
                else
                {
                    _services.StateManager.Transition(new GameOverState(_services), context);
                }
                return;
            }

            context.ActiveObjects.RemoveAll(x => x.RemoveCount >= 4);

            if (context.ActiveObjects.Count <= 0)
            {
                System.Diagnostics.Debug.WriteLine("listDynamics är tom i GameplayState");
                throw new InvalidOperationException("ActiveObjects är tom i GameplayState");
            }

            _services.Input.Poll();
            _detHarBallatUrLog = false;

            // Input — hoppas om dialog är aktiv; dialog-avfärdning hanteras separat
            if (_services.Dialog.IsActive)
                HandleDialogInput();
            else if (_services.Input.IsWindowFocused)
                HandleInput(context, elapsed);

            // Fysik + kollision per objekt
            bool bossAlive = context.CurrentLevel?.IsBossArena == true &&
                             context.ActiveObjects.Any(x => x is DynamicCreatureEnemyBoss);

            foreach (var obj in context.ActiveObjects)
            {
                obj.detHarBallatUr = false;

                // Speciell boss-bana-logik (boss-arenor)
                if (context.CurrentLevel?.IsBossArena == true)
                {
                    if (!bossAlive)
                    {
                        if (obj is Teleport)
                        { obj.px = context.Player.px; obj.py = context.Player.py; }
                    }
                    else
                    {
                        if (obj is DynamicCreatureEnemyBoss)
                            _services.TriggerBossCheck();
                        if (obj.Id > 0)
                            obj.px = _services.GetBossObjectX(obj.Id);
                    }
                }

                if (!obj.Redundant)
                {
                    UpdateObject(obj, context, elapsed);
                }
                else
                {
                    obj.RemoveCount += 1;
                    obj.Update(elapsed, context.Player);
                }
            }

            // Energi-regn
            if (_energiRain.MakeItRain)
                MakeItRainEnergi(context);

            // Slutbossens fas-logik: dränera värme / vänd i akt 4 / avgör utfall.
            context.BossPhase?.Tick(elapsed);

            // Värmen slocknade → förlust. Behandlas som hjältens död (vanlig game over):
            // nollställ hälsan så den befintliga död-hanteringen (överst i Update) tar vid.
            if (context.BossPhase?.Outcome == BossOutcome.PlayerLost && context.Player != null)
                context.Player.Health = 0;

            // Akt 1: spegel-Scarlet är aktiv bara under Mirror-akten (svärm/jätte byggs i fas 4–5).
            // Argt läge när akt-hälsan är låg → snabbare, tätare/slumpigare språng.
            if (context.BossPhase != null)
            {
                bool mirrorAct = context.BossPhase.CurrentAct == BossAct.Mirror;
                bool angry = context.BossPhase.BossMaxHealth > 0 &&
                             context.BossPhase.BossHealth <= context.BossPhase.BossMaxHealth * 0.4f;
                foreach (var o in context.ActiveObjects)
                    if (o is DynamicCreatureMirrorScarlet ms)
                    {
                        ms.Active = mirrorAct;
                        ms.Angry = angry;
                    }
            }

            // Uppdatera mjuk kameraposition mot spelarens slutposition för denna tick.
            if (context.Player != null && context.CurrentLevel != null)
                _services.Camera.Advance(context.Player.px, context.Player.py, elapsed);
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            if (context.CurrentLevel != null && context.Player != null)
            {
                var cam = _services.Camera.GetView(
                    context.CurrentLevel.Width, context.CurrentLevel.Height,
                    context.ScreenWidth, context.ScreenHeight);

                // Fyll bakgrunden för områden utanför kartgränsen (t.ex. smala kartor på bred skärm).
                _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, RenderColor.Black);

                // Rita parallax-bakgrundslager (himmel → mellanskikt) bakom tiles.
                // Lager som inte har fått en årstid (t.ex. världskartan) ritar ingenting.
                _parallax.Draw(_rc, cam.OffsetX, context.ScreenWidth, context.ScreenHeight);

                foreach (var call in _services.TileRenderer.GetDrawCalls(cam, context.CurrentLevel))
                    _rc.DrawPartialSprite(SpriteId.MapTileSheet,
                        call.ScreenX, call.ScreenY, call.SpriteX, call.SpriteY,
                        call.TileWidth, call.TileHeight);

                foreach (var obj in context.ActiveObjects)
                    obj.DrawSelf(_rc, cam.OffsetX, cam.OffsetY);
            }

            HudRenderer.Draw(_rc, context);

            if (_services.Dialog.IsActive)
                _services.Dialog.Render(_rc);
        }

        public void Exit(GameContext context) { }

        // ── Preview-hjälpmetod ───────────────────────────────────────────────────
        /// <summary>
        /// Avslutar preview-läget och återvänder till den EditorState som
        /// startade preview-sessionen.
        /// </summary>
        private void ReturnToEditor(GameContext context)
        {
            _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
            _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);

            var returnState = context.PreviewReturnState!;
            context.IsPreviewMode      = false;
            context.PreviewReturnState = null;
            _services.StateManager.Transition(returnState, context);
        }

        private void ReturnToUserMaps(GameContext context)
        {
            _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
            _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
            context.UserMapSlotId = null;
            _services.StateManager.Transition(new UserMapsState(_services), context);
        }

        // ── Dialog-input ─────────────────────────────────────────────────────────
        private void HandleDialogInput()
        {
            if (_services.Input.IsConfirmPressed)
            {
                _services.Dialog.Dismiss();
                _services.Script.CompleteCurrentCommand();
                _services.Input.ButtonsHasGoneIdle = false;
            }
        }

        // ── Input ────────────────────────────────────────────────────────────────
        private void HandleInput(GameContext context, float elapsed)
        {
            var hero = (DynamicCreatureHero)context.Player!;

            _bPower = _services.Input.IsRunDown || _services.Input.IsSelectDown;

            hero.LookUp   = _services.Input.IsUpDown;
            hero.LookDown = _services.Input.IsDownDown;

            // Hopp
            bool jumpDown = _services.Input.IsJumpDown || _services.Input.IsConfirmPressed;
            if (jumpDown)
            {
                if (_services.Input.JumpButtonDownReleaseOnce)
                    _jumpMemory = GameConstants.JumpBufferSeconds;

                if (_services.Input.JumpButtonState < 3)
                    _services.Input.JumpButtonState++;

                if ((hero.vy == 0 && _services.Input.JumpButtonDownRelease) ||
                    (_allowCoyoteTime && _services.Input.JumpButtonDownReleaseOnce) ||
                    _enemyJump > 0f)
                {
                    if (hero.vy != 0 && _allowCoyoteTime)
                        _tempMemCoyoteCounter++;

                    _services.Audio.Play(Global.GlobalNamespace.SoundRef.Jump);

                    hero.vy = GameConstants.JumpVelocity;
                    _services.Input.JumpButtonDownRelease = false;
                    _jumpMemory = 0f;
                    _enemyJump = 0f;
                }
                _services.Input.JumpButtonDownReleaseOnce = false;
            }
            else
            {
                _services.Input.JumpButtonDownReleaseOnce = true;
                _services.Input.JumpButtonState = 0;
                _services.Input.JumpButtonPressRelease = true;
                if (context.HeroLandedState != 0)
                {
                    _services.Input.JumpButtonDownRelease = true;
                    _services.Input.JumpButtonCounter = 0;
                }
            }

            if (_jumpMemory > 0f && hero.Grounded)
            {
                _tempMemJumpCounter++;
                hero.vy = GameConstants.JumpVelocity;
                _services.Input.JumpButtonDownRelease = false;
                _jumpMemory = 0f;
            }

            // Höger
            if (_services.Input.IsRightDown)
            {
                float acc = (hero.Grounded ? GameConstants.MoveAccelerationGround : GameConstants.MoveAccelerationAir) * elapsed;
                hero.vx += acc;
                float maxSpd = _bPower ? GameConstants.MaxSpeedPower : GameConstants.MaxSpeedNormal;
                if (hero.vx > maxSpd) hero.vx = maxSpd;
            }

            // Vänster
            if (_services.Input.IsLeftDown)
            {
                float acc = (-1f * (hero.Grounded ? GameConstants.MoveAccelerationGround : GameConstants.MoveAccelerationAir)) * elapsed;
                hero.vx += acc;
                float maxSpd = _bPower ? GameConstants.MaxSpeedPower : GameConstants.MaxSpeedNormal;
                if (hero.vx < -maxSpd) hero.vx = -maxSpd;
            }

            // Pause
            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsPausePressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                if (context.IsPreviewMode)
                    ReturnToEditor(context);
                else if (context.UserMapSlotId != null)
                    ReturnToUserMaps(context);
                else
                    _services.StateManager.Transition(new PauseState(_services, PauseOrigin.Gameplay), context);
                return;
            }

            // Idle-animation
            // IdleCounter normaliseras till 60 fps-ekvivalenta enheter (som DynamicItem._subTick)
            // så att fördröjningen är korrekt oavsett bildfrekvens.
            if (_services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
            {
                context.IdleCounter += elapsed * 60f;
                if (context.IdleCounter > GameConstants.IdleTimeout)
                {
                    hero.IsIdle = true;
                    // Lyft: applicera ett enda vertikalt impuls i starten av lyftzonen (220-221),
                    // låt sedan gravitation ta vid naturligt. Den gamla varianten (vy -= 20.1f*elapsed)
                    // tog ut sig nästan exakt mot GravityNormal i samma frame och gav ingen rörelse.
                    if (context.IdleCounter > 220 && context.IdleCounter < 245)
                    {
                        if (hero.vy >= -0.1f && hero.Grounded)
                            hero.vy = -2.0f;
                    }
                }
                if (context.IdleCounter > 250)
                {
                    context.IdleCounter = 0;
                    hero.IsIdle = false;
                }
            }
            else
            {
                context.IdleCounter = 0;
                if (hero.IsIdle) hero.IsIdle = false;
            }
        }

        // ── Fysik + kollision per objekt ──────────────────────────────────────────
        private void UpdateObject(DynamicGameObject obj, GameContext context, float elapsed)
        {
            var map = context.CurrentLevel!;
            float fBorder = GameConstants.CollisionBorderPrecision;

            // Gravitation
            float rjc = _rememberJumpCollision;
            PhysicsSystem.ApplyGravity(obj, obj.IsHero, _bPower, ref rjc, elapsed);
            _rememberJumpCollision = rjc;

            // Luftmotstånd
            bool isIcy = map.Name == MapName.MapSeven || map.Name == MapName.MapEight || map.Name == MapName.MapNine || map.Name == MapName.MapTen;
            bool anyDir = _services.Input.IsLeftDown || _services.Input.IsRightDown;
            PhysicsSystem.ApplyDrag(obj, _bPower, isIcy, anyDir, elapsed);

            if (obj.IsHero)
            {
                if (obj.vx < _maxL) _maxL = obj.vx;
                if (obj.vx > _maxR) _maxR = obj.vx;
            }

            if (PhysicsSystem.ClampVelocities(obj))
            {
                obj.detHarBallatUr = true;
                _detHarBallatUrLog = true;
            }

            float newX = obj.px + obj.vx * elapsed;
            float newY = obj.py + obj.vy * elapsed;

            if (obj.IsHero && _rememberJumpCollision > 0f)
                if (newY > obj.py) newY = obj.py;

            // Horisontell kollision (karta)
            if (obj.vx <= 0)
            {
                var (adjX, hitLeft) = CollisionSystem.ResolveHorizontal(obj.py, newX, obj.vx, fBorder, map);
                bool turnPatrol = false;
                if (hitLeft) { newX = adjX; if (obj is not DynamicCreatureEnemyFrost) obj.vx = 0; turnPatrol = true; }
                obj.OnWallCollision(ref newX, turnPatrol, true, map, fBorder);
            }
            else
            {
                var (adjX, hitRight) = CollisionSystem.ResolveHorizontal(obj.py, newX, obj.vx, fBorder, map);
                bool turnPatrol = false;
                if (hitRight) { if (obj is not DynamicCreatureEnemyFrost) { newX = adjX; obj.vx = 0; } turnPatrol = true; }
                obj.OnWallCollision(ref newX, turnPatrol, false, map, fBorder);
            }

            obj.Grounded = false;

            // Vertikal kollision (karta)
            if (obj.vy <= 0)
            {
                if (obj.IsHero) { _jumpMemory = 0f; _allowCoyoteTime = false; _fallCounter = 0f; if (context.HeroAirBornState < 3) context.HeroAirBornState++; }
                var (adjY, hitCeil, _) = CollisionSystem.ResolveVertical(newX, newY, obj.vy, map);
                if (hitCeil) { newY = adjY; obj.vy = 0; if (obj.IsHero && _rememberJumpCollision <= 0f) _rememberJumpCollision = GameConstants.CeilingBonkSeconds; }
                if (obj.IsHero) context.HeroLandedState = 0;
            }
            else
            {
                if (obj is not DynamicCreatureEnemyBoss && obj is not DynamicCreatureEnemyIcicle)
                {
                    var (adjY, _, grounded) = CollisionSystem.ResolveVertical(newX, newY, obj.vy, map);
                    if (grounded)
                    {
                        newY = adjY; obj.vy = 0; obj.Grounded = true;
                        if (obj.IsHero)
                        {
                            _fallCounter = 0f; _allowCoyoteTime = true;
                            if (context.HeroLandedState < 3) context.HeroLandedState++;
                            if (context.HeroLandedState <= 1)
                                _services.Audio.Play(Global.GlobalNamespace.SoundRef.Land);
                        }
                    }
                }
                if (obj.IsHero)
                {
                    context.HeroAirBornState = 0;
                    if (_jumpMemory > 0f) _jumpMemory -= elapsed;
                    if (obj.vy > 1 && _fallCounter < GameConstants.CoyoteFallCapSeconds) _fallCounter += elapsed;
                    if (_fallCounter > GameConstants.CoyoteFallCutoffSeconds) _allowCoyoteTime = false;
                }
            }

            // AI: fast-detektion (no-op för de flesta, Frost overridar)
            obj.OnStuckCheck();

            // Dynamisk kollision (objekt vs objekt)
            float dx = newX, dy = newY;
            foreach (var other in context.ActiveObjects)
            {
                if (other == obj) continue;
                if (other.SolidVsDynamic && obj.SolidVsDynamic)
                    HandleDynamicCollision(obj, other, context, ref dx, ref dy);
                else if (obj.IsHero)
                    HandleHeroPickup(obj, other, context, dx);
            }

            if (!obj.detHarBallatUr)
            { obj.px = dx; obj.py = dy; }
            else if (_detHarBallatUrLog && obj is not DynamicItem)
                System.Diagnostics.Debug.WriteLine($"Position ej uppdaterad. {obj.Name} vx={obj.vx} vy={obj.vy}");

            obj.Update(elapsed, context.Player!);
        }

        // ── Dynamisk kollision ────────────────────────────────────────────────────
        private void HandleDynamicCollision(DynamicGameObject obj, DynamicGameObject other,
            GameContext context, ref float dx, ref float dy)
        {
            // Horisontell krock
            if (dx < (other.px + 1f) && (dx + 1f) > other.px &&
                obj.py < (other.py + 1f) && (obj.py + 1f) > other.py)
            {
                if (obj.vx < 0)
                {
                    dx = other.px + 1f;
                    if (other.Friendly != obj.Friendly)
                    {
                        if (other.IsHero) DamageHero((Creature)obj, (Creature)other, "3");
                        else              DamageHero((Creature)other, (Creature)obj, "2");
                    }
                }
                else
                {
                    dx = other.px - 1f;
                    if (other.Friendly != obj.Friendly)
                    {
                        if (other.IsHero) DamageHero((Creature)obj, (Creature)other, "2");
                        else              DamageHero((Creature)other, (Creature)obj, "2");
                    }
                }

                if ((obj is DynamicCreatureEnemyWalrus || obj is DynamicCreatureEnemyFrost) && !other.Friendly)
                {
                    if (obj.Patrol == Enum.Actions.Right) { obj.Patrol = Enum.Actions.Left;  obj.vx = -2; }
                    else                                  { obj.Patrol = Enum.Actions.Right; obj.vx =  2; }
                }
            }

            // Vertikal krock
            if (dx < (other.px + 1f) && (dx + 1f) > other.px &&
                dy < (other.py + 1f) && (dy + 1f) > other.py)
            {
                if (obj.vy < 0)
                {
                    dy = other.py + 1f;
                    if (other.Friendly != obj.Friendly)
                    {
                        if (!other.Friendly)
                        { if (context.Player!.px > other.px) DamageHero((Creature)obj, (Creature)other, "1"); }
                        else
                        { if (!obj.IsHero) { context.Player!.vy = -5.5f; context.Player.Grounded = true; JumpDamage((Creature)context.Player, (Creature)obj, context); } }
                    }
                }
                else
                {
                    dy = other.py - 1f;
                    if (other.Friendly != obj.Friendly)
                    {
                        if (!other.Friendly)
                        {
                            if (other is DynamicCreatureEnemyIcicle)
                                DamageHero((Creature)other, (Creature)context.Player!, "1");
                            else
                            { context.Player!.vy = -5.5f; context.Player.Grounded = true; JumpDamage((Creature)context.Player, (Creature)other, context); }
                        }
                        else
                            DamageHero((Creature)obj, (Creature)context.Player!, "1");
                    }
                }
            }
        }

        private void HandleHeroPickup(DynamicGameObject hero, DynamicGameObject other,
            GameContext context, float dx)
        {
            if (dx < (other.px + 1f) && (dx + 1f) > other.px &&
                hero.py < (other.py + 1f) && (hero.py + 1f) > other.py)
            {
                if (hero.IsAttackable)
                    _services.Audio.Play(Global.GlobalNamespace.SoundRef.PickUp);

                if (other.CoinId > 0)
                    context.CollectedEnergiIds.Add(other.CoinId);

                context.CurrentLevel!.OnInteraction(context.ActiveObjects, other, Enum.NATURE.WALK);
                other.OnInteract(hero);
            }
        }

        // ── Skada / kollision-hjälpare ────────────────────────────────────────────
        private const int SpillPerFrame = 4;

        private void DamageHero(Creature assailant, Creature victim, string from = "")
        {
            _services.Audio.Play(Global.GlobalNamespace.SoundRef.DamageHero);

            if (victim == null || !victim.IsAttackable) return;

            victim.Health -= assailant.DamageGiven;

            // Energi-kaskad: mängden utkastad (uppsamlingsbar) energi är slump[DamageGiven/2,
            // DamageGiven] men BEGRÄNSAD av nuvarande hälsa. Det är begränsningen som ger
            // "mer insamlad energi → större kaskad" — har du mycket energi släpps hela spillet
            // igenom, har du lite kapas det. (Originalmekaniken, återställd.)
            int n      = assailant.DamageGiven;
            int health = Math.Max(0, victim.Health);
            int min    = n / 2 >= health ? health : n / 2;
            int max    = n       >= health ? health : n;
            int count  = min >= max ? min : _rng.Next(min, max);

            _energiRain.RemainingToSpawn += count;
            _energiRain.StartPosX         = victim.px;
            _energiRain.StartPosY         = victim.py;
            _energiRain.MakeItRain        = true;

            float tx = victim.px - assailant.px;
            float ty = victim.py - assailant.py;
            float d  = (float)Math.Sqrt(tx * tx + ty * ty);
            if (d < 1) d = 1f;

            victim.KnockBack(tx / d, ty / d - 1f, 0.3f);

            // Spegel-Scarlet drar sig ur efter att ha gett skada → bryter loop där spelaren
            // annars fastnar i upprepade träffar (t.ex. under en platå).
            if (victim.IsHero && assailant is DynamicCreatureMirrorScarlet dealer)
                dealer.OnDealtDamage(victim.px);

            if (victim.IsHero) victim.SolidVsDynamic = true;
            else               victim.OnInteract(assailant);
        }

        // Skada bossen tar per lyckad stamp (mirrorHealth 30 / 6 ≈ 5 stamps per akt).
        private const int MirrorStompDamage = 6;

        private void JumpDamage(Creature assailant, Creature victim, GameContext context)
        {
            _services.Audio.Play(Global.GlobalNamespace.SoundRef.Damage);

            // Spegel-Scarlet: stamp dränerar boss-baren (controllern), inte hennes egen Health.
            if (victim is DynamicCreatureMirrorScarlet scarlet)
            {
                if (!scarlet.IsAttackable) return;
                context.BossPhase?.TakeHit(MirrorStompDamage);
                scarlet.OnStomped(assailant.px);
                return;
            }

            if (victim is DynamicCreatureEnemyBoss)
            {
                if (!victim.IsAttackable) return;
                victim.IsAttackable = false;
                victim.Health -= 10;
                if (victim.Health <= 0) { victim.Health = 0; victim.Redundant = true; victim.RemoveCount = 1; _enemyJump = GameConstants.EnemyStompWindowSeconds; }
            }
            else if (!victim.IsIndestructible)
            {
                victim.Health = 0; victim.Redundant = true; victim.RemoveCount = 1; _enemyJump = GameConstants.EnemyStompWindowSeconds;
            }
        }

        private void MakeItRainEnergi(GameContext context)
        {
            const int whenCollectable = 35; // ~0.6 sekunder skydd (normaliserat till 60 fps-ekvivalenta frames)
            float sx = _energiRain.StartPosX, sy = _energiRain.StartPosY;

            int toSpawn = Math.Min(SpillPerFrame, _energiRain.RemainingToSpawn);
            for (int i = 0; i < toSpawn; i++)
                context.ActiveObjects.Add(new DynamicItem(sx, sy, _services.Assets.GetItem(ItemRef.Energi)!, whenCollectable));

            _energiRain.RemainingToSpawn -= toSpawn;
            if (_energiRain.RemainingToSpawn == 0)
                _energiRain.MakeItRain = false;
        }
    }
}
