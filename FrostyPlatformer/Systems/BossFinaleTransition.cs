#nullable enable
using System;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Driver slutbossens avslutande övergång (akt 4 → slutskärm): en filmisk iris-in där
    /// bossen poffar i en pixel-explosion medan en vit cirkel växer ut bakom hjälten och
    /// sväljer arenan, innan klippet till slutskärmen.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Tillståndsmaskin (ren domänlogik, motor-agnostisk).
    ///
    /// MOTIVERING:
    /// Slutet var förr en hård klippning (en enda vit blixt). En riktig övergång är ren
    /// koreografi ovanpå motorn: fas-ordning, timing och tillväxt-kurvor hör hemma i en
    /// deterministisk, testbar klass — inte i den motorbundna render-koden (SRP/DIP).
    /// GameplayState läser de normaliserade 0..1-värdena och ritar därefter (cirkel, poff);
    /// klassen känner inte till pixlar eller <see cref="Rendering.IRenderContext"/>.
    ///
    /// ANVÄNDNING:
    /// Skapas när akt 4 vunnits (<see cref="BossOutcome.PlayerWon"/>). GameplayState anropar
    /// <see cref="Update"/> varje frame, läser <see cref="CircleGrowth01"/>/<see cref="BossGlitch01"/>/
    /// <see cref="PoffProgress01"/> för renderingen, reagerar på <see cref="TryConsumePoff"/>
    /// (göm boss-kroppen, spela ev. effekt) och byter till EndState när <see cref="IsComplete"/>.
    /// </remarks>
    public sealed class BossFinaleTransition
    {
        // Fas-längder i sekunder (koreografin). Fas A → boss glitchar upp på plats; fas B →
        // boss poffar OCH cirkeln växer och sväljer arenan; fas C → en kort helvit andning.
        private const float GlitchHoldDur = 0.45f;   // fas A
        private const float PoffGrowDur   = 0.95f;   // fas B
        private const float WhiteHoldDur  = 0.20f;   // fas C
        private const float PoffFxDur     = 0.40f;   // hur länge pixel-explosionen breder ut sig (in i fas B)

        private float _elapsed;
        private bool  _poffConsumed;

        /// <summary>Övergångens tre synliga faser plus det avslutade läget.</summary>
        public enum Phase
        {
            /// <summary>Fas A: bossen glitchar upp till full intensitet på plats. Ingen cirkel än.</summary>
            GlitchHold,
            /// <summary>Fas B: bossen poffar i en pixel-explosion medan den vita cirkeln växer.</summary>
            PoffGrow,
            /// <summary>Fas C: cirkeln täcker allt — en kort helvit andning.</summary>
            WhiteHold,
            /// <summary>Övergången är klar → byt till slutskärmen.</summary>
            Done,
        }

        private const float PoffStart  = GlitchHoldDur;                            // fas B börjar
        private const float WhiteStart  = GlitchHoldDur + PoffGrowDur;             // fas C börjar
        private const float TotalDur    = GlitchHoldDur + PoffGrowDur + WhiteHoldDur;

        /// <summary>Total tid övergången har körts (sekunder). Monotont växande.</summary>
        public float Elapsed => _elapsed;

        /// <summary>Aktuell fas, härledd ur <see cref="Elapsed"/>.</summary>
        public Phase CurrentPhase =>
            _elapsed < PoffStart  ? Phase.GlitchHold :
            _elapsed < WhiteStart ? Phase.PoffGrow   :
            _elapsed < TotalDur   ? Phase.WhiteHold  :
                                    Phase.Done;

        /// <summary>
        /// Bossens glitch-intensitet 0..1. Rampar upp under fas A och når full precis när poffen
        /// sker; därefter är kroppen dold och värdet saknar betydelse (ligger kvar på 1).
        /// </summary>
        public float BossGlitch01 => Clamp01(_elapsed / GlitchHoldDur);

        /// <summary>
        /// Cirkelns tillväxt 0..1 (0 = inget, 1 = täcker hela skärmen). Noll under fas A, växer
        /// med en mjuk smoothstep-kurva genom fas B, och ligger kvar på 1 under fas C / Done.
        /// </summary>
        public float CircleGrowth01 => Smoothstep(Clamp01((_elapsed - PoffStart) / PoffGrowDur));

        /// <summary>
        /// Pixel-explosionens förlopp 0..1 (0 vid poffen, 1 = skärvorna har nått sin ytterkant).
        /// Noll innan poffen; används för att sprida skärvorna utåt.
        /// </summary>
        public float PoffProgress01 => Clamp01((_elapsed - PoffStart) / PoffFxDur);

        /// <summary>Sant från och med att poffen skett — boss-kroppen ska inte längre ritas.</summary>
        public bool BossPoffed => _elapsed >= PoffStart;

        /// <summary>Sant när hela övergången spelat klart → dags att byta till slutskärmen.</summary>
        public bool IsComplete => _elapsed >= TotalDur;

        /// <summary>
        /// Returnerar sant EXAKT en gång — i den frame då poffen ska utlösas (göm boss-kroppen,
        /// spawna explosionen, spela ev. ljud). Anropas varje frame; idempotent efter första sant.
        /// </summary>
        public bool TryConsumePoff()
        {
            if (_poffConsumed || _elapsed < PoffStart) return false;
            _poffConsumed = true;
            return true;
        }

        /// <summary>Stega övergången framåt. Negativa tidssteg ignoreras (klockan går bara framåt).</summary>
        public void Update(float elapsed)
        {
            if (elapsed > 0f) _elapsed += elapsed;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        // Klassisk smoothstep (mjuk in/ut) — cirkeln accelererar mjukt och landar mjukt på full täckning.
        private static float Smoothstep(float t) => t * t * (3f - 2f * t);
    }
}
