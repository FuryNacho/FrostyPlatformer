
namespace FrostyPlatformer
{
    public class Enum
    {
        public enum DevState
        {
            None,
            GoToLastStage,
            GoToEnding, // #1
            GoToEndingDone,// #1
            GoToEndingNearPrefect,
            GoToEndingPertect
        };

        public enum TypeOfEnding
        {
            None,
            Done,
            NerePerfect,
            Perfect
        }

        public enum State
        {
            SplashScreen,
            Menu,
            WorldMap,
            GameMap,
            Pause,
            Settings,
            GameOver,
            End,
            HighScore,
            EnterHighScore
        };

        public enum MenuState
        {
            None,
            StartMenu,
            PauseMenu,
            SettingsMenu,
            Audio,
            ClearHighScore,
            ClearSavedGame,
            Load,
            Save,
            CreditsMenu

            /*"Start New Game",
"Load Saved Game",
"View High Score",
"Settings",
"Exit",*/
            /*"Resume",
            "Save",
            "Exit"*/
        }

        public enum Direction
        {
            SOUTH = 0,
            WEST = 1,
            NORTH = 2,
            EAST = 3
        }

        public enum GraphicsState
        {
            Standing,
            Walking,
            Celebrating,
            Dead,
            Falling,
            Jumping,
            TakingDamage
        }


        public enum PlayerOrientation
        {
            Right = 0,
            Left = 1
        }

        public enum Actions
        {
            None = 0,
            Right = 1,
            Left = 2,
            jump = 3
        }

        public enum NATURE
        {
            TALK,
            WALK
        }

        public enum LastStage
        {
            StayDown = 3,
            MovingUp = 4,
            StayUp = 0,
            MovingDown = 1,
        }

        public enum StageStatus
        {
            NotPassed = 0,
            Current = 1,
            Passed = 2,
        }

    }
  
}
