using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DotNetCampus.Localizations;

namespace LocalizationSample;

internal class Program
{
    public static void Main(string[] args)
    {
        var tags = Lang.SupportedLanguageTags;
        Console.WriteLine(string.Join(", ", tags));

        Lang.ILocalizedStringProvider currentProvider = Lang.Current;
        Lang.AddProvider(new SampleLocalizedStringProvider(), priority: 100);

        var a = Lang.Current.A.A2.ToString(1);
        Console.WriteLine(a);
        Console.WriteLine(currentProvider["A.A1"]);

        if (Lang.Current is INotifyPropertyChanged changed)
        {
            changed.PropertyChanged += ChangedOnPropertyChanged;
        }

        Lang.SetCurrent("en");
    }

    private static void ChangedOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Console.WriteLine($"语言项变更：{e.PropertyName}");
    }
}

[LocalizedConfiguration(Default = "zh-Hans",
    EnsureKeysIdentical = true,
    DependencyMode = DependencyMode.NestedSource,
    GenerationMode = GenerationMode.Dictionary,
    NotificationMode = NotificationMode.InitOnly,
    SupportsAddingProviders = true)]
internal static partial class Lang;

internal sealed class SampleLocalizedStringProvider : Lang.ILocalizedStringProvider
{
    public string IetfLanguageTag => "zh-Hans";

    public string this[string key] => key == "A.A1" ? "覆盖文本" : string.Empty;
}
