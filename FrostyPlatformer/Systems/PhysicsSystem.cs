#nullable enable
using System;
using FrostyPlatformer.Global;
using FrostyPlatformer.Models.Objects;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Rena fysikberäkningar: gravitation, luftmotstånd och hastighetsbegränsning.
    /// Alla metoder är tillståndslösa och muterar bara det DynamicGameObject som skickas in.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: System (statisk hjälpklass)
    ///
    /// MOTIVERING:
    /// Fysiklogiken låg tidigare inbäddad i Program.cs och var omöjlig att testa isolerat.
    /// Extraheringen till en statisk klass utan tillstånd gör varje beräkning till en
    /// ren in/ut-funktion som kan testas fristående från spelmotor och hårdvara (SRP).
    ///
    /// ANVÄNDNING:
    /// Anropas från GameplayState varje frame för hjälte och fiender.
    /// Tar alltid ett DynamicGameObject som muteras in-place — ingen instansiering krävs.
    /// </remarks>
    public static class PhysicsSystem
    {
        // ─────────────────────────────────────────────────────────────
        // Gravitation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Applicerar gravitation på ett objekt för en frame.
        ///
        /// Hjältar har tre lägen:
        ///   • Stiger uppåt + BPower  → mjukare gravitation (GravityPowerJump)
        ///   • Stiger uppåt, normalt  → normal gravitation (GravityNormal)
        ///   • Faller neråt           → tyngre gravitation (GravityHeavy)
        ///
        /// rememberJumpCollision minskas med 1 varje frame (räknar ned taket-kontaktfönster).
        /// Alla andra objekt får GravityNormal oavsett riktning.
        /// </summary>
        /// <param name="obj">Spelobj som ska uppdateras.</param>
        /// <param name="isHero">Sant om obj är spelaren.</param>
        /// <param name="bPower">Sant om B-knappen (snabbläge) är nedtryckt.</param>
        /// <param name="rememberJumpCollision">
        ///     Räknare för taket-kontaktfönster.
        ///     Skickas som ref och minskas här.
        /// </param>
        /// <param name="elapsed">Tid sedan förra frame (sekunder).</param>
        public static void ApplyGravity(
            DynamicGameObject obj,
            bool isHero,
            bool bPower,
            ref int rememberJumpCollision,
            float elapsed)
        {
            if (isHero)
            {
                if (rememberJumpCollision > -1)
                    rememberJumpCollision--;

                if (obj.vy < 0) // stiger uppåt
                {
                    if (bPower)
                    {
                        if (rememberJumpCollision < 0)
                            obj.vy += GameConstants.GravityPowerJump * elapsed;
                    }
                    else
                    {
                        if (rememberJumpCollision < 0)
                            obj.vy += GameConstants.GravityNormal * elapsed;
                    }
                }
                else // faller neråt
                {
                    obj.vy += GameConstants.GravityHeavy * elapsed;
                }
            }
            else
            {
                obj.vy += GameConstants.GravityNormal * elapsed;
            }
        }


        // ─────────────────────────────────────────────────────────────
        // Luftmotstånd / mark-bromsning
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Applicerar luftmotstånd (bromsning) på en hjälte som är markad.
        /// Påverkar bara objekt med IsHero=true och Grounded=true.
        ///
        /// Isiga banor (mapseven, mapeight, mapnine) har lägre friktion.
        /// BPower-läge har mjukare bromsning (glider längre).
        /// </summary>
        /// <param name="obj">Hjälteobjektet.</param>
        /// <param name="bPower">Sant om B-knappen är nedtryckt.</param>
        /// <param name="isIcyMap">Sant om aktuell bana är isig.</param>
        /// <param name="anyDirectionAtAll">Sant om vänster- eller högerknapp är nedtryckt.</param>
        /// <param name="elapsed">Tid sedan förra frame (sekunder).</param>
        public static void ApplyDrag(
            DynamicGameObject obj,
            bool bPower,
            bool isIcyMap,
            bool anyDirectionAtAll,
            float elapsed)
        {
            if (!obj.IsHero || !obj.Grounded)
                return;

            float stopThreshold = isIcyMap
                ? (bPower ? GameConstants.SlipperinessIceBPower : GameConstants.SlipperinessIce)
                : (bPower ? GameConstants.SlipperinessNormalBPower : GameConstants.SlipperinessNormal);

            if (!bPower)
            {
                obj.vx += -GameConstants.DragNormal * obj.vx * elapsed;
                if (!anyDirectionAtAll && Math.Abs(obj.vx) < stopThreshold)
                    obj.vx = 0.0f;
            }
            else
            {
                obj.vx += -GameConstants.DragBPower * obj.vx * elapsed;
                if (!anyDirectionAtAll && Math.Abs(obj.vx) < stopThreshold)
                    obj.vx = 0.0f;
            }
        }


        // ─────────────────────────────────────────────────────────────
        // Hastighetsbegränsning
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Begränsar ett objekts hastighet till säkra värden.
        ///
        /// Om hastigheten överstiger crashgränsen återställs den till 0.0f
        /// och metoden returnerar true (indikerar att objektet "ballat ur").
        /// Annars kläms hastigheten till maxvärdet.
        /// </summary>
        /// <param name="obj">Spelobj vars hastighet begränsas.</param>
        /// <returns>True om objektet gick utanför crashgränsen (detHarBallatUr).</returns>
        public static bool ClampVelocities(DynamicGameObject obj)
        {
            bool ballat = false;

            // Horisontellt – höger
            if (obj.vx > GameConstants.MaxVelocityX)
            {
                if (obj.vx > GameConstants.MaxVelocityXCrash) { obj.vx = 0.0f; ballat = true; }
                else obj.vx = GameConstants.MaxVelocityX;
            }
            // Horisontellt – vänster
            else if (obj.vx < -GameConstants.MaxVelocityX)
            {
                if (obj.vx < -GameConstants.MaxVelocityXCrash) { obj.vx = 0.0f; ballat = true; }
                else obj.vx = -GameConstants.MaxVelocityX;
            }

            // Vertikalt – neråt (fall)
            if (obj.vy > GameConstants.FallSpeedThreshold)
       