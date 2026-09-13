using Enigma.Icons.Phosphor;

namespace Enigma.Icons.AppIconStudio;

/// <summary>One row of the icon catalog: a Phosphor icon and its upstream kebab-case name.</summary>
/// <param name="Kind">The strongly-typed icon, passed straight to <c>ei:Icon</c>'s <c>Kind</c>.</param>
/// <param name="Name">The upstream name from <see cref="PhosphorIconNames.All"/>, shown beside the
/// glyph and matched against by the search box.</param>
/// <remarks>
/// The entry carries no weight: that is shared presentation state owned by the ViewModel, bound up to
/// from the row template so changing the weight repaints the already-realized rows rather than
/// rebuilding the list.
/// </remarks>
public sealed record IconEntry(PhosphorIcon Kind, string Name);
