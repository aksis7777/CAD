using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(MiniPdm.Desktop.Tests.HeadlessTestApplicationBuilder))]

namespace MiniPdm.Desktop.Tests;

public static class HeadlessTestApplicationBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessTestApplication>()
        .WithInterFont()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public sealed class HeadlessTestApplication : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}
