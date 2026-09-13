using System;
using Avalonia.Controls;

namespace Enigma.Icons.AppIconStudio;

/// <summary>The studio window.</summary>
/// <remarks>
/// The ViewModel arrives by constructor injection, resolved from the host in
/// <see cref="App.OnFrameworkInitializationCompleted"/> — no code-behind instantiates a service.
/// The consequence, accepted for a maintainer's tool: the XAML previewer cannot instantiate this
/// window, because there is no parameterless constructor and none will be added just to please the
/// designer.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>Creates the window and binds it to its ViewModel.</summary>
    /// <param name="viewModel">The studio ViewModel.</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> is null.</exception>
    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
