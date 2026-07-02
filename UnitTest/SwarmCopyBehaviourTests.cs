#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.States;
using FrostyPlatformer.Global;

namespace UnitTest
{
    /// <summary>
    /// Beteendetester för akt 2-svärmens anti-klump-logik. Behaviour sätter bara hastighet
    /// (ingen fysik körs här), så vi verifierar BESLUTEN deterministiskt: vx-tecknet visar
    /// rörelseriktning, vy &lt; 0 betyder hopp/studs uppåt.
    /// </summary>
    [TestClass]
    public class SwarmCopyBehaviourTests
    {
        private const float Dt = 1f / 60f;

        // ── Reträtt efter att ha gett skada ────────────────────────────────────────
        [TestMethod]
        public void OnDealtDamage_RetreatsAwayFromHero()
        {
            // Hjälten till höger → kopian ska backa åt VÄNSTER (bort), inte fortsätta jaga in i högen.
            var copy = new DynamicCreatureSwarmCopy { px = 5f, py = 12f, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 10f, py = 12f };

            copy.OnDealtDamage(hero.px);
            copy.Behaviour(Dt, hero);

            Assert.IsTrue(copy.vx < -0.1f, $"Ska backa bort från hjälten (vx={copy.vx}).");
            Assert.IsTrue(copy.vy < 0f, $"Ska studsa upp för att separera vertikalt (vy={copy.vy}).");
        }

        [TestMethod]
        public void Retreat_Expires_ResumesChase()
        {
            var copy = new DynamicCreatureSwarmCopy { px = 5f, py = 12f, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 10f, py = 12f };

            copy.OnDealtDamage(hero.px);
            // Reträtten är ~0.6 s → kör väl förbi den.
            for (int i = 0; i < 60; i++) copy.Behaviour(Dt, hero);

            Assert.IsTrue(copy.vx > 0.1f, $"Efter reträtten ska kopian jaga hjälten igen (vx={copy.vx}).");
        }

        // ── Av-stapling (hoppa av en kopia man landat på) ───────────────────────────
        [TestMethod]
        public void OnStackedOn_HopsSidewaysAndUp()
        {
            // Kopian (px=10) landar ovanpå en annan strax till vänster (px=9) → hoppa åt HÖGER + upp.
            var copy = new DynamicCreatureSwarmCopy { px = 10f, py = 11f, Grounded = false };

            copy.OnStackedOn(otherX: 9f);

            Assert.IsTrue(copy.vy < 0f, $"Ska hoppa upp av högen (vy={copy.vy}).");
            Assert.IsTrue(copy.vx > 0.1f, $"Ska skjuta sidledes bort från kopian under (vx={copy.vx}).");
        }

        [TestMethod]
        public void OnStackedOn_IsCooldownGated()
        {
            var copy = new DynamicCreatureSwarmCopy { px = 10f, py = 11f };

            copy.OnStackedOn(otherX: 9f);   // första hoppet — sätter cooldown
            copy.vy = 0f;                   // nollställ för att se om ett andra hopp slår igenom
            copy.OnStackedOn(otherX: 9f);   // direkt igen, inom cooldown → ska ignoreras

            Assert.AreEqual(0f, copy.vy, 1e-6f, "Ett andra av-stapel-hopp inom cooldown ska inte ge ny impuls.");
        }

        [TestMethod]
        public void Separate_OverridesChaseWhileActive()
        {
            // Av-stapling skjuter åt höger (bort från kopia till vänster), men hjälten står till
            // VÄNSTER. Under separations-fönstret ska sidledsfarten vinna över jakten.
            var copy = new DynamicCreatureSwarmCopy { px = 10f, py = 12f, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 2f, py = 12f };

            copy.OnStackedOn(otherX: 9f);
            copy.Behaviour(Dt, hero);

            Assert.IsTrue(copy.vx > 0.1f,
                $"Under av-stapling ska kopian fortsätta bort från högen, inte jaga hjälten åt vänster (vx={copy.vx}).");
        }

        // ── Normalfall: utan störning jagar kopian fortfarande hjälten ──────────────
        [TestMethod]
        public void Undisturbed_ChasesHero()
        {
            var copy = new DynamicCreatureSwarmCopy { px = 5f, py = 12f, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 10f, py = 12f };

            copy.Behaviour(Dt, hero);

            Assert.IsTrue(copy.vx > 0.1f, $"Ostörd ska kopian jaga hjälten (vx={copy.vx}).");
        }

        // ── Exit: fall → idle-glitch på marken → poff (akt 2→3-övergången) ──────────
        [TestMethod]
        public void BeginExit_DisarmsImmediately()
        {
            var copy = new DynamicCreatureSwarmCopy { px = 5f, py = 12f, Grounded = true };

            copy.BeginExit();

            Assert.IsTrue(copy.GlitchExitInProgress, "Ska vara i exit-läget.");
            Assert.IsFalse(copy.IsAttackable, "Ska vara ofarlig/icke-stampbar direkt.");
            Assert.IsFalse(copy.SolidVsDynamic, "Ska inte längre kollidera/skada direkt.");
        }

        [TestMethod]
        public void Exit_DoesNotGlitchWhileAirborne()
        {
            // I luften ska kopian behålla sin (inkastade) rörelse och INTE börja glitcha.
            var copy = new DynamicCreatureSwarmCopy { px = 5f, py = 4f, vx = 6f, Grounded = false };
            var hero = new DynamicCreatureEnemyPenguin { px = 30f, py = 12f };

            copy.BeginExit();
            for (int i = 0; i < 30; i++) copy.Behaviour(Dt, hero);   // ~0.5 s i luften

            Assert.IsFalse(copy.IsGlitchingOut, "Får inte glitcha medan den fortfarande är i luften.");
            Assert.AreEqual(6f, copy.vx, 1e-6f, "Sidofarten (bågen) ska behållas i luften, inte nollas.");
        }

