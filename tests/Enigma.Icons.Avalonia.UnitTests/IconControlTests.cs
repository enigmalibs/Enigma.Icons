using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Enigma.Icons.Avalonia.UnitTests.TestSupport;
using Enigma.Icons.Phosphor;
using Xunit;

namespace Enigma.Icons.Avalonia.UnitTests;

/// <summary>SPEC §12.3, the <c>Icon</c> control bullet.</summary>
/// <remarks>
/// The layer walk is asserted through <see cref="RenderRecorder"/>, which drives a direct
/// <c>Render</c> call into Avalonia's own recording drawing context. A separate test drives the real
/// headless render pass end to end; that one can only prove rendering does not throw, because the
/// headless platform exposes no draw list.
/// </remarks>
public sealed class IconControlTests
{
    private const double Tolerance = 1e-9;

    [AvaloniaFact]
    public void Size_DefaultsTo16()
        => Assert.Equal(16.0, new Icon().Size);

    [AvaloniaFact]
    public void Weight_DefaultsToRegular()
        => Assert.Equal(PhosphorWeight.Regular, new Icon().Weight);

    [AvaloniaFact]
    public void Stretch_DefaultsToUniform()
        => Assert.Equal(Stretch.Uniform, new Icon().Stretch);

    [AvaloniaFact]
    public void IconSetIconNameAndVariant_DefaultToNull()
    {
        var icon = new Icon();

        Assert.Null(icon.IconSet);
        Assert.Null(icon.IconName);
        Assert.Null(icon.Variant);
    }

    [AvaloniaFact]
    public void Focusable_IsFalse()
        => Assert.False(new Icon().Focusable);

    [AvaloniaFact]
    public void MeasureOverride_HonoursSize()
    {
        var icon = new Icon { Size = 24 };

        icon.Measure(Size.Infinity);

        Assert.Equal(new Size(24, 24), icon.DesiredSize);
    }

    [AvaloniaFact]
    public void MeasureOverride_ExplicitWidthAndHeightWinOverSize()
    {
        // Not implemented by the control: Layoutable.MeasureCore coerces the measured result with the
        // explicit Width/Height, so the precedence falls out of the framework.
        var icon = new Icon { Size = 24, Width = 40, Height = 12 };

        icon.Measure(Size.Infinity);

        Assert.Equal(new Size(40, 12), icon.DesiredSize);
    }

    [AvaloniaFact]
    public void MeasureOverride_NonFiniteSize_MeasuresEmptyRatherThanThrowing()
    {
        var icon = new Icon { Size = double.NaN };

        icon.Measure(Size.Infinity);

        Assert.Equal(default, icon.DesiredSize);
    }

    [AvaloniaFact]
    public void ChangingSize_InvalidatesMeasure()
    {
        var icon = new Icon();
        icon.Measure(Size.Infinity);
        Assert.True(icon.IsMeasureValid);

        icon.Size = 32;

        Assert.False(icon.IsMeasureValid);
    }

    [AvaloniaFact]
    public void ChangingStretch_InvalidatesMeasure()
    {
        var icon = new Icon();
        icon.Measure(Size.Infinity);
        Assert.True(icon.IsMeasureValid);

        icon.Stretch = Stretch.Fill;

        Assert.False(icon.IsMeasureValid);
    }

    [AvaloniaFact]
    public void Render_DuotoneKind_DrawsBothLayersAndHonoursTheBackingOpacity()
    {
        var icon = new Icon
        {
            Kind = PhosphorIcon.Acorn,
            Weight = PhosphorWeight.Duotone,
            Foreground = Brushes.Black,
        };

        DrawingGroup recorded = RenderRecorder.Record(icon, 64, 64);

        Assert.Equal(2, RenderRecorder.Leaves(recorded).Count);
        Assert.Contains(
            RenderRecorder.Groups(recorded),
            g => Math.Abs(g.Opacity - TestGlyphs.DuotoneBackingOpacity) < Tolerance);
    }

    [AvaloniaFact]
    public void Render_RegularKind_DrawsOneLayerWithNoOpacityGroup()
    {
        var icon = new Icon { Kind = PhosphorIcon.Acorn, Foreground = Brushes.Black };

        DrawingGroup recorded = RenderRecorder.Record(icon, 64, 64);

        GeometryDrawing only = Assert.Single(RenderRecorder.Leaves(recorded));
        Assert.Same(Brushes.Black, only.Brush);
        Assert.DoesNotContain(RenderRecorder.Groups(recorded), g => g.Opacity < 1.0);
    }

    [AvaloniaFact]
    public void Render_NullForeground_PaintsNothing()
    {
        var icon = new Icon { Kind = PhosphorIcon.Acorn, Foreground = null };

        Assert.Empty(RenderRecorder.Leaves(RenderRecorder.Record(icon, 64, 64)));
    }

