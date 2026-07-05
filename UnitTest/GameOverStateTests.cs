#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Core;
using FrostyPlatformer.States;
using FrostyPlatformer.Global.GlobalNamespace;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Regression: game over-skärmen ska tysta bakgrundsmusiken. Tidigare transitionade
    /// döds-övergången i GameplayState hit utan att stoppa musiken, så boss-temat (akt 1-3),
    /// acceptans-temat (akt 4) eller banmusiken fortsatte spela på game over-skärmen.
    /// </summary>
    [TestClass]
    public class GameOverStateTests
    {
        // Bygger ett GameServices där bara Audio + RenderContext är riktiga (fejkar). GameOverState.Enter
        // rör bara Audio och context.Player, så resten är null!. Namngivna argument gör anropet robust
        // mot omordning av den 27-parameters-konstruktorn.
        private static GameServices Services(FakeAudioSystem audio) => new GameServices(
            input: null!, camera: null!, tileRenderer: null!, renderContext: new FakeRenderContext(),
            stateManager: null!, audio: audio, score: null!, script: null!,
            settings: null!, assets: null!, gameMaps: null!, userMaps: null!,
            dialog: null!, quests: null!, items: null!, worldMap: null!,
            saveLoad: null!, parallax: null!, userMapScores: null!,
            changeMap: null!, reset: null!, exitGame: null!, setScreenMode: null!,
            checkAndClearSwitchedState: null!, clearSwitchedState: null!,
            triggerBossCheck: null!, getBossObjectX: null!);

        [TestMethod]
        public void Enter_StopsBossMusic_Acts1To3()
        {
            var audio = new FakeAudioSystem();
            audio.Play(SoundRef.BGSoundFinalStage);   // boss-temat spelar när hjälten dör

            new GameOverState(Services(audio)).Enter(new GameContext());

            Assert.IsFalse(audio.IsPlaying(SoundRef.BGSoundFinalStage),
                "Boss-temat (akt 1-3) ska tystas på game over-skärmen.");
        }

        [TestMethod]
        public void Enter_StopsAcceptanceMusic_Act4()
        {
            var audio = new FakeAudioSystem();
            audio.Play(SoundRef.BGSoundAcceptance);       // akt 4-temat (loop)
            audio.Play(SoundRef.BGSoundAcceptanceIntro);  // akt 4-upptakten

            new GameOverState(Services(audio)).Enter(new GameContext());

            Assert.IsFalse(audio.IsPlaying(SoundRef.BGSoundAcceptance),
                "Acceptans-temat (akt 4) ska tystas på game over-skärmen.");
            Assert.IsFalse(audio.IsPlaying(SoundRef.BGSoundAcceptanceIntro),
                "Acceptans-upptakten (akt 4) ska tystas på game over-skärmen.");
        }

        [TestMethod]
        public void Enter_StopsLevelMusic()
        {
            var audio = new FakeAudioSystem();
            audio.Play(SoundRef.BGSoundGame);   // vanlig banmusik (död utanför bossen)

            new GameOverState(Services(audio)).Enter(new GameContext());

            Assert.IsFalse(audio.IsPlaying(SoundRef.BGSoundGame),
                "Banmusiken ska tystas på game over-skärmen.");
        }

        [TestMethod]
        public void Enter_LeavesHitSfxPlaying()
        {
            // Träffljudet (döds-stöten) ska få klinga ut — vi tystar bara musik, inte StopAll.
            var audio = new FakeAudioSystem();
            audio.Play(SoundRef.BGSoundFinalStage);
            audio.Play(SoundRef.DamageHero);

            new GameOverState(Services(audio)).Enter(new GameContext());

            Assert.IsTrue(audio.IsPlaying(SoundRef.DamageHero),
                "Träff-SFX ska inte tystas — bara bakgrundsmusiken stoppas.");
        }
    }
}
