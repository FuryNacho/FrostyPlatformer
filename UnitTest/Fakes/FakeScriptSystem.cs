#nullable enable
using FrostyPlatformer.Systems;

namespace UnitTest.Fakes
{
    /// <summary>
    /// Teststubb för IScriptSystem. Räknar tick-anrop utan att köra
    /// kommandoköns logik — möjliggör verifiering utan Aggregate-beroende.
    /// </summary>
    public class FakeScriptSystem : IScriptSystem
    {
        public int TickCount { get; private set; }
        public int CompleteCommandCount { get; private set; }

        public void Tick(float elapsed) => TickCount++;

        public void CompleteCurrentCommand() => CompleteCommandCount++;
    }
}
