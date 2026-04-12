#nullable enable
using System;
using System.Collections.Generic;
using FrostyPlatformer.Core;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Testimplementation av IEventBus som registrerar publicerade händelser
    /// utan att faktiskt skicka dem vidare till prenumeranter.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Test Double (Spy) — del av Observer-teststrategin
    ///
    /// MOTIVERING:
    /// System som publicerar händelser ska kunna testas isolerat utan att
    /// faktiska prenumeranter (AudioSystem, ScoreSystem etc.) behöver vara
    /// aktiva. FakeEventBus låter testerna verifiera att rätt händelse
    /// publicerades med rätt data, utan sidoeffekter.
    ///
    /// ANVÄNDNING:
    /// Instantieras i testklass och injiceras i det system som testas.
    /// Kontrollera PublishedEvents efter systemanropet för att verifiera
    /// att rätt händelsetyp och data publicerades.
    /// </remarks>
    public class FakeEventBus : IEventBus
    {
        /// <summary>Alla publicerade händelser i publiceringsordning.</summary>
        public List<object> PublishedEvents { get; } = new List<object>();

        /// <summary>No-op — faken registrerar inga prenumeranter.</summary>
        public void Subscribe<T>(Action<T> handler) { }

        /// <summary>No-op.</summary>
        public void Unsubscribe<T>(Action<T> handler) { }

        /// <summary>Sparar händelsen i PublishedEvents utan att skicka den vidare.</summary>
        public void Publish<T>(T gameEvent) => PublishedEvents.Add(gameEvent!);

        /// <summary>
        /// Returnerar den senaste publicerade händelsen av typ T, eller null om ingen finns.
        /// Hjälpmetod för kompakta testassertioner.
        /// </summary>
        public T? LastOf<T>() where T : class
        {
            for (int i = PublishedEvents.Count - 1; i >= 0; i--)
            {
                if (PublishedEvents[i] is T match)
                    return match;
            }
            return null;
        }

        /// <summary>Returnerar antalet publicerade händelser av typ T.</summary>
        public int CountOf<T>()
        {
            int count = 0;
            foreach (var e in PublishedEvents)
                if (e is T) count++;
            return count;
        }
    }
}
