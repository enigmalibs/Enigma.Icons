using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Icons.AppIconStudio.Design;
using Enigma.Icons.AppIconStudio.Export;
using Enigma.Icons.AppIconStudio.Rendering;
using Enigma.Icons.AppIconStudio.Services;
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

    /// <summary>
    /// The frame sizes the icon list offers, all ticked by default — Windows' recommended set, and
    /// the largest an ICO directory can address is 256.
    /// </summary>
    private static readonly int[] OfferedIcoSizes = [16, 24, 32, 48, 64, 128, 256];

    /// <summary>
    /// The standalone PNG sizes the list offers. Only the three largest start ticked: those are the
    /// About/splash assets the icon itself cannot serve well, and the small ones are already inside
    /// the <c>.ico</c>.
    /// </summary>
    private static readonly int[] OfferedPngSizes = [16, 32, 64, 128, 256, 512, 1024];

    private static readonly int[] DefaultPngSizes = [256, 512, 1024];

    private readonly IIconRasterizer _rasterizer;
    private readonly IconExporter _exporter;
    private readonly IFolderPicker _folderPicker;

    /// <summary>Coalesces slider drags and keystrokes into one re-render.</summary>
    private readonly DispatcherTimer _previewDebounce;

    /// <summary>Creates the ViewModel and renders the initial preview.</summary>
    /// <param name="rasterizer">Turns the current design into the images on screen.</param>
    /// <param name="exporter">Writes the icon and PNG files.</param>
    /// <param name="folderPicker">Asks the user where to write them.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public MainWindowViewModel(IIconRasterizer rasterizer, IconExporter exporter, IFolderPicker folderPicker)
    {
        _rasterizer = rasterizer ?? throw new ArgumentNullException(nameof(rasterizer));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _folderPicker = folderPicker ?? throw new ArgumentNullException(nameof(folderPicker));

        _previewDebounce = new DispatcherTimer { Interval = PreviewDelay };
        _previewDebounce.Tick += OnPreviewDebounceTick;

        IcoSizes = BuildSizeOptions(OfferedIcoSizes, OfferedIcoSizes);
        PngSizes = BuildSizeOptions(OfferedPngSizes, DefaultPngSizes);

        BrowseCommand = new AsyncRelayCommand(OnBrowseAsync);
        GenerateCommand = new AsyncRelayCommand(OnGenerateAsync, CanGenerate);
        ResetCommand = new RelayCommand(Reset);

        // Assigned here rather than left to Reset(): ActiveIcon and CurrentDesign are non-nullable,
        // and the compiler's definite-assignment analysis does not see through a method call.
        ActiveIcon = DefaultIconEntry;
        SelectedIcon = ActiveIcon;
        CurrentDesign = BuildDesign();

        ApplyFilter();
        RegeneratePreview();
        RefreshGenerateState();

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
    } = StudioDefaults.SearchText;

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
    } = FindWeight(StudioDefaults.Weight);

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
    } = FindFillMode(StudioDefaults.FillMode);

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
    } = StudioDefaults.PrimaryColor;

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
    } = StudioDefaults.SecondaryColor;

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
    } = StudioDefaults.GradientAngleDegrees;

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
    } = StudioDefaults.GlyphColor;

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
    } = StudioDefaults.CornerRadiusRatio;

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
    } = StudioDefaults.GlyphScale;

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

    /// <summary>The directory the next export writes into. Must exist before Generate lights up.</summary>
    public string OutputDirectory
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RefreshGenerateState();
            }
        }
    } = string.Empty;

    /// <summary>The file-name stem: <c>app</c> writes <c>app.ico</c> and <c>app-256.png</c>.</summary>
    public string BaseName
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RefreshGenerateState();
            }
        }
    } = StudioDefaults.BaseName;

    /// <summary>The frame sizes offered for the <c>.ico</c>.</summary>
    public IReadOnlyList<SizeOption> IcoSizes { get; }

    /// <summary>The sizes offered for the standalone PNGs.</summary>
    public IReadOnlyList<SizeOption> PngSizes { get; }

    /// <summary>True while an export is running; the button stays disabled until it finishes.</summary>
    public bool IsExporting
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RefreshGenerateState();
            }
        }
    }

    /// <summary>Why Generate is disabled, or empty when it is ready.</summary>
    /// <remarks>
    /// A disabled button with no explanation is a guessing game — especially the "pick a folder
    /// first" case, which is where every run starts.
    /// </remarks>
    public string GenerateHint
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Asks for an output directory.</summary>
    public AsyncRelayCommand BrowseCommand { get; }

    /// <summary>Writes the icon and the PNGs.</summary>
    public AsyncRelayCommand GenerateCommand { get; }

    /// <summary>Puts every design and output setting back to the value the window opened with.</summary>
    /// <remarks>
    /// Always enabled. Tracking whether the window is already at its defaults would mean comparing
    /// eleven values on every property change, and "why is Reset greyed out?" is a worse question
    /// than a click that does nothing.
    /// </remarks>
    public RelayCommand ResetCommand { get; }

    /// <summary>Builds the request the next export would run.</summary>
    /// <remarks>
    /// Composed from the <i>current</i> control values rather than from <see cref="CurrentDesign"/>:
    /// the preview is debounced, so pressing Generate within 150 ms of moving a slider would
    /// otherwise write the design as it was before the move.
    /// </remarks>
    /// <returns>The validated, normalized request.</returns>
    /// <exception cref="ArgumentException">The output directory, base name or size selection is not
    /// usable — the same rules <see cref="CanGenerate"/> tests before enabling the button.</exception>
    public ExportRequest BuildExportRequest()
        => new ExportRequest(
            BuildDesign(),
            OutputDirectory.Trim(),
            BaseName,
            SelectedSizes(IcoSizes),
            SelectedSizes(PngSizes));

    /// <summary>
    /// Restores every design and output setting to its <see cref="StudioDefaults"/> value, and
    /// re-renders immediately.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The output folder is deliberately kept.</b> It is a session destination rather than a
    /// design value: clearing it would re-disable <see cref="GenerateCommand"/> and send the user
    /// back through the file dialog, which costs more than the reset saves. The button's tooltip says
    /// so, so the exception is not a surprise.
    /// </para>
    /// <para>
    /// The render is synchronous rather than debounced. <see cref="SchedulePreview"/> exists to
    /// coalesce slider drags and keystrokes; a reset is one deliberate click, so the preview, the
    /// thumbnail strip and <see cref="StatusText"/> are correct the instant the button is released —
    /// the same closing move the constructor makes.
    /// </para>
    /// </remarks>
    public void Reset()
    {
        // The icon first: ApplyFilter reads ActiveIcon to decide what the list highlights.
        ActiveIcon = DefaultIconEntry;
        SearchText = StudioDefaults.SearchText;

        SelectedWeight = FindWeight(StudioDefaults.Weight);
        SelectedFillMode = FindFillMode(StudioDefaults.FillMode);
        PrimaryColor = StudioDefaults.PrimaryColor;
        SecondaryColor = StudioDefaults.SecondaryColor;
        GradientAngle = StudioDefaults.GradientAngleDegrees;
        GlyphColor = StudioDefaults.GlyphColor;
        CornerRadiusRatio = StudioDefaults.CornerRadiusRatio;
        GlyphScale = StudioDefaults.GlyphScale;

        BaseName = StudioDefaults.BaseName;
        RestoreDefaultSizes(IcoSizes);
        RestoreDefaultSizes(PngSizes);

        // Unconditionally, because SearchText's setter only filters when the text actually changed —
        // and a reset from an already-empty search box still has to re-highlight the restored icon.
        ApplyFilter();

        _previewDebounce.Stop();
        RegeneratePreview();
    }

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

    /// <summary>The catalog row a new — or a freshly reset — design starts from.</summary>
    /// <remarks>
    /// <see cref="PhosphorIconNames.All"/> is in enum order (SPEC §8.4), so the cast <i>is</i> the
    /// index — no <c>Enum.Parse</c>, no reflection (SPEC §2.11).
    /// </remarks>
    private static IconEntry DefaultIconEntry => Catalog[(int)StudioDefaults.Icon];

    /// <summary>Finds the selector entry for a weight.</summary>
    /// <param name="weight">The weight to look up.</param>
    /// <returns>The matching <see cref="WeightOption"/>.</returns>
    /// <exception cref="InvalidOperationException">No entry carries that weight.</exception>
    /// <remarks>
    /// A lookup rather than <c>WeightOptionList[4]</c>: the default is stated once, as a weight, in
    /// <see cref="StudioDefaults"/> — reordering the selector must not silently change it. Six
    /// comparisons, run on construction and on a reset, never on the render path.
    /// </remarks>
    private static WeightOption FindWeight(PhosphorWeight weight)
    {
        foreach (WeightOption option in WeightOptionList)
        {
            if (option.Weight == weight)
            {
                return option;
            }
        }

        // Unreachable while the list covers all six declared weights. A throw rather than a silent
        // fallback to the first entry, so an edit that drops one fails loudly instead of quietly
        // changing what the studio opens with.
        throw new InvalidOperationException("No weight option matches the requested weight.");
    }

    /// <summary>Finds the selector entry for a plate fill mode.</summary>
    /// <param name="mode">The mode to look up.</param>
    /// <returns>The matching <see cref="FillModeOption"/>.</returns>
    /// <exception cref="InvalidOperationException">No entry carries that mode.</exception>
    private static FillModeOption FindFillMode(PlateFillMode mode)
    {
        foreach (FillModeOption option in FillModeOptionList)
        {
            if (option.Mode == mode)
            {
                return option;
            }
        }

        throw new InvalidOperationException("No fill mode option matches the requested mode.");
    }

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

    /// <summary>Whether the Generate button should be enabled, and why not when it should not.</summary>
    /// <remarks>
    /// The base-name rule is <see cref="ExportRequest.IsValidBaseName"/> — the same predicate the
    /// request constructor enforces, so the button and the constructor can never disagree, and
    /// neither has to learn the answer by catching an exception per keystroke.
    /// </remarks>
    private bool CanGenerate() => DescribeGenerateBlocker() is null;

    private string? DescribeGenerateBlocker()
    {
        if (IsExporting)
        {
            return "Writing…";
        }

        string directory = OutputDirectory.Trim();
        if (directory.Length == 0)
        {
            return "Choose an output folder.";
        }

        if (!Directory.Exists(directory))
        {
            return "That folder does not exist.";
        }

        if (!ExportRequest.IsValidBaseName(BaseName.Trim()))
        {
            return "The base name must be a plain file name.";
        }

        if (!HasAnySize(IcoSizes) && !HasAnySize(PngSizes))
        {
            return "Select at least one size.";
        }

        return null;
    }

    private void RefreshGenerateState()
    {
        GenerateHint = DescribeGenerateBlocker() ?? string.Empty;
        GenerateCommand.NotifyCanExecuteChanged();
    }

    private async Task OnBrowseAsync()
    {
        string? start = string.IsNullOrWhiteSpace(OutputDirectory) ? null : OutputDirectory.Trim();
        string? chosen = await _folderPicker.PickFolderAsync(start).ConfigureAwait(true);

        // Null is a cancelled dialog, an unavailable picker, or a folder with no local path. In every
        // case the right answer is to leave what the user already had.
        if (chosen is not null)
        {
            OutputDirectory = chosen;
        }
    }

    private async Task OnGenerateAsync()
    {
        IsExporting = true;
        try
        {
            ExportResult result = await _exporter
                .ExportAsync(BuildExportRequest(), CancellationToken.None)
                .ConfigureAwait(true);

            StatusText = string.Format(
                CultureInfo.InvariantCulture,
                "Wrote {0} file{1} to {2}",
                result.WrittenFiles.Count,
                result.WrittenFiles.Count == 1 ? string.Empty : "s",
                result.OutputDirectory);
        }
        catch (Exception exception)
        {
            // Deliberately broad. An export touches the file system and a rendering backend, and this
            // runs inside a command whose task nobody awaits — an escaping exception would be an
            // unobserved crash rather than a message. The failure is reported, not hidden.
            StatusText = string.Format(
                CultureInfo.InvariantCulture,
                "Export failed: {0}",
                exception.Message);
        }
        finally
        {
            IsExporting = false;
        }
    }

    private SizeOption[] BuildSizeOptions(int[] offered, int[] selected)
    {
        var options = new SizeOption[offered.Length];

        for (int i = 0; i < offered.Length; i++)
        {
            options[i] = new SizeOption(offered[i], Array.IndexOf(selected, offered[i]) >= 0);

            // The Generate button's CanExecute depends on at least one box being ticked, and a
            // CheckBox reports that to its own item, not to this ViewModel.
            options[i].PropertyChanged += OnSizeOptionChanged;
        }

        return options;
    }

    private void OnSizeOptionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(SizeOption.IsSelected))
        {
            RefreshGenerateState();
        }
    }

    /// <summary>Re-ticks a size list exactly as the window opened it.</summary>
    /// <param name="options">The list to restore.</param>
    /// <remarks>
    /// Each option knows its own starting state, so this never has to learn which sizes
    /// <see cref="BuildSizeOptions"/> was handed. Every change routes back through
    /// <see cref="OnSizeOptionChanged"/>, so <see cref="GenerateCommand"/> re-evaluates on its own.
    /// </remarks>
    private static void RestoreDefaultSizes(IReadOnlyList<SizeOption> options)
    {
        for (int i = 0; i < options.Count; i++)
        {
            options[i].IsSelected = options[i].IsSelectedByDefault;
        }
    }

    private static bool HasAnySize(IReadOnlyList<SizeOption> options)
    {
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].IsSelected)
            {
                return true;
            }
        }

        return false;
    }

    private static int[] SelectedSizes(IReadOnlyList<SizeOption> options)
    {
        var selected = new List<int>(options.Count);

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].IsSelected)
            {
                selected.Add(options[i].SizePx);
            }
        }

        return selected.ToArray();
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
