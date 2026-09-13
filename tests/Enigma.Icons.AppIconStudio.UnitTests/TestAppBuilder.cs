using Avalonia;
using Avalonia.Headless;
using Enigma.Icons.AppIconStudio.UnitTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Enigma.Icons.AppIconStudio.UnitTests;

/// <summary>
/// The headless Avalonia application every <c>[AvaloniaFact]</c> in this assembly runs inside.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>UseHeadlessDrawing = false</c> plus <c>UseSkia()</c> is load-bearing.</b> The headless
/// platform's default drawing backend is a stub: it satisfies <c>Geometry.Parse</c> but a
/// <c>RenderTargetBitmap</c> driven through it produces nothing to inspect. The studio's whole point
/// is the pixels it writes, so the rasterizer tests need the real Skia backend. Everything else in
/// the assembly — the design model, the layout maths, the ICO container — is pure and needs no
/// platform at all.
/// </para>
/// <para>
/// No Fluent theme: nothing under test is templated, so there is no style to load.
/// </para>
/// </remarks>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia();
}
