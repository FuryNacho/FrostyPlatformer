#nullable enable
using FrostyPlatformer.Models;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Adaptrar ett LevelObj till IMapData så att TileMapRenderer kan rendera
    /// kartdata som editorn håller i minnet och modifierar direkt.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter (Object Adapter)
    ///
    /// MOTIVERING:
    /// TileMapRenderer kräver IMapData men editorn arbetar med LevelObj direkt
    /// (TileIndex[], AttributeIndex[]) för att tile-placering ska ge omedelbar
    /// visuell feedback utan kopiering. Att lägga till IMapData på LevelObj
    /// i Shared-projektet vore ett för brett interface-kontrakt. Den här adaptern
    /// håller ansvaret lokalt i Editor-namnrymden och undviker att ändra Shared (OCP).
    ///
    /// ANVÄNDNING:
    /// Skapas av EditorState.Enter() tillsammans med LevelObj. Levandegör
    /// TileMapRenderer.GetDrawCalls() mot det redigerbara LevelObj-tillståndet.
    /// Ändringar i LevelObj.TileIndex/AttributeIndex syns omedelbart nästa frame.
    /// </remarks>
    internal sealed class LevelObjMapAdapter : IMapData
    {
        private readonly LevelObj _level;

        public int Width  => _level.Width;
        public int Height => _level.Height;

        /// <summary>Skapar adaptern kring ett LevelObj.</summary>
        public LevelObjMapAdapter(LevelObj level)
        {
            _level = level;
        }

        /// <summary>
        /// Returnerar tile-indexet på (x, y). Returnerar 0 utanför kartgränsen
        /// (TileMapRenderer hoppar ändå över OOB-tiles, men 0 är säkert fallback).
        /// </summary>
        public int GetIndex(int x, int y)
        {
            if (x < 0 || x >= _level.Width || y < 0 || y >= _level.Height) return 0;
            return _level.TileIndex[y * _level.Width + x];
        }

        /// <summary>
        /// Returnerar true om tilen är solid. Returnerar true utanför kartgränsen
        /// (kartens ytterkant behandlas som solid, konsistent med CollisionSystem).
        /// </summary>
        public bool GetSolid(int x, int y)
        {
            if (x < 0 || x >= _level.Width || y < 0 || y >= _level.Height) return true;
            return _level.AttributeIndex[y * _level.Width + x] != 0;
        }

        /// <summary>Sätter tile-index på (x, y). Ignorerar koordinater utanför kartgränsen.</summary>
        public void SetTile(int x, int y, int tileId)
        {
            if (x < 0 || x >= _level.Width || y < 0 || y >= _level.Height) return;
            _level.TileIndex[y * _level.Width + x] = tileId;
        }

        /// <summary>Sätter kollisionsattribut på (x, y). Ignorerar koordinater utanför kartgränsen.</summary>
        public void SetSolid(int x, int y, bool solid)
        {
            if (x < 0 || x >= _level.Width || y < 0 || y >= _level.Height) return;
            _level.AttributeIndex[y * _level.Width + x] = solid ? 1 : 0;
        }
    }
}
