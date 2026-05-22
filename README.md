# DotNetCampus.SourceLocalizations

| Build | NuGet |
|--|--|
|![](https://github.com/dotnet-campus/DotNetCampus.SourceLocalizations/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/DotNetCampus.SourceLocalizations.svg)](https://www.nuget.org/packages/DotNetCampus.SourceLocalizations)|

DotNetCampus.SourceLocalizations is a source generator that can convert text localization files (e.g. .toml) into C# code and provide strong type support for localization keys.

## Features

```csharp
static void Main()
{
    Console.WriteLine(LocalizedText.Current.App.Title);         // "Hello, World!"
    Console.WriteLine(LocalizedText.Current.App.Description);   // "This is a sample application."
    Console.WriteLine(LocalizedText.Current.Cli.Usage);         // "Usage: DotNetCampus.SourceLocalizations [options]"
    Console.WriteLine(LocalizedText.Current.PressAnyKeyToExit); // "Press any key to exit..."
}
```

- Source Generators
    - [x] Generate C# codes
    - [x] Generate properties for implementation types (so that reflections on types can get localized properties which is very important for WPF Bindings)
    - [ ] Generate localized types for each language item which contains more than one arguments (This fixes different argument orders among different languages.)
- File formats
    - [x] TOML
    - [x] YAML `🤡 Might be deprecated in the future.`
- UI Frameworks Support
    - [x] Avalonia      `😉 We look forward to your better suggestions.`
    - [ ] MAUI          `😶‍🌫️ Not tested yet`
    - [x] Uno Platform  `😉 We look forward to your better suggestions.`
    - [x] Wpf           `😉 We look forward to your better suggestions.`
- Diagnostics Analyzers and Code Fixes
    - [ ] Detect (and generate) missing localization keys
    - [ ] Detect (and remove) unused localization keys
    - [ ] Detect arguments mismatch among localized texts (e.g. `Hello, {name:string}` in en but `こんにちは、{errorCode:int}` in ja)
    - [ ] Detect invalid IETF language tags and report errors

## Installation

[![](https://img.shields.io/nuget/v/DotNetCampus.SourceLocalizations.svg)](https://www.nuget.org/packages/DotNetCampus.SourceLocalizations)

```shell
dotnet add package DotNetCampus.SourceLocalizations
```

## Usage

### 1. Create localization files

```toml
// Localizations/en.toml
App.Title = "Hello, World!"
App.Description = "This is a sample application."
Cli.Usage = "Usage: DotNetCampus.SourceLocalizations [options]"
PressAnyKeyToExit = "Press any key to exit..."
```

```toml
// Localizations/zh-hans.toml
App.Title = "你好，世界！"
App.Description = "这是一个示例应用程序。"
Cli.Usage = "用法：dotnetCampus.SourceLocalizations [选项]"
PressAnyKeyToExit = "按任意键退出..."
```

The file name must conform to the [IETF BCP 47 standard](https://en.wikipedia.org/wiki/IETF_language_tag).

### 2. Write a localization class

```csharp
// LocalizedText.cs
using DotNetCampus.SourceLocalizations;

namespace SampleApp;

// The default language is used to generate localization interfaces, so it must be the most complete one.
// The current language is optional. If not specified, the current OS UI language will be used.
// The notification is optional. If true, when the current language changes, the UI will be notified to update the localization text.
[LocalizedConfiguration(Default = "en", Current = "zh-hans", SupportsNotification = false)]
public partial class LocalizedText;
```

### 3. Use the generated code

Console, library or any other UI framework:

```csharp
// Program.cs
static void Main()
{
    Console.WriteLine(LocalizedText.Current.App.Title);         // "Hello, World!"
    Console.WriteLine(LocalizedText.Current.App.Description);   // "This is a sample application."
    Console.WriteLine(LocalizedText.Current.Cli.Usage);         // "Usage: DotNetCampus.SourceLocalizations [options]"
    Console.WriteLine(LocalizedText.Current.PressAnyKeyToExit); // "Press any key to exit..."
}
```

---

Avalonia:

```xml
<!-- Avalonia MainWindow.axaml -->
<TextBlock Text="{Binding App.Title, Source={x:Static l:LocalizedText.Current}}" />
<TextBlock Text="{Binding App.Description, Source={x:Static l:LocalizedText.Current}}" />
```

---

WPF:

```xml
<!-- WPF MainWindow.xaml -->
<TextBlock Text="{Binding App.Title, Source={x:Static l:LocalizedText.Current}, Mode=OneWay}" />
<TextBlock Text="{Binding App.Description, Source={x:Static l:LocalizedText.Current}, Mode=OneWay}" />
```

---

Uno Platform:

```xml
<!-- Uno Platform MainPage.xaml -->
<TextBlock Text="{x:Bind l:Lang.Current.App.Title}" />
<TextBlock Text="{x:Bind l:Lang.Current.App.Description}" />
```

```csharp
// Uno Platform MainPage.xaml.cs
using DotNetCampus.Localizations;
namespace DotNetCampus.SampleUnoApp;
public sealed partial class MainPage : Page
{
    public MainPage() => InitializeComponent();
    // IMPORTANT: The Lang property must be public.
    public ILocalizedValues Lang => global::DotNetCampus.SampleUnoApp.Localizations.LocalizedText.Current;
}
```

## Advanced Usage

If you want to add real-time language switching support, you can modify the `LocalizedText` class as follows:

```csharp
[LocalizedConfiguration(Default = "en-US", SupportsNotification = true)]
public static partial class LocalizedText
{
    public static AppBuilder UseCompiledLang(this AppBuilder appBuilder)
    {
        if (OperatingSystem.IsWindows())
        {
            var language = GetUserProfileLanguage() ?? CultureInfo.CurrentUICulture.Name;
            _ = SetCurrent(language);
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }
        else
        {
            // On other operating systems, the current language is automatically set to the current UI culture.
        }
        return appBuilder;
    }

    [SupportedOSPlatform("windows")]
    private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.Locale)
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var language = GetUserProfileLanguage();
                if (language is not null)
                {
                    await SetCurrent(language);
                }
            }, DispatcherPriority.Background);
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? GetUserProfileLanguage()
    {
        // Retrieve the current language settings from the registry.
        //
        // Compared to CultureInfo.CurrentUICulture.Name or Win32 API's GetUserDefaultUILanguage, the registry can get updated standard language tags,
        // and supports user-defined language preferences without needing to log off.
        // Note: Even restarting the application will get the old settings; only logging off the system will get the new ones.
        var languageNames = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
            .OpenSubKey(@"Control Panel\International\User Profile", false)?
            .GetValue("Languages", null) as IReadOnlyList<string>;
        return languageNames?.FirstOrDefault();
    }
}
```

Then, you can use the `UseCompiledLang` method in your `App.xaml.cs` file:

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .UseCompiledLang()
        .XxxOthers()
    ;
```
