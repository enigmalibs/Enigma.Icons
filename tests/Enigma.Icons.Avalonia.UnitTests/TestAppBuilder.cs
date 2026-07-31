using Avalonia;
using Avalonia.Headless;
using Enigma.Icons.Avalonia.UnitTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Enigma.Icons.Avalonia.UnitTests;

/// <summary>
/// The headless Avalonia application every test runs inside. A plain <c>Application</c> with no
/// Fluent theme: nothing under test is templated, so there is no style to load.
/// </summary>
/// <remarks>
/// This fixture is required even for the pure geometry tests. <c>Geometry.Parse</c> needs the
/// platform render interface, and a test method missing <c>[AvaloniaFact]</c> fails with an obscure
/// "Unable to locate IPlatformRenderInterface" rather than an assertion failure.
/// </remarks>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
