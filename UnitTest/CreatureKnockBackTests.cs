#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Rendering;

namespace UnitTest
{
    /// <summary>
    /// Regression för "spök"-buggen: <see cref="Creature.KnockBack"/> nollar SolidVsDynamic, men
    /// tidigare återställdes den aldrig när knockbacken tog slut. Knuffade fiender blev då permanenta
    /// icke-solida spöken som hjälten gick rakt igenom (mest märkbart i akt 2-svärmen, där hjälten
    /// ofta hoppar upp i kopior underifrån → kontaktskade-knockback). Update ska återställa soliditeten
    /// när timern löper ut, precis som Controllable/IsAttackable — men bara för levande fiender.
    ///
    /// Testet kör basklassens knockback-livscykel direkt via en minimal Creature utan eget Behaviour,
    /// så kontraktet verifieras generellt (alla fiender), inte bara för en specifik subklass.
    /// </summary>
    [TestClass]
    public class CreatureKnockBackTests
    {
        private const float Dt = 1f / 60f;

        // Minimal Creature utan eget Behaviour → isolerar basklassens knockback-logik.
        private sealed class TestCreature : Creature
        {
            public TestCreature() : base("test", SpriteId.EnemySwarmCopy) { }
            public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null) { }
        }

        [TestMethod]
        public void KnockBack_ClearsSolidityWhileActive()
        {
            var c = new TestCreature { Health = 5 };
            Assert.IsTrue(c.SolidVsDynamic, "Utgångsläge: solid.");

            c.KnockBack(1f, -1f, 0.3f);

            Assert.IsFalse(c.SolidVsDynamic, "Under knockback ska fienden vara icke-solid.");
        }

        [TestMethod]
        public void KnockBack_RestoresSolidityAfterExpiry_WhenAlive()
        {
            var c = new TestCreature { Health = 5 };

            c.KnockBack(1f, -1f, 0.3f);
            for (int i = 0; i < 30; i++) c.Update(Dt);   // ~0.5 s > 0.3 s knockback

            Assert.IsTrue(c.SolidVsDynamic,
                "Efter knockback ska en LEVANDE fiende bli solid igen (annars: spöke man går igenom).");
            Assert.IsTrue(c.IsAttackable, "IsAttackable ska också ha återställts efter knockback.");
        }

        [TestMethod]
        public void KnockBack_KeepsNonSolid_WhenKnockedToDeath()
        {
            var c = new TestCreature { Health = 0 };

            c.KnockBack(1f, -1f, 0.3f);
            for (int i = 0; i < 30; i++) c.Update(Dt);

            Assert.IsFalse(c.SolidVsDynamic,
                "En fiende som knuffats till döds (Health<=0) ska förbli icke-solid.");
        }
    }
}
