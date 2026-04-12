#nullable enable
using FrostyPlatformer.Core;

namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Delegerar Tick-anrop till Aggregates ScriptProcessor. Bryter states-beroendet
    /// på Aggregate.Instance utan att flytta eller duplicera kommandoköns logik.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Adapter
    ///
    /// MOTIVERING:
    /// ScriptProcessor ägs av Aggregate och används även av Map och Quest via statiska
    /// referenser. Att flytta den hade krävt bred omskrivning. ScriptSystem är ett tunt
    /// lager som exponerar enbart Tick(elapsed) mot states — utan att avslöja
    /// Aggregate som helhet.
    ///
    /// ANVÄNDNING:
    /// new ScriptSystem() i Program.OnCreate(). Kräver att Aggregate.Instance är
    /// initialiserat. Injiceras via GameServices.Script.
    /// </remarks>
    public class ScriptSystem : IScriptSystem
    {
        /// <inheritdoc/>
        public void Tick(float elapsed) => Aggregate.Instance.Script.ProcessCommands(elapsed);

        /// <inheritdoc/>
        public void CompleteCurrentCommand() => Aggregate.Instance.Script.CompletedCommand();
    }
}
