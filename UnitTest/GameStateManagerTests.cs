#nullable enable
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Core;
using FrostyPlatformer.Rendering;
using FrostyPlatformer.States;
using UnitTest.Fakes;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för GameStateManager.
    /// </summary>
    /// <remarks>
    /// Testar att tillståndsövergngar är korrekt uppskjutna (deferred), att Enter/Exit/Update/Draw
    /// delegeras till rätt tillstånd, och att SetInitial aktiverar det initiala tillståndet.
    /// </remarks>
    [TestClass]
    public class GameStateManagerTests
    {
        // ── Test doubles ─────────────────────────────────────────────────────────

        /// <summary>Registrerande IGameState-fake som loggar alla anrop.</summary>
        private class SpyState : IGameState
        {
            public List<string> Calls { get; } = new List<string>();

            public void Enter(GameContext context)  => Calls.Add("Enter");
            public void Update(GameContext context, float elapsed) => Calls.Add("Update");
            public void Draw(IRenderContext rc)     => Calls.Add("Draw");
            public void Exit(GameContext context)   => Calls.Add("Exit");
        }

        // Bygghjälpare
        private static GameContext Context() => new GameContext();

        // ── SetInitial ────────────────────────────────────────────────────────────

        [TestMethod]
        public void SetInitial_SetsCurrentState()
        {
            var gsm   = new GameStateManager();
            var state = new SpyState();

            gsm.SetInitial(state, Context());

            Assert.AreEqual(state, gsm.Current);
        }

        [TestMethod]
        public void SetInitial_CallsEnterOnState()
        {
            var gsm   = new GameStateManager();
            var state = new SpyState();

            gsm.SetInitial(state, Context());

            CollectionAssert.Contains(state.Calls, "Enter");
        }

        [TestMethod]
        public void SetInitial_DoesNotCallUpdate()
        {
            var gsm   = new GameStateManager();
            var state = new SpyState();

            gsm.SetInitial(state, Context());

            CollectionAssert.DoesNotContain(state.Calls, "Update");
        }

        // ── Transition (deferred) ─────────────────────────────────────────────────

        [TestMethod]
        public void Transition_DoesNotSwitchImmediately()
        {
            var gsm    = new GameStateManager();
            var first  = new SpyState();
            var second = new SpyState();
            var ctx    = Context();

            gsm.SetInitial(first, ctx);
            gsm.Transition(second, ctx);

            // Fortfarande first fram till nästa Update
            Assert.AreEqual(first, gsm.Current);
        }

        [TestMethod]
        public void Update_AppliesPendingTransitionBeforeCallingUpdate()
        {
            var gsm    = new GameStateManager();
            var first  = new SpyState();
            var second = new SpyState();
            var ctx    = Context();

            gsm.SetInitial(first, ctx);
            gsm.Transition(second, ctx);
            gsm.Update(ctx, 0.016f);

            // Nuvarande tillstånd är second
            Assert.AreEqual(second, gsm.Current);
            // second fick Enter innan Update
            Assert.AreEqual("Enter",  second.Calls[0]);
            Assert.AreEqual("Update", second.Calls[1]);
        }

        [TestMethod]
        public void Update_CallsExitOnOldStateBeforeEnterOnNew()
        {
            var gsm    = new GameStateManager();
            var first  = new SpyState();
            var second = new SpyState();
            var ctx    = Context();

            gsm.SetInitial(first, ctx);
            gsm.Transition(second, ctx);
            gsm.Update(ctx, 0.016f);

            // first fick Exit
            CollectionAssert.Contains(first.Calls, "Exit");
        }

        [TestMethod]
        public void Update_WithNoPendingTransition_CallsUpdateOnCurrentState()
        {
            var gsm   = new GameStateManager();
            var state = new SpyState();
            var ctx   = Context();

            gsm.SetInitial(state, ctx);
            state.Calls.Clear(); // rensa Enter-anropet

            gsm.Update(ctx, 0.016f);

            Assert.AreEqual(1, state.Calls.Count);
            Assert.AreEqual("Update", state.Calls[0]);
        }

        // ── Draw ─────────────────────────────────────────────────────────────────

        [TestMethod]
        public void Draw_DelegatesToCurrentState()
        {
            var gsm   = new GameStateManager();
            var state = new SpyState();
            var rc    = new FakeRenderContext();

            gsm.SetInitial(state, Context());
            state.Calls.Clear();

            gsm.Draw(rc);

            CollectionAssert.Contains(state.Calls, "Draw");
        }

        // ── Sekvens ───────────────────────────────────────────────────────────────

        [TestMethod]
        public void MultipleTransitions_OnlyLatestPendingIsApplied()
        {
            var gsm    = new GameStateManager();
            var first  = new SpyState();
            var second = new SpyState();
            var third  = new SpyState();
            var ctx    = Context();

            gsm.SetInitial(first, ctx);
            gsm.Transition(second, ctx);
            gsm.Transition(third, ctx); // ersätter second

            gsm.Update(ctx, 0.016f);

            Assert.AreEqual(third, gsm.Current);
            CollectionAssert.DoesNotContain(second.Calls, "Enter");
        }
    }
}
