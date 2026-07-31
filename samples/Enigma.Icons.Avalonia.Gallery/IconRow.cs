using System.Collections.Generic;

namespace Enigma.Icons.Avalonia.Gallery;

/// <summary>One row of the gallery grid — up to <c>ColumnsPerRow</c> cells.</summary>
/// <param name="Cells">The row's cells, left to right. The last row of a result set may be partial.</param>
/// <remarks>
/// The row, not the cell, is the virtualized unit. Avalonia 12 ships exactly one general-purpose
/// virtualizing panel — <c>VirtualizingStackPanel</c>, a one-dimensional list — so a grid is built as
/// a vertical virtualized list of rows, each row a non-virtualizing <c>UniformGrid</c>. That realizes
/// a screenful of rows instead of all 1,512 icons at once.
/// </remarks>
public sealed record IconRow(IReadOnlyList<IconEntry> Cells);