        [TestMethod]
        public void Exit_StartsGlitchingAfterLandingDelay()
        {
            var copy = new DynamicCreatureSwarmCopy { px = 5f, py = 12f, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 30f, py = 12f };

            copy.BeginExit();
            for (int i = 0; i < 60; i++) copy.Behaviour(Dt, hero);   // ~1 s > max landnings-jitter

            Assert.IsTrue(copy.IsGlitchingOut, "På marken efter fördröjningen ska den glitcha (poffbar).");
            Assert.AreEqual(0f, copy.vx, 1e-6f, "På marken står den stilla (vx=0).");
            Assert.IsFalse(copy.Redundant, "Glitchar idle — poppar INTE själv, väntar på Poff().");
        }

        [TestMethod]
        public void Poff_RemovesCopy()
        {
            var copy = new DynamicCreatureSwarmCopy { px = 5f, py = 12f, Grounded = true };

            copy.BeginExit();
            copy.Poff();

            Assert.IsTrue(copy.Redundant, "Poff ska markera kopian för borttagning.");
            Assert.IsFalse(copy.GlitchExitInProgress, "Efter poff är exiten över.");
        }
    }

    /// <summary>
    /// Tester för svärmens crescendo-trappa (akt 2): antalet samtidiga kopior fördubblas mot slutet.
    /// Ren funktion av boss-barens hälsa → deterministiskt utan motor.
    /// </summary>
    [TestClass]
    public class SwarmCrescendoTests
    {
        // SwarmStompDamage = 4, basantal = 4. Tröskel: ≤8 (2 stamp kvar) → ×2, ≤4 (1 stamp kvar) → ×4.
        [TestMethod]
        [DataRow(24, 4)]   // full bar
        [DataRow(12, 4)]   // 3 stamp kvar
        [DataRow(9,  4)]   // strax över tröskeln
        [DataRow(8,  8)]   // 2 stamp kvar → fördubbla
        [DataRow(5,  8)]   // fortf. 2 stamp-zonen
        [DataRow(4, 16)]   // 1 stamp kvar → fördubbla igen
        [DataRow(1, 16)]   // sista stampet på väg
        public void CurrentSwarmTarget_DoublesTowardTheEnd(int bossHealth, int expected)
            => Assert.AreEqual(expected, GameplayState.CurrentSwarmTarget(bossHealth));
    }

    /// <summary>
    /// Tester för akt 3:s istapps-skur per näv-nedslag: antalet (intervall) ökar med skadan som getts
    /// jätten. Ren funktion av hälsa/maxhälsa → deterministiskt.
    /// </summary>
    [TestClass]
    public class SlamBurstRangeTests
    {
        // giantHealth = 40. dealt = 1 - health/max. Trösklar: ≥1/3 → 2–3, ≥2/3 → 2–4, annars 1–2.
        [TestMethod]
        [DataRow(40, 1, 2)]   // 0 skada given
        [DataRow(32, 1, 2)]   // 20% given
        [DataRow(27, 1, 2)]   // strax under 1/3 given (dealt 0.325)
        [DataRow(26, 2, 3)]   // ≥1/3 given (dealt 0.35)
        [DataRow(16, 2, 3)]   // 60% given
        [DataRow(14, 2, 3)]   // strax under 2/3 given (dealt 0.65)
        [DataRow(13, 2, 4)]   // ≥2/3 given (dealt 0.675)
        [DataRow(8,  2, 4)]   // sista stompen
        public void SlamBurstRange_GrowsWithDamageDealt(int bossHealth, int expectedLo, int expectedHi)
        {
            var (lo, hi) = GameplayState.SlamBurstRange(bossHealth, bossMaxHealth: 40);
            Assert.AreEqual(expectedLo, lo, "min-antal");
            Assert.AreEqual(expectedHi, hi, "max-antal");
        }
    }

    /// <summary>
    /// Tester för akt 3:s näv-liggtid (Stuck): kortare ju mindre hälsa bossen har kvar — tre steg
    /// (översta tredjedelen 3s, mitten 1.5s, sista 1s). Ren funktion av hälsa/maxhälsa → deterministisk.
    /// </summary>
    [TestClass]
    public class StuckSecondsForHealthTests
    {
        // giantHealth = 40. Trösklar på KVARVARANDE hälsa: > 2/3 → 3s, > 1/3 → 1.5s, annars 1s.
        [TestMethod]
        [DataRow(40, 3.0f)]   // 3/3 kvar
        [DataRow(32, 3.0f)]   // 80% kvar
        [DataRow(27, 3.0f)]   // strax över 2/3 (0.675)
        [DataRow(26, 1.5f)]   // strax under 2/3 (0.65)
        [DataRow(24, 1.5f)]   // 60% kvar
        [DataRow(16, 1.5f)]   // 40% kvar
        [DataRow(14, 1.5f)]   // strax över 1/3 (0.35)
        [DataRow(13, 1.0f)]   // strax under 1/3 (0.325)
        [DataRow(8,  1.0f)]   // sista tredjedelen
        public void StuckSeconds_ShortensWithLostHealth(int bossHealth, float expected)
        {
            Assert.AreEqual(expected, GameplayState.StuckSecondsForHealth(bossHealth, bossMaxHealth: 40), 0.001f);
        }
    }
}
