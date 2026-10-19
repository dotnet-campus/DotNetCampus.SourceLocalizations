using DotNetCampus.Localizations;
using ProviderPriority;

var positive = new TestProvider(("Text.Own", "Positive"));
var zero = new TestProvider(("Text.Missing", "Zero"));
var negative = new TestProvider(("Text.Fallback", "Negative"));

Lang.AddProvider(positive, 100);
Lang.AddProvider(zero);
Lang.AddProvider(negative, -1);

AssertEqual("Positive", Lang.Current.Text.Own.Value, "Positive priority provider should override Lang resources.");
AssertEqual("Zero", Lang.Current.Text.Missing.Value, "Priority zero provider should fill a missing Lang value.");
AssertEqual("Negative", Lang.Current.Text.Fallback.Value, "Negative priority provider should act as fallback.");

Lang.RemoveProvider(positive);
AssertEqual("Own", Lang.Current.Text.Own.Value, "Removing override should restore Lang resources.");

Console.WriteLine("PASS");
return 0;

static void AssertEqual(string expected, string actual, string message)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{message} Expected: {expected}; Actual: {actual}.");
    }
}

internal sealed class TestProvider(params (string Key, string Value)[] values) : ILocalizedStringProvider
{
    private readonly Dictionary<string, string> _values = values.ToDictionary(item => item.Key, item => item.Value);

    public string IetfLanguageTag => "zh-Hans";

    public string this[string key] => _values.TryGetValue(key, out var value) ? value : string.Empty;
}
