using System.Threading.Tasks;
using Enigma.Icons.AppIconStudio.Services;

namespace Enigma.Icons.AppIconStudio.UnitTests.TestSupport;

/// <summary>A folder picker that answers with whatever the test put in it.</summary>
/// <remarks>
/// <see cref="Result"/> null is the real picker's cancelled / unavailable / no-local-path case, which
/// the ViewModel has to leave the current folder alone for.
/// </remarks>
public sealed class FakeFolderPicker : IFolderPicker
{
    /// <summary>What the next call returns. Null means "nothing was chosen".</summary>
    public string? Result { get; set; }

    /// <summary>How many times the picker was shown.</summary>
    public int CallCount { get; private set; }

    /// <summary>The start location the last call was given.</summary>
    public string? LastSuggestedStartLocation { get; private set; }

    /// <inheritdoc />
    public Task<string?> PickFolderAsync(string? suggestedStartLocation)
    {
        CallCount++;
        LastSuggestedStartLocation = suggestedStartLocation;

        return Task.FromResult(Result);
    }
}
