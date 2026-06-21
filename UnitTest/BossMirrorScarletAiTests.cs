#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.Models.Objects;
using FrostyPlatformer.Systems;
using FrostyPlatformer.Global;

namespace UnitTest
{
    /// <summary>
    /// Beteendetester för spegel-Scarlets akt 1-AI (klättra/descend/attack). Behaviour är
    /// deterministisk givet Arena + spelarposition, så vi kan verifiera besluten utan motor:
    /// sätter vi vy &lt; 0 ett givet läge betyder det att hon HOPPAR; vx-tecknet visar rörelseriktning.
    /// </summary>
    [TestClass]
    public class BossMirrorScarletAiTests
    {
        /// <summary>IMapData där solida rutor styrs av en predikatfunktion.</summary>
        private sealed class GridMap : IMapData
        {
            private readonly Func<int, int, bool> _solid;
            public GridMap(Func<int, int, bool> solid) { _solid = solid; }
            public int Width => 40;
            public int Height => 20;
            public int GetIndex(int x, int y) => 0;
            public bool GetSolid(int x, int y) => _solid(x, y);
        }

        // Platå (solid) på rad 5, kol 10–13. Golv på rad 13–14 överallt.
        private static GridMap PlatformMap() =>
            new GridMap((x, y) => (y == 5 && x >= 10 && x <= 13) || (y >= 13 && y <= 14));

        // ── Fysik-simulering ──────────────────────────────────────────────────────
        // Kör SAMMA loop-ordning som spelets UpdateObject (gravitation → clamp → flytta →
        // horisontell kollision + OnWallCollision → vertikal kollision → Behaviour) så vi kan
        // verifiera att bossen FAKTISKT tar sig upp/ner, inte bara att enskilda frames ser rätt ut.
        // Steg = exakt spelets per-objekt-loop (GameplayState.UpdateObject), men med spelets RIKTIGA
        // PhysicsSystem + CollisionSystem i stället för en approximation. Då fångar simmen även
        // sub-pixel-/marginal-beteenden (t.ex. kant-vinglet) trovärdigt.
        private static void Step(DynamicCreatureMirrorScarlet boss, DynamicGameObject hero, IMapData map, float dt)
        {
            float fBorder = GameConstants.CollisionBorderPrecision;
            float rjc = 0f;
            PhysicsSystem.ApplyGravity(boss, isHero: false, bPower: false, ref rjc, dt);
            PhysicsSystem.ClampVelocities(boss);

            float newX = boss.px + boss.vx * dt;
            float newY = boss.py + boss.vy * dt;

            // Horisontell kollision + OnWallCollision (som UpdateObject).
            bool turnPatrol = false;
            bool movingLeft = boss.vx <= 0f;
            var (adjX, hitWall) = CollisionSystem.ResolveHorizontal(boss.py, newX, boss.vx, fBorder, map);
            if (hitWall) { newX = adjX; boss.vx = 0f; turnPatrol = true; }
            boss.OnWallCollision(ref newX, turnPatrol, movingLeft, map, fBorder);

            boss.Grounded = false;

            // Vertikal kollision (tak vid stigning, mark vid fall).
            if (boss.vy <= 0f)
            {
                var (adjY, hitCeil, _) = CollisionSystem.ResolveVertical(newX, newY, boss.vy, map);
                if (hitCeil) { newY = adjY; boss.vy = 0f; }
            }
            else
            {
                var (adjY, _, grounded) = CollisionSystem.ResolveVertical(newX, newY, boss.vy, map);
                if (grounded) { newY = adjY; boss.vy = 0f; boss.Grounded = true; }
            }

            boss.px = newX; boss.py = newY;
            boss.Behaviour(dt, hero);
        }

        // ── Issue 1: hjälten UNDER bossen → hon ska ner, aldrig hoppa upp ──────────────
        [TestMethod]
        public void HeroBelow_NeverJumpsUp()
        {
            var map = PlatformMap();
            // Boss står på platån (rad 5 → py=4), hjälten rakt under på golvet.
            var boss = new DynamicCreatureMirrorScarlet { px = 11f, py = 4f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 11f, py = 12f };

            for (int i = 0; i < 180; i++)
            {
                boss.Behaviour(1f / 60f, hero);
                Assert.IsTrue(boss.vy >= 0f,
                    $"Hjälten är under → bossen ska ALDRIG hoppa upp (frame {i}, vy={boss.vy}).");
            }
        }

