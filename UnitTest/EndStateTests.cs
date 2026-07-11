using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer;
using FrostyPlatformer.Core;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.States;

namespace UnitTest
{
    /// <summary>
    /// Tester för EndState.DetermineEnding — slutvillkoret som avgör Perfect/NerePerfect/Done.
    ///
    /// Låser regressionen som fanns innan: villkoret jämförde CollectedEnergiIds.Count == 100,
    /// men det finns bara TotalEnergiCount (93) insamlingsbara energier — så Perfect/NerePerfect
    /// var i praktiken oåtkomliga. Nu räknas StartingEnergi (7) + insamlade, vilket för en full
    /// runda landar på MaxEnergi (100).
    /// </summary>
    [TestClass]
    public class EndStateTests
    {
        /// <summary>Bygger en kontext med angivet antal insamlade energier och angiven hälsa.</summary>
        private static GameContext MakeContext(int collectedCount, int health)
        {
            var ctx = new GameContext { Player = new DynamicCreatureHero() };
            ctx.Player.Health = health;
            for (int id = 1; id <= collectedCount; id++)
                ctx.CollectedEnergiIds.Add(id);
            return ctx;
        }

        [TestMethod]
        public void FullRun_FullHealth_IsPerfect()
        {
            // Alla insamlingsbara energier + fullt liv → Perfect
            var ctx = MakeContext(GameConstants.TotalEnergiCount, GameConstants.PerfectEndingHealth);

            Assert.AreEqual(Enum.TypeOfEnding.Perfect, EndState.DetermineEnding(ctx));
        }

        [TestMethod]
        public void FullRun_DamagedHealth_IsNearPerfect()
        {
            // Alla energier men tappat liv i kollision med fiende → NerePerfect
            var ctx = MakeContext(GameConstants.TotalEnergiCount, GameConstants.PerfectEndingHealth - 5);

            Assert.AreEqual(Enum.TypeOfEnding.NerePerfect, EndState.DetermineEnding(ctx));
        }

        [TestMethod]
        public void MissingOneEnergi_IsDone_EvenWithFullHealth()
        {
            // En energi kvar oplockad → Done, oavsett hälsa
            var ctx = MakeContext(GameConstants.TotalEnergiCount - 1, GameConstants.PerfectEndingHealth);

            Assert.AreEqual(Enum.TypeOfEnding.Done, EndState.DetermineEnding(ctx));
        }

        [TestMethod]
        public void NoEnergi_IsDone()
        {
            var ctx = MakeContext(0, GameConstants.StartingEnergi);

            Assert.AreEqual(Enum.TypeOfEnding.Done, EndState.DetermineEnding(ctx));
        }
    }
}
