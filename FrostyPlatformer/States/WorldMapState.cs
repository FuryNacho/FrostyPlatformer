#nullable enable
using System;
using System.Linq;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Global.GlobalNamespace;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar världskartan — hjälten navigerar diskret mellan stoppunkt-noder
    /// och väljer nästa bana.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Ursprungligen använde WorldMapState hårdkodade X-koordinater (WorldMapStage1X …
    /// WorldMapStage9X) och nio bool-flaggor (_no1 … _no9) för att avgöra vilken nod
    /// spelaren befann sig på. Det kopplade karttopologin hårt till koden — flytta en
    /// nod och logiken slutade fungera tyst. Nu delegeras all nod-logik till
    /// IWorldMapSystem.GetCurrentNode / GetNextNode / GetNodeState, som arbetar mot
    /// PlacedObject-stoppunkter från worldmap.json. WorldMapState hanterar bara
    /// input, rörelsedelegering och state-övergångar.
    ///
    /// Navigation är strikt 4-riktningsbaserad (höger/vänster/upp/ned) som i SMB3.
    /// Spelaren rör sig kontinuerligt mot _targetNode och snäpps till nod-positionen
    /// när den anlänt inom ArrivalThreshold.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från MenuState ("Start New Game"/"Resume"), SettingsState (Load)
    /// och PauseState (Select). unlockAll-parametern sätts av Konami-koden i PauseState.
    /// </remarks>
    internal sealed class WorldMapState : IGameState
    {
        // Rörelsehastighet i tiles/sekund (matchar tidigare vx = 3)
        private const float MoveSpeed = 3.0f;

        // Spelaren snäpps till nod-positionen när Manhattan-distansen underskrider
        // detta värde. Sätts generöst (0.4 tiles) för att täcka frame-rate-variation.
        private const float ArrivalThreshold = 0.4f;

        // Fallback-spawn om SpawnAtWorldMap saknar matchande nod i worldmap.json
        private const int DefaultSpawnTileX = 3;
        private const int DefaultSpawnTileY = 8;

        // Tilesheet-index (tilesheetwm) för avklarad-markören — "gula pricken" på
        // rad 4, kolumn 3 (0-baserat index 17). Ritas ovanpå nodens vanliga
        // bana-markör (rad 4, kolumn 1) när stoppunktens bana är avklarad.
        private const int CompletedMarkerTileIndex = 17;

        private readonly GameServices  _services;
        private readonly IRenderContext _rc;

        private bool          _unlockAll;
        private bool          _hasAccumulated;
        private PlacedObject? _currentNode;   // noden spelaren befinner sig på
        private PlacedObject? _targetNode;    // noden spelaren rör sig mot

        public WorldMapState(GameServices services, bool unlockAll = false)
        {
            _services  = services;
            _rc        = services.RenderContext;
            // Dev-genväg: DevConfig.UnlockAllStages låser upp allt utan att varje
            // call-site behöver känna till flaggan (konami-koden skickar redan true).
            _unlockAll = unlockAll || DevConfig.UnlockAllStages;
        }

        public void Enter(GameContext context)
        {
            _hasAccumulated = false;
            _currentNode    = null;
            _targetNode     = null;

            // På världskartan pågår ingen boss-strid — rensa fas-controllern så att
            // nästa boss-besök startar färskt (annars bär den med sig gammal värme/akt).
            context.BossPhase = null;

            // Hjälten ska aldrig animera "idle" (vingflax) på världskartan — nollställ
            // idle-flaggan som annars hänger kvar från gameplay.
            if (context.Player != null) context.Player.IsIdle = false;
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Script.Tick(elapsed);

            // Ljud
            _services.Audio.Stop(SoundRef.BGSoundGame);
            _services.Audio.Pause(SoundRef.BGSoundEnd);
            _services.Audio.Pause(SoundRef.BGSoundFinalStage);
            if (!_services.Audio.IsPlaying(SoundRef.BGSoundWorld))
                _services.Audio.Play(SoundRef.BGSoundWorld);

            if (_services.Settings.ActivePlayer.ShowEnd)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(new EndState(_services), context);
                return;
            }

            // Återställ spelaren en gång vid enter
            if (!_hasAccumulated)
            {
                _hasAccumulated = true;
                context.Player!.vx = 0;
                context.Player.vy  = 0;
                context.Player.TurnedTo = Enum.PlayerOrientation.Right;
                context.Player.ChangeStageKnockBackReset();
            }

            // Ladda världskartan om den inte redan är aktiv
            int spawn = _services.Settings.ActivePlayer.SpawnAtWorldMap;
            var (spawnTileX, spawnTileY) = _services.WorldMap.GetSpawnTile(
                spawn, DefaultSpawnTileX, DefaultSpawnTileY);

            if (context.CurrentLevel?.Name != MapName.WorldMap)
            {
                _services.ChangeMap(MapName.WorldMap, spawnTileX, spawnTileY);
                _services.Input.ButtonsHasGoneIdle = false;
            }

            // Snäpp till spawn-nod vid uppstart (innan spelaren rört sig)
            if (context.Player!.vx == 0 && context.Player.vy == 0
                && _currentNode == null && _targetNode == null)
            {
                context.Player.px = spawnTileX;
                context.Player.py = spawnTileY;
                _currentNode = _services.WorldMap.GetCurrentNode(spawnTileX, spawnTileY);
            }

            _services.Input.Poll();

            // Input — enbart när spelaren är stilla och inte mitt i en rörelse
            bool canInput = _services.Input.IsWindowFocused
                         && context.Player.vx == 0 && context.Player.vy == 0
                         && _targetNode == null;

            if (canInput)
            {
                if (!_services.Input.ButtonsHasGoneIdle
                    && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                    _services.Input.ButtonsHasGoneIdle = true;

                if (_services.Input.ButtonsHasGoneIdle)
                {
                    if (_services.Input.IsRightDown)  TryStartMovement(context.Player,  1,  0);
                    if (_services.Input.IsLeftDown)   TryStartMovement(context.Player, -1,  0);
                    if (_services.Input.IsDownDown)   TryStartMovement(context.Player,  0,  1);
                    if (_services.Input.IsUpDown)     TryStartMovement(context.Player,  0, -1);

                    if (_services.Input.IsConfirmPressed)
                        if (TryEnterStage(context)) return;

                    if (_services.Input.IsPausePressed)
                    {
                        _services.Input.ButtonsHasGoneIdle = false;
                        _services.StateManager.Transition(new PauseState(_services, PauseOrigin.WorldMap), context);
                        return;
                    }
                }
            }

            if (context.CurrentLevel != null)
                UpdateObjects(context, elapsed);

            // Världskartan ska ha en statisk kamera — direkt centrerad på hjälten utan
            // den mjuka lerp-följningen som banorna använder (den ger en "efterglidande"
            // känsla när hjälten stannar, vilket känns fel på en diskret nod-karta).
            if (context.Player != null && context.CurrentLevel != null)
                _services.Camera.SnapTo(context.Player.px, context.Player.py);
        }

        public void Draw(IRenderContext renderContext, GameContext context)
        {
            if (context.CurrentLevel != null && context.Player != null)
            {
                var cam = _services.Camera.GetView(
                    context.CurrentLevel.Width, context.CurrentLevel.Height,
                    context.ScreenWidth, context.ScreenHeight);

                _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, RenderColor.Black);

                foreach (var call in _services.TileRenderer.GetDrawCalls(cam, context.CurrentLevel))
                    _rc.DrawPartialSprite(SpriteId.WorldMapTileSheet,
                        call.ScreenX, call.ScreenY, call.SpriteX, call.SpriteY,
                        call.TileWidth, call.TileHeight);

                DrawCompletedNodeMarkers(cam);

                foreach (var obj in context.ActiveObjects)
                    obj.DrawSelf(_rc, cam.OffsetX, cam.OffsetY);

                var hero = context.ActiveObjects.FirstOrDefault(x => x.IsHero);
                if (hero != null) hero.DrawSelf(_rc, cam.OffsetX, cam.OffsetY);
            }

            // Banner längst ned: nästa bana att klara (StageCompleted + 1).
            // Full bredd (svart stapel) med horisontellt och vertikalt centrerad text.
            int displayStage = _services.Settings.ActivePlayer.StageCompleted + 1;
            string stageText = "World Map. Stage: " + displayStage;

            const int fontSize     = 8;  // Teckensnittet är 8×8 px per tecken
            const int bannerHeight = 11;
            int bannerY = context.ScreenHeight - bannerHeight;
            int textX   = Math.Max(0, (context.ScreenWidth - stageText.Length * fontSize) / 2);
            int textY   = bannerY + (bannerHeight - fontSize) / 2;

            _rc.FillRect(0, bannerY, context.ScreenWidth, bannerHeight, RenderColor.Black);
            _rc.DrawText(stageText, textX, textY);
            HudRenderer.Draw(_rc, context);
        }

        // Ritar avklarad-markören (gul prick) ovanpå varje avklarad bana-nod. Tilen är
        // opak, så den täcker nodens vanliga bana-markör helt. Position beräknas som i
        // TileMapRenderer: round((tile - offset) * tileSize) — samma sub-pixel-avrundning
        // så markören sitter exakt på tile-rutan vid scrollning.
        private void DrawCompletedNodeMarkers(CameraView cam)
        {
            int completed = _services.Settings.ActivePlayer.StageCompleted;

            int sx      = CompletedMarkerTileIndex % GameConstants.TileSheetColumns;
            int sy      = CompletedMarkerTileIndex / GameConstants.TileSheetColumns;
            int spriteX = sx * cam.TileWidth;
            int spriteY = sy * cam.TileHeight;

            foreach (var (tileX, tileY) in _services.WorldMap.GetCompletedNodeTiles(completed))
            {
                int screenX = (int)Math.Round((tileX - cam.OffsetX) * cam.TileWidth,  MidpointRounding.AwayFromZero);
                int screenY = (int)Math.Round((tileY - cam.OffsetY) * cam.TileHeight, MidpointRounding.AwayFromZero);
                _rc.DrawPartialSprite(SpriteId.WorldMapTileSheet,
                    screenX, screenY, spriteX, spriteY, cam.TileWidth, cam.TileHeight);
            }
        }

        public void Exit(GameContext context) { }

        // ── Nod-navigation ─────────────────────────────────────────────────────────

        /// <summary>
        /// Försöker starta rörelse mot nästa nod i given riktning.
        /// Blockeras om nästa nod är Locked (och _unlockAll ej satt).
        /// </summary>
        private void TryStartMovement(DynamicGameObject hero, int dX, int dY)
        {
            if (_currentNode == null) return;

            var next = _services.WorldMap.GetNextNode(_currentNode, dX, dY);
            if (next == null) return;

            int completed = _services.Settings.ActivePlayer.StageCompleted;
            var state     = _services.WorldMap.GetNodeState(next, completed);

            if (!_unlockAll && state == NodeState.Locked) return;

            hero.vx     = dX * MoveSpeed;
            hero.vy     = dY * MoveSpeed;
            _targetNode = next;
        }

        /// <summary>
        /// Kontrollerar om spelaren anlänt till _targetNode.
        /// Snäpper till exakt tile-position, uppdaterar _currentNode och sparar progression.
        /// </summary>
        private void UpdateNodeArrival(DynamicGameObject hero)
        {
            if (_targetNode == null) return;

            float dist = Math.Abs(hero.px - _targetNode.TileX) + Math.Abs(hero.py - _targetNode.TileY);
            if (dist > ArrivalThreshold) return;

            hero.px      = _targetNode.TileX;
            hero.py      = _targetNode.TileY;
            hero.vx      = 0f;
            hero.vy      = 0f;
            _currentNode = _targetNode;
            _targetNode  = null;

            // Spara vilken bana-nod spelaren nådde (junctions sparas ej — ingen stage-ändring)
            int stageIdx = _services.WorldMap.GetStageIndex(_currentNode.SubType);
            if (stageIdx > 0)
                _services.Settings.ActivePlayer.SpawnAtWorldMap = stageIdx;
        }

        // ── Banval ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Startar banan som _currentNode pekar på, om noden är nåbar.
        /// Junction-noder och Locked-noder (utan unlockAll) ignoreras.
        /// </summary>
        private bool TryEnterStage(GameContext context)
        {
            if (_currentNode == null) return false;

            int stageIdx = _services.WorldMap.GetStageIndex(_currentNode.SubType);
            if (stageIdx < 1) return false;

            int completed = _services.Settings.ActivePlayer.StageCompleted;
            var state     = _services.WorldMap.GetNodeState(_currentNode, completed);
            if (!_unlockAll && state == NodeState.Locked) return false;

            var entry = _services.WorldMap.GetStageEntry(stageIdx);
            if (entry == null) return false;

            _hasAccumulated = false;
            _services.ChangeMap(entry.Value.MapName, entry.Value.X, entry.Value.Y);
            _services.Input.ButtonsHasGoneIdle = false;
            _services.StateManager.Transition(new GameplayState(_services), context);
            return true;
        }

        // ── Objektuppdatering + kollision ───────────────────────────────────────────

        private void UpdateObjects(GameContext context, float elapsed)
        {
            var map = context.CurrentLevel!;

            foreach (var obj in context.ActiveObjects)
            {
                if (obj.IsHero) UpdateNodeArrival(obj);

                float newX = obj.px + obj.vx * elapsed;
                float newY = obj.py + obj.vy * elapsed;

                float border = GameConstants.CollisionBorderPrecision;
                newX = ResolveHorizontal(obj, newX, map, border);
                newY = ResolveVertical(obj, newX, newY, map);

                // Dynamisk kollision (sällan aktuellt på världskartan)
                float dx = newX, dy = newY;
                foreach (var other in context.ActiveObjects)
                {
                    if (other == obj) continue;
                    if (other.SolidVsDynamic && obj.SolidVsDynamic)
                    {
                        if (dx < (other.px + 1f) && (dx + 1f) > other.px &&
                            obj.py < (other.py + 1f) && (obj.py + 1f) > other.py)
                        {
                            if (obj.vx < 0) { dx = other.px + 1f; }
                            else            { dx = other.px - 1f; }
                        }
                        if (dx < (other.px + 1f) && (dx + 1f) > other.px &&
                            dy < (other.py + 1f) && (dy + 1f) > other.py)
                        {
                            if (obj.vy < 0) dy = other.py + 1f;
                            else            dy = other.py - 1f;
                        }
                    }
                    else if (obj.IsHero)
                    {
                        if (dx < (other.px + 1f) && (dx + 1f) > other.px &&
                            obj.py < (other.py + 1f) && (obj.py + 1f) > other.py)
                        {
                            map.OnInteraction(context.ActiveObjects, other, Enum.NATURE.WALK);
                            other.OnInteract(obj);
                        }
                    }
                }

                obj.px = dx;
                obj.py = dy;
                obj.Update(elapsed, context.Player!);
            }
        }

        private static float ResolveHorizontal(DynamicGameObject obj, float newX, Map map, float border)
        {
            float inset = GameConstants.HitboxInset;
            if (obj.vx <= 0)
            {
                if (map.GetSolid((int)(newX + 0f), (int)(obj.py + 0f)) ||
                    map.GetSolid((int)(newX + 0f), (int)(obj.py + inset)))
                { newX = (int)newX + 1; obj.vx = 0; }
            }
            else
            {
                if (map.GetSolid((int)(newX + (1f - border)), (int)(obj.py + border)) ||
                    map.GetSolid((int)(newX + (1f - border)), (int)(obj.py + inset)))
                { newX = (int)newX; obj.vx = 0; }
            }
            return newX;
        }

        private static float ResolveVertical(DynamicGameObject obj, float newX, float newY, Map map)
        {
            obj.Grounded = false;
            if (obj.vy <= 0)
            {
                if (map.GetSolid((int)(newX + 0f), (int)newY) ||
                    map.GetSolid((int)(newX + 0.9f), (int)newY))
                { newY = (int)newY + 1; obj.vy = 0; }
            }
            else
            {
                if (map.GetSolid((int)(newX + 0f), (int)(newY + 1f)) ||
                    map.GetSolid((int)(newX + 0.9f), (int)(newY + 1f)))
                { newY = (int)newY; obj.vy = 0; obj.Grounded = true; }
            }
            return newY;
        }
    }
}
