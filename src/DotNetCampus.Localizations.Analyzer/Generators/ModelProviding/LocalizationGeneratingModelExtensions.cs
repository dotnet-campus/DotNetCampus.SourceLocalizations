using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using DotNetCampus.Localizations.Utils.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetCampus.Localizations.Generators.ModelProviding;

/// <summary>
/// 为 <see cref="LocalizationGeneratingModel"/> 提供扩展方法。
/// </summary>
public static class LocalizationGeneratingModelExtensions
{
    public static IncrementalValuesProvider<LocalizationFileModel> SelectLocalizationFileModels(this IncrementalGeneratorInitializationContext context) =>
        context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(pair =>
                // 标记了 DotNetCampusLocalization 的文件才生成。
                (pair.Right.GetOptions(pair.Left).TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var t) && t.Equals("DotNetCampusLocalization", StringComparison.OrdinalIgnoreCase))
                // 目前只支持 toml 和 yaml 格式的文件。
                && (pair.Left.Path.EndsWith(".toml", StringComparison.OrdinalIgnoreCase)
                    || pair.Left.Path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                    || pair.Left.Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            )
            .Select((x, ct) => x.Left)
            .Select((x, ct) =>
            {
                var ietfLanguageTag = IetfLanguageTagExtensions.GuessIetfLanguageTagFromFileName(Path.GetFileNameWithoutExtension(x.Path));
                var extension = Path.GetExtension(x.Path) switch
                {
                    ".toml" => "toml",
                    ".yaml" or ".yml" => "yaml",
                    _ => throw new NotSupportedException($"Unsupported localization file format: {x.Path}"),
                };
                var text = x.GetText(ct)!.ToString();
                return new LocalizationFileModel(extension, ietfLanguageTag, text);
            });

    /// <summary>
    /// 从增量源生成器的语法值提供器中挑选出所有的 <see cref="LocalizationGeneratingModel"/>。
    /// </summary>
    /// <param name="syntaxValueProvider">语法值提供器。</param>
    /// <returns>增量值提供器。</returns>
    public static IncrementalValuesProvider<LocalizationGeneratingModel> SelectGeneratingModels(this SyntaxValueProvider syntaxValueProvider) =>
        syntaxValueProvider.ForAttributeWithMetadataName(typeof(LocalizedConfigurationAttribute).FullName!, (node, ct) =>
        {
            if (node is not ClassDeclarationSyntax cds)
            {
                // 必须是类型。
                return false;
            }

            if (!cds.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                // 必须是分部类。
                return false;
            }

            return true;
        }, (c, ct) =>
        {
            var typeSymbol = c.TargetSymbol;
            var rootNamespace = typeSymbol.ContainingNamespace.ToDisplayString();
            var typeName = typeSymbol.Name;
            var attribute = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass!.IsAttributeOf<LocalizedConfigurationAttribute>());
            var namedArguments = attribute!.NamedArguments.ToImmutableDictionary();
            var defaultLanguage = namedArguments.GetValueOrDefault(nameof(LocalizedConfigurationAttribute.Default)).Value?.ToString()!;
            var currentLanguage = namedArguments.GetValueOrDefault(nameof(LocalizedConfigurationAttribute.Current)).Value?.ToString();
            var generationMode = namedArguments.GetValueOrDefault(nameof(LocalizedConfigurationAttribute.GenerationMode)).Value is int gm ? (GenerationMode)gm : GenerationMode.Dictionary;
            var notificationMode = namedArguments.GetValueOrDefault(nameof(LocalizedConfigurationAttribute.NotificationMode)).Value is int nm ? (NotificationMode)nm : NotificationMode.InitOnly;
            var dependencyMode = namedArguments.GetValueOrDefault(nameof(LocalizedConfigurationAttribute.DependencyMode)).Value is int dm ? (DependencyMode)dm : DependencyMode.Library;
            var ensureKeysIdentical = namedArguments.GetValueOrDefault(nameof(LocalizedConfigurationAttribute.EnsureKeysIdentical)).Value is true;

            // 兼容旧属性：仅当未显式设置 NotificationMode 时，SupportsNotification = true 才生效。
            if (notificationMode == NotificationMode.InitOnly
#pragma warning disable CS0618
                && namedArguments.GetValueOrDefault(nameof(LocalizedConfigurationAttribute.SupportsNotification)).Value is true)
#pragma warning restore CS0618
            {
                notificationMode = NotificationMode.CurrentCulturePropertyChanged;
            }

            return new LocalizationGeneratingModel(rootNamespace, typeName)
            {
                DefaultLanguage = defaultLanguage,
                CurrentLanguage = currentLanguage,
                GenerationMode = generationMode,
                NotificationMode = notificationMode,
                DependencyMode = dependencyMode,
                EnsureKeysIdentical = ensureKeysIdentical,
            };
        });
}