        [TestMethod]
        public void HeroBelow_CommitsToNearestEdge_NoFlip()
        {
            var map = PlatformMap();
            // Boss på kol 11 (platå 10–13). Närmaste kant är VÄNSTER (kol 9, 2 tiles) vs höger (kol 14, 3 tiles).
            // Hon ska commit:a åt vänster mot närmaste kant och ALDRIG flippa (det var vägglandet på stället).
            var boss = new DynamicCreatureMirrorScarlet { px = 11f, py = 4f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 13f, py = 12f };   // hjälten under till höger

            bool movedToNearestEdge = false;
            for (int i = 0; i < 180; i++)
            {
                boss.Behaviour(1f / 60f, hero);
                if (boss.vx < -0.1f) movedToNearestEdge = true;
                Assert.IsTrue(boss.vx <= 0.1f, $"Får inte flippa mot hjälten/andra hållet (frame {i}, vx={boss.vx}).");
            }
            Assert.IsTrue(movedToNearestEdge, "Bossen ska gå mot närmaste kant (vänster) för att kliva av.");
        }

        // ── Issue 2/3: klättra — hoppa bara när rätt positionerad, inte medan hon går fram ──
        [TestMethod]
        public void HeroAboveFarAway_DoesNotJump_WhileApproaching()
        {
            var map = PlatformMap();
            // Boss på golvet långt till höger om platån (kol 20). Platån (rad 5) är klättermålet.
            // Hjälten uppe på platån. Hon ska GÅ mot platån, inte hoppa förrän hon är vid kanten.
            var boss = new DynamicCreatureMirrorScarlet { px = 20f, py = 11f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 11f, py = 4f };

            for (int i = 0; i < 30; i++)
            {
                boss.Behaviour(1f / 60f, hero);
                Assert.IsTrue(boss.vy >= 0f,
                    $"Långt från platån ska hon GÅ, inte hoppa (frame {i}, vy={boss.vy}).");
            }
        }

        // ── Fysik-sim: hon ska FAKTISKT ta sig ner till en hjälte under ──────────────
        [TestMethod]
        public void Sim_HeroBelow_BossActuallyDescends()
        {
            var map = PlatformMap();
            // Boss på platån (rad 5 → py=4), hjälten på golvet (rad 13 → py=12) rakt under.
            var boss = new DynamicCreatureMirrorScarlet { px = 11f, py = 4f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 11f, py = 12f };

            // Spåra LÄGSTA punkt hon nått (störst py) — hon kan pounca på hjälten vid golvet när loopen
            // slutar, så slut-py är opålitlig; det vi vill veta är att hon faktiskt tog sig NER dit.
            float lowest = boss.py;
            for (int i = 0; i < 300; i++) { Step(boss, hero, map, 1f / 60f); if (boss.py > lowest) lowest = boss.py; }

            Assert.IsTrue(lowest >= 11.5f,
                $"Bossen ska ha tagit sig NER till golvnivån (py≈12) från platån, men kom som lägst till py={lowest}.");
        }

        // ── Fysik-sim: hon ska FAKTISKT ta sig upp på en platå mot en hjälte ovanför ──
        [TestMethod]
        public void Sim_HeroAbove_BossActuallyClimbs()
        {
            // Platå (solid) rad 11, kol 10–13. Golv rad 13–14. Hjälten uppe på platån.
            var map = new GridMap((x, y) => (y == 11 && x >= 10 && x <= 13) || (y >= 13 && y <= 14));
            var boss = new DynamicCreatureMirrorScarlet { px = 16f, py = 12f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 11f, py = 10f };

            float best = boss.py;
            for (int i = 0; i < 360; i++) { Step(boss, hero, map, 1f / 60f); if (boss.py < best) best = boss.py; }

            Assert.IsTrue(best <= 10.5f,
                $"Bossen ska ha klättrat upp på platån (py≈10), men kom som högst till py={best}.");
        }

        // ── Fysik-sim: TRE nivåer — hon ska ta sig hela vägen upp (golv → mellan → topp) ──
        // Arena som speglar bossbanan: golv rad 13–14, mellanplatå rad 11 kol 2–5, toppplatå rad 9
        // kol 6–8 (intilliggande så vägen finns: golv→11 via kol 2–5, 11→9 via kol 6–8).
        private static GridMap ThreeLevelMap() => new GridMap((x, y) =>
            (y >= 13 && y <= 14) ||
            (y == 11 && x >= 2 && x <= 5) ||
            (y == 9  && x >= 6 && x <= 8));

        [TestMethod]
        public void Sim_HeroTwoLevelsUp_BossClimbsAllTheWay()
        {
            var map = ThreeLevelMap();
            var boss = new DynamicCreatureMirrorScarlet { px = 10f, py = 12f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 7f, py = 8f };   // uppe på toppplatån (rad 9)

            float best = boss.py;
            for (int i = 0; i < 600; i++) { Step(boss, hero, map, 1f / 60f); if (boss.py < best) best = boss.py; }

            Assert.IsTrue(best <= 8.5f,
                $"Bossen ska klättra HELA vägen upp till toppnivån (py≈8), men kom som högst till py={best}.");
        }

