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
        private const float RetreatDuration  = 0.6f;   // kort reträtt efter att ha gett skada → hjälten hinner ur klungan
        private const float RetreatSpeed     = 3.0f;   // tydlig disengage (snabbare än jakten)
        private const float SeparateDuration = 0.4f;   // håll sidledsfart efter ett av-stapel-hopp tills den landar bredvid
        private const float SeparateSpeed    = 2.9f;   // sidledsfart vid av-stapling (sprider ut klungan)
        private const float StackHopCooldown = 0.5f;   // minsta tid mellan av-stapel-hopp (ej varje frame → ej vibrerande)

        private const float GlitchRampRate   = 2.5f;  // hur snabbt glitch-intensiteten trappas upp
        private const float GlitchIdleLevel  = 0.7f;  // intensitet kopian håller medan den glitchar idle (väntar på poff)
        private const float GlitchStartJitter = 0.6f; // slumpad fördröjning EFTER landning innan glitchen börjar

        private readonly Random _rng = new Random();
        private float _hopTimer;
        private float _animTimer;
        private int   _animFrame;
        private float _retreatTimer;
        private float _retreatDir = 1f;
        private float _separateTimer;
        private float _separateDir = 1f;
        private float _stackCooldown;
        private bool  _exiting;           // akt 2→3-final: ofarlig, slutar jaga; bågar in/faller och glitchar FÖRST på marken
        private bool  _exitGlitching;     // har landat + fördröjning klar → glitchar nu (poffbar)
        private float _glitchDelay;       // slumpad fördröjning efter landning innan glitchen börjar
        private float _glitch;            // 0..1 glitch-intensitet

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

            // Exit (akt 2→3-final): ofarlig direkt, slutar jaga. I LUFTEN (inkastad båge / fall) behåller
            // den sin rörelse och glitchar INTE. Först när den nått marken stannar den, väntar en liten
            // slumpad fördröjning, och börjar sedan glitcha idle — i väntan på att GameplayState poffar
            // bort den (accelererande cascade). Gravitationen sköter fallet (vy rörs ej i luften).
            if (_exiting)
            {
                if (!_exitGlitching)
                {
                    if (!Grounded) return;                                      // i luften → behåll bågen, ingen glitch
                    vx = 0f;                                                    // landat → stanna
                    if (_glitchDelay > 0f) { _glitchDelay -= fElapsedTime; return; }   // liten slumpad fördröjning
                    _exitGlitching = true;                                      // fördröjning klar → börja glitcha
                }
                vx = 0f;
                _glitch = Math.Min(GlitchIdleLevel, _glitch + fElapsedTime * GlitchRampRate);
                return;
            }

            if (player == null) { vx = 0; return; }

            // Glitch-flimmer: cykla animationsrutan oberoende av rörelse.
            _animTimer += fElapsedTime;
            if (_animTimer >= 0.12f) { _animTimer = 0f; _animFrame = (_animFrame + 1) & 3; }

            if (_stackCooldown > 0f) _stackCooldown -= fElapsedTime;

            // Reträtt efter att ha gett skada: backa undan en kort stund så svärmen inte
            // staplar upp sig på hjälten och låser styrningen. vy lämnas orörd (OnDealtDamage
            // gav redan en liten studs upp som separerar vertikalt).
            if (_retreatTimer > 0f)
            {
                _retreatTimer -= fElapsedTime;
                vx = _retreatDir * RetreatSpeed;
                return;
            }

            // Av-stapling: efter ett hopp av en annan kopia, håll sidledsfarten tills den
            // hunnit landa BREDVID i stället för ovanpå (vy bär hoppet vidare).
            if (_separateTimer > 0f)
            {
                _separateTimer -= fElapsedTime;
                vx = _separateDir * SeparateSpeed;
                return;
            }

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

        /// <summary>
        /// Efter att ha gett skada: kort reträtt bort från hjälten + liten studs upp. Svärmen är
        /// dum men talrik — utan detta klumpar kopiorna ihop sig PÅ hjälten och loop-låser styrningen.
        /// </summary>
        public void OnDealtDamage(float victimX)
        {
            _retreatTimer = RetreatDuration;
            _retreatDir   = px >= victimX ? 1f : -1f;
            vy = -4f;   // separera vertikalt direkt (annars står den kvar ovanpå och re-skadar)
        }

        /// <summary>
        /// Landade ovanpå en annan kopia → hoppa SIDLEDES av den, så svärmen sprider ut sig i stället
        /// för att torna upp sig. Cooldown-gatad så det inte blir vibrerande studs varje frame.
        /// Anropas av Gameplayskollisionen på den ÖVRE kopian när två kopior staplas.
        /// </summary>
        public void OnStackedOn(float otherX)
        {
            if (_stackCooldown > 0f) return;
            float away = px >= otherX ? 1f : -1f;
            vy = GameConstants.JumpVelocity;   // hoppa av högen
            vx = away * SeparateSpeed;
            _separateDir   = away;
            _separateTimer = SeparateDuration;
            _stackCooldown = StackHopCooldown;
        }

        /// <summary>Sant medan kopian är i akt 2→3-exiten (ofarlig, slutar jaga) — oavsett om den
        /// fortfarande bågar in/faller eller redan glitchar på marken.</summary>
        public bool GlitchExitInProgress => _exiting && !Redundant;

        /// <summary>Sant först när kopian har landat och faktiskt börjat glitcha (då är den poffbar).
        /// En kopia som fortfarande bågar in i luften är INTE detta.</summary>
        public bool IsGlitchingOut => _exitGlitching && !Redundant;

        /// <summary>
        /// Sätter kopian i akt 2→3-exit: ofarlig DIREKT och slutar jaga, men behåller sin rörelse i
        /// luften (inkastad båge / fall). Den börjar glitcha FÖRST när den nått marken, efter en liten
        /// slumpad fördröjning, och poffas sedan av GameplayState. Idempotent — redan i exit/borttagen rörs inte.
        /// </summary>
        public void BeginExit()
        {
            if (_exiting || Redundant) return;
            _exiting = true;
            _glitchDelay = (float)_rng.NextDouble() * GlitchStartJitter;   // randomiserad start efter landning
            SolidVsDynamic = false;    // ofarlig + ej stampbar direkt
            IsAttackable = false;
        }

        /// <summary>Poff: kopian försvinner ur arenan i en glitch (markeras för borttagning). Anropas
        /// av GameplayState i den accelererande poff-cascaden under akt 2→3-övergången.</summary>
        public void Poff()
        {
            if (Redundant) return;
            _glitch = 1f;   // full glitch i sista frame:n → liten visuell "poff"
            Health = 0; Redundant = true; RemoveCount = 1;
        }

        public override void DrawSelf(IRenderContext gfx, float ox, float oy)
        {
            int screenX = ToPixel(px, ox);
            int screenY = ToPixel(py, oy);
            // Vänd ansiktet efter färdriktningen (TurnedTo sätts av basklassens Update utifrån vx).
            bool faceLeft = TurnedTo == Enum.PlayerOrientation.Left;

            // Glitch-out (akt 2→3): horisontella band som rivs i sidled + cyan/magenta-skärvor,
            // upptrappat med _glitch. Samma upplösnings-språk som spegel-Scarlets exit.
            if (_glitch > 0f)
            {
                for (int sy = 0; sy < 16; sy += 2)
                {
                    int off = 0;
                    if (_rng.NextDouble() < _glitch * 0.7f)
                        off = (int)((_rng.NextDouble() * 2 - 1) * (1f + _glitch * 4f));
                    if (faceLeft)
                        gfx.DrawPartialSpriteFlippedX(SpriteId, screenX + off, screenY + sy, _animFrame * 16, sy, 16, 2);
                    else
                        gfx.DrawPartialSprite(SpriteId, screenX + off, screenY + sy, _animFrame * 16, sy, 16, 2);
                }

                int shards = (int)(_glitch * 5f);
                for (int i = 0; i < shards; i++)
                {
                    int bx = screenX + _rng.Next(0, 14);
                    int by = screenY + _rng.Next(0, 14);
                    var col = _rng.Next(2) == 0 ? new RenderColor(40, 230, 230) : new RenderColor(230, 40, 230);
                    gfx.FillRect(bx, by, 2 + _rng.Next(0, 2), 2, col);
                }
                return;
            }

            // Översta raden i arket (16×16-rutor) är jaga-animationen; speglad när hon går vänster.
            if (faceLeft)
                gfx.DrawPartialSpriteFlippedX(SpriteId, screenX, screenY, _animFrame * 16, 0, 16, 16);
            else
                gfx.DrawPartialSprite(SpriteId, screenX, screenY, _animFrame * 16, 0, 16, 16);
        }
    }
}
