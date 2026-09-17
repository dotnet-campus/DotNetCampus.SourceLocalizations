using System.ComponentModel;
using ProviderLanguageChange.Application;
using ProviderLanguageChange.Library;

var notificationCount = 0;
LibraryLang.AddProvider(AppLang.Current, 100);
LibraryLang.Current.PropertyChanged += OnPropertyChanged;

AssertEqual("应用中文", LibraryLang.Current.Shared.Title.Value, "App provider should override library text.");
AppLang.SetCurrent("en");
AssertEqual("Application English", LibraryLang.Current.Shared.Title.Value, "Library should read the switched App language.");

if (notificationCount == 0)
{
    throw new InvalidOperationException("Library did not forward App provider language-change notifications.");
}

Console.WriteLine("PASS");
return 0;

void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    notificationCount++;
}

static void AssertEqual(string expected, string actual, string message)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{message} Expected: {expected}; Actual: {actual}.");
    }
}
