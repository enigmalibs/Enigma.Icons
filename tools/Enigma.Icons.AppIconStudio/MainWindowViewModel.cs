using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Rendering;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.AppIconStudio;

/// <summary>Drives the studio window: the icon catalog, the design controls, and the live preview.</summary>
/// <remarks>
/// <para>
/// Explicit CommunityToolkit.Mvvm style — <c>field</c>-backed properties with <c>SetProperty</c> and
/// get-only command properties initialized in the constructor. No <c>[ObservableProperty]</c> /
/// <c>[RelayCommand]</c> source generators, so the class is not <c>partial</c>. The house convention
/// the gallery already follows; see <c>docs/done/FEATURE-469B.md</c>.
/// </para>
/// <para>
/// <b>The preview is the export.</b> Every image on screen comes from the same
/// <see cref="IIconRasterizer"/>, at the size it will be written, through the same
/// <see cref="IconDesign"/> instance the exporter will receive. There is no second composition path
/// that could quietly disagree with the files on disk.
/// </para>
/// </remarks>
public sealed class MainWindowViewModel : ObservableObject
{
    /// <summary>The edge of the large preview, in pixels.</summary>
    public const int PreviewSizePx = 256;

    /// <summary>How long after the last change the preview is re-rendered.</summary>
    private static readonly TimeSpan PreviewDelay = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// The small sizes shown beside the main preview, largest first — where a too-generous glyph
    /// scale stops being legible.
    /// </summary>
    private static readonly int[] ThumbnailSizes = [64, 48, 32, 16];

    /// <summary>
    /// The one-time catalog of all 1,512 icons. <see cref="PhosphorIconNames.All"/> is in enum order
    /// (SPEC §8.4), so the index <i>is</i> the <see cref="PhosphorIcon"/> value — no
    /// <c>Enum.Parse</c>, no reflection (SPEC §2.11).
    /// </summary>
    private static readonly IconEntry[] Catalog = BuildCatalog();

    private readonly IIconRasterizer _rasterizer;

    /// <summary>Coalesces slider drags and keystrokes into one re-render.</summary>
    private readonly DispatcherTimer _previewDebounce;

    /// <summary>Creates the ViewModel and renders the initial preview.</summary>
    /// <param name="rasterizer">Turns the current design into the images on screen.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rasterizer"/> is null.</exception>
    public MainWindowViewModel(IIconRasterizer rasterizer)
    {
        _rasterizer = rasterizer ?? throw new ArgumentNullException(nameof(rasterizer));

        _previewDebounce = new DispatcherTimer { Interval = PreviewDelay };
        _previewDebounce.Tick += OnPreviewDebounceTick;

        // The blue plate and white glyph of the existing Enigma.MarkdownEditor icon, so the reference
        // look is where the studio starts rather than something to rebuild by hand.
        ActiveIcon = Catalog[(int)PhosphorIcon.MarkdownLogo];
        SelectedIcon = ActiveIcon;
        CurrentDesign = BuildDesign();

        ApplyFilter();
        RegeneratePreview();

        // ActiveIcon's setter armed the debounce on the way in; the preview is already current, so
        // there is nothing for that tick to do.
        _previewDebounce.Stop();
    }

    /// <summary>The search term. Matching is case-insensitive substring over the icon names.</summary>
    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = string.Empty;

