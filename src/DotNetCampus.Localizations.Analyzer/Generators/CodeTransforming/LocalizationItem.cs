using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 本地化项。
/// </summary>
/// <param name="Key">本地化项的键。</param>
/// <param name="Value">本地化项的值。</param>
/// <param name="SampleValue">
/// 有可能 <see cref="Value"/> 是格式化字符串，此时需要提供一个示例值，用于给开发者提供参考。
/// </param>
/// <param name="ValueArgumentTypes">
/// 当 <see cref="Value"/> 是格式化字符串时，此值表示格式化字符串中的参数类型。
/// </param>
/// <param name="Comments">此本地化项的注释。</param>
public readonly record struct LocalizationItem(
    string Key,
    string Value,
    string? SampleValue,
    ImmutableArray<string> ValueArgumentTypes,
    string? Comments)
{
    public static IEqualityComparer<LocalizationItem> KeyEqualityComparer { get; } = new KeyEqualityComparerImplementation();

    private class KeyEqualityComparerImplementation : IEqualityComparer<LocalizationItem>
    {
        public bool Equals(LocalizationItem x, LocalizationItem y)
        {
            return x.Key.Equals(y.Key, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(LocalizationItem obj)
        {
            return obj.Key.ToLowerInvariant().GetHashCode();
        }
    }
}
