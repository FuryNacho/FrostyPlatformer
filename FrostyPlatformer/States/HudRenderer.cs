#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Rendering;

namespace FrostyPlatformer.States
{
    /// <summary>
    /// Renderar spelets HUD (hälsomätare, tid) för de states som visar den.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Helper / Utility (statisk klass)
    ///
    /// MOTIVERING:
    /// DrawHUD anropades från tre states (WorldMap, Gameplay, Pause). Istället för
    /// att duplicera metoden i tre klasser samlas den här. Den är statisk eftersom
    /// den saknar eget tillstånd — allt den behöver skickas som parametrar.
    ///
    /// ANVÄNDNING:
    /// Anropas från Update() i WorldMapState, GameplayState och PauseState.
    /// GameContext.GameTotalTime ska vara uppdaterat av Program.OnUpdate() innan
    /// Update anropas.
    /// </remarks>
    internal static class HudRenderer
    {
        /// <summary>
        /// Ritar HUD-baren med hälsomätare och tid. Om mode är "pause" visas
        /// paus-instruktioner.
        /// </summary>
        public static void Draw(IRenderContext rc, GameContext context, string mode = "")
        {
            if (context.Player == null) return;

            rc.FillRect(2, 2, context.ScreenWidth - 4, 7, RenderColor.Brown);
            rc.DrawPartialSprite(SpriteId.Items, 3, 3, 0, 16 * 4, 16, 4);

            string text = context.Player.Health.ToString() + "% " +
                          context.GameTotalTime.ToString("hh':'mm':'ss'.'fff");
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int sx = ((c - 32) % 16) * 8;
                int sy;
                if (sx > 72)
                {
                    sx = 72;
                    sy = 69;
                }
                else
                {
                    sy = (((c - 32) / 16) * 8) == 8 ? 75 : 69;
                }
                rc.DrawPartialSprite(SpriteId.Items, 30 + i * 5, 3, sx, sy, 8, 5);
            }

            var color = RenderColor.Green;
            int health = context.Player.Health;
            int bar = HealthToBar(health, ref color);

            rc.DrawLine(3 + 1, 3 + 1, 3 + bar, 3 + 1, color);
            rc.DrawLine(3 + 1, 3 + 2, 3 + bar, 3 + 2, color);

            if (mode == "pause")
            {
                rc.DrawText("Pause", 25, 25);
                rc.DrawText("Start resume.", 25, 35);
                rc.DrawText("Select go back.", 25, 45);
            }
        }

        private static int HealthToBar(int health, ref RenderColor color)
        {
            if (health < 7)   { color = RenderColor.DarkRed; return 1; }
            if (health < 14)  { color = RenderColor.Yellow;  return 2; }
            if (health < 21)  { color = RenderColor.Yellow;  return 3; }
            if (health < 28)  return 4;
            if (health < 35)  return 5;
            if (health < 42)  return 6;
            if (health < 49)  return 7;
            if (health < 56)  return 8;
            if (health < 62)  return 9;
            if (health < 69)  return 10;
            if (health < 76)  return 11;
            if (health < 82)  return 12;
            if (health < 89)  return 13;
            return 14;
        }
    }
}
