#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Listar sparade user maps och startar en körning av vald karta.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Separerar My Maps-flödet (välj karta, sätt context, starta GameplayState)
    /// från EditorState och MenuState. Analogt med hur WorldMapState hanterar
    /// inbyggda banor — håller GameplayState fri från nav-logik.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från MenuState ("My Maps"). Visar upp till 7 slots med best time.
    /// Enter startar GameplayState med context.UserMapSlotId satt; Esc → MenuState.
    /// </remarks>
    internal sealed class UserMapsState : IGameState
    {
        private readonly GameServices   _services;
        private readonly IRenderContext _rc;

        private List<string> _slots = new List<string>();
        private int          _selectedIndex;
        private string       _message      = "";
        private float        _messageTimer;

        public UserMapsState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            _slots         = BuildSlotList();
            _selectedIndex = 0;
            _services.Input.ButtonsHasGoneIdle = false;
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Audio.PauseAll();
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            if (_messageTimer > 0f) _messageTimer -= elapsed;

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            DrawList(context);

            if (!_services.Input.IsWindowFocused) return;

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsUpPressed && _selectedIndex > 0)
            {
                _selectedIndex--;
                _services.Input.ButtonsHasGoneIdle = false;
            }
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsDownPressed && _selectedIndex < _slots.Count - 1)
            {
                _selectedIndex++;
                _services.Input.ButtonsHasGoneIdle = false;
            }

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsCancelPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                context.MenuNavigation = Enum.MenuState.StartMenu;
                _services.StateManager.Transition(new MenuState(_services), context);
                return;
            }

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsConfirmPressed && _slots.Count > 0)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                StartMap(context, _slots[_selectedIndex]);
            }
        }

        public void Draw(IRenderContext renderContext) { }

        public void Exit(GameContext context) { }

        // ── Privata hjälpmetoder ─────────────────────────────────────────────────

        private void ShowMessage(string message)
        {
            _message      = message;
            _messageTimer = 3.0f;
        }

        private List<string> BuildSlotList()
        {
            var result = new List<string>();
            foreach (var id in _services.UserMaps.GetAvailableMapIds())
                if (id.StartsWith("slot"))
                    result.Add(id);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private void DrawList(GameContext context)
        {
            int cx = context.ScreenWidth / 2;

            string header = "My Maps";
            _rc.DrawText(header, cx - (header.Length * GameConstants.FontCharWidth) / 2, 8);

            if (_slots.Count == 0)
            {
                string empty = "No maps saved yet";
                _rc.DrawText(empty, cx - (empty.Length * GameConstants.FontCharWidth) / 2, 30);
                _rc.DrawText("Esc = back", cx - (10 * GameConstants.FontCharWidth) / 2, 46);
                return;
            }

            const int IconToTextGap = 25;
            const int RowHeight     = 18;
            int startY              = 28;

            for (int i = 0; i < _slots.Count; i++)
            {
                string slotId = _slots[i];
                var    record = _services.UserMapScores.GetRecord(slotId);
                string time   = record != null ? FormatTime(record.BestTime) : "—";
                string label  = $"{slotId}   {time}";

                int screenX = cx - (IconToTextGap + label.Length * GameConstants.FontCharWidth) / 2;
                int screenY = startY + i * RowHeight;

                int srcX = (i == _selectedIndex) ? 0 : 16;
                _rc.DrawPartialSprite(SpriteId.Items, screenX, screenY, srcX, 48, 16, 16);
                _rc.DrawText(label, screenX + IconToTextGap, screenY + 5);
            }

            int helpY = startY + _slots.Count * RowHeight + 6;
            _rc.DrawText("Enter = play   Esc = back", cx - (25 * GameConstants.FontCharWidth) / 2, helpY);

            if (_messageTimer > 0f)
                _rc.DrawText(_message, cx - (_message.Length * GameConstants.FontCharWidth) / 2, helpY + 14);
        }

        private void StartMap(GameContext context, string slotId)
        {
            var levelObj = _services.UserMaps.Load(slotId);
            if (levelObj == null) return;

            if (!levelObj.HasSpawn)
            {
                ShowMessage("Map has no spawn point — edit in Level Editor");
                return;
            }
            if (!levelObj.HasGoal)
            {
                ShowMessage("Map has no goal — edit in Level Editor");
                return;
            }

            var userMap = new UserMap(levelObj, slotId, _services.Assets);

            var hero = context.Player!;
            hero.px     = levelObj.SpawnX;
            hero.py     = levelObj.SpawnY;
            hero.vx     = 0f;
            hero.vy     = 0f;
            hero.Health = 9;

            context.CollectedEnergiIds.RemoveWhere(id => id >= 1000);
            context.ActiveObjects.Clear();
            context.ActiveObjects.Add(hero);
            userMap.PopulateDynamics(context.ActiveObjects);

            context.CurrentLevel        = userMap;
            context.UserMapSlotId       = slotId;
            context.UserMapRunStartTime = context.GameTotalTime;

            RegisterTilesheet(levelObj.TilesetSource);

            _services.StateManager.Transition(new GameplayState(_services), context);
        }

        private void RegisterTilesheet(string tilesetSource)
        {
            string assetName = tilesetSource.Replace(".tsx", "");
            string? path     = _services.Assets.GetSpritePath(assetName);
            if (path != null)
                _rc.RegisterSprite(SpriteId.MapTileSheet, path);
        }

        private static string FormatTime(TimeSpan t)
        {
            int minutes    = (int)t.TotalMinutes;
            int seconds    = t.Seconds;
            int hundredths = t.Milliseconds / 10;
            return $"{minutes}:{seconds:D2}.{hundredths:D2}";
        }
    }
}
