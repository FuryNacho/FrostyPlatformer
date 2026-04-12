#nullable enable
namespace FrostyPlatformer.Systems
{
    /// <summary>
    /// Abstraherar skript-processorn och tillåter states att ticka kommandokön
    /// utan att känna till Aggregate.Instance eller ScriptProcessor direkt.
    /// </summary>
    /// <remarks>
    /// MÖNSTER: Dependency Inversion (Adapter)
    ///
    /// MOTIVERING:
    /// States anropade tidigare Aggregate.Instance.Script.ProcessCommands(elapsed)
    /// direkt — sex identiska singleton-beroenden utspridda i alla states. IScriptSystem
    /// bryter beroendet: states anropar Tick(elapsed) utan vetskap om ScriptProcessor
    /// eller Aggregate.
    ///
    /// ANVÄNDNING:
    /// Skapas i Program.OnCreate() som new ScriptSystem() och injiceras via
    /// GameServices.Script. Varje state anropar _services.Script.Tick(elapsed)
    /// en gång per Update-anrop.
    /// </remarks>
    public interface IScriptSystem
    {
        /// <summary>Tickar kommandokön ett steg. Anropas en gång per frame i varje state.</summary>
        void Tick(float elapsed);

        /// <summary>
        /// Markerar det pågående skriptkommandot som klart så att köen kan gå vidare.
        /// Anropas av GameplayState när spelaren avfärdar en dialogruta.
        /// </summary>
        void CompleteCurrentCommand();
    }
}