        [TestMethod]
        public void Sim_HeroTwoLevelsDown_BossDescendsAllTheWay()
        {
            var map = ThreeLevelMap();
            var boss = new DynamicCreatureMirrorScarlet { px = 7f, py = 8f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 10f, py = 12f };   // nere på golvet

            float lowest = boss.py;
            for (int i = 0; i < 600; i++) { Step(boss, hero, map, 1f / 60f); if (boss.py > lowest) lowest = boss.py; }

            Assert.IsTrue(lowest >= 11.5f,
                $"Bossen ska ta sig HELA vägen ner till golvet (py≈12), men kom som lägst till py={lowest}.");
        }

        // ── Fysik-sim: hjälten LÅNGT bort i sidled → bossen ska traversera mot henne, inte studsa lokalt ──
        [TestMethod]
        public void Sim_HeroFarAway_BossTraversesTowardHero()
        {
            // Brett golv (rad 13–14) + en platå till höger (rad 11, kol 30–33) där bossen står.
            // Hjälten långt till vänster på golvet. Bossen ska komma ner och gå mot henne — inte
            // klättra/studsa på platåerna i sin närhet.
            var map = new GridMap((x, y) => (y >= 13 && y <= 14) || (y == 11 && x >= 30 && x <= 33));
            var boss = new DynamicCreatureMirrorScarlet { px = 31f, py = 10f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 4f, py = 12f };

            float startDist = System.Math.Abs(boss.px - hero.px);
            for (int i = 0; i < 1500; i++) Step(boss, hero, map, 1f / 60f);
            float endDist = System.Math.Abs(boss.px - hero.px);

            Assert.IsTrue(endDist < 8f,
                $"Bossen ska traversera mot hjälten (start {startDist:0} tiles bort), men slutade {endDist:0} tiles bort (px={boss.px:0.0}).");
        }

        // ── Fysik-sim: närmare "fel" platå åt ena hållet, hjälten åt andra → klättra MOT hjälten ──
        [TestMethod]
        public void Sim_NearestPlatformWrongWay_ClimbsTowardHeroInstead()
        {
            // Golv + en platå till VÄNSTER (hjälten, rad 11 kol 2–5) och en NÄRMARE platå till HÖGER
            // (rad 11 kol 12–15). Bossen står på golvet mellan dem, närmare den högra. Hon ska klättra
            // upp på hjältens (vänstra) platå — inte studsa upp på den närmare högra.
            var map = new GridMap((x, y) => (y >= 13 && y <= 14)
                || (y == 11 && x >= 2 && x <= 5) || (y == 11 && x >= 12 && x <= 15));
            var boss = new DynamicCreatureMirrorScarlet { px = 10f, py = 12f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 3f, py = 10f };   // på vänstra platån

            bool reachedHeroPlatform = false;
            bool wentRight = false;
            for (int i = 0; i < 600; i++)
            {
                Step(boss, hero, map, 1f / 60f);
                if (boss.py <= 10.5f && boss.px <= 6f) reachedHeroPlatform = true;   // uppe på vänstra (hjälte-)platån
                if (boss.px >= 12f) wentRight = true;                                 // klättrade upp på fel (högra) platån
            }
            Assert.IsTrue(reachedHeroPlatform, $"Bossen ska klättra upp på hjältens (vänstra) platå (px={boss.px:0.0}, py={boss.py:0.0}).");
            Assert.IsFalse(wentRight, "Bossen ska INTE ge sig av till den närmare högra platån (bort från hjälten).");
        }

        // ── Fysik-sim: hög smal platå, hjälten under åt sidan → kliv av RENT (ingen vinglig kant) ──
        [TestMethod]
        public void Sim_HighPlatform_HeroBelow_DropsOffCleanly()
        {
            // Golv (rad 13–14) + en smal hög platå (rad 9, kol 6–8). Bossen står på platån, hjälten
            // på golvet åt HÖGER. Hon ska kliva av och nå golvet snabbt — inte vingla vid kanten i
            // flera sekunder för att hjälte-jakten drar henne tillbaka in över platån.
            var map = new GridMap((x, y) => (y >= 13 && y <= 14) || (y == 9 && x >= 6 && x <= 8));
            var boss = new DynamicCreatureMirrorScarlet { px = 7f, py = 8f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 10f, py = 12f };

            // Räkna RE-GRUNDNINGAR vid platånivå (py < 11, dvs inte golvet). Vinglet = hon kliver av,
            // grundningsmarginalen drar tillbaka henne, hon re-grundas på kanten, om och om igen.
            // Ren avkliv = noll sådana (hon landar bara på golvet). En attack-pounce från golvet är
            // luftburen (re-grundas på GOLVET py≈12), så den räknas inte.
            bool prevG = boss.Grounded, reachedFloor = false;
            int platformRelandings = 0;
            for (int i = 0; i < 300; i++)
            {
                Step(boss, hero, map, 1f / 60f);
                if (!prevG && boss.Grounded && boss.py < 11f) platformRelandings++;
                prevG = boss.Grounded;
                if (boss.py >= 11.5f) reachedFloor = true;
            }
            Assert.IsTrue(reachedFloor, "Bossen ska kliva av och nå golvet.");
            Assert.AreEqual(0, platformRelandings,
                $"Bossen ska fullfölja fallet, inte re-grundas vid platåkanten ({platformRelandings} gånger = vingel).");
        }

