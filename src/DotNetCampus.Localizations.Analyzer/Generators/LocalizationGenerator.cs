using Microsoft.CodeAnalysis;

namespace DotNetCampus.Localizations.Generators;

/// <summary>
/// 本地化源生成器的统一入口。
/// </summary>
[Generator]
public class LocalizationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        new InterfaceTreeGenerator().Register(context);
        new LocalizationMainClassGenerator().Register(context);
        new NestedTypesGenerator().Register(context);
        new CompiledValuesGenerator().Register(context);
        new DictionaryValuesGenerator().Register(context);
        new StringsProviderGenerator().Register(context);
    }
}
