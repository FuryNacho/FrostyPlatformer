#nullable enable
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Core;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för ReadWrite.NormalizeSeparators — verifierar att
    /// sökvägskonstanter skrivna med Windows-backslash normaliseras till
    /// plattformens egen separator, så att fil-I/O fungerar även på Linux
    /// (t.ex. Raspberry Pi där '\' är ett giltigt filnamnstecken, inte en
    /// mappavgränsare). Ren strängtestning utan disk-I/O — NormalizeSeparators
    /// är internal och nås via InternalsVisibleTo.
    /// </summary>
    [TestClass]
    public class ReadWriteTests
    {
        // Plattformens separator ('\' på Windows, '/' på Linux) och den "främmande"
        // separatorn som INTE får finnas kvar efter normalisering. Uttryckt relativt
        // körplattformen så att testerna passerar på både Windows och Linux.
        private static readonly char Sep     = Path.DirectorySeparatorChar;
        private static readonly char Foreign = Path.DirectorySeparatorChar == '\\' ? '/' : '\\';

        [TestMethod]
        public void NormalizeSeparators_Backslashes_BecomePlatformSeparator()
        {
            var result = ReadWrite.NormalizeSeparators(@"\Resources\Assets\Sprites");

            Assert.AreEqual($"{Sep}Resources{Sep}Assets{Sep}Sprites", result);
        }

        [TestMethod]
        public void NormalizeSeparators_ForwardSlashes_BecomePlatformSeparator()
        {
            var result = ReadWrite.NormalizeSeparators("Resources/Assets/Sprites");

            Assert.AreEqual($"Resources{Sep}Assets{Sep}Sprites", result);
        }

        [TestMethod]
        public void NormalizeSeparators_MixedSeparators_AllBecomePlatformSeparator()
        {
            var result = ReadWrite.NormalizeSeparators(@"root/sub\file");

            Assert.AreEqual($"root{Sep}sub{Sep}file", result);
        }

        [TestMethod]
        public void NormalizeSeparators_ConcatenatedSpritePath_LeavesNoForeignSeparators()
        {
            // Efterliknar det CreateIfNotExists bygger: rot + \Resources...\ + \hero + .png.
            // Just detta fall (backslash på Linux) gjorde att Texture2D.FromFile inte
            // hittade filen — hela porten till Raspberry Pi hängde på det här.
            var result = ReadWrite.NormalizeSeparators(@"root/game/\Resources\Assets\Sprites\hero.png");

            Assert.IsFalse(result.Contains(Foreign),
                "Ingen främmande separator får finnas kvar — det är exakt vad som bryter fil-I/O på Linux.");
            Assert.IsTrue(result.EndsWith($"Sprites{Sep}hero.png"));
        }

        [TestMethod]
        public void NormalizeSeparators_AlreadyNormalized_IsIdempotent()
        {
            var once  = ReadWrite.NormalizeSeparators(@"\a\b\c");
            var twice = ReadWrite.NormalizeSeparators(once);

            Assert.AreEqual(once, twice);
        }
    }
}
