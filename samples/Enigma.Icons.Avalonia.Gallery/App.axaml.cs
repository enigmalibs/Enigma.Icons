using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Enigma.Icons.Avalonia.Gallery;

/// <summary>The Avalonia application, and the owner of the generic host that supplies its services.</summary>
/// <remarks>
/// Avalonia's lifetime runs the app and the host only provides services (SPEC §11): the host is
/// started, never run. The main window is <b>resolved</b> from the container rather than
/// constructed, so no code-behind ever instantiates a service.
/// </remarks>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>Loads the application XAML.</summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>Builds and starts the host, then hands Avalonia its resolved main window.</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<IClipboardTextWriter, AvaloniaClipboardTextWriter>();
        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
        _host.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_host is null)
        {
            return;
        }

        // Exit runs on the UI thread as the lifetime shuts down, so the host is stopped
        // synchronously — there is no message loop left to await on.
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
        _host = null;
    }
}
