using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Systems;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Tester för input-abstraktion via IInputProvider/FakeInputProvider.
    /// Verifierar att kontrakt och idle-logik fungerar korrekt
    /// utan behov av riktig hårdvara eller PixelEngine.
    /// </summary>
    [TestClass]
    public class InputProviderTests
    {
        private FakeInputProvider _input = null!;

        [TestInitialize]
        public void Setup()
        {
            _input = new FakeInputProvider();
        }

        // ─────────────────────────────────────────────
        // Standardtillstånd
        // ─────────────────────────────────────────────

        [TestMethod]
        public void AllButtons_AreOff_ByDefault()
        {
            Assert.IsFalse(_input.IsRightDown);
            Assert.IsFalse(_input.IsLeftDown);
            Assert.IsFalse(_input.IsJumpDown);
            Assert.IsFalse(_input.IsJumpPressed);
            Assert.IsFalse(_input.IsConfirmPressed);
            Assert.IsFalse(_input.IsCancelPressed);
        }

        [TestMethod]
        public void ButtonsHasGoneIdle_IsFalse_ByDefault()
        {
            Assert.IsFalse(_input.ButtonsHasGoneIdle);
        }

        // ─────────────────────────────────────────────
        // Idle-logik
        // ─────────────────────────────────────────────

        [TestMethod]
        public void ResetIdle_SetsBhasGoneIdle_ToFalse()
        {
            _input.ButtonsHasGoneIdle = true;
            _input.ResetIdle();
            Assert.IsFalse(_input.ButtonsHasGoneIdle);
        }

        [TestMethod]
        public void CanSetIdle_AndReadItBack()
        {
            _input.IsIdle = true;
            Assert.IsTrue(_input.IsIdle);
        }

        // ─────────────────────────────────────────────
        // Hoppknappens tillstånd
        // ─────────────────────────────────────────────

        [TestMethod]
        public void JumpButtonState_IsZero_ByDefault()
        {
            Assert.AreEqual(0, _input.JumpButtonState);
        }

        [TestMethod]
        public void JumpButtonDownRelease_CanBeSetAndReset()
        {
            _input.JumpButtonDownRelease = true;
            Assert.IsTrue(_input.JumpButtonDownRelease);

            _input.JumpButtonDownRelease = false;
            Assert.IsFalse(_input.JumpButtonDownRelease);
        }

        [TestMethod]
        public void JumpButtonDownReleaseOnce_CanBeSetAndReset()
        {
            _input.JumpButtonDownReleaseOnce = true;
            Assert.IsTrue(_input.JumpButtonDownReleaseOnce);

            _input.JumpButtonDownReleaseOnce = false;
            Assert.IsFalse(_input.JumpButtonDownReleaseOnce);
        }

        // ─────────────────────────────────────────────
        // IInputProvider-kontrakt
        // ─────────────────────────────────────────────

        [TestMethod]
        public void FakeInputProvider_ImplementsIInputProvider()
        {
            // Verifierar att FakeInputProvider faktiskt implementerar interfacet
            IInputProvider provider = _input;
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Poll_DoesNotThrow()
        {
            // Poll ska vara en no-op i fake – ska inte kasta
            _input.Poll();
        }

        [TestMethod]
        public void SimulatedRightMovement_ReturnsCorrectState()
        {
            _input.IsRightDown    = true;
            _input.IsRightPressed = true;

            Assert.IsTrue(_input.IsRightDown);
            Assert.IsTrue(_input.IsRightPressed);
            Assert.IsFalse(_input.IsLeftDown);
        }

        [TestMethod]
        public void SimulatedJump_SetsJumpState()
        {
            _input.IsJumpPressed    = true;
            _input.IsJumpDown       = true;
            _input.JumpButtonState  = 1;
            _input.JumpButtonCounter = 1;

            Assert.IsTrue(_input.IsJumpPressed);
            Assert.AreEqual(1, _input.JumpButtonState);
            Assert.AreEqual(1, _input.JumpButtonCounter);
        }

        // ─────────────────────────────────────────────
        // Editorn (E2c)
        // ─────────────────────────────────────────────

        [TestMethod]
        public void IsEditorToggleCollision_IsFalse_ByDefault()
        {
            Assert.IsFalse(_input.IsEditorToggleCollision);
        }

        [TestMethod]
        public void IsEditorToggleCollision_CanBeSimulated()
        {
            _input.IsEditorToggleCollision = true;
            Assert.IsTrue(_input.IsEditorToggleCollision);
        }

        // ─────────────────────────────────────────────
        // Mus-input (E1a)
        // ─────────────────────────────────────────────

        [TestMethod]
        public void MouseProperties_AreZeroOrFalse_ByDefault()
        {
            Assert.AreEqual(0, _input.MouseX);
            Assert.AreEqual(0, _input.MouseY);
            Assert.IsFalse(_input.IsMouseLeftDown);
            Assert.IsFalse(_input.IsMouseRightDown);
            Assert.IsFalse(_input.IsMouseLeftPressed);
            Assert.IsFalse(_input.IsMouseRightPressed);
            Assert.AreEqual(0, _input.MouseScrollDelta);
        }

        [TestMethod]
        public void MousePosition_CanBeSet_AndReadBack()
        {
            _input.MouseX = 320;
            _input.MouseY = 240;

            Assert.AreEqual(320, _input.MouseX);
            Assert.AreEqual(240, _input.MouseY);
        }

        [TestMethod]
        public void IsMouseLeftDown_CanBeSimulated()
        {
            _input.IsMouseLeftDown = true;
            Assert.IsTrue(_input.IsMouseLeftDown);
            Assert.IsFalse(_input.IsMouseRightDown);
        }

        [TestMethod]
        public void IsMouseRightDown_CanBeSimulated()
        {
            _input.IsMouseRightDown = true;
            Assert.IsTrue(_input.IsMouseRightDown);
            Assert.IsFalse(_input.IsMouseLeftDown);
        }

        [TestMethod]
        public void IsMouseLeftPressed_CanBeSimulated_IndependentlyOfDown()
        {
            // Pressed är en edge-trigger — kan vara true utan att Down är true (fast osannolikt i prod)
            _input.IsMouseLeftPressed = true;
            Assert.IsTrue(_input.IsMouseLeftPressed);
            Assert.IsFalse(_input.IsMouseLeftDown);
        }

        [TestMethod]
        public void IsMouseRightPressed_CanBeSimulated()
        {
            _input.IsMouseRightPressed = true;
            Assert.IsTrue(_input.IsMouseRightPressed);
        }

        [TestMethod]
        public void MouseScrollDelta_CanBePositive_And_Negative()
        {
            _input.MouseScrollDelta = 120;
            Assert.AreEqual(120, _input.MouseScrollDelta);

            _input.MouseScrollDelta = -120;
            Assert.AreEqual(-120, _input.MouseScrollDelta);
        }

        [TestMethod]
        public void FakeInputProvider_ImplementsMousePropertiesOfIInputProvider()
        {
            IInputProvider provider = _input;
            _input.MouseX = 100;
            _input.MouseY = 200;
            _input.IsMouseLeftDown = true;
            _input.MouseScrollDelta = 40;

            Assert.AreEqual(100, provider.MouseX);
            Assert.AreEqual(200, provider.MouseY);
            Assert.IsTrue(provider.IsMouseLeftDown);
            Assert.AreEqual(40, provider.MouseScrollDelta);
        }
    }
}
