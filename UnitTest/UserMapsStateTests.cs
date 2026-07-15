using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Models;
using FrostyPlatformer.States;

namespace UnitTest
{
    /// <summary>
    /// Tester för UserMapsState-listans radformatering.
    /// Låser buggen där spelarens initialer (Handle) sparades men aldrig visades —
    /// de ska stå efter banans tid i My Maps-listan.
    /// </summary>
    [TestClass]
    public class UserMapsStateTests
    {
        [TestMethod]
        public void FormatSlotLabel_WithRecord_ShowsTimeThenHandle()
        {
            var record = new UserMapScore
            {
                SlotId   = "slot1",
                Handle   = "ACE",
                BestTime = new TimeSpan(0, 1, 30)
            };

            string label = UserMapsState.FormatSlotLabel("slot1", record);

            StringAssert.Contains(label, "slot1");
            StringAssert.Contains(label, "1:30.00");
            // Initialerna ska stå efter tiden — kärnan i buggfixen
            Assert.IsTrue(label.TrimEnd().EndsWith("ACE"),
                $"Initialerna ska visas sist i raden, men label var: '{label}'");
            Assert.IsTrue(label.IndexOf("1:30.00", StringComparison.Ordinal)
                        < label.IndexOf("ACE", StringComparison.Ordinal),
                "Tiden ska komma före initialerna");
        }

        [TestMethod]
        public void FormatSlotLabel_NoRecord_ShowsNoHighScore()
        {
            string label = UserMapsState.FormatSlotLabel("slot3", null);

            StringAssert.Contains(label, "slot3");
            StringAssert.Contains(label, "No high score set");
        }
    }
}
