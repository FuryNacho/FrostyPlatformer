namespace FrostyPlatformer.Rendering
{
    /// <summary>
    /// Identifierar alla sprite-ark som spelet använder, utan att referera till
    /// PixelEngine-typen Sprite.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (del av IRenderContext-abstraktionen)
    ///
    /// MOTIVERING:
    /// Spelkoden behöver ett sätt att säga "rita med fiende-tvåarnas sprite-ark"
    /// utan att hålla en PixelEngine.Sprite-referens. SpriteId är den motor-agnostiska
    /// identifieraren. PixelEngineRenderContext håller en intern Dictionary&lt;SpriteId, Sprite&gt;
    /// och löser upp identifieraren till den faktiska sprite-resursen vid renderingstillfället.
    ///
    /// ANVÄNDNING:
    /// Creature-subklasser deklarerar sin SpriteId i konstruktorn. IRenderContext-metoder
    /// tar SpriteId som parameter. PixelEngineRenderContext populeras med faktiska sprites
    /// via RegisterSprite() under spelets initialisering i Program.cs (Composition Root).
    /// </remarks>
    public enum SpriteId
    {
        /// <summary>Bitmappsteckensnittet — används av DrawText.</summary>
        Font,

        /// <summary>Sprite-ark för föremål, HUD-element och energimätaren.</summary>
        Items,

        /// <summary>Hjältens (pingvinens) sprite-ark.</summary>
        Hero,

        /// <summary>Fiendetyp 1: liten pingvin-fiende.</summary>
        EnemyPenguin,

        /// <summary>Fiendetyp 2: valrossen.</summary>
        EnemyWalrus,

        /// <summary>Fiendetyp 3: frost-fienden.</summary>
        EnemyFrost,

        /// <summary>Fiendetyp 0: icicle-projektilet (oförstörbart).</summary>
        EnemyIcicle,

        /// <summary>Bossen och boss-överläggsgrafikens sprite-ark.</summary>
        EnemyBoss,

        /// <summary>Vind-fiendens sprite-ark.</summary>
        EnemyWind,

        /// <summary>Spegel-Scarlet (slutbossens akt 1) sprite-ark.</summary>
        EnemyMirrorScarlet,

        /// <summary>Svärm-kopia (slutbossens akt 2) sprite-ark.</summary>
        EnemySwarmCopy,

        /// <summary>Jätten (slutbossens akt 3) — huvudets uttrycks-frames (80×64 per frame).</summary>
        GiantHead,

        /// <summary>Jättens arm/näve — slam-frames (48×64 per frame).</summary>
        GiantArm,

        /// <summary>Jättens svagpunkt — 5 states (16×16 per state), den stampbara enheten.</summary>
        GiantWeakPoint,

        /// <summary>Jättens arena-hazards (akt 3) — istapp, krasch, varningsmarkör (16×16 per frame).</summary>
        GiantHazard,

        /// <summary>Tile-arket för världskartan.</summary>
        WorldMapTileSheet,

        /// <summary>
        /// Tile-arket för den aktiva spelkartan — byts ut vid kartbyte via
        /// PixelEngineRenderContext.RegisterSprite().
        /// </summary>
        MapTileSheet,

        /// <summary>Startskärmens splash-bild.</summary>
        SplashStart,

        /// <summary>Slutskärmens splash-bild.</summary>
        SplashEnd,

        /// <summary>Slutanimationens sprite-ark (igloo, grind m.m.).</summary>
        EndArt,

        // ── Parallax-bakgrundslager ─────────────────────────────────────────
        // Varje årstid har ett himmel-lager (Sky) och ett mellanskikt (Mid).
        // Sky-lager: solid bakgrund, scroll-faktor ~0.10 (nästan statisk).
        // Mid-lager: siluetter med alpha-transparens, scroll-faktor ~0.30.
        // Bildernas mått: 512×224 spelpixlar (2× skärmbredden för sömlös tiling).

        /// <summary>Vår — himmelslager (ljusblå, avlägsna kullar).</summary>
        ParallaxSkySpring,

        /// <summary>Vår — mellanskikt (trädsiluetter med transparens).</summary>
        ParallaxMidSpring,

        /// <summary>Sommar — himmelslager (djupblå, distansmoln).</summary>
        ParallaxSkySummer,

        /// <summary>Sommar — mellanskikt (lummiga trädtoppar med transparens).</summary>
        ParallaxMidSummer,

        /// <summary>Höst — himmelslager (varmt orange-rosa, solnedgångsstämning).</summary>
        ParallaxSkyFall,

        /// <summary>Höst — mellanskikt (grena träd och löv med transparens).</summary>
        ParallaxMidFall,

        /// <summary>Vinter — himmelslager (mörknattblå, stjärnor).</summary>
        ParallaxSkyWinter,

        /// <summary>Vinter — mellanskikt (gransiluetter med snö, transparens).</summary>
        ParallaxMidWinter,

        /// <summary>Anpassad bana — himmelslager.</summary>
        ParallaxSkyCustom,

        /// <summary>Anpassad bana — mellanskikt med transparens.</summary>
        ParallaxMidCustom,

        /// <summary>Slutboss — himmelslager (mörk natthimmel, kall stjärnglimt).</summary>
        ParallaxSkyBoss,

        /// <summary>Slutboss — mellanskikt (isspira-siluetter med cyan-kant, transparens).</summary>
        ParallaxMidBoss,
    }
}
