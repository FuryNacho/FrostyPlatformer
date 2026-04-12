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
    /// Hanterar inmatningsskärmen för high score — låter spelaren skriva in sin tag
    /// med en ASCII-väljare och bekräftar posten.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayEnterHighScore (ca 325 rader). Isolerar
    /// HSSelectX, HSSelectY, Select, NameInAscii som tidigare var lösa fält i
    /// Program.cs.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från EndState när spelaren placerar sig på high score-listan.
    /// context.ReturnToEndAfterHighScore sätts till true av EndState före övergången.
    /// </remarks>
    internal sealed class EnterHighScoreState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;
        private const int ScreenW = GameConstants.ScreenWidth;

        private int _hsSelectX;
        private int _hsSelectY = 1;
        private int _select    = 1;
        private List<int> _nameAscii = new List<int> { 65, 65, 65 };

        // Sprite-positioner för pilarna och OK-knappen i ASCII-väljaren
        private static readonly List<HighScoreEnterName> ActionList = BuildActionList();

        public EnterHighScoreState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            _hsSelectX = 0;
            _hsSelectY = 1;
            _select    = 1;
            _nameAscii = new List<int> { 65, 65, 65 };
        }

        public void Update(GameContext context, float elapsed)
        {
            _services.Script.Tick(elapsed);
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            bool newTop = _services.Score.IsNewFirstPlace(context.EndTotalTime);

            // Rita rubriker
            if (newTop)
            {
                string gratz = "Congratulations!";
                _rc.DrawText(gratz, (ScreenW / 2) - ((gratz.Length * 8) / 2), 8);
            }
            string header = newTop ? "You've Beaten The Top High Score" : "New High Score";
            _rc.DrawText(header, (ScreenW / 2) - ((header.Length * 8) / 2), 20);

            string endTime = context.EndTotalTime.ToString("hh':'mm':'ss");
            _rc.DrawText(endTime, (ScreenW / 2) - ((endTime.Length * 8) / 2), 35);

            string inst = "Enter Your Tag";
            _rc.DrawText(inst, (ScreenW / 2) - ((inst.Length * 8) / 2), 58);

            // Rita ASCII-väljaren
            for (int i = 0; i < ActionList.Count; i++)
            {
                var item = ActionList[i];
                int x = i % 4;
                int y = i / 4;

                if (_hsSelectX == x) _select = x;

                int fx = (8 + x * 20) + 90;
                int fy = (20 + y * 20) + 55;

                int sx = 0, sy = 0;
                switch (item.MyProperty)
                {
                    case "pilupp":
                        sx = _select + 1 == i + 1 ? 32 : 32;
                        sy = _select + 1 == i + 1 ? 48 : 52;
                        _rc.DrawPartialSprite(SpriteId.Items, fx, fy, sx, sy, 9, 4);
                        break;
                    case "inget":
                        _rc.DrawPartialSprite(SpriteId.Items, fx, fy, 16, 48, 9, 4);
                        break;
                    case "ok":
                        sx = 48; sy = _select == 3 ? 48 : 56;
                        _rc.DrawPartialSprite(SpriteId.Items, fx, fy, sx, sy, 16, 8);
                        break;
                    case "pilner":
                        sx = 32; sy = _select + 9 == i + 1 ? 60 : 56;
                        _rc.DrawPartialSprite(SpriteId.Items, fx, fy, sx, sy, 9, 4);
                        break;
                    case "bokstav":
                        _rc.DrawText(((char)_nameAscii[x]).ToString(), fx, fy);
                        break;
                    default:
                        _rc.DrawPartialSprite(SpriteId.Items, fx, fy, 0, 0, 16, 16);
                        break;
                }
            }

            _rc.DrawText("Press OK when done", 8, 210);

            // Input
            if (!_services.Input.IsWindowFocused) return;

            if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                _services.Input.ButtonsHasGoneIdle = true;

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsLeftReleased)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _hsSelectX--;
            }
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsRightReleased)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                _hsSelectX++;
            }
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsUpReleased)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                if (_select < 3)
                {
                    int v = _nameAscii[_select] + 1;
                    if (v > 126) v = 32;
                    _nameAscii[_select] = v;
                }
            }
            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsDownReleased)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                if (_select < 3)
                {
                    int v = _nameAscii[_select] - 1;
                    if (v < 32) v = 126;
                    _nameAscii[_select] = v;
                }
            }

            // Klamma X/Y
            if (_hsSelectX < 0) _hsSelectX = 3;
            if (_hsSelectX >= 4) _hsSelectX = 0;
            if (_hsSelectY < 0) _hsSelectY = 3;
            if (_hsSelectY >= 4) _hsSelectY = 0;

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsCancelPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                context.MenuNavigation = Enum.MenuState.StartMenu;
                _services.StateManager.Transition(new MenuState(_services), context);
                return;
            }

            if (_services.Input.ButtonsHasGoneIdle && _services.Input.IsConfirmPressed)
            {
                _services.Input.ButtonsHasGoneIdle = false;
                if (_select == 3) // OK-knappen
                {
                    string name = "";
                    foreach (int ascii in _nameAscii)
                        name += ((char)ascii).ToString();

                    _services.Score.PutOnHighScore(new HighScoreObj
                    {
                        DateTime = DateTime.Now,
                        Handle   = name,
                        TimeSpan = context.EndTotalTime,
                        Percent  = context.CollectedEnergiIds.Count
                    });
                    _services.Score.Save();

                    _services.Input.ButtonsHasGoneIdle = false;
                    _services.StateManager.Transition(new HighScoreState(_services), context);

                    _nameAscii = new List<int> { 65, 65, 65 };
                    _hsSelectX = 0;
                }
            }
        }

        public void Draw(IRenderContext renderContext) { }

        public void Exit(GameContext context) { }

        // ── Statisk lista med ASCII-väljarens element ─────────────────────────────
        private static List<HighScoreEnterName> BuildActionList()
        {
            return new List<HighScoreEnterName>
            {
                new HighScoreEnterName { MyProperty = "pilupp" },
                new HighScoreEnterName { MyProperty = "pilupp" },
                new HighScoreEnterName { MyProperty = "pilupp" },
                new HighScoreEnterName { MyProperty = "inget" },
                new HighScoreEnterName { MyProperty = "bokstav", letter = "A" },
                new HighScoreEnterName { MyProperty = "bokstav", letter = "A" },
                new HighScoreEnterName { MyProperty = "bokstav", letter = "A" },
                new HighScoreEnterName { MyProperty = "ok" },
                new HighScoreEnterName { MyProperty = "pilner" },
                new HighScoreEnterName { MyProperty = "pilner" },
                new HighScoreEnterName { MyProperty = "pilner" },
                new HighScoreEnterName { MyProperty = "inget" },
            };
        }
    }
}
