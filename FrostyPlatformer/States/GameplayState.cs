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
        private bool _bPower;
        private float _rememberJumpCollision;
        private float _jumpMemory;
        private float _fallCounter;
        private bool _allowCoyoteTime;
        private int _tempMemJumpCounter;
        private int _tempMemCoyoteCounter;
        private bool _detHarBallatUrLog;
        private float _maxR, _maxL;

        // Fiende-AI
        private float _enemyJump;

        // Energi-regn vid träff
        private readonly EnergiRainObject _energiRain = new EnergiRainObject();
        private readonly Random _rng = new Random();

        public GameplayState(GameServices services)
        {
            _services = services;
            _rc = services.RenderContext;
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

            // Input-nollställning vid banstart: samma knapp (A/confirm) som startar banan från
            // världskartan hoppar inne i banan (jump triggas på IsConfirmPressed). Utan detta
            // ligger hoppknappen kvar nedtryckt vid spawn och tolkas som ett färskt hopp direkt.
            // Genom att tvinga latchen till "inte släppt sedan ett tryck" kräver vi att knappen
            // släpps och trycks på nytt innan ett hopp registreras. Nollställ även hopp-buffert/
            // coyote så ingen köad input fyrar av vid spawn.
            _services.Input.JumpButtonDownRelease     = false;
            _services.Input.JumpButtonDownReleaseOnce = false;
            _services.Input.JumpButtonState           = 0;
            _services.Input.ButtonsHasGoneIdle        = false;
            _jumpMemory = 0f;
            _allowCoyoteTime = false;
            _tempMemCoyoteCounter = 0;

            // Slutboss-arena: skapa fas-controllern EN gång per fight. Vid pause→resume
            // körs Enter om (ny GameplayState) — då finns controllern redan och får INTE
            // återskapas. Rensas på världskartan.
            if (context.CurrentLevel?.IsBossArena == true)
            {
                if (context.BossPhase == null)
                {
                    // Dev: BossStartAct hoppar in i en senare akt direkt (default Mirror = hela striden).
                    context.BossPhase = new BossPhaseController(startAct: DevConfig.BossStartAct);

                    // Akt 1: bossen ligger idle tills hjälten tar initiativet. Spara spawn-läget
                    // så vi kan upptäcka första rörelsen; dev-hopp förbi akt 1 vaknar direkt.
                    _bossAwake = DevConfig.BossStartAct != BossAct.Mirror;
                    _bossIdleTimer = 0f;
                    _heroSpawnPx = context.Player!.px;
                    _heroSpawnPy = context.Player!.py;
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

            // Akt 1: spegel-Scarlet är aktiv bara under Mirror-akten (svärm/jätte byggs i fas 4–5).
            // Argt läge när akt-hälsan är låg → snabbare, tätare/slumpigare språng. När Mirror-akten
            // är slut glitchar hon ut ur arenan (BeginExit, idempotent) — kroppen behålls dold för
            // akt 4 men syns inte under svärmen/jätten.
            if (context.BossPhase != null)
            {
                bool mirrorAct = context.BossPhase.CurrentAct == BossAct.Mirror;
                bool angry = context.BossPhase.BossMaxHealth > 0 &&
                             context.BossPhase.BossHealth <= context.BossPhase.BossMaxHealth * 0.4f;

                // Akt 1: bossen vaknar först när hjälten tar initiativet — hon ligger idle tills
                // hjälten rört sig från sin spawn, eller efter en idle-frist (BossWakeIdleSeconds)
                // så att en helt passiv spelare ändå får igång striden.
                if (mirrorAct && !_bossAwake && context.Player != null)
                {
                    _bossIdleTimer += elapsed;
                    bool heroMoved = Math.Abs(context.Player.px - _heroSpawnPx) > 0.05f ||
                                     Math.Abs(context.Player.py - _heroSpawnPy) > 0.05f;
                    if (heroMoved || _bossIdleTimer >= BossWakeIdleSeconds)
                        _bossAwake = true;
                }

                foreach (var o in context.ActiveObjects)
                    if (o is DynamicCreatureMirrorScarlet ms)
                    {
                        ms.Active = mirrorAct && _bossAwake;
                        ms.Angry = angry;
                        // Akt 1→2: animerad glitch-exit. Akt 3 (Giant): säkerställ att hon är borta —
                        // normalt redan dold via exiten, men vid dev-start direkt i Giant har den aldrig
                        // skett, så göm henne direkt. (Akt 4 återanvänder kroppen → rör henne inte där.)
                        if (context.BossPhase.CurrentAct == BossAct.Swarm)
                            ms.BeginExit();
                        else if (context.BossPhase.CurrentAct == BossAct.Giant && !ms.Accepting)
                            ms.Vanish();
                    }
            }

            // Akt 2: håll svärmen vid liv (eller lös upp kvarvarande kopior efter akten).
            ManageSwarm(context, elapsed);

            // Akt 3: visa jätten + driv näv-slammen (eller städa bort efter akten).
            if (_giantCollapseFlash > 0f) _giantCollapseFlash -= elapsed;
            ManageGiant(context, elapsed);

            // Akt 3: istappsregn (anti-camp-hazard).
            ManageHazards(context, elapsed);

            // Akt 4: acceptans — gå-mot-henne-vinsten.
            ManageAcceptance(context, elapsed);

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

            // Falsk seger-blixt när jätten rasar (bryggan till akt 4) — kort vit helskärm.
            if (_giantCollapseFlash > 0f)
                _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, new RenderColor(255, 255, 255));

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
            context.IsPreviewMode = false;
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
            // Akt 4 (Spegeln): b-power avstängd — lugnt, avsiktligt tempo.
            if (context.BossPhase?.CurrentAct == BossAct.Acceptance) _bPower = false;

            hero.LookUp = _services.Input.IsUpDown;
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
            // Istapp mot jättens näve → krossas PÅ PLATS (som mot marken) i stället för att fastna/blockeras
            // på den (båda är icke-vänliga → vanlig kollision skulle bara knuffa). Hoppar över positions-
            // resolutionen så skärvorna lossnar där de möttes.
            if ((obj is DynamicCreatureBossIcicle || other is DynamicCreatureBossIcicle) &&
                (obj is DynamicCreatureGiantArm    || other is DynamicCreatureGiantArm) &&
                dx < other.px + 1f && dx + 1f > other.px &&
                dy < other.py + 1f && dy + 1f > other.py)
            {
                var ic = (obj as DynamicCreatureBossIcicle) ?? (DynamicCreatureBossIcicle)other;
                ic.Shatter();
                return;
            }

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
                        else DamageHero((Creature)other, (Creature)obj, "2");
                    }
                }
                else
                {
                    dx = other.px - 1f;
                    if (other.Friendly != obj.Friendly)
                    {
                        if (other.IsHero) DamageHero((Creature)obj, (Creature)other, "2");
                        else DamageHero((Creature)other, (Creature)obj, "2");
                    }
                }

                if ((obj is DynamicCreatureEnemyWalrus || obj is DynamicCreatureEnemyFrost) && !other.Friendly)
                {
                    if (obj.Patrol == Enum.Actions.Right) { obj.Patrol = Enum.Actions.Left; obj.vx = -2; }
                    else { obj.Patrol = Enum.Actions.Right; obj.vx = 2; }
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
                            if (other is DynamicCreatureEnemyIcicle || other is DynamicCreatureBossIcicle)
                                DamageHero((Creature)other, (Creature)context.Player!, "1");
                            else
                            { context.Player!.vy = -5.5f; context.Player.Grounded = true; JumpDamage((Creature)context.Player, (Creature)other, context); }
                        }
                        else
                            DamageHero((Creature)obj, (Creature)context.Player!, "1");
                    }
                    // Akt 2: en svärm-kopia som landar ovanpå en annan hoppar sidledes av (sprider
                    // ut klungan i stället för att torna). Den ÖVRE kopian (obj) får impulsen.
                    else if (obj is DynamicCreatureSwarmCopy topCopy && other is DynamicCreatureSwarmCopy)
                        topCopy.OnStackedOn(other.px);
                }
            }
        }

        private void HandleHeroPickup(DynamicGameObject hero, DynamicGameObject other,
            GameContext context, float dx)
        {
            // Spegel-Scarlet är icke-solid i akt 4 men är ingen pickup — hennes interaktion
            // (sammansmältning/stamp) sköts i ManageAcceptance.
            if (other is DynamicCreatureMirrorScarlet) return;

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


            int count = 0;
            victim.Health = HealthRemainingAfterDamage(assailant.DamageGiven, victim.Health, out count);


            _energiRain.RemainingToSpawn += count;
            _energiRain.StartPosX = victim.px;
            _energiRain.StartPosY = victim.py;
            _energiRain.MakeItRain = true;

            float tx = victim.px - assailant.px;
            float ty = victim.py - assailant.py;
            float d = (float)Math.Sqrt(tx * tx + ty * ty);
            if (d < 1) d = 1f;

            victim.KnockBack(tx / d, ty / d - 1f, 0.3f);

            // Bossar drar sig ur efter att ha gett skada → bryter loop där spelaren annars fastnar
            // i upprepade träffar. Scarlet (akt 1): under en platå. Svärm-kopior (akt 2): hela
            // klungan backar en aning så hjälten kommer loss ur högen.
            if (victim.IsHero)
            {
                if (assailant is DynamicCreatureMirrorScarlet dealer)         dealer.OnDealtDamage(victim.px);
                else if (assailant is DynamicCreatureSwarmCopy swarmDealer)   swarmDealer.OnDealtDamage(victim.px);
                else if (assailant is DynamicCreatureBossIcicle icicle)       icicle.Shatter();   // krossas mot hjälten, som mot marken
            }

            if (victim.IsHero) victim.SolidVsDynamic = true;
            else victim.OnInteract(assailant);
        }

        private int HealthRemainingAfterDamage(int damageGivenValue, int healthBeforeDamage, out int numberOfEnergyToSpawn)
        {
            int calculatedDamage = CalculateDamage(damageGivenValue, healthBeforeDamage);
            int healthRemaining = healthBeforeDamage - calculatedDamage;
            numberOfEnergyToSpawn = RemainingEnergyToSpawn(calculatedDamage, healthRemaining);
            return healthRemaining;
        }

        private int CalculateDamage(int damageGivenValue, int healthBeforeDamage)
        {
            int calculatedDamage = damageGivenValue;
            if (damageGivenValue >= healthBeforeDamage)
            {
                // Låg hälsa. Kommer troligen dö.
                // Om mer än 1 i hälsa så ska vi ge en liten chans till överlevnad, annars toast. 
                if (healthBeforeDamage > 1)
                {
                    if (IsLucky())
                    {
                        // Låt undkomma med 1 i hälsa.
                        calculatedDamage = (healthBeforeDamage - 1);
                    }
                }

                return calculatedDamage;
            }

            if (healthBeforeDamage <= damageGivenValue * 2)
            {
                // Lite mer hälsa än fienden delar ut. Ge minimum skada PLUS eventuellt lite till. 
                int min = damageGivenValue;
                int max = damageGivenValue + (damageGivenValue / 2);
                calculatedDamage = _rng.Next(min, max);
            }
            else
            {
                // Vanlig skada för relativt hög hälsa.
                int min = damageGivenValue;
                int max = damageGivenValue * 2;
                calculatedDamage = _rng.Next(min, max);
            }

            return calculatedDamage;
        }

        private bool IsLucky()
        {
            // Dra ett tal i [0, LuckyChanceDenominator). Exakt ett utfall räknas som "tur"
            // → 1 av LuckyChanceDenominator chans.
            int luckyNumber = _rng.Next(GameConstants.LuckyChanceDenominator);
            return luckyNumber == GameConstants.LuckyChanceDenominator - 1;
        }
        
        /// <summary>
        /// Avgör hur många (uppsamlingsbara) energi-klot som kastas ut när hjälten tar skada.
        ///
        /// Antalet slumpas i intervallet [skada/2, skada) och kapas sedan av hjältens
        /// kvarvarande hälsa. Två egenskaper följer av detta:
        ///
        ///  • Antalet är ALLTID mindre än skadan som togs (när skada &gt; 0). Även om spelaren
        ///    plockar upp varenda utkastad energi nettoförlorar hen minst 1 hälsa — en träff
        ///    kan aldrig bli "gratis".
        ///  • Nära döden (lågt healthRemaining) kapas kaskaden ned mot noll: lite hälsa kvar
        ///    innebär lite energi att spilla.
        /// </summary>
        /// <param name="damageGiven">Faktiskt utdelad skada denna träff (ej fiendens rå-DamageGiven).</param>
        /// <param name="healthRemaining">Hjältens hälsa efter att skadan dragits av.</param>
        private int RemainingEnergyToSpawn(int damageGiven, int healthRemaining)
        {
            int n = damageGiven;
            int health = Math.Max(0, healthRemaining);
            int min = n / 2 >= health ? health : n / 2;
            int max = n >= health ? health : n;
            int count = min >= max ? min : _rng.Next(min, max);
            return count;
        }

        // Skada bossen tar per lyckad stamp (mirrorHealth 30 / 6 ≈ 5 stamps per akt).
        private const int MirrorStompDamage = 6;

        // Akt 1: bossen ligger idle vid spawn och vaknar på hjältens första rörelse — eller efter
        // denna frist om spelaren står helt stilla (så striden ändå kommer igång).
        private const float BossWakeIdleSeconds = 13f;
        private bool _bossAwake;
        private float _bossIdleTimer;
        private float _heroSpawnPx;
        private float _heroSpawnPy;

        // Akt 2 — svärmen. Skada på svärm-baren per dödad kopia (swarmHealth 24 / 4 = 6 kopior
        // att stampa). SwarmTargetAlive = hur många kopior som svärmar samtidigt; nya spawnar
        // tills baren töms. Spawn-flaggan växlar sida så de kommer från båda håll.
        private const int SwarmStompDamage = 4;
        private const int SwarmTargetAlive = 4;
        private int _swarmSpawnFlip;

        // Crescendo mot slutet: när det är 2 stamp kvar fördubblas svärmen, när det är 1 kvar
        // fördubblas den igen → ett stigande tryck strax innan akten faller. Förstärkningsvågorna
        // kastas in UTSPRITT från EN sida (som en näve slängd från utanför bild), inte alla på en frame.
        private const float SwarmSpawnInterval = 0.16f;   // tid mellan inkastade kopior (utspritt, en i taget)
        private int   _swarmTier;                         // senast nådda crescendo-nivå (0/1/2) → upptäcker nya vågor
        private bool  _waveFromLeft;                      // sida den aktuella förstärkningsvågen kastas in från
        private float _swarmSpawnTimer;                   // tid till nästa inkastade kopia

        // Akt 2→3-övergången är medvetet långsam och sinematisk: efter kaoset ska spelaren hinna inse
        // att den är safe innan det drar igång igen. Sekvensen (fas 1→3) efter sista stompen:
        //   1 Gather  — alla kvarvarande kopior + en svärm EXTRA ofarliga kopior dräller in, faller till
        //               marken och glitchar idle en stund (smälta-att-man-överlevde-beat).
        //   2 Cascade — kopiorna poffar bort en och en i ÖKANDE takt (poff … poff poff … poffpoffpoff).
        //   3 Quiet   — tyst tom arena en stund, sen får akt 3 sakta dra igång (jätte + is grindas).
        private const int   SwarmExitExtra     = 16;     // extra ofarliga kopior som kastas in bara för att poffa
        private const float ExtraSpawnInterval = 0.10f;  // takten de extra kastas in i
        private const float ExitTossY          = 6f;     // höjd de kastas in på vid sidan
        private const float ExitTossUp         = -4.5f;  // liten knuff uppåt → ballistisk båge
        private const float ExitTossSpeedMin   = 4f;     // min sidofart inåt (varieras → sprids över arenan)
        private const float ExitTossSpeedMax   = 8.5f;   // max sidofart inåt (< MaxVelocityX)
        private const float SwarmGatherTime    = 1.6f;   // håll-fas: faller, idle, glitchar
        private const float PoffStartInterval  = 0.55f;  // första gapet i poff-cascaden (långsam start)
        private const float PoffMinInterval    = 0.05f;  // snabbaste poff-takt (svansen: poffpoffpoff)
        private const float PoffAccel          = 0.80f;  // gapet × detta per poff → accelererar
        private const float SwarmQuietTime     = 2.2f;   // tyst tom arena innan akt 3 sakta drar igång
        private const float HazardFirstDelay   = 1.4f;   // isen väntar in en beat efter att jätten klivit fram

        private bool  _swarmActWasActive;  // har svärm-akten faktiskt körts? → exit-sekvensen får bara köra EFTER en riktig akt 2
        private int   _swarmExitPhase;     // 0 ingen, 1 gather/idle, 2 poff-cascade, 3 tyst, 4 klar (akt 3 får starta)
        private float _swarmExitTimer;     // tids-räknare inom aktuell fas
        private int   _extraSpawned;       // hur många extra effekt-kopior som kastats in
        private float _extraSpawnTimer;    // tid till nästa extra kopia
        private float _poffTimer;          // tid till nästa poff i cascaden
        private float _poffInterval;       // nuvarande poff-gap (krymper → accelererar)

        /// <summary>
        /// Hur många svärm-kopior som ska vara i luften givet boss-barens hälsa — crescendo mot slutet:
        /// fördubblas vid 2 stamp kvar, fördubblas igen vid 1 stamp kvar. Ren funktion (testbar).
        /// </summary>
        internal static int CurrentSwarmTarget(int bossHealth)
        {
            if (bossHealth <= SwarmStompDamage)     return SwarmTargetAlive * 4;   // 1 stamp kvar
            if (bossHealth <= SwarmStompDamage * 2) return SwarmTargetAlive * 2;   // 2 stamp kvar
            return SwarmTargetAlive;
        }

        /// <summary>
        /// Intervall (min..max, inklusive) för hur många istappar ett näv-nedslag släpper, som funktion
        /// av hur mycket skada som getts jätten: mer skada → fler istappar. Ren funktion (testbar).
        /// 0 skada → 1–2; ≥1/3 given → 2–3; ≥2/3 given → 2–4.
        /// </summary>
        internal static (int lo, int hi) SlamBurstRange(int bossHealth, int bossMaxHealth)
        {
            float dealt = bossMaxHealth > 0 ? 1f - (float)bossHealth / bossMaxHealth : 0f;
            if (dealt >= 2f / 3f) return (2, 4);
            if (dealt >= 1f / 3f) return (2, 3);
            return (1, 2);
        }

        // Slumpar antalet istappar för ETT nedslag givet aktuell jätte-hälsa (se SlamBurstRange).
        private int SlamBurstCount(int bossHealth, int bossMaxHealth)
        {
            var (lo, hi) = SlamBurstRange(bossHealth, bossMaxHealth);
            return _rng.Next(lo, hi + 1);
        }

        // Akt 3 — jätten. Skada på jätte-baren per lyckad svagpunkts-stamp (giantHealth 40 / 8 = 5
        // stamps). Slam-takten: en näve åt gången; ny efter SlamInterval när föregående är borta.
        private const int GiantStompDamage = 8;
        private const float SlamFirstDelay = 1.5f;
        private const float SlamInterval = 1.4f;
        private float _giantSlamTimer;
        private bool _giantSlamLeftNext;   // växlar vilken arm som slår härnäst
        private bool _giantHasSlammed;     // har jätten slagit näven i marken (arm i Stuck) minst en gång? → grindar istappsregnet

        // Bryggan akt 3→4: jätten rasar (kollaps) + en kort vit blixt (falsk seger).
        private const float CollapseFlashDur = 0.12f;
        private float _giantCollapseFlash;

        // Akt 4 — Spegeln. Vinst = gå in i din spegelbild på marken; stamp straffar dig själv.
        private const float WinMergeDur = 1.3f;    // sammansmältnings-beat (blixt) före EndState
        private bool _acceptanceStaged;
        private float _winTimer = -1f;

        // Akt 3 — istappar. Normalt faller is BARA som en skur strax efter varje näv-nedslag (bundet
        // till jätten), spridd i området KRING nedslaget men med en dödzon så själva näven går att
        // stampa. Det jämna bakgrundsregnet på timer slås PÅ först när bara ett stomp på näven återstår
        // (svårare på upploppet, när man nästan vunnit).
        private const float IcicleInterval = 1.0f;
        private const float SlamBurstDelay    = 0.18f; // fördröjning efter nedslaget innan skuren börjar
        private const float SlamBurstSpacing  = 0.12f; // gap mellan skurens istappar
        private const int   SlamBurstDeadZone = 2;     // istapparna landar MINST så här långt från näven (plats att stampa)
        private const int   SlamBurstSpread   = 9;     // ...och som mest så här långt bort (bred spridning)
        // Antalet istappar per nedslag är randomiserat och ökar med skadan som getts bossen — se SlamBurstRange.
        private float _icicleTimer;
        private int   _slamBurstRemaining;            // istappar kvar att släppa i pågående skur
        private float _slamBurstTimer;                // tid till nästa skur-istapp
        private float _slamImpactX;                   // kolumn nedslaget skedde i (skuren biasar hit)

        private void JumpDamage(Creature assailant, Creature victim, GameContext context)
        {
            _services.Audio.Play(Global.GlobalNamespace.SoundRef.Damage);

            // Spegel-Scarlet: stamp dränerar boss-baren (controllern), inte hennes egen Health.
            if (victim is DynamicCreatureMirrorScarlet scarlet)
            {
                // Akt 4: hon är icke-solid → JumpDamage nås inte (stamp hanteras i ManageAcceptance).
                if (!scarlet.IsAttackable) return;
                context.BossPhase?.TakeHit(MirrorStompDamage);
                scarlet.OnStomped(assailant.px);
                return;
            }

            // Svärm-kopia (akt 2): ett tramp dödar kopian OCH dränerar svärm-baren.
            if (victim is DynamicCreatureSwarmCopy)
            {
                if (!victim.IsAttackable) return;
                context.BossPhase?.TakeHit(SwarmStompDamage);
                victim.Health = 0; victim.Redundant = true; victim.RemoveCount = 1;
                _enemyJump = GameConstants.EnemyStompWindowSeconds;

                // Var detta sista stampet (akten lämnade Swarm)? Avväpna ALLA kvarvarande kopior i
                // SAMMA frame så ingen kan skada hjälten efter segern — konsekvent, hela svärmen på en
                // gång. ManageSwarm startar deras synliga glitch-out direkt efteråt (inga frysta statyer).
                if (context.BossPhase != null && context.BossPhase.CurrentAct != BossAct.Swarm)
                    foreach (var o in context.ActiveObjects)
                        if (o is DynamicCreatureSwarmCopy other && !other.Redundant)
                        { other.IsAttackable = false; other.SolidVsDynamic = false; }
                return;
            }

            // Jättens arm (akt 3): stamp räknas BARA när svagpunkten är exponerad (Stuck).
            if (victim is DynamicCreatureGiantArm arm)
            {
                if (arm.Phase != GiantArmPhase.Stuck || !arm.IsAttackable) return;
                context.BossPhase?.TakeHit(GiantStompDamage);
                arm.OnStomped();
                _enemyJump = GameConstants.EnemyStompWindowSeconds;
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

        // ── Akt 2: svärm-hantering ─────────────────────────────────────────────────
        /// <summary>
        /// Driver svärmen (akt 2): så länge BossPhaseController är i Swarm-akten hålls
        /// <see cref="CurrentSwarmTarget"/> kopior vid liv (nya spawnar in när någon stampats),
        /// med ett crescendo mot slutet. När akten vinns kör en sinematisk övergång (fas-maskin):
        /// idle-glitch på marken → accelererande poff-cascade → tyst paus, innan akt 3 sakta drar
        /// igång. Körs efter objekt-loopen så listan kan muteras säkert.
        /// </summary>
        private void ManageSwarm(GameContext context, float elapsed)
        {
            if (context.BossPhase == null) return;

            bool swarmAct = context.BossPhase.CurrentAct == BossAct.Swarm &&
                            context.BossPhase.Outcome == BossOutcome.Ongoing;

            if (swarmAct)
            {
                // I akten: markera att svärmen körts + nollställ exit-sekvensen (en framtida seger
                // ska kunna dra igång den igen).
                _swarmActWasActive = true;
                _swarmExitPhase = 0;

                // Vänta in akt 1-bossens glitch-exit innan svärmen droppar in: en kort beat med
                // tom arena ger tydlig separation mellan akterna (annars överlappar de visuellt).
                if (context.ActiveObjects.Any(o => o is DynamicCreatureMirrorScarlet ms && ms.ExitInProgress))
                    return;

                int bossHealth = context.BossPhase.BossHealth;

                // Crescendo-nivå: 0 normalt, 1 vid 2 stamp kvar, 2 vid 1 stamp kvar. När en NY nivå
                // nås startar en förstärkningsvåg som kastas in från EN sida (växlar mellan vågorna)
                // — som en jätte utanför bild som slänger in en näve kopior på en horisontell rad.
                int tier = bossHealth <= SwarmStompDamage ? 2 : bossHealth <= SwarmStompDamage * 2 ? 1 : 0;
                if (tier != _swarmTier)
                {
                    if (tier > 0) _waveFromLeft = (_swarmSpawnFlip++ & 1) == 0;
                    _swarmTier = tier;
                }

                // Fyll på mot målantalet, men UTSPRITT — en kopia i taget med mellanrum, inte hela
                // vågen på en frame. Crescendo-vågorna (tier > 0) kommer alla från vågens sida;
                // baspåfyllningen växlar sida som vanligt.
                int target = CurrentSwarmTarget(bossHealth);
                int alive  = context.ActiveObjects.Count(o => o is DynamicCreatureSwarmCopy c && c.Health > 0);
                if (_swarmSpawnTimer > 0f) _swarmSpawnTimer -= elapsed;
                if (alive < target && _swarmSpawnTimer <= 0f)
                {
                    bool fromLeft = tier > 0 ? _waveFromLeft : (_swarmSpawnFlip++ & 1) == 0;
                    context.ActiveObjects.Add(MakeSwarmCopy(context, fromLeft));
                    _swarmSpawnTimer = SwarmSpawnInterval;
                }
                return;
            }

            // Akt 1 (Mirror) eller tidigare → nollställ exit-state (ren körning/replay) och gör INGET.
            // Annars triggades den sinematiska övergången (extra glitch-kopior!) redan under akt 1.
            if (context.BossPhase.CurrentAct == BossAct.Mirror)
            {
                _swarmActWasActive = false;
                _swarmExitPhase = 0;
                return;
            }

            // Efter svärm-akten (Giant/Acceptance/Resolved): kör exit-sekvensen EN gång — men bara om
            // svärmen faktiskt har körts (annars t.ex. dev-start direkt i akt 3 → ingen falsk exit).
            if (!_swarmActWasActive) return;

            // ── Akt 2 vunnen → sinematisk övergång (fas-maskin) ──
            switch (_swarmExitPhase)
            {
                case 0: BeginSwarmExit(context);                 break;   // precis vunnit → starta sekvensen
                case 1: UpdateSwarmGather(context, elapsed);     break;   // dräll in extra + håll idle-glitch
                case 2: UpdateSwarmPoffCascade(context, elapsed); break;  // accelererande poff-cascade
                case 3:                                                   // tyst tom arena, sen får akt 3 starta
                    _swarmExitTimer -= elapsed;
                    if (_swarmExitTimer <= 0f) _swarmExitPhase = 4;
                    break;
            }
        }

        /// <summary>Sant medan svärmens akt 2→3-övergång pågår (idle-glitch, poff-cascade eller den
        /// tysta pausen) — både jätten och istappsregnet väntar in detta innan akt 3 sakta drar igång.</summary>
        private bool SwarmExitInProgress => _swarmExitPhase >= 1 && _swarmExitPhase <= 3;

        // Fas 0→1: alla kvarvarande kopior blir ofarliga och slutar jaga; de glitchar FÖRST när de
        // landat (de som är i luften faller klart innan dess).
        private void BeginSwarmExit(GameContext context)
        {
            foreach (var o in context.ActiveObjects)
                if (o is DynamicCreatureSwarmCopy c && !c.Redundant)
                    c.BeginExit();

            _extraSpawned    = 0;
            _extraSpawnTimer = 0f;
            _swarmExitTimer  = SwarmGatherTime;
            _swarmExitPhase  = 1;
        }

        // Fas 1 (Gather): dräll in extra OFARLIGA kopior bara för effekt medan håll-timern löper.
        private void UpdateSwarmGather(GameContext context, float elapsed)
        {
            if (_extraSpawned < SwarmExitExtra)
            {
                _extraSpawnTimer -= elapsed;
                if (_extraSpawnTimer <= 0f)
                {
                    context.ActiveObjects.Add(MakeExitCopy(context));
                    _extraSpawned++;
                    _extraSpawnTimer = ExtraSpawnInterval;
                }
            }

            _swarmExitTimer -= elapsed;
            if (_swarmExitTimer <= 0f)
            {
                _poffInterval   = PoffStartInterval;
                _poffTimer      = _poffInterval;
                _swarmExitPhase = 2;
            }
        }

        // Fas 2 (Cascade): poffa bort en slumpvis kopia, krymp intervallet → accelererande poff-band.
        // Poffar BARA kopior som faktiskt landat och börjat glitcha — aldrig en som fortfarande bågar in.
        private void UpdateSwarmPoffCascade(GameContext context, float elapsed)
        {
            var all = context.ActiveObjects.OfType<DynamicCreatureSwarmCopy>()
                             .Where(c => !c.Redundant).ToList();
            if (all.Count == 0)
            {
                _swarmExitTimer = SwarmQuietTime;
                _swarmExitPhase = 3;
                return;
            }

            var glitching = all.Where(c => c.IsGlitchingOut).ToList();
            if (glitching.Count == 0) return;   // alla kvar är fortfarande på väg ner → vänta in dem

            _poffTimer -= elapsed;
            if (_poffTimer <= 0f)
            {
                glitching[_rng.Next(glitching.Count)].Poff();
                _poffInterval = Math.Max(PoffMinInterval, _poffInterval * PoffAccel);
                _poffTimer    = _poffInterval;
            }
        }

        // Extra effekt-kopia: KASTAS in i en ballistisk båge från en sida (som av en jätte utanför bild),
        // ofarlig. Varierad sidofart sprider dem över arenan; de glitchar först när de landat.
        private DynamicCreatureSwarmCopy MakeExitCopy(GameContext context)
        {
            int w = context.CurrentLevel?.Width ?? 36;
            bool fromLeft = _rng.Next(2) == 0;
            float x = fromLeft ? 1f : w - 2f;
            var c = new DynamicCreatureSwarmCopy { px = x, py = ExitTossY };
            float speed = ExitTossSpeedMin + (float)_rng.NextDouble() * (ExitTossSpeedMax - ExitTossSpeedMin);
            c.vx = (fromLeft ? 1f : -1f) * speed;   // hög sidofart inåt
            c.vy = ExitTossUp;                      // liten knuff uppåt → båge
            c.BeginExit();
            return c;
        }

        // Skapar en kopia vid en arenakant (sida vald av anroparen) nära taket, med en liten
        // inåt-knuff så den läser som inkastad från sidan innan jakten tar över.
        private DynamicCreatureSwarmCopy MakeSwarmCopy(GameContext context, bool fromLeft)
        {
            int w = context.CurrentLevel?.Width ?? 36;
            float x    = fromLeft ? 2f : w - 3f;
            float toss = fromLeft ? 3f : -3f;
            return new DynamicCreatureSwarmCopy { px = x, py = 2f, vx = toss };
        }

        // ── Akt 3: jätte-hantering ─────────────────────────────────────────────────
        // Jätten är 5 rutor bred (giant_boss-atlasen). Förankringspunkt (övre vänstra) centreras
        // i arenan; py väljs så kroppen sitter i övre/mellersta delen av banan (justeras vid look-test).
        private const int GiantWidthTiles = 5;
        private const float GiantAnchorY = 3f;   // tornar i övre delen av arenan (ansiktet är 4 rutor högt)

        /// <summary>
        /// Driver jättens närvaro (akt 3): visar EN jätte centrerad i arenan så länge
        /// BossPhaseController är i Giant-akten, och städar bort den annars. Körs efter
        /// objekt-loopen så listan kan muteras säkert. Ritas först (Insert(0)) så kroppen
        /// hamnar bakom hjälten.
        /// </summary>
        private void ManageGiant(GameContext context, float elapsed)
        {
            if (context.BossPhase == null) return;

            bool giantAct = context.BossPhase.CurrentAct == BossAct.Giant &&
                            context.BossPhase.Outcome == BossOutcome.Ongoing;

            if (!giantAct)
            {
                _giantHasSlammed = false;   // nästa giant-akt börjar utan registrerat slag (replay/akt-byte)

                // Bryggan till akt 4: jätten GÅS SÖNDER (kollaps + vit blixt) i stället för att
                // bara försvinna. Armarna snäpps av i blixten; huvudet smulas och tas bort självt.
                var g = context.ActiveObjects.OfType<DynamicCreatureGiant>().FirstOrDefault(x => !x.Redundant);
                if (g != null && !g.IsCollapsing)
                {
                    g.BeginCollapse();
                    _giantCollapseFlash = CollapseFlashDur;
                    foreach (var o in context.ActiveObjects)
                        if (o is DynamicCreatureGiantArm a && !a.Redundant)
                        { a.Health = 0; a.Redundant = true; a.RemoveCount = 1; }
                }
                return;
            }

            var giant = context.ActiveObjects.OfType<DynamicCreatureGiant>().FirstOrDefault(g => !g.Redundant);

            // Spawna jätten (huvud) + två armar (en per axel) en gång.
            if (giant == null)
            {
                // Vänta in hela akt 2→3-övergången (idle-glitch + poff-cascade + tyst paus) innan jätten.
                if (SwarmExitInProgress) return;

                var map = context.CurrentLevel!;
                float gx = (map.Width - GiantWidthTiles) / 2f;   // huvudet centrerat i arenan
                giant = new DynamicCreatureGiant { px = gx, py = GiantAnchorY };
                context.ActiveObjects.Insert(0, giant);

                float shoulderY = GiantAnchorY + 2.5f;           // axlarna vid huvudets nedre hörn
                // Symmetriskt kring huvudets mitt: näven ritas 2 tiles bred (centrerad +0.5),
                // så axlarna placeras på gx-0.5 / gx+4.5 → båda armarna sticker ut vid kanterna.
                var left = new DynamicCreatureGiantArm();
                left.Configure(gx - 0.5f, shoulderY, true, giant, map);
                var right = new DynamicCreatureGiantArm();
                right.Configure(gx + GiantWidthTiles - 0.5f, shoulderY, false, giant, map);
                context.ActiveObjects.Insert(0, left);
                context.ActiveObjects.Insert(0, right);

                _giantSlamTimer = SlamFirstDelay;
                return;
            }

            var arms = context.ActiveObjects.OfType<DynamicCreatureGiantArm>().Where(a => !a.Redundant).ToList();
            if (arms.Count == 0) return;

            // Latcha första gången en näve faktiskt slagit i marken (arm i Stuck) → grindar istappsregnet.
            if (!_giantHasSlammed && arms.Any(a => a.Phase == GiantArmPhase.Stuck))
                _giantHasSlammed = true;

            // Varje nedslag (engångs-signal per arm) → schemalägg en extra is-skur nära nedslaget.
            // Antalet är slumpat och ökar med skadan som getts jätten (SlamBurstRange).
            foreach (var a in arms)
                if (a.ConsumeSlamLanded())
                {
                    _slamBurstRemaining = SlamBurstCount(context.BossPhase.BossHealth, context.BossPhase.BossMaxHealth);
                    _slamBurstTimer     = SlamBurstDelay;
                    _slamImpactX        = a.ImpactX;
                }

            // En arm slår åt gången; växla sida. Räkna ned bara när båda armarna vilar.
            if (!arms.Any(a => a.IsSlamming))
            {
                _giantSlamTimer -= elapsed;
                if (_giantSlamTimer <= 0f)
                {
                    var next = arms.FirstOrDefault(a => a.IsLeft == _giantSlamLeftNext) ?? arms[0];
                    next.TriggerSlam();
                    _giantSlamLeftNext = !_giantSlamLeftNext;
                    _giantSlamTimer = SlamInterval;
                }
            }
        }

        // ── Akt 3: istappsregn ─────────────────────────────────────────────────────
        /// <summary>
        /// Släpper telegraferade istappar i slumpade kolumner under Giant-akten (anti-camp).
        /// Städar bort kvarvarande istappar när akten är över. Körs efter objekt-loopen.
        /// </summary>
        private void ManageHazards(GameContext context, float elapsed)
        {
            if (context.BossPhase == null) return;

            bool giantAct = context.BossPhase.CurrentAct == BossAct.Giant &&
                            context.BossPhase.Outcome == BossOutcome.Ongoing;

            if (!giantAct)
            {
                foreach (var o in context.ActiveObjects)
                    if (o is DynamicCreatureBossIcicle ic && !ic.Redundant)
                    { ic.Health = 0; ic.Redundant = true; ic.RemoveCount = 1; }
                return;
            }

            // Akt 3 ska dra igång SAKTA, och istappsregnet är BUNDET till jätten: inga istappar förrän
            // (a) hela svärm-övergången är klar OCH (b) jätten slagit näven i marken minst en gång. Sedan
            // väntar första istappen in en beat (HazardFirstDelay) efter det första slaget.
            if (SwarmExitInProgress || !_giantHasSlammed) { _icicleTimer = HazardFirstDelay; return; }

            // Extra is-skur strax efter varje näv-nedslag — i området KRING nedslaget, men minst
            // SlamBurstDeadZone kolumner bort åt något håll så själva näven går att stampa.
            if (_slamBurstRemaining > 0)
            {
                _slamBurstTimer -= elapsed;
                if (_slamBurstTimer <= 0f)
                {
                    int dist = _rng.Next(SlamBurstDeadZone, SlamBurstSpread + 1);
                    int bx   = (int)Math.Round(_slamImpactX) + dist * (_rng.Next(2) == 0 ? -1 : 1);
                    SpawnIcicle(context, bx);
                    _slamBurstRemaining--;
                    _slamBurstTimer = SlamBurstSpacing;
                }
            }

            // Bakgrundsregn (timer): slås PÅ först när bara ett stomp på näven återstår — svårare
            // precis på upploppet (när man nästan vunnit).
            bool lastStompLeft = context.BossPhase.BossHealth <= GiantStompDamage;
            if (lastStompLeft)
            {
                _icicleTimer -= elapsed;
                if (_icicleTimer <= 0f)
                {
                    int tx = _rng.Next(2, Math.Max(3, context.CurrentLevel!.Width - 2));
                    SpawnIcicle(context, tx);
                    _icicleTimer = IcicleInterval;
                }
            }
        }

        // Spawnar en istapp i kolumn tx (klampad inom arenan) med markören ovanpå golvet i den kolumnen.
        private void SpawnIcicle(GameContext context, int tx)
        {
            var map = context.CurrentLevel!;
            tx = Math.Clamp(tx, 2, Math.Max(2, map.Width - 3));
            float markerY = FloorTopForColumn(map, tx) - 1f;
            var icicle = new DynamicCreatureBossIcicle();
            icicle.Configure(tx, markerY);
            context.ActiveObjects.Add(icicle);
        }

        // Golvets ovansida i en kolumn (skannar nedifrån upp → huvudgolvet, ignorerar hyllor).
        private static int FloorTopForColumn(IMapData map, int tx)
        {
            tx = Math.Clamp(tx, 0, map.Width - 1);
            int y = map.Height - 1;
            while (y >= 0 && map.GetSolid(tx, y)) y--;
            return y + 1;
        }

        // ── Akt 4: acceptans ───────────────────────────────────────────────────────
        /// <summary>
        /// Den inverterade finalen: när jätten rasat sätter spegel-Scarlet ihop sig i mitten,
        /// stilla. Att GÅ MOT henne (och hålla) fyller närmande-mätaren; stamp backar den (loopen
        /// fortsätter). Full mätare → sammansmältning → EndState. Värmen vänder/regenererar i
        /// controllern. Körs efter objekt-loopen.
        /// </summary>
        private void ManageAcceptance(GameContext context, float elapsed)
        {
            var bp = context.BossPhase;
            if (bp == null) return;
            if (bp.CurrentAct != BossAct.Acceptance && bp.CurrentAct != BossAct.Resolved)
            { _acceptanceStaged = false; return; }

            var scarlet = context.ActiveObjects.OfType<DynamicCreatureMirrorScarlet>().FirstOrDefault();
            if (scarlet == null) return;

            // Vänta tills jätten rasat klart, ställ sedan scenen: hon "sätter ihop sig" i mitten.
            if (!_acceptanceStaged)
            {
                bool giantGone = !context.ActiveObjects.Any(o => o is DynamicCreatureGiant && !o.Redundant);
                if (!giantGone) return;
                StageMirror(context, scarlet);
                _acceptanceStaged = true;
            }

            // Vunnet → kort sammansmältnings-beat (blixt), sen slutet.
            if (bp.Outcome == BossOutcome.PlayerWon)
            {
                if (_winTimer < 0f) { _winTimer = WinMergeDur; _giantCollapseFlash = 0.15f; }
                _winTimer -= elapsed;
                if (_winTimer <= 0f)
                {
                    _services.Settings.ActivePlayer.ShowEnd = true;
                    context.BossPhase = null;
                    _services.StateManager.Transition(new EndState(_services), context);
                }
                return;
            }

            // Spegel-interaktion (Scarlet är icke-solid i akt 4 → hanteras manuellt här):
            var hero = context.Player!;
            bool overlap = Math.Abs(hero.px - scarlet.px) < 0.9f && Math.Abs(hero.py - scarlet.py) < 0.9f;
            if (overlap)
            {
                if (hero.py < scarlet.py - 0.3f && hero.vy > 0f)
                {
                    // Stamp = fel väg → det är HJÄLTEN som tar skada (din spegel slår inte tillbaka).
                    DamageHero(scarlet, (Creature)hero);
                    hero.vy = -5.5f;
                }
                else if (hero.Grounded && scarlet.Grounded)
                {
                    // Båda på marken, gå in i varandra → sammansmältning (vinst).
                    bp.ApproachToward(1f);
                }
            }
        }

        // Placerar spegel-Scarlet på den översta ytan (plattform om en finns) på MOTSATT sida
        // från hjälten — aldrig ovanpå dig. Spegel-rörelsen tar sedan över.
        private static void StageMirror(GameContext context, DynamicCreatureMirrorScarlet scarlet)
        {
            var map = context.CurrentLevel!;
            var hero = context.Player!;
            int center = map.Width / 2;
            int sx = hero.px < center ? center + 4 : center - 4;
            sx = Math.Clamp(sx, 2, map.Width - 3);

            int sy = 3;                                   // skanna uppifrån → första ytan (plattform/golv)
            while (sy < map.Height && !map.GetSolid(sx, sy)) sy++;

            scarlet.px = sx; scarlet.py = sy - 1;
            scarlet.vx = 0; scarlet.vy = 0;
            scarlet.Accepting = true;
        }
    }
}
