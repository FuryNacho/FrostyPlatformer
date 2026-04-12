#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FrostyPlatformer.Core;
using FrostyPlatformer.Events;

namespace UnitTest
{
    /// <summary>
    /// Enhetstester för EventBus — verifierar att publish-subscribe-kontraktet
    /// fungerar korrekt utan spelberoenden.
    /// </summary>
    [TestClass]
    public class EventBusTests
    {
        // ─────────────────────────────────────────────
        // Subscribe / Publish
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Publish_WithSubscriber_HandlerReceivesEvent()
        {
            var bus = new EventBus();
            EnemyDiedEvent? received = null;
            bus.Subscribe<EnemyDiedEvent>(e => received = e);

            var evt = new EnemyDiedEvent("enemyone", 5f, 10f);
            bus.Publish(evt);

            Assert.IsNotNull(received);
            Assert.AreSame(evt, received);
        }

        [TestMethod]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            var bus = new EventBus();
            // Ska inte kasta — inga prenumeranter är ett giltigt tillstånd
            bus.Publish(new PlayerDiedEvent());
        }

        [TestMethod]
        public void Publish_WithMultipleSubscribers_AllReceiveEvent()
        {
            var bus = new EventBus();
            int callCount = 0;
            bus.Subscribe<PlayerHitEvent>(_ => callCount++);
            bus.Subscribe<PlayerHitEvent>(_ => callCount++);

            bus.Publish(new PlayerHitEvent(5));

            Assert.AreEqual(2, callCount);
        }

        // ─────────────────────────────────────────────
        // Unsubscribe
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Unsubscribe_RegisteredHandler_HandlerNoLongerReceivesEvent()
        {
            var bus = new EventBus();
            int callCount = 0;
            void Handler(ScoreChangedEvent _) => callCount++;

            bus.Subscribe<ScoreChangedEvent>(Handler);
            bus.Publish(new ScoreChangedEvent(100));
            Assert.AreEqual(1, callCount, "Förväntat ett anrop före Unsubscribe.");

            bus.Unsubscribe<ScoreChangedEvent>(Handler);
            bus.Publish(new ScoreChangedEvent(200));
            Assert.AreEqual(1, callCount, "Förväntat noll ytterligare anrop efter Unsubscribe.");
        }

        [TestMethod]
        public void Unsubscribe_NonRegisteredHandler_DoesNotThrow()
        {
            var bus = new EventBus();
            // Ska inte kasta — okänd prenumerant är ett giltigt anrop
            bus.Unsubscribe<ItemCollectedEvent>(_ => { });
        }

        // ─────────────────────────────────────────────
        // Typoberoende
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Subscribe_DifferentEventTypes_OnlyCorrectHandlerFires()
        {
            var bus = new EventBus();
            bool enemyHandlerFired = false;
            bool playerHandlerFired = false;

            bus.Subscribe<EnemyDiedEvent>(_ => enemyHandlerFired = true);
            bus.Subscribe<PlayerHitEvent>(_ => playerHandlerFired = true);

            bus.Publish(new EnemyDiedEvent("enemyone", 0f, 0f));

            Assert.IsTrue(enemyHandlerFired,   "EnemyDied-handlern ska ha körts.");
            Assert.IsFalse(playerHandlerFired, "PlayerHit-handlern ska INTE ha körts.");
        }

        // ─────────────────────────────────────────────
        // Händelsedata
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Publish_EnemyDiedEvent_DataPassedCorrectly()
        {
            var bus = new EventBus();
            EnemyDiedEvent? received = null;
            bus.Subscribe<EnemyDiedEvent>(e => received = e);

            bus.Publish(new EnemyDiedEvent("enemyboss", 3.5f, 12.0f));

            Assert.AreEqual("enemyboss", received!.EnemyName);
            Assert.AreEqual(3.5f,        received.PositionX);
            Assert.AreEqual(12.0f,       received.PositionY);
        }

        [TestMethod]
        public void Publish_PlayerHitEvent_DamagePassedCorrectly()
        {
            var bus = new EventBus();
            int receivedDamage = 0;
            bus.Subscribe<PlayerHitEvent>(e => receivedDamage = e.Damage);

            bus.Publish(new PlayerHitEvent(10));

            Assert.AreEqual(10, receivedDamage);
        }

        [TestMethod]
        public void Publish_ScoreChangedEvent_NewScorePassedCorrectly()
        {
            var bus = new EventBus();
            int receivedScore = -1;
            bus.Subscribe<ScoreChangedEvent>(e => receivedScore = e.NewScore);

            bus.Publish(new ScoreChangedEvent(999));

            Assert.AreEqual(999, receivedScore);
        }

        // ─────────────────────────────────────────────
        // Unsubscribe under pågående Publish
        // ─────────────────────────────────────────────

        [TestMethod]
        public void Publish_HandlerUnsubscribesDuringPublish_DoesNotThrow()
        {
            var bus = new EventBus();
            void Handler(LevelCompletedEvent _) => bus.Unsubscribe<LevelCompletedEvent>(Handler);

            bus.Subscribe<LevelCompletedEvent>(Handler);
            // Ska inte kasta InvalidOperationException (listan kopieras internt)
            bus.Publish(new LevelCompletedEvent("mapone"));
        }
    }
}
