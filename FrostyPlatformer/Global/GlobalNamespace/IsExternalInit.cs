// Workaround: möjliggör C# 9/10 records och init-accessors på .NET Framework 4.7.2.
// Typen System.Runtime.CompilerServices.IsExternalInit finns normalt i .NET 5+
// och krävs av kompilatorn för att tillåta init-only setters.
// Denna stub är en tom platshållare — inga funktioner, inget beteende.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