    [AvaloniaFact]
    public void Render_UndefinedKind_PaintsNothingRatherThanThrowing()
    {
        // PhosphorIconSet throws ArgumentOutOfRangeException for a cast integer; Render must swallow
        // it, because a throwing render pass kills the XAML previewer for the whole window.
        var icon = new Icon { Kind = (PhosphorIcon)999999, Foreground = Brushes.Black };

        Assert.Empty(RenderRecorder.Leaves(RenderRecorder.Record(icon, 64, 64)));
    }

    [AvaloniaFact]
    public void Render_MissingGlyphInACustomSet_PaintsNothing()
    {
        var icon = new Icon
        {
            IconSet = SvgIconSet.FromSvgSources(TestGlyphs.LogoSvgSource()),
            IconName = "no-such-icon",
            Foreground = Brushes.Black,
        };

        Assert.Empty(RenderRecorder.Leaves(RenderRecorder.Record(icon, 64, 64)));
    }

    [AvaloniaFact]
    public void Render_IconSetWithoutAName_PaintsNothing()
    {
        var icon = new Icon
        {
            IconSet = SvgIconSet.FromSvgSources(TestGlyphs.LogoSvgSource()),
            IconName = null,
            Foreground = Brushes.Black,
        };

        Assert.Empty(RenderRecorder.Leaves(RenderRecorder.Record(icon, 64, 64)));
    }

    [AvaloniaFact]
    public void Render_ThrowingIconSet_PaintsNothingRatherThanThrowing()
    {
        var icon = new Icon
        {
            IconSet = new ThrowingIconSet(),
            IconName = "anything",
            Foreground = Brushes.Black,
        };

        Assert.Empty(RenderRecorder.Leaves(RenderRecorder.Record(icon, 64, 64)));
    }

    [AvaloniaFact]
    public void Render_IconSetWinsOverKindAndWeight()
    {
        var icon = new Icon
        {
            // Duotone Acorn would draw two layers; the custom logo draws one.
            Kind = PhosphorIcon.Acorn,
            Weight = PhosphorWeight.Duotone,
            IconSet = SvgIconSet.FromSvgSources(TestGlyphs.LogoSvgSource(), name: "Test"),
            IconName = "logo",
            Foreground = Brushes.Black,
        };

        DrawingGroup recorded = RenderRecorder.Record(icon, 64, 64);

        Assert.Single(RenderRecorder.Leaves(recorded));
        Assert.DoesNotContain(RenderRecorder.Groups(recorded), g => g.Opacity < 1.0);
    }

    [AvaloniaTheory]
    [InlineData(Stretch.None, 1.0, 1.0)]
    [InlineData(Stretch.Uniform, 50.0 / 256.0, 50.0 / 256.0)]
    [InlineData(Stretch.UniformToFill, 100.0 / 256.0, 100.0 / 256.0)]
    [InlineData(Stretch.Fill, 100.0 / 256.0, 50.0 / 256.0)]
    public void Render_ScalesTheViewBoxIntoTheBoundsPerStretch(Stretch stretch, double expectedX, double expectedY)
    {
        // A deliberately non-square 100×50 target against Phosphor's 0 0 256 256 view box.
        var icon = new Icon { Kind = PhosphorIcon.Acorn, Foreground = Brushes.Black, Stretch = stretch };

        Matrix transform = RenderRecorder.Transform(RenderRecorder.Record(icon, 100, 50));

        Assert.Equal(expectedX, transform.M11, Tolerance);
        Assert.Equal(expectedY, transform.M22, Tolerance);

        // Centred on both axes.
        Assert.Equal((100.0 - (256.0 * expectedX)) / 2, transform.M31, Tolerance);
        Assert.Equal((50.0 - (256.0 * expectedY)) / 2, transform.M32, Tolerance);
    }

    [AvaloniaFact]
    public void Render_UniformToFill_ClipsToTheBounds()
    {
        var icon = new Icon
        {
            Kind = PhosphorIcon.Acorn,
            Foreground = Brushes.Black,
            Stretch = Stretch.UniformToFill,
        };

        DrawingGroup recorded = RenderRecorder.Record(icon, 100, 50);

        Assert.Contains(RenderRecorder.Groups(recorded), g => g.ClipGeometry is not null);
    }

    [AvaloniaTheory]
    [InlineData(Stretch.None)]
    [InlineData(Stretch.Uniform)]
    [InlineData(Stretch.Fill)]
    public void Render_OtherStretchModes_DoNotClip(Stretch stretch)
    {
        var icon = new Icon { Kind = PhosphorIcon.Acorn, Foreground = Brushes.Black, Stretch = stretch };

        DrawingGroup recorded = RenderRecorder.Record(icon, 100, 50);

        Assert.DoesNotContain(RenderRecorder.Groups(recorded), g => g.ClipGeometry is not null);
    }

