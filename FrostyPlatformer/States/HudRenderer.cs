#nullable enable
using FrostyPlatformer.Core;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.Systems;

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
                          ShownTime(context).ToString("hh':'mm':'ss'.'fff");
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

            // Slutboss: boss-hälsobar under HUD-baren. Ritas bara i en boss-arena —
            // annars hänger baren kvar på världskarta/meny.
            if (context.BossPhase != null && context.CurrentLevel?.IsBossArena == true)
                DrawBossBar(rc, context);

            if (mode == "pause")
            {
                rc.DrawText("Pause", 25, 25);
                rc.DrawText("Start resume.", 25, 35);
                rc.DrawText("Select go back.", 25, 45);
            }
        }

        /// <summary>
        /// Tiden som ska visas i HUD:en. För kampanjen (ordinariebanor) är det den absoluta
        /// session-klockan <see cref="GameContext.GameTotalTime"/> som ackumuleras mellan banor.
        /// För en user map-körning (My Maps eller editor-preview) visas i stället per-körnings-
        /// deltat GameTotalTime − UserMapRunStartTime, så klockan börjar på 0 varje spelomgång.
        /// Ändrar bara VISNINGEN — själva GameTotalTime rörs aldrig, så kampanj-timingen är oförändrad.
        /// internal för enhetstest.
        /// </summary>
        internal static System.TimeSpan ShownTime(GameContext context)
        {
            bool isUserMapRun = context.UserMapSlotId != null || context.IsPreviewMode;
            return isUserMapRun
                ? context.GameTotalTime - context.UserMapRunStartTime
                : context.GameTotalTime;
        }

        /// <summary>
        /// Ritar slutbossens hälsobar: rött fält som töms när du stampar bossen (placeholder-stil).
        /// Akt 4 (Spegeln) visar ingen bar — hotet läses i hennes glitch, inte i en mätare.
        /// </summary>
        private static void DrawBossBar(IRenderContext rc, GameContext context)
        {
            var bp = context.BossPhase!;

            // Akt 4 (Spegeln): ingen bar — hotet läses i hennes glitch, inte i en mätare.
            if (bp.CurrentAct == BossAct.Acceptance || bp.CurrentAct == BossAct.Resolved)
                return;

            int x = 2;
            int w = context.ScreenWidth - 4;
            int h = 3;

            // Boss-hälsa (töms när du stampar).
            int y1 = 11;
            rc.FillRect(x, y1, w, h, RenderColor.DarkGrey);
            if (bp.BossMaxHealth > 0)
            {
                int fill = (int)(w * (bp.BossHealth / (float)bp.BossMaxHealth));
                if (fill > 0) rc.FillRect(x, y1, fill, h, RenderColor.Red);
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