        // ── Fysik-sim: GAP-HOPP — hjälten på en hög platå tvärs över ett gap från en mellanplatå ──
        // Reproducerar stale-mate-loopen: bossen klättrar upp på mellanplatån, når inte den högre platån
        // (gap i vägen), faller av mot hjälten, klättrar igen... Med ett gap-hopp ska hon ta sig över.
        // (Förväntas FAILA tills gap-hoppet finns — testet definierar målet.)
        private static GridMap GapJumpMap() => new GridMap((x, y) =>
            (y >= 13 && y <= 14)                       // golv
            || (y == 11 && x >= 5 && x <= 9)           // mellanplatå (1 niva upp)
            || (y == 9  && x >= 13 && x <= 17));        // hog plata (2 nivaer upp), gap kol 10-12 emellan

        [TestMethod]
        public void Sim_HeroOnHighPlatformAcrossGap_BossGapJumpsToReach()
        {
            var map = GapJumpMap();
            var boss = new DynamicCreatureMirrorScarlet { px = 7f, py = 12f, Arena = map, Active = true, Grounded = true };
            var hero = new DynamicCreatureEnemyPenguin { px = 15f, py = 8f };   // pa hoga platan

            float best = boss.py;
            for (int i = 0; i < 900; i++) { Step(boss, hero, map, 1f / 60f); if (boss.py < best) best = boss.py; }

            Assert.IsTrue(best <= 8.5f,
                $"Bossen ska ta sig HELA vagen upp till den hoga platan via ett gap-hopp, men kom som hogst till py={best}.");
        }

        // ── Regression: efter att ha GETT skada ska hon studsa UPP (separera från hjälten) ──
        [TestMethod]
        public void OnDealtDamage_PopsUpToSeparateFromHero()
        {
            var boss = new DynamicCreatureMirrorScarlet { px = 10f, py = 10f, vy = 5f };   // föll på hjälten (vy > 0)
            boss.OnDealtDamage(victimX: 10f);
            Assert.IsTrue(boss.vy < 0f,
                $"Efter att ha gett skada ska bossen studsa UPP (vy < 0) så hon inte blir stående på hjälten, men vy={boss.vy}.");
        }

        // ── Regression: väggträff ska INTE ge ett reflex-hopp (kängurustudsandet) ──────
        [TestMethod]
        public void WallHit_DoesNotReflexJump()
        {
            var map = PlatformMap();
            var boss = new DynamicCreatureMirrorScarlet { px = 11f, py = 4f, Arena = map, Active = true, Grounded = true };
            float newX = 11f;

            // Simulera att kollisionssystemet slagit i en vägg (turnPatrol=true) flera frames i rad —
            // som när hon pressas mot en plattforms sida. Hon får ALDRIG hoppa av det.
            for (int i = 0; i < 10; i++)
            {
                boss.OnWallCollision(ref newX, turnPatrol: true, movingLeft: true, map, fBorder: 0.1f);
                Assert.IsTrue(boss.vy >= 0f,
                    $"Väggträff ska inte ge reflex-hopp (frame {i}, vy={boss.vy}).");
            }
        }

        // ── Vanish: göm direkt (dev-start i akt 3 → hon ska inte synas) ─────────────────
        [TestMethod]
        public void NotVanished_IsDrawn()
        {
            var boss = new DynamicCreatureMirrorScarlet { px = 10f, py = 10f };
            var gfx = new Fakes.FakeRenderContext();

            boss.DrawSelf(gfx, 0f, 0f);

            Assert.AreEqual(1, gfx.DrawnSprites.Count, "Normalt (osynlig-flagga ej satt) ska kroppen ritas.");
        }

        [TestMethod]
        public void Vanish_HidesBodyAndDisarms()
        {
            var boss = new DynamicCreatureMirrorScarlet { px = 10f, py = 10f };
            var gfx = new Fakes.FakeRenderContext();

            boss.Vanish();
            boss.DrawSelf(gfx, 0f, 0f);

            Assert.AreEqual(0, gfx.DrawnSprites.Count, "Efter Vanish ska kroppen inte ritas (akt 3).");
            Assert.IsFalse(boss.IsAttackable, "Vanish ska avväpna henne.");
            Assert.IsFalse(boss.SolidVsDynamic, "Vanish ska göra henne icke-solid.");
        }
    }
}