    [AvaloniaFact]
    public void Render_OffsetViewBox_SubtractsTheScaledOrigin()
    {
        var icon = new Icon
        {
            IconSet = new SingleGlyphIconSet(TestGlyphs.OffsetViewBox()),   // view box 10 20 100 50
            IconName = "only",
            Foreground = Brushes.Black,
            Stretch = Stretch.Fill,
        };

        Matrix transform = RenderRecorder.Transform(RenderRecorder.Record(icon, 100, 50));

        Assert.Equal(1.0, transform.M11, Tolerance);
        Assert.Equal(1.0, transform.M22, Tolerance);
        Assert.Equal(-10.0, transform.M31, Tolerance);
        Assert.Equal(-20.0, transform.M32, Tolerance);
    }

    [AvaloniaFact]
    public void Render_ZeroSizedBounds_PaintsNothing()
    {
        var icon = new Icon { Kind = PhosphorIcon.Acorn, Foreground = Brushes.Black };

        Assert.Empty(RenderRecorder.Leaves(RenderRecorder.Record(icon, 0, 0)));
    }

    [AvaloniaFact]
    public void Render_StrokedCustomGlyph_DrawsWithAPen()
    {
        var icon = new Icon
        {
            IconSet = new SingleGlyphIconSet(TestGlyphs.StrokedOnly()),
            IconName = "only",
            Foreground = Brushes.Black,
        };

        GeometryDrawing only = Assert.Single(RenderRecorder.Leaves(RenderRecorder.Record(icon, 64, 64)));

        Assert.Null(only.Brush);
        Assert.NotNull(only.Pen);
    }

    [AvaloniaFact]
    public void Foreground_InheritsFromTheEnclosingTextElementScope()
    {
        var icon = new Icon();
        var border = new Border { Child = icon };
        TextElement.SetForeground(border, Brushes.Red);

        var window = new Window { Content = border, Width = 100, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Same(Brushes.Red, icon.Foreground);
    }

    [AvaloniaFact]
    public void Render_ThroughAHeadlessWindow_DoesNotThrow()
    {
        var icon = new Icon
        {
            Kind = PhosphorIcon.Acorn,
            Weight = PhosphorWeight.Duotone,
            Size = 32,
            Foreground = Brushes.Black,
        };

        var window = new Window { Content = icon, Width = 100, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        Assert.True(icon.IsMeasureValid);
        Assert.Equal(new Size(32, 32), icon.DesiredSize);
    }

    [AvaloniaFact]
    public void AutomationPeer_ReportsAnImageThatIsNotContentUntilNamed()
    {
        AutomationPeer anonymous = ControlAutomationPeer.CreatePeerForElement(new Icon());

        Assert.Equal(AutomationControlType.Image, anonymous.GetAutomationControlType());
        Assert.False(anonymous.IsContentElement());

        var named = new Icon();
        AutomationProperties.SetName(named, "Delete");
        AutomationPeer namedPeer = ControlAutomationPeer.CreatePeerForElement(named);

        Assert.True(namedPeer.IsContentElement());
    }

    /// <summary>An icon set whose every lookup blows up — arbitrary third-party code, in effect.</summary>
    private sealed class ThrowingIconSet : IIconSet
    {
        public string Name => "Throwing";

        public IReadOnlyList<string> Variants => [];

        public string? DefaultVariant => null;

        public IEnumerable<string> IconNames => [];

        public bool TryGetGlyph(string icon, string? variant, out IconGlyph? glyph)
            => throw new InvalidOperationException("boom");

        public IconGlyph GetGlyph(string icon, string? variant = null)
            => throw new InvalidOperationException("boom");
    }

    /// <summary>An icon set serving exactly one glyph under the name <c>only</c>.</summary>
    private sealed class SingleGlyphIconSet(IconGlyph glyph) : IIconSet
    {
        public string Name => "Single";

        public IReadOnlyList<string> Variants => [];

        public string? DefaultVariant => null;

        public IEnumerable<string> IconNames => ["only"];

        public bool TryGetGlyph(string icon, string? variant, out IconGlyph? found)
        {
            found = string.Equals(icon, "only", StringComparison.Ordinal) ? glyph : null;
            return found is not null;
        }

        public IconGlyph GetGlyph(string icon, string? variant = null)
            => TryGetGlyph(icon, variant, out IconGlyph? found) && found is not null
                ? found
                : throw new IconNotFoundException(icon, variant, Name);
    }
}
