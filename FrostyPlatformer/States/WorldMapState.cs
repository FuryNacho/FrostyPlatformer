#nullable enable
using System.Collections.Generic;
using System.Linq;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Hanterar världskartan — hjälten rör sig bland stage-ikoner och väljer nästa bana.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayWorldMap (ca 820 rader). Isolerar
    /// unlockAllStages, no1–no8, currentStage och hasAccumulatedAllSpeed
    /// som tidigare låg som lösa fält i Program.cs.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från MenuState ("Start New Game"/"Resume"), SettingsState (Load)
    /// och PauseState (Select). unlockAll-parametern sätts av Konami-koden i
    /// PauseState.
    /// </remarks>
    internal sealed class WorldMapState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        // Världskart-specifikt tillstånd
        private bool _unlockAll;
        private bool _no1 = true, _no2, _no3, _no4, _no5, _no6, _no7, _no8;
        private int  _currentStage;
        private bool _hasAccumulated;

        public WorldMapState(GameServices services, bool unlockAll = false)
        {
            _services   = services;
            _rc         = services.RenderContext;
            _unlockAll  = unlockAll;
        }

        public void Enter(GameContext context)
        {
            _hasAccumulated = false;
            _currentStage   = 0;
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Script.Tick(elapsed);

            if (_currentStage == 0)
                _currentStage = _services.Settings.ActivePlayer.StageCompleted;

            // Ljud
            _services.Audio.Stop(Global.GlobalNamespace.SoundRef.BGSoundGame);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundEnd);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);
            if (!_services.Audio.IsPlaying(Global.GlobalNamespace.SoundRef.BGSoundWorld))
                _services.Audio.Play(Global.GlobalNamespace.SoundRef.BGSoundWorld);

            if (_services.Settings.ActivePlayer.ShowEnd)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _services.StateManager.Transition(new EndState(_services), context);
                return;
            }

            _rc.Clear(RenderColor.Black);

            if (!_hasAccumulated)
            {
                _hasAccumulated = true;
                context.Player!.vx = 0;
                context.Player.vy  = 0;
                context.Player.TurnedTo = Enum.PlayerOrientation.Right;
                context.Player.ChangeStageKnockBackReset();
            }

            int spawn = _services.Settings.ActivePlayer.SpawnAtWorldMap;
            var (corrX, corrY) = _services.WorldMap.GetSpawnPosition(spawn);

            if (context.CurrentLevel?.Name != "worldmap")
            {
                _services.ChangeMap("worldmap", corrX, corrY);
                _services.Input.ButtonsHasGoneIdle = false;
            }

            if (context.Player!.vx == 0 && context.Player.vy == 0 &&
                (context.Player.px != corrX || context.Player.py != corrY))
            {
                context.Player.vx = 0; context.Player.vy = 0;
                context.Player.px = corrX; context.Player.py = corrY;
            }

            _services.Input.Poll();

            // Input
            if (_services.Input.IsWindowFocused && context.Player.vx == 0 && context.Player.vy == 0)
            {
                if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                    _services.Input.ButtonsHasGoneIdle = true;

                if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsUpDown)
                    context.Player.vy = -3.0f;
                if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsDownDown)
                    context.Player.vy = 3.0f;

                if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsRightDown)
                {
                    if (_unlockAll || (_currentStage != 0 && _currentStage <= _services.Settings.ActivePlayer.StageCompleted))
                        context.Player.vx = 3;
                }

                if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsConfirmPressed)
                {
                    if (TryEnterStage(spawn, context)) return;
                }

                if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsCancelPressed)
                {
                    context.MenuNavigation = Enum.MenuState.PauseMenu;
                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(new MenuState(_services), context);
                    return;
                }
            }

            if (context.CurrentLevel != null)
                UpdateObjects(context, elapsed);

            if (context.CurrentLevel != null && context.Player != null)
            {
                var cam = _services.Camera.Calculate(
                    context.Player.px, context.Player.py,
                    context.CurrentLevel.Width, context.CurrentLevel.Height,
                    context.ScreenWidth, context.ScreenHeight);

                // Fyll bakgrunden för områden utanför kartgränsen (t.ex. smala kartor på bred skärm).
                _rc.FillRect(0, 0, context.ScreenWidth, context.ScreenHeight, RenderColor.Black);

                foreach (var call in _services.TileRenderer.GetDrawCalls(cam, context.CurrentLevel))
                    _rc.DrawPartialSprite(SpriteId.WorldMapTileSheet,
                        call.ScreenX, call.ScreenY, call.SpriteX, call.SpriteY,
                        call.TileWidth, call.TileHeight);

                foreach (var obj in context.ActiveObjects)
                    obj.DrawSelf(_rc, cam.OffsetX, cam.OffsetY);

                var hero = context.ActiveObjects.FirstOrDefault(x => x.IsHero);
                if (hero != null) hero.DrawSelf(_rc, cam.OffsetX, cam.OffsetY);
            }

            string stageText = "        World Map. Stage: " +
                               (_currentStage == 0 ? 1 : _currentStage) +
                               "                     ";
            _rc.DrawText(stageText, 0, 217);
            HudRenderer.Draw(_rc, context);
        }

        public void Draw(IRenderContext renderContext) { }

        public void Exit(GameContext context) { }

        // ── Banval ───────────────────────────────────────────────────────────────
        private bool TryEnterStage(int spawnSlot, GameContext context)
        {
            var entry = _services.WorldMap.GetStageEntry(spawnSlot);
            if (entry == null) return false;

            _hasAccumulated = false;
            _services.ChangeMap(entry.Value.MapName, entry.Value.X, entry.Value.Y);
            _services.Input.ButtonsHasGoneIdle = false;
            _services.StateManager.Transition(new GameplayState(_services), context);
            return true;
        }

        // ── Objektuppdatering + kollision (ekvivalent med DisplayWorldMap loop) ──
        private void UpdateObjects(GameContext context, float elapsed)
        {
            var map = context.CurrentLevel!;

            foreach (var obj in context.ActiveObjects)
            {
                if (obj.IsHero) UpdateHeroStageStop(obj, context);

                if (obj is DynamicCreatureOverlayWorldMap)
                {
                    int completed = _services.Settings.ActivePlayer.StageCompleted;
                    if (obj.Id < completed + 1)         obj.StageStatus = Enum.StageStatus.Passed;
                    else if (obj.Id == completed + 1)   obj.StageStatus = Enum.StageStatus.Current;
                    else                                 obj.StageStatus = Enum.StageStatus.NotPassed;
                }

                float newX = obj.px + obj.vx * elapsed;
                float newY = obj.py + obj.vy * elapsed;

                float border = GameConstants.CollisionBorderPrecision;
                newX = ResolveHorizontal(obj, newX, map, border);
                newY = ResolveVertical(obj, newX, newY, map);

                // Dynamisk kollision (aldrig aktuellt på världskartan i praktiken)
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

        private void UpdateHeroStageStop(DynamicGameObject hero, GameContext context)
        {
            int completed = _services.Settings.ActivePlayer.StageCompleted;
            float p = hero.px;

            if (CheckStageZone(p, GameConstants.WorldMapStage1X, _no1, completed, 0))
            { SetStage(1, context); hero.vx = 0; }
            else if (CheckStageZone(p, GameConstants.WorldMapStage2X, _no2, completed, 1))
            { SetStage(2, context); hero.vx = 0; }
            else if (CheckStageZone(p, GameConstants.WorldMapStage3X, _no3, completed, 2))
            { SetStage(3, context); hero.vx = 0; }
            else if (CheckStageZone(p, GameConstants.WorldMapStage4X, _no4, completed, 3))
            { SetStage(4, context); hero.vx = 0; }
            else if (CheckStageZone(p, GameConstants.WorldMapStage5X, _no5, completed, 4))
            { SetStage(5, context); hero.vx = 0; }
            else if (CheckStageZone(p, GameConstants.WorldMapStage6X, _no6, completed, 5))
            { SetStage(6, context); hero.vx = 0; }
            else if (CheckStageZone(p, GameConstants.WorldMapStage7X, _no7, completed, 6))
            { SetStage(7, context); hero.vx = 0; }
            else if (CheckStageZone(p, GameConstants.WorldMapStage8X, _no8, completed, 7))
            { SetStage(8, context); hero.vx = 0; }
        }

        private static bool CheckStageZone(float px, float stageX, bool noFlag, int completed, int stageIdx)
            => (!noFlag || completed == stageIdx) &&
               (px >= stageX && px <= stageX + GameConstants.WorldMapStageTolerance);

        private void SetStage(int stage, GameContext context)
        {
            _no1 = stage == 1; _no2 = stage == 2; _no3 = stage == 3; _no4 = stage == 4;
            _no5 = stage == 5; _no6 = stage == 6; _no7 = stage == 7; _no8 = stage == 8;
            _services.Settings.ActivePlayer.SpawnAtWorldMap = stage;
            _currentStage = stage;
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
