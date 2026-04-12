#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Immutabel datastruktur med resultatet av ett kollisionstest mot kartan.
    /// Innehåller justerade positioner och flaggor för vilka sidor som träffades.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Value Object (readonly record struct)
    ///
    /// MOTIVERING:
    /// CollisionResult är ett rent dataobjekt — CollisionSystem producerar ett nytt
    /// värde per test utan sidoeffekter. Readonly record struct garanterar att
    /// resultatet inte muteras av konsumenten.
    ///
    /// ANVÄNDNING:
    /// Returneras av CollisionSystem.Move() och läses av GameplayState för att
    /// avgöra om hjälten är på marken, träffar en vägg, osv. Skickas även till
    /// fiendeklassernas OnWallCollision/OnStuckCheck.
    /// </remarks>
    public readonly record struct CollisionResult
    {
        /// <summary>Justerad X-position efter horisontell kollision.</summary>
        public float NewPosX { get; init; }
        /// <summary>Justerad Y-position efter vertikal kollision.</summary>
        public float NewPosY { get; init; }
        /// <summary>Sant om objektet träffade en solid tile horisontellt.</summary>
        public bool HitWall { get; init; }
        /// <summary>Sant om objektet träffade en solid tile uppåt (tak).</summary>
        public bool HitCeiling { get; init; }
        /// <summary>Sant om objektet landade på en solid tile (golvet).</summary>
        public bool Grounded { get; init; }
    }
}
