using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace Enigma.Icons.Avalonia.UnitTests.TestSupport;

/// <summary>
/// Drives <c>Icon.Render</c> into a recording <see cref="DrawingContext"/> and flattens the result,
/// so the layer walk can be asserted exactly.
/// </summary>
/// <remarks>
/// <para>
/// The recorder is Avalonia's own <c>DrawingGroup.Open()</c> context, not a hand-rolled one:
/// <see cref="DrawingContext"/> cannot be subclassed outside Avalonia 12 — one of its abstract
/// members takes the internal <c>IRef&lt;T&gt;</c>. Using the framework's own context has the happy
/// side effect that the recorded tree carries real Avalonia semantics: a pushed transform, opacity or
/// clip each becomes a nested <see cref="DrawingGroup"/>.
/// </para>
/// <para>
/// Calling <c>Render</c> directly is possible because <c>Visual.Render</c> is public in Avalonia 12.
/// The headless platform can be driven to a real render pass too, but it exposes no draw list, so it
/// can only prove that rendering does not throw.
/// </para>
/// </remarks>
internal static class RenderRecorder
{
    /// <summary>Lays the icon out at the given size and records its render pass.</summary>
    /// <param name="icon">The icon to render.</param>
    /// <param name="width">The arranged width.</param>
    /// <param name="height">The arranged height.</param>
    /// <returns>The recorded drawing tree.</returns>
    public static DrawingGroup Record(Icon icon, double width, double height)
    {
        icon.Measure(new Size(width, height));
        icon.Arrange(new Rect(0, 0, width, height));

        var recorded = new DrawingGroup();
        using (DrawingContext context = recorded.Open())
        {
            icon.Render(context);
        }

        return recorded;
    }

    /// <summary>Every <see cref="GeometryDrawing"/> in the tree, in paint order — one per drawn layer.</summary>
    /// <param name="root">The recorded tree.</param>
    /// <returns>The leaves.</returns>
    public static List<GeometryDrawing> Leaves(DrawingGroup root)
    {
        var leaves = new List<GeometryDrawing>();
        Walk(root, leaves, null);
        return leaves;
    }

    /// <summary>Every nested <see cref="DrawingGroup"/> in the tree, excluding <paramref name="root"/> itself.</summary>
    /// <param name="root">The recorded tree.</param>
    /// <returns>The nested groups.</returns>
    public static List<DrawingGroup> Groups(DrawingGroup root)
    {
        var groups = new List<DrawingGroup>();
        Walk(root, null, groups);
        return groups;
    }

    /// <summary>The single matrix pushed by the render pass, or <see cref="Matrix.Identity"/> when none was.</summary>
    /// <param name="root">The recorded tree.</param>
    /// <returns>The pushed matrix.</returns>
    public static Matrix Transform(DrawingGroup root)
    {
        foreach (DrawingGroup group in Groups(root))
        {
            if (group.Transform is MatrixTransform matrix)
            {
                return matrix.Matrix;
            }
        }

        return Matrix.Identity;
    }

    private static void Walk(DrawingGroup group, List<GeometryDrawing>? leaves, List<DrawingGroup>? groups)
    {
        foreach (Drawing child in group.Children)
        {
            switch (child)
            {
                case GeometryDrawing geometry:
                    leaves?.Add(geometry);
                    break;
                case DrawingGroup nested:
                    groups?.Add(nested);
                    Walk(nested, leaves, groups);
                    break;
                default:
                    break;
            }
        }
    }
}
