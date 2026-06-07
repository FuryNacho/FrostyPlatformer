#nullable enable
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    /// <summary>
    /// Spegel-Scarlet — slutbossens akt 1-antagonist (den kalla maskinen under skalet).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Konkret Creature-subklass.
    ///
    /// MOTIVERING:
    /// Bossens stridslogik (akt-progression, hälsa, dränering) bor i den testbara
    /// BossPhaseController — inte här (SRP). Den här klassen är bara den fysiska/visuella
    /// närvaron i arenan. Byggs som en ren ny typ istället för i den röriga
    /// DynamicCreatureEnemyBoss (mapnine-legacy), så akt-koreografin kan växa fram rent.
    ///
    /// ANVÄNDNING:
    /// Spawnas av MapTen.PopulateDynamics. I fas 2 står hon still (ren närvaro + HUD-test).
    /// Duell-AI:n (hoppa/stampa som spelaren, sårbara landningsframes) byggs i fas 3.
    /// </remarks>
    public class DynamicCreatureMirrorScarlet : Creature
    {
        public DynamicCreatureMirrorScarlet() : base("mirror_scarlet", SpriteId.EnemyMirrorScarlet)
        {
            Friendly = false;
            Health = 100;       // BossPhaseController är auktoritativ för stridshälsan; detta är bara kroppen.
            MaxHealth = 100;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 0;    // Fas 2: ingen kontaktskada än — strid byggs i fas 3.
            IsAttackable = false;
        }

        // Fas 2: ingen autonom rörelse. Faller till golvet via fysiken och står still.
        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null) { }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            // Rita idle-framen (övre vänstra rutan i placeholder-arket).
            int screenX = ToPixel(px, ox);
            int screenY = ToPixel(py, oy);
            gfx.DrawPartialSprite(SpriteId, screenX, screenY, 0, 0, 16, 16);
        }
    }
}
