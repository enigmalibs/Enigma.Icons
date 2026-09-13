using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Enigma.Icons.AppIconStudio.Services;

/// <summary>Shows the folder picker of the desktop lifetime's main window.</summary>
/// <remarks>
/// <para>
/// It takes <b>no constructor dependencies</b> and resolves the window lazily, per call. Injecting
/// the window instead would close the cycle
/// <c>MainWindow → MainWindowViewModel → IFolderPicker → MainWindow</c>, which the container would
/// refuse to build. The same shape the gallery's clipboard writer uses.
/// </para>
/// <para>
/// Every failure path returns null rather than throwing: a file dialog is a negotiation with the
/// windowing system, and on Linux it may be brokered through a portal that is simply not running.
/// A picker that cannot open is a "no folder chosen", not a crash.
/// </para>
/// </remarks>
public sealed class AvaloniaFolderPicker : IFolderPicker
{
    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string? suggestedStartLocation)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        Window? window = desktop.MainWindow;
        if (window is null)
        {
            return null;
        }

        IStorageProvider storage = window.StorageProvider;
        if (!storage.CanPickFolder)
        {
            return null;
        }

        try
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Choose where to write the icon",
                AllowMultiple = false,
            };

            if (!string.IsNullOrWhiteSpace(suggestedStartLocation))
            {
                // Null when the path no longer exists, which is exactly the "ignore it" behaviour the
                // interface promises — SuggestedStartLocation is optional.
                options.SuggestedStartLocation =
                    await storage.TryGetFolderFromPathAsync(suggestedStartLocation).ConfigureAwait(true);
            }

            IReadOnlyList<IStorageFolder> picked = await storage.OpenFolderPickerAsync(options).ConfigureAwait(true);
            if (picked.Count == 0)
            {
                return null;
            }

            // Null for a location with no local path — a cloud or virtual folder. The exporter writes
            // with System.IO, so such a folder is genuinely unusable here.
            return picked[0].TryGetLocalPath();
        }
        catch (Exception)
        {
            // See the class remarks: a picker that cannot open is a "no folder chosen".
            return null;
        }
    }
}
