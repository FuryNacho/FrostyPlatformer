#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Spelar upp slutanimationen och visar sluttexten efter att spelaren klarat alla banor.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: State Machine (konkret tillstånd)
    ///
    /// MOTIVERING:
    /// Extraherat från Program.DisplayEnd (ca 300 rader) inklusive AnimationEnd,
    /// DrawEndTextList, ReSetForEnd och MakeItSnow. Isolerar alla
    /// animationsfält (GraphicCounter, time, worldPosIncrement m.fl.) och
    /// sluttextens tillstånd (ListEndText, typingEndTextIsDone) som tidigare låg
    /// som lösa properties i Program.cs.
    ///
    /// ANVÄNDNING:
    /// Aktiveras från WorldMapState (när ShowEnd == true). Enter() avgör
    /// sluttyp, startar rätt musik och omdirigerar till EnterHighScoreState om
    /// spelaren platsar på high score-listan. Övergår annars till MenuState
    /// när spelaren bekräftar/trycker en knapp efter att sluttexten visats.
    /// </remarks>
    internal sealed class EndState : IGameState
    {
        private readonly GameServices _services;
        private readonly IRenderContext _rc;

        // Sluttyp
        private Enum.TypeOfEnding _typeOfEnding = Enum.TypeOfEnding.None;

        // Animations-tillstånd
        private int   _graphicCounter;
        private float _time;
        private int   _timeTotalCounter;
        private int   _worldPosIncrement    = -15;
        private int   _worldPosIncrementIgloo = 160;
        private int   _doGest;
        private bool  _drawHeroEnd          = true;

        // Sluttextens tillstånd
        private float        _incrementerDisplayEnd;
        private List<string> _listEndText           = new List<string>();
        private bool         _typingEndTextIsDone;
        private bool         _skippTypingRow;

        // Signalerar att första Update-anropet fortfarande behövs för podium-redirect
        private bool _isFirstUpdate;

        // Snöeffekt-tillstånd (samma mönster som SplashState)
        private Aggregate.MakeItSnow? _snowSlow;
        private Aggregate.MakeItSnow? _snowFast;
        private int   _counterSlow = 1;
        private int   _counterFast = 1;
        private float _incSlow;
        private float _incFast;

        public EndState(GameServices services)
        {
            _services = services;
            _rc       = services.RenderContext;
        }

        public void Enter(GameContext context)
        {
            // Avgör sluttypen baserat på antal samlade energier och hero-hälsa
            _typeOfEnding = DetermineEnding(context);

            // Stoppa alla bakgrundsljud och starta rätt slut-musik
            _services.Audio.StopAll();
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundWorld);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundGame);
            _services.Audio.Pause(Global.GlobalNamespace.SoundRef.BGSoundFinalStage);

            if (!_services.Audio.IsPlaying(Global.GlobalNamespace.SoundRef.BGSoundEnd))
            {
                if (_typeOfEnding == Enum.TypeOfEnding.Perfect)
                    _services.Audio.Play(Global.GlobalNamespace.SoundRef.BGPerfectEnd);
                else if (_typeOfEnding == Enum.TypeOfEnding.NerePerfect)
                    _services.Audio.Play(Global.GlobalNamespace.SoundRef.BGNearPerfectEnd);
                else
                    _services.Audio.Play(Global.GlobalNamespace.SoundRef.BGSoundEnd);
            }

            // Spara sluttiden och nollställ animationen
            context.EndTotalTime = context.GameTotalTime;
            ReSetForEnd();

            // Snö
            _snowSlow = null;
            _snowFast = null;
            _counterSlow = 1;
            _counterFast = 1;
            _incSlow = 0f;
            _incFast = 0f;

            _isFirstUpdate = true;
        }

        public void Update(GameContext context, float elapsed)
        {
            // Första uppdateringen: kontrollera om spelaren platsar på high score-listan
            if (_isFirstUpdate)
            {
                _isFirstUpdate = false;

                if (context.RightToAccessPodium)
                {
                    context.RightToAccessPodium = false;

                    if (_services.Score.PlacesOnHighScore(context.EndTotalTime))
                    {
                        context.ReturnToEndAfterHighScore = true;
                        _services.Input.ButtonsHasGoneIdle = false;
                        _services.StateManager.Transition(new EnterHighScoreState(_services), context);
                        return;
                    }
                }
            }

            _services.Script.Tick(elapsed);
            _rc.Clear(RenderColor.Black);
            _services.Input.Poll();

            _services.Settings.ActivePlayer.ShowEnd = false;

            // Input
            if (_services.Input.IsWindowFocused)
            {
                if (!_services.Input.ButtonsHasGoneIdle && _services.Input.IsIdle && !_services.Input.IsAnyKeyPressed)
                    _services.Input.ButtonsHasGoneIdle = true;

                if (_services.Input.ButtonsHasGoneIdle &&
                    (_services.Input.IsAnyKeyPressed || !_services.Input.IsIdle))
                {
                    _services.Input.ButtonsHasGoneIdle = false;

                    if (_typingEndTextIsDone || _services.Input.IsCancelPressed)
                    {
                        context.MenuNavigation = Enum.MenuState.StartMenu;
                        _services.StateManager.Transition(new MenuState(_services), context);
                        ReSetForEnd();
                        return;
                    }
                    else
                    {
                        _skippTypingRow = true;
                    }
                }
            }

            // Rita slutskärms-bakgrunden centrerad (256×224 sprite på dynamisk skärm)
            int cx = (context.ScreenWidth  - GameConstants.ScreenWidth)  / 2;
            int cy = (context.ScreenHeight - GameConstants.ScreenHeight) / 2;
            _rc.DrawSprite(SpriteId.SplashEnd, cx, cy);

            // Snöeffekt
            const int SnowScreenMargin = 4; // Lämnar en tunn kant nedtill utanför snöeffekten
            MakeItSnow(elapsed, context.ScreenHeight - SnowScreenMargin);

            // Hero/igloo-animation
            AnimationEnd(elapsed, context);

            // Skriv-animerad sluttext
            DrawEndTextList(elapsed, cx, cy);
        }

        public void Draw(IRenderContext renderContext) { }

        public void Exit(GameContext context) { }

        // ── Sluttyps-bestämning ───────────────────────────────────────────────────
        private static Enum.TypeOfEnding DetermineEnding(GameContext context)
        {
            if (context.CollectedEnergiIds.Count == 100)
            {
                return context.Player?.Health == 100
                    ? Enum.TypeOfEnding.Perfect
                    : Enum.TypeOfEnding.NerePerfect;
            }
            return Enum.TypeOfEnding.Done;
        }

        // ── Nollställning (ekvivalent med Program.ReSetForEnd) ────────────────────
        private void ReSetForEnd()
        {
            _listEndText            = new List<string>();
            _typingEndTextIsDone    = false;
            _skippTypingRow         = false;
            _worldPosIncrement      = -15;
            _worldPosIncrementIgloo = 160;
            _drawHeroEnd            = true;
            _timeTotalCounter       = 0;
            _graphicCounter         = 0;
            _time                   = 0f;
            _doGest                 = 0;
            _incrementerDisplayEnd  = 0f;
        }

        // ── Hero/igloo-animation (ekvivalent med Program.AnimationEnd) ────────────
        private void AnimationEnd(float elapsed, GameContext context)
        {
            if (_typeOfEnding == Enum.TypeOfEnding.Done)
                AnimateDone(elapsed, context);
            else if (_typeOfEnding == Enum.TypeOfEnding.NerePerfect)
                AnimateNearPerfect(elapsed, context);
            else if (_typeOfEnding == Enum.TypeOfEnding.Perfect)
                AnimatePerfect(elapsed, context);
        }

        private void AnimateDone(float elapsed, GameContext context)
        {
            _time += elapsed;
            if (_time <= 0.3f)       _graphicCounter = 1;
            else if (_time <= 0.6f)  _graphicCounter = 2;
            else if (_time <= 0.9f)  _graphicCounter = 3;
            else
            {
                _graphicCounter = 4;
                _time = 0f;
                _timeTotalCounter++;

                if (_timeTotalCounter < 85)
                    { _worldPosIncrement++; _doGest = 1; }
                else if (_timeTotalCounter > 85 && _timeTotalCounter < 87)
                    _doGest = 0;
                else if (_timeTotalCounter > 87 && _timeTotalCounter < 95)
                    _doGest = 2;
                else if (_timeTotalCounter > 95 && _timeTotalCounter < 96)
                    _doGest = 0;
                else if (_timeTotalCounter > 96 && _timeTotalCounter < 400)
                    { _worldPosIncrement++; _doGest = 1; }
            }

            const int HeroBottomMargin = 22; // Hero-spritens Y-avstånd från skärmens nederkant
            DrawHero(_worldPosIncrement, context.ScreenHeight - HeroBottomMargin);
        }

        private void AnimateNearPerfect(float elapsed, GameContext context)
        {
            float speed1 = _drawHeroEnd ? 0.3f : 0.1f;
            float speed2 = _drawHeroEnd ? 0.6f : 0.2f;
            float speed3 = _drawHeroEnd ? 0.9f : 0.4f;

            _time += elapsed;
            if (_time <= speed1)      _graphicCounter = 1;
            else if (_time <= speed2) _graphicCounter = 2;
            else if (_time <= speed3) _graphicCounter = 3;
            else
            {
                _graphicCounter = 4;
                _time = 0f;
                if (_timeTotalCounter < 1000) _timeTotalCounter++;

                if (_timeTotalCounter < 65)
                    { _worldPosIncrement++; _doGest = 1; }
                else if (_timeTotalCounter > 65 && _timeTotalCounter < 67)
                    _doGest = 0;
                else if (_timeTotalCounter > 67 && _timeTotalCounter < 75)
                    _doGest = 2;
                else if (_timeTotalCounter > 75 && _timeTotalCounter < 76)
                    _doGest = 0;
                else if (_timeTotalCounter > 76 && _timeTotalCounter < 80)
                    { _worldPosIncrement++; _doGest = 1; }
                else if (_timeTotalCounter > 80 && _timeTotalCounter < 149)
                    { _worldPosIncrement--; _doGest = 1; }

                if (_timeTotalCounter > 149 && _drawHeroEnd)
                {
                    _drawHeroEnd = false;
                    _timeTotalCounter = 0;
                }
            }

            const int HeroBottomMargin      = 22; // Hero-spritens Y-avstånd från skärmens nederkant
            const int IglooPortBottomMargin = 40; // Igloo-portens Y-avstånd från skärmens nederkant
            const int IglooPortCenterOffset =  9; // Igloo-portens X-offset från skärmens mitt
            const int IglooBodyBottomMargin = 81; // Igloo-kropp Y-avstånd från skärmens nederkant
            const int IglooBodyCenterOffset = 18; // Igloo-kropp X-offset från skärmens mitt

            int sw = context.ScreenWidth;
            int sh = context.ScreenHeight;
            if (_drawHeroEnd)
            {
                DrawHero(_worldPosIncrement, sh - HeroBottomMargin);
            }
            else
            {
                int iglooPos = _timeTotalCounter < 120 ? 120 - _timeTotalCounter : 0;
                _rc.DrawPartialSprite(SpriteId.EndArt, sw / 2 + IglooPortCenterOffset + iglooPos, sh - IglooPortBottomMargin, 0,  0,  24,  34);
                _rc.DrawPartialSprite(SpriteId.EndArt, sw / 2 + IglooBodyCenterOffset + iglooPos, sh - IglooBodyBottomMargin, 48, 0, 160, 160);
            }
        }

        private void AnimatePerfect(float elapsed, GameContext context)
        {
            _time += elapsed;
            if (_time <= 0.3f)       _graphicCounter = 1;
            else if (_time <= 0.6f)  _graphicCounter = 2;
            else if (_time <= 0.9f)  _graphicCounter = 3;
            else
            {
                _graphicCounter = 4;
                _time = 0f;
                if (_timeTotalCounter < 1000) _timeTotalCounter++;

                if (_timeTotalCounter < 129)
                    { _worldPosIncrement++; _worldPosIncrementIgloo--; _doGest = 1; }
                else if (_timeTotalCounter >= 129 && _timeTotalCounter < 138)
                    _doGest = 2;
                else if (_timeTotalCounter >= 138 && _timeTotalCounter < 179)
                    { _worldPosIncrement++; _worldPosIncrementIgloo--; _doGest = 1; }
                else if (_timeTotalCounter >= 179 && _timeTotalCounter < 180)
                    _worldPosIncrementIgloo--;

                if (_timeTotalCounter > 179) _drawHeroEnd = false;
            }

            const int HeroBottomMargin      = 22;
            const int IglooPortBottomMargin = 40;
            const int IglooPortCenterOffset =  9;
            const int IglooBodyBottomMargin = 81;
            const int IglooBodyCenterOffset = 18;

            int sw = context.ScreenWidth;
            int sh = context.ScreenHeight;
            int iglooPos = _worldPosIncrementIgloo;

            // Igloo-port (alltid synlig)
            _rc.DrawPartialSprite(SpriteId.EndArt, sw / 2 + IglooPortCenterOffset + iglooPos, sh - IglooPortBottomMargin, 0,  0,  24,  34);

            // Hero (döljs när den går in i iglooporten)
            if (_drawHeroEnd)
                DrawHero(_worldPosIncrement, sh - HeroBottomMargin);

            // Igloo
            _rc.DrawPartialSprite(SpriteId.EndArt, sw / 2 + IglooBodyCenterOffset + iglooPos, sh - IglooBodyBottomMargin, 48, 0, 160, 160);
        }

        /// <summary>Ritar hero-spriten med rätt gesture-frame vid angiven skärmposition.</summary>
        private void DrawHero(int worldX, int worldY)
        {
            int sx, sy;
            if (_doGest == 1)       { sx = 16 * _graphicCounter; sy = 0; }      // gå
            else if (_doGest == 2)  { sx = 16 * 2;               sy = 16 * 2; } // titta upp
            else                    { sx = 0;                     sy = 0; }      // idle

            _rc.DrawPartialSprite(SpriteId.Hero, worldX, worldY, sx, sy, 16, 16);
        }

        // ── Skriv-animerad sluttext (ekvivalent med Program.DrawEndTextList) ──────
        private void DrawEndTextList(float elapsed, int cx, int cy)
        {
            var template = BuildEndTextTemplate();

            // Lägg till ett tecken per typingSpeed sekunder
            _incrementerDisplayEnd += elapsed;
            float typingSpeed = _typingEndTextIsDone ? 1.0f : 0.2f;
            bool addLetter = false;
            if (_incrementerDisplayEnd >= typingSpeed)
            {
                _incrementerDisplayEnd = 0f;
                addLetter = true;
            }

            if (addLetter && !_typingEndTextIsDone)
                AdvanceTyping(template);

            // Rita max 11 rader av den hittills uppbyggda texten
            int jumpTo = _listEndText.Count > 11 ? _listEndText.Count - 11 : 0;
            int rowNumberActual = 0;
            for (int row = 0; row < _listEndText.Count; row++)
            {
                if (row < jumpTo) continue;

                string text = _listEndText[row];
                if (text.StartsWith(" ")) text = text.Substring(1);
                const int TextMargin = 4; // Marginal från splash-sprites kant till sluttextens start
                _rc.DrawText(text, cx + TextMargin, cy + TextMargin + rowNumberActual * 10);
                rowNumberActual++;
            }
        }

        /// <summary>Bygger sluttextlistan för aktuell sluttyp.</summary>
        private List<string> BuildEndTextTemplate()
        {
            if (_typeOfEnding == Enum.TypeOfEnding.Perfect)
            {
                return new List<string>
                {
                    "So I came with weirdos.",
                    "Bring a new one.",
                    "Let's put the old one",
                    "Your hand.",
                    "You don't know what",
                    " he's going to do",
                    "In your eyes.",
                    "Oakwood desert sand",
                    "pcs",
                    "Legs curved",
                    "Mountains.",
                    "Give your way",
                    "A card with your ",
                    "fingertips and then",
                    "And when you go",
                    "Line on the palm.",
                    "Let's take you deep",
                    " into the snow",
                    "breathe",
                    "And your spirit",
                    "To enlarge the ice.",
                    "In your mouth",
                    "New shape.",
                    "I eat?",
                    "You don't eat",
                    "Spring aliens",
                    "The river is your navel.",
                    "Life at home;",
                    "There is no house.",
                    "Walk carefully, dear;",
                    "With all my heart I am;",
                    "And fear not thy beloved, ",
                    "thy God, ",
                    "and walk not in the way.",
                    "Back behind us;",
                    "Please always go home.",
                    "Press any button -to exit"
                };
            }
            else if (_typeOfEnding == Enum.TypeOfEnding.NerePerfect)
            {
                return new List<string>
                {
                    "In the valley",
                    "You see the blue of the hill,",
                    "And the blue area on the left",
                    "Right and rainbow",
                    "The spring becomes a",
                    "After the rain",
                    "And maybe say",
                    "\"That's the way it is,\"",
                    "But tell me.",
                    "\"A little more.\"",
                    "I hope you go",
                    "You see the roof",
                    "In a small town",
                    "The mountain is wild and yellow",
                    "oatmeal",
                    "Nosuri is floating",
                    "The woman is singing",
                    "The shade dries",
                    "Time and maybe say",
                    "\"Stop, this is it!\"",
                    "However, he said:",
                    "\"A little more.\"",
                    "continue consultation",
                    "Uzura",
                    "Next to the fountain;",
                    "Valley,",
                    "after thinking",
                    "The bottom of the river",
                    "Natural mountains behind",
                    "Below we tell you",
                    "\"Isn't that a dollar!\"",
                    "And I can't do all the things",
                    "that",
                    "Drink this spring water",
                    "Please rest for a moment\"",
                    "However, this is a long way to",
                    "go,",
                    "And I can't be sure he is.",
                    "Press any button -to exit"
                };
            }
            else
            {
                // Enum.TypeOfEnding.Done
                return new List<string>
                {
                    "Congratulations!",
                    "You beat the game.",
                    "Thank you so much for",
                    "trying to help Scarlett",
                    "find her way back home and",
                    "playing Penguin After All.",
                    "This game was built on the",
                    "olcPixelGameEngine",
                    "created by Javidx9",
                    "and then ported to C# by",
                    "DevChrome.",
                    "The music was created by",
                    "Fiskifickorna.",
                    "And the guy who put it",
                    "all together goes by",
                    "the name",
                    "FuryNacho.",
                    "We are penguins.. after all.",
                    "Much in common after all.",
                    "Thank you for playing",
                    "Penguin After All",
                    "Press any button -to exit"
                };
            }
        }

        /// <summary>Avancerar typing-animationen ett steg — lägger till ett tecken eller hoppar en rad.</summary>
        private void AdvanceTyping(List<string> template)
        {
            int rowIndex = 0;
            foreach (string textRow in template)
            {
                rowIndex++;
                if (_listEndText.Count < rowIndex) _listEndText.Add("");

                if (_listEndText[rowIndex - 1].Length == textRow.Length)
                    continue; // Raden är klar

                if (template.Count == rowIndex)
                {
                    // Sista raden — kasta in hela raden direkt
                    _listEndText[rowIndex - 1] = textRow;
                    break;
                }
                else if (_skippTypingRow)
                {
                    // Hoppa direkt till nästa rad
                    _listEndText[rowIndex - 1] = textRow;
                    _skippTypingRow = false;
                    _services.Input.ButtonsHasGoneIdle = false;
                    break;
                }
                else
                {
                    // Lägg till ett tecken i taget
                    char next = textRow[_listEndText[rowIndex - 1].Length];
                    _listEndText[rowIndex - 1] += next;
                    break;
                }
            }

            try
            {
                if (_listEndText.Count == template.Count && _listEndText.Count > 0)
                {
                    if (_listEndText[_listEndText.Count - 1].Length ==
                        template[template.Count - 1].Length)
                    {
                        _typingEndTextIsDone = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EndState.AdvanceTyping " + ex.ToString());
            }
        }

        // ── Snöeffekt (samma mönster som SplashState.MakeItSnow) ─────────────────
        private void MakeItSnow(float elapsed, int height)
        {
            if (_snowSlow == null || _snowFast == null ||
                _snowSlow.arrayList.Count <= height - 1)
            {
                _snowSlow = new Aggregate.MakeItSnow(1, 400, height);
                _snowFast = new Aggregate.MakeItSnow(1, 400, height);
            }

            _incSlow += elapsed;
            if (_incSlow >= 0.5f)
            {
                _incSlow = 0f;
                _counterSlow = _counterSlow < height ? _counterSlow + 1 : 1;
            }

            _incFast += elapsed;
            if (_incFast >= 0.2f)
            {
                _incFast = 0f;
                _counterFast = _counterFast < height ? _counterFast + 1 : 1;
            }

            for (int i = height; i > 0; i--)
            {
                int absI = System.Math.Abs(_counterSlow - i);
                if (absI < 1) absI = 1;
                if (absI > height - 1) absI = height - 1;
                foreach (var x in _snowSlow!.arrayList[absI])
                    _rc.DrawPixel(x, i, RenderColor.White);
            }
            for (int i = height; i > 0; i--)
            {
                int absI = System.Math.Abs(_counterFast - i);
                if (absI < 1) absI = 1;
                if (absI > height - 1) absI = height - 1;
                foreach (var x in _snowFast!.arrayList[absI])
                    _rc.DrawPixel(x, i, RenderColor.White);
            }
        }
    }
}
