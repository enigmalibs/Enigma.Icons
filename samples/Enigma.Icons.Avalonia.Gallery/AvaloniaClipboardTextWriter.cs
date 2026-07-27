using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace Enigma.Icons.Avalonia.Gallery;

/// <summary>Writes to the clipboard of the desktop lifetime's main window.</summary>
/// <remarks>
/// <para>
/// It takes <b>no constructor dependencies</b> and resolves the clipboard lazily, per call. Injecting
/// the window instead would close the cycle
/// <c>MainWindow → MainWindowViewModel → IClipboardTextWriter → MainWindow</c>, which the container
/// would refuse to build.
/// </para>
/// <para>
/// <c>SetTextAsync</c> is an <i>extension</i> on <c>IClipboard</c> in Avalonia 12 (the interface
/// itself exposes only <c>SetDataAsync</c>/<c>TryGetDataAsync</c>), hence the
/// <c>Avalonia.Input.Platform</c> using.
/// </para>
/// </remarks>
public sealed class AvaloniaClipboardTextWriter : IClipboardTextWriter
{
    /// <inheritdoc />
    public async Task<bool> TryWriteAsync(string text)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return false;
        }

        IClipboard? clipboard = desktop.MainWindow?.Clipboard;
        if (clipboard is null)
        {
            return false;
        }

        try
        {
            await clipboard.SetTextAsync(text).ConfigureAwait(true);
            return true;
        }
        catch (Exception)
        {
            // A clipboard write is a negotiation with the windowing system — on X11 it is selection
            // ownership, on Wayland a data-device offer — and either can fail transiently. A failed
            // copy is reported as false and shown in the status line; it must never take the app down.
            return false;
        }
    }
}
