using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Enigma.Icons;

/// <summary>
/// One resolved icon: a view box plus the ordered layers that paint it. Immutable and thread-safe.
/// </summary>
public sealed class IconGlyph
{
    /// <summary>Initializes a new glyph.</summary>
    /// <param name="viewBox">The coordinate rectangle the layers' path data is expressed in.</param>
    /// <param name="layers">
    /// The layers in paint order — index 0 is painted first. Enumerated exactly once and copied
    /// defensively into a read-only snapshot.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="layers"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="layers"/> is empty, or contains a null element.</exception>
    public IconGlyph(IconViewBox viewBox, IReadOnlyList<IconLayer> layers)
    {
        if (layers is null)
        {
            throw new ArgumentNullException(nameof(layers));
        }

        // Materialize first: the argument may be a lazy sequence, and it must be enumerated once.
        var copy = new IconLayer[layers.Count];
        for (int i = 0; i < copy.Length; i++)
        {
            IconLayer layer = layers[i];
            if (layer is null)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Layer at index {0} is null.", i),
                    nameof(layers));
            }

            copy[i] = layer;
        }

        if (copy.Length == 0)
        {
            throw new ArgumentException("A glyph must have at least one layer.", nameof(layers));
        }

        ViewBox = viewBox;

        // ReadOnlyCollection, not the array itself: the exposed IReadOnlyList must not be mutable
        // through a cast back to IconLayer[].
        Layers = new ReadOnlyCollection<IconLayer>(copy);

        IconLayer? single = copy.Length == 1 ? copy[0] : null;
        IsSingleLayer = single is not null && single.Opacity >= 1.0 && !single.IsStroked;
    }

    /// <summary>The coordinate rectangle the layers' path data is expressed in.</summary>
    public IconViewBox ViewBox { get; }

    /// <summary>Paint order: index 0 is painted first (bottom-most). Never empty.</summary>
    public IReadOnlyList<IconLayer> Layers { get; }

    /// <summary>
    /// True when the glyph has exactly one fully-opaque, unstroked layer —
    /// the common case, and the one a renderer can collapse to a single filled geometry.
    /// </summary>
    public bool IsSingleLayer { get; }
}
