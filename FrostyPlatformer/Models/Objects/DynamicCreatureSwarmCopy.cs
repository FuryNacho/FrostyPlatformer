#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.Models.Objects
{
    /// <summary>
    /// Svärm-kopia — slutbossens akt 2-antagonist (en av de många splittringarna av maskinen).
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Konkret Creature-subklass med en enkel jaga-AI (samma kontrakt som Penguin).
    ///
    /// MOTIVERING:
    /// Akt 2 ("De många") är ett antalsspel, inte en duell — fantasin är att bli överkörd av
    /// sina egna tvivel. Därför är varje kopia avsiktligt enkel och billig (1 HP, stampbar
    /// med ett tramp), men de kommer i mängd och jagar utan paus. Stridens akt-progression
    /// bor i den testbara BossPhaseController (SRP); här bor bara en kopias rörelse. Antalet
    /// kopior och hur stamp dränerar svärm-baren styrs av GameplayState (spawn/despawn).
    ///
    /// ANVÄNDNING:
    /// Spawnas av GameplayState så länge BossPhaseController.CurrentAct == Swarm. Stampas bort
    /// som vanlig fiende (JumpDamage), och varje stamp dränerar svärm-baren via controllern.
    /// </remarks>
    public class DynamicCreatureSwarmCopy : Creature
    {
        private const float ChaseSpeed  = 2.4f;   // snabbare än Scarlet — svärmen pressar på
        private const float HopInterval = 1.0f;   // tid mellan hopp (jaga upp på hyllor / över hjälten)

        private readonly Random _rng = new Random();
        private float _hopTimer;
        private float _animTimer;
        private int   _animFrame;

        public DynamicCreatureSwarmCopy() : base("swarm_copy", SpriteId.EnemySwarmCopy)
        {
            Friendly = false;
            Health = 1;          // ett tramp räcker — utmaningen är antalet, inte tåligheten
            MaxHealth = 1;
            SolidVsDynamic = true;
            SolidVsMap = true;
            DamageGiven = 4;     // som vanliga fiender → samma energi-kaskad vid kontakt
            IsAttackable = true;
        }

        public override void Behaviour(float fElapsedTime, DynamicGameObject? player = null)
        {
            if (Health <= 0)
            {
                vx = 0; vy = 0;
                SolidVsDynamic = false;
                IsAttackable = false;
                return;
            }

            if (player == null) { vx = 0; return; }

            // Glitch-flimmer: cykla animationsrutan oberoende av rörelse.
            _animTimer += fElapsedTime;
            if (_animTimer >= 0.12f) { _animTimer = 0f; _animFrame = (_animFrame + 1) & 3; }

            // Jaga hjälten oavbrutet i sidled.
            float dx  = player.px - px;
            float dir = dx >= 0f ? 1f : -1f;
            vx = dir * ChaseSpeed;

            // Hoppa upp mot hjälten ovanför (hyllorna) eller en oregelbunden skutt framåt.
            _hopTimer -= fElapsedTime;
            bool heroAbove = player.py < py - 1.2f;
            if (Grounded && _hopTimer <= 0f && (heroAbove || _rng.NextDouble() < 0.4))
            {
                vy = GameConstants.JumpVelocity;   // ~2 tiles (max tillåtet av fysikens klamp)
                _hopTimer = HopInterval;
            }
        }

        // Träffar kopian en vägg medan den jagar → hoppa upp för att ta sig över (klättra på hyllorna).
        public override void OnWallCollision(ref float newX, bool turnPatrol, bool movingLeft, IMapData map, float fBorder)
        {
            if (turnPatrol && Grounded)
                vy = GameConstants.JumpVelocity;
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            int screenX = ToPixel(px, ox);
            int screenY = ToPixel(py, oy);
            // Översta raden i arket (16×16-rutor) är jaga-animationen.
            gfx.DrawPartialSprite(SpriteId, screenX, screenY, _animFrame * 16, 0, 16, 16);
        }
    }
}