    /// <summary>The catalog rows currently shown, after the search filter.</summary>
    /// <remarks>
    /// Replaced wholesale, never mutated: clearing and refilling an <c>ObservableCollection</c> would
    /// raise 1,512 notifications and defeat the list's virtualization.
    /// </remarks>
    public IReadOnlyList<IconEntry> FilteredIcons
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>
    /// The row highlighted in the list, or null when the filter has hidden the chosen icon.
    /// </summary>
    /// <remarks>
    /// Null is a <i>list</i> state, not a design state — see <see cref="ActiveIcon"/>.
    /// </remarks>
    public IconEntry? SelectedIcon
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                ActiveIcon = value;
            }
        }
    }

    /// <summary>The icon the design actually uses. Never null.</summary>
    /// <remarks>
    /// Separate from <see cref="SelectedIcon"/> because a <c>ListBox</c> pushes null the moment a
    /// filter hides the selected row, and a keystroke in the search box must not empty the preview.
    /// The last icon the user actually picked stays the design's until they pick another.
    /// </remarks>
    public IconEntry ActiveIcon
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    }

    /// <summary>The message shown in place of the list when the filter matches nothing.</summary>
    public string EmptyMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>False when the filter matched nothing; hides the list and shows <see cref="EmptyMessage"/>.</summary>
    public bool HasResults
    {
        get;
        private set => SetProperty(ref field, value);
    } = true;

    /// <summary>The weight the glyph is drawn in.</summary>
    public WeightOption SelectedWeight
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    } = WeightOptionList[4];

    /// <summary>Flat colour or two-stop gradient.</summary>
    public FillModeOption SelectedFillMode
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsGradient));
                SchedulePreview();
            }
        }
    } = FillModeOptionList[0];

    /// <summary>True when the plate is a gradient; enables the second colour and the angle slider.</summary>
    public bool IsGradient => SelectedFillMode.Mode == PlateFillMode.LinearGradient;

    /// <summary>The flat plate colour, or gradient stop 0.</summary>
    public Color PrimaryColor
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    } = Color.FromRgb(0x3B, 0x72, 0xF0);

    /// <summary>Gradient stop 1. Ignored while the plate is solid.</summary>
    public Color SecondaryColor
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    } = Color.FromRgb(0x1E, 0x3A, 0x8A);

    /// <summary>The gradient direction in degrees, clockwise from left-to-right.</summary>
    public double GradientAngle
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    } = PlateFill.DefaultAngleDegrees;

    /// <summary>The colour every glyph layer paints with.</summary>
    public Color GlyphColor
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    } = Colors.White;

    /// <summary>Corner radius as a fraction of the plate edge, 0.0–0.5.</summary>
    public double CornerRadiusRatio
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    } = IconDesign.DefaultCornerRadiusRatio;

    /// <summary>The fraction of the plate edge the glyph spans, 0.2–1.0.</summary>
    public double GlyphScale
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SchedulePreview();
            }
        }
    } = IconDesign.DefaultGlyphScale;

    /// <summary>The design the preview last rendered, and the one an export will use. Never null.</summary>
    public IconDesign CurrentDesign
    {
        get;
        private set => SetProperty(ref field, value);
        // Assigned in the constructor, before the object escapes, so no binding can ever read a null
        // here. Not null-forgiven with `= null!`: the constructor's own assignment is what the
        // compiler is being shown, and a real value costs nothing.
    }

    /// <summary>The 256 px preview, or null when the last render failed.</summary>
    public Bitmap? PreviewImage
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>The small-size strip beside the main preview.</summary>
    public IReadOnlyList<IconThumbnail> Thumbnails
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>The status line at the bottom of the window.</summary>
    public string StatusText
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>The six weights, with their display names.</summary>
    public IReadOnlyList<WeightOption> Weights => WeightOptionList;

    /// <summary>The two plate fill modes, with their display names.</summary>
    public IReadOnlyList<FillModeOption> FillModes => FillModeOptionList;

    /// <summary>
    /// Backs <see cref="Weights"/>. A static property rather than an instance one because
    /// <see cref="SelectedWeight"/>'s initializer reads it, and instance initializers cannot see
    /// instance members.
    /// </summary>
    private static IReadOnlyList<WeightOption> WeightOptionList { get; } =
    [
        new WeightOption("Thin", PhosphorWeight.Thin),
        new WeightOption("Light", PhosphorWeight.Light),
        new WeightOption("Regular", PhosphorWeight.Regular),
        new WeightOption("Bold", PhosphorWeight.Bold),

        // Index 4, and the default: a solid glyph is what reads at 16 px, which is the size an app
        // icon is judged at most often.
        new WeightOption("Fill", PhosphorWeight.Fill),
        new WeightOption("Duotone", PhosphorWeight.Duotone),
    ];

    /// <summary>Backs <see cref="FillModes"/>; static for the same reason as <see cref="WeightOptionList"/>.</summary>
    private static IReadOnlyList<FillModeOption> FillModeOptionList { get; } =
    [
        new FillModeOption("Solid", PlateFillMode.Solid),
        new FillModeOption("Gradient", PlateFillMode.LinearGradient),
    ];

    /// <summary>Rebuilds <see cref="CurrentDesign"/> from the current control state.</summary>
    /// <returns>The design the preview and any export will use.</returns>
    private IconDesign BuildDesign()
    {
        PlateFill plate = SelectedFillMode.Mode == PlateFillMode.Solid
            ? PlateFill.Solid(PrimaryColor)
            : PlateFill.LinearGradient(PrimaryColor, SecondaryColor, GradientAngle);

        return new IconDesign(
            ActiveIcon.Kind,
            SelectedWeight.Weight,
            GlyphColor,
            plate,
            CornerRadiusRatio,
            GlyphScale);
    }

    /// <summary>Queues a preview re-render, restarting the debounce window.</summary>
    /// <remarks>
    /// Restart, do not extend: one render, <see cref="PreviewDelay"/> after the last change, so
    /// dragging a slider costs one render rather than one per pixel.
    /// </remarks>
    private void SchedulePreview()
    {
        _previewDebounce.Stop();
        _previewDebounce.Start();
    }

    /// <summary>Renders the current design into the preview and the thumbnail strip.</summary>
    private void RegeneratePreview()
    {
        try
        {
            IconDesign design = BuildDesign();
            CurrentDesign = design;

            PreviewImage = Decode(_rasterizer.RenderPng(design, PreviewSizePx));

            var thumbnails = new List<IconThumbnail>(ThumbnailSizes.Length);
            foreach (int size in ThumbnailSizes)
            {
                thumbnails.Add(new IconThumbnail(size, Decode(_rasterizer.RenderPng(design, size))));
            }

            Thumbnails = thumbnails;
            StatusText = DescribeDesign();
        }
        catch (Exception exception)
        {
            // Deliberately broad, for the same reason Icon.Render swallows everything (SPEC §10.2,
            // §15): this runs off a dispatcher tick, and an escaping exception takes the window down
            // rather than the one image that failed. The failure is reported, not hidden.
            PreviewImage = null;
            Thumbnails = [];
            StatusText = string.Format(
                CultureInfo.InvariantCulture,
                "Preview failed: {0}",
                exception.Message);
        }
    }

    private static IconEntry[] BuildCatalog()
    {
        IReadOnlyList<string> names = PhosphorIconNames.All;
        var catalog = new IconEntry[names.Count];

        for (int i = 0; i < catalog.Length; i++)
        {
            catalog[i] = new IconEntry((PhosphorIcon)i, names[i]);
        }

        return catalog;
    }

    /// <summary>
    /// Decodes a rendered PNG into a bitmap the UI can show.
    /// </summary>
    /// <remarks>
    /// <b>The result is never disposed.</b> An <c>Image</c> keeps the <c>Bitmap</c> it was handed and
    /// draws it on the render thread, so disposing the previous one when a new render arrives is a
    /// race with a native surface, not a tidy-up. They are left to the garbage collector, and the
    /// 150 ms debounce is what keeps the churn bounded — a 256 px preview plus four thumbnails is
    /// about 340 KB per render.
    /// </remarks>
    private static Bitmap Decode(byte[] png)
    {
        using var stream = new MemoryStream(png, writable: false);

        return new Bitmap(stream);
    }

    private void OnPreviewDebounceTick(object? sender, EventArgs e)
    {
        // One shot: stop before rendering, so a tick never repeats.
        _previewDebounce.Stop();
        RegeneratePreview();
    }

    private void ApplyFilter()
    {
        string term = SearchText.Trim();

        List<IconEntry> matches;
        if (term.Length == 0)
        {
            matches = new List<IconEntry>(Catalog);
        }
        else
        {
            matches = [];
            foreach (IconEntry entry in Catalog)
            {
                if (entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(entry);
                }
            }
        }

        FilteredIcons = matches;
        HasResults = matches.Count > 0;
        EmptyMessage = string.Format(CultureInfo.InvariantCulture, "No icons match '{0}'", term);

        // Restore the highlight when the chosen icon is back in view, so clearing the search box
        // leaves the list showing what the preview is showing.
        SelectedIcon = matches.Contains(ActiveIcon) ? ActiveIcon : null;
    }

    private string DescribeDesign()
        => string.Format(
            CultureInfo.InvariantCulture,
            "{0} · {1} · {2} plate · {3:0}% corner · {4:0}% glyph",
            ActiveIcon.Name,
            SelectedWeight.Name,
            SelectedFillMode.Name,
            CornerRadiusRatio * 100,
            GlyphScale * 100);
}
