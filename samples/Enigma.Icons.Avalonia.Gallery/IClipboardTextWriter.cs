using System.Threading.Tasks;

namespace Enigma.Icons.Avalonia.Gallery;

/// <summary>Writes plain text to the system clipboard.</summary>
/// <remarks>
/// An interface only so the ViewModel does not reach into Avalonia's application lifetime itself.
/// It never throws: a clipboard that is unavailable is reported, not raised.
/// </remarks>
public interface IClipboardTextWriter
{
    /// <summary>Attempts to place <paramref name="text"/> on the clipboard.</summary>
    /// <param name="text">The text to write.</param>
    /// <returns><see langword="true"/> when the clipboard accepted the text; <see langword="false"/>
    /// when no clipboard was reachable or the platform rejected the write.</returns>
    Task<bool> TryWriteAsync(string text);
}
