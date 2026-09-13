using System.Threading.Tasks;

namespace Enigma.Icons.AppIconStudio.Services;

/// <summary>Asks the user for a directory.</summary>
/// <remarks>
/// An interface only so the ViewModel does not reach into Avalonia's application lifetime itself. It
/// never throws: a cancelled dialog, a platform with no folder picker, and a picker that fails all
/// report the same thing — no folder.
/// </remarks>
public interface IFolderPicker
{
    /// <summary>Shows a folder picker.</summary>
    /// <param name="suggestedStartLocation">
    /// Where the dialog should open, or null for the platform's default. A path that does not exist
    /// is ignored rather than treated as an error.
    /// </param>
    /// <returns>The chosen directory's local path, or null when nothing was chosen.</returns>
    Task<string?> PickFolderAsync(string? suggestedStartLocation);
}
