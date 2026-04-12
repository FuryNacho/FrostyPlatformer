using System;
using System.Collections.Generic;

namespace FrostyPlatformer.Core
{
    /// <summary>
    /// Konkret implementation av IEventBus — en typbaserad publish-subscribe-buss
    /// som kopplar samman spelsystem utan direkta referenser.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Observer (Publish-Subscribe)
    ///
    /// MOTIVERING:
    /// Se IEventBus för fullständig motivering. EventBus är den produktion-
    /// implementering som Program.cs (Composition Root) skapar och injicerar.
    /// Implementeringen är avsiktligt enkel: en Dictionary från händelsetyp
    /// till lista av delegater. Inga trådsäkerhetsgarantier ges — spelet
    /// körs enkeltrådat i spelloopen.
    ///
    /// ANVÄNDNING:
    /// Skapas en gång i Program.cs och injiceras i alla system som behöver
    /// publicera eller prenumerera på händelser. Använd FakeEventBus i tester.
    /// </remarks>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers
            = new Dictionary<Type, List<Delegate>>();

        /// <inheritdoc />
        public void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();
            _handlers[type].Add(handler);
        }

        /// <inheritdoc />
        public void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
                list.Remove(handler);
        }

        /// <inheritdoc />
        public void Publish<T>(T gameEvent)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
                return;

            // Kopiera listan innan iteration — en handler kan anropa Unsubscribe
            // under sin körning utan att orsaka InvalidOperationException.
            foreach (var handler in list.ToArray())
                ((Action<T>)handler)(gameEvent);
        }
    }
}
