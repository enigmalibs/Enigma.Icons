using Enigma.Icons.Phosphor;

namespace Enigma.Icons.Avalonia.Gallery;

/// <summary>One cell of the gallery grid: a Phosphor icon and its upstream kebab-case name.</summary>
/// <param name="Kind">The strongly-typed icon, passed straight to <c>ei:Icon</c>'s <c>Kind</c>.</param>
/// <param name="Name">The upstream name from <see cref="PhosphorIconNames.All"/>, shown under the
/// glyph and matched against by the search box.</param>
/// <remarks>
/// The entry deliberately carries <b>no</b> weight, size or brush. Those are shared presentation
/// state owned by the ViewModel and bound up to it from the cell template, so changing one repaints
/// the already-realized <c>Icon</c> controls through their <c>AffectsRender</c> registrations
/// (SPEC §10.2) instead of rebuilding the list.
/// </remarks>
public sealed record IconEntry(PhosphorIcon Kind, string Name);
