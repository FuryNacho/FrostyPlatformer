using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Core;
using FrostyPlatformer.States;

namespace UnitTest
{
    /// <summary>
    /// Tester för HudRenderer.ShownTime — vilken tid HUD-klockan visar.
    /// Låser buggen där my maps-körningar visade den ackumulerade session-klockan
    /// i stället för att nollställas per spelomgång, samtidigt som kampanjens
    /// (ordinariebanornas) ackumulerande tid måste vara oförändrad.
    /// </summary>
    [TestClass]
    public class HudRendererTests
    {
        [TestMethod]
        public void Campaign_ShowsAbsoluteGameTotalTime()
        {
            var ctx = new GameContext
            {
                GameTotalTime       = new TimeSpan(0, 5, 30),
                // Stale baslinje från en tidigare körning ska INTE påverka kampanjen
                UserMapRunStartTime = new TimeSpan(0, 2, 0),
                UserMapSlotId       = null,
                IsPreviewMode       = false
            };

            Assert.AreEqual(new TimeSpan(0, 5, 30), HudRenderer.ShownTime(ctx));
        }

        [TestMethod]
        public void MyMapsRun_ShowsDeltaSinceRunStart()
        {
            var ctx = new GameContext
            {
                GameTotalTime       = new TimeSpan(0, 10, 0),
                UserMapRunStartTime = new TimeSpan(0, 8, 30),
                UserMapSlotId       = "slot1"
            };

            Assert.AreEqual(new TimeSpan(0, 1, 30), HudRenderer.ShownTime(ctx));
        }

        [TestMethod]
        public void PreviewRun_ShowsDeltaSinceRunStart()
        {
            var ctx = new GameContext
            {
                GameTotalTime       = new TimeSpan(0, 3, 0),
                UserMapRunStartTime = new TimeSpan(0, 3, 0),
                IsPreviewMode       = true
            };

            Assert.AreEqual(TimeSpan.Zero, HudRenderer.ShownTime(ctx));
        }
    }
}
