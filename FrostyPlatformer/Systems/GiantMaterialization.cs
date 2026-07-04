#nullable enable
using System;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Driver jättebossens materialiserings-effekt (akt 3): en energiboll i jättens färger byggs upp
    /// på jättens plats, jätten "snäpper in" när bollen når toppen, och bollen kollapsar sedan bakom
    /// henne medan en gnist-burst flyger utåt — så jätten framträder ur en explosion i stället för att
    /// poppa in abrupt.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Tillståndsmaskin (ren domänlogik, motor-agnostisk).
    ///
    /// MOTIVERING:
    /// Samma skäl som <see cref="BossFinaleTransition"/>: en tids-koreograferad visuell effekt är
    /// ren logik (faser, timing, tillväxt-kurvor) som hör hemma i en deterministisk, testbar klass —
    /// inte i den motorbundna render-koden (SRP/DIP). GameplayState läser de normaliserade 0..1-värdena
    /// och ritar; klassen känner inte till pixlar eller <see cref="Rendering.IRenderContext"/>.
    ///
    /// ANVÄNDNING:
    /// Skapas i GameplayState.ManageGiant när akt 3 ska börja (efter svärm-exiten). GameplayState anropar
    /// <see cref="Update"/> varje frame, spawnar jätten när <see cref="SpawnReady"/> blir sant, läser
    /// <see cref="BallRadius01"/>/<see cref="SparkProgress01"/> för renderingen, och nollställer effekten
    /// när <see cref="IsComplete"/>.
    /// </remarks>
    public sealed class GiantMaterialization
    {
        // Fas-längder i sekunder. Charge → bollen växer och laddar; Reveal → jätten spawnar (vid
        // Charge-slutet) och bollen kollapsar bakom henne medan gnistorna flyger ut.
        private const float ChargeDur = 0.8f;   // bollen växer 0 → full
        private const float FadeDur   = 0.5f;   // bollen kollapsar full → 0 (avslöjar jätten)
        private const float SparkDur  = 0.4f;   // gnist-burstens utbredning (in i Reveal)

        private float _elapsed;

        /// <summary>Effektens tre synliga faser plus det avslutade läget.</summary>
        public enum Phase
        {
            /// <summary>Bollen byggs upp/laddar. Jätten syns inte än.</summary>
            Charge,
            /// <summary>Jätten har spawnat; bollen kollapsar bakom henne och gnistorna flyger ut.</summary>
            Reveal,
            /// <summary>Effekten är klar.</summary>
            Done,
        }

        private const float TotalDur = ChargeDur + FadeDur;

        /// <summary>Total tid effekten har körts (sekunder). Monotont växande.</summary>
        public float Elapsed => _elapsed;

        /// <summary>Aktuell fas, härledd ur <see cref="Elapsed"/>.</summary>
        public Phase CurrentPhase =>
            _elapsed < ChargeDur ? Phase.Charge :
            _elapsed < TotalDur  ? Phase.Reveal :
                                   Phase.Done;

        /// <summary>
        /// Bollens radie 0..1 (0 = inget, 1 = full jätte-storlek). Växer med smoothstep under Charge,
        /// står på 1 vid toppen, och kollapsar tillbaka mot 0 under Reveal (avslöjar jätten bakom).
        /// </summary>
        public float BallRadius01 =>
            _elapsed < ChargeDur
                ? Smoothstep(Clamp01(_elapsed / ChargeDur))
                : 1f - Smoothstep(Clamp01((_elapsed - ChargeDur) / FadeDur));

        /// <summary>
        /// Gnist-burstens förlopp 0..1 (0 vid jättens framträdande, 1 = gnistorna nått sin ytterkant).
        /// Noll under Charge; används för att sprida gnistorna utåt.
        /// </summary>
        public float SparkProgress01 => Clamp01((_elapsed - ChargeDur) / SparkDur);

        /// <summary>Sant från och med att bollen laddat klart → dags att spawna jätten (latchar).</summary>
        public bool SpawnReady => _elapsed >= ChargeDur;

        /// <summary>Sant när hela effekten spelat klart → nollställ den.</summary>
        public bool IsComplete => _elapsed >= TotalDur;

        /// <summary>Stega effekten framåt. Negativa tidssteg ignoreras (klockan går bara framåt).</summary>
        public void Update(float elapsed)
        {
            if (elapsed > 0f) _elapsed += elapsed;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static float Smoothstep(float t) => t * t * (3f - 2f * t);
    }
}
