using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyPlatformer.Global.GlobalNamespace
{
    //static class StaticRes
    //{ }
    public static class SoundRef
    {
        //public static string Jump { get { return "jump_01.wav"; } } // jump_01.wav
        //public static string Land { get { return "jump_02.wav"; } } // jump_02.wav
        //public static string Damage { get { return "jumpland.wav"; } } // jumpland.wav
        //public static string DamageHero { get { return "hit.wav"; } } // hit
        //public static string PickUp { get { return "coin.wav"; } } // key01.wav // coin01.wav
        public static string Jump { get { return "PAA hopp.wav"; } } // jump_01.wav
        public static string Land { get { return "PAA landa.wav"; } } // jump_02.wav
        public static string Damage { get { return "PAA hoppa pa krak.wav"; } } // jumpland.wav
        public static string DamageHero { get { return "PAA traffljud.wav"; } } // hit.wav
        public static string PickUp { get { return "PAA PLOCKA UPP.wav"; } } // key01.wav // coin01.wav

        public static string BGSoundWorld { get { return "uno.wav"; } } 
        public static string BGSoundGame { get { return "theone.wav"; } }

        public static string BGSoundFinalStage { get { return "bossong.wav"; } }
        public static string BGSoundEnd { get { return "theend.wav"; } } 

        public static string BGNearPerfectEnd { get { return "finalend.wav"; } }

        public static string BGPerfectEnd  { get { return "Caveman.wav"; }
}
    }

    public static class SplashScreenRef
    {
        public static string Start { get { return "splashstart"; } }
        public static string End { get { return "splashend"; } }
    }



}
