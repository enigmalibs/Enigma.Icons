using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Icons.Phosphor;

namespace Enigma.Icons.Avalonia.Gallery;

/// <summary>Drives the whole gallery window: the catalog, the filter, the shared presentation
/// state every cell binds up to, and the copy-to-clipboard command.</summary>
/// <remarks>
/// Explicit CommunityToolkit.Mvvm style — <c>field</c>-backed properties with <c>SetProperty</c> and
/// a get-only command initialized in the constructor. No <c>[ObservableProperty]</c> /
/// <c>[RelayCommand]</c> source generators, so the class does not need to be <c>partial</c>. This is
/// a deliberate deviation from SPEC §11, which predates the house convention; see
/// <c>docs/done/FEATURE-469B.md</c>.
/// </remarks>
public sealed class MainWindowViewModel : ObservableObject
{
    /// <summary>Cells per grid row. The row, not the cell, is the virtualized unit.</summary>
    private const int ColumnsPerRow = 8;

    /// <summary>
    /// Weight display names, indexed by <see cref="PhosphorWeight"/>'s numeric value — SPEC §2.11's
    /// no-<c>Enum.ToString</c> rule applied to the status line, which is recomputed on every filter.
    /// </summary>
    private static readonly string[] WeightNames =
        ["Thin", "Light", "Regular", "Bold", "Fill", "Duotone"];

    /// <summary>
    /// The one-time catalog of all 1,512 icons. <see cref="PhosphorIconNames.All"/> is guaranteed to
    /// be in enum order (SPEC §8.4, asserted by the §12.2 tests), so the index <i>is</i> the
    /// <see cref="PhosphorIcon"/> value — no <c>Enum.Parse</c>, no reflection.
    /// </summary>
    private static readonly IconEntry[] Catalog = BuildCatalog();

    /// <summary>
    /// A single-glyph set built from inline SVG — the by-eye cross-check on FEATURE-24DD's
    /// shape → arc conversion (SPEC §5.1), and proof that the <c>Icon</c> control works with any
    /// <see cref="IIconSet"/>, not just Phosphor.
    /// </summary>
    private static readonly IIconSet Shapes = SvgIconSet.FromSvgSources(
        [
            new KeyValuePair<string, string>(
                "shapes",
                """
                <svg viewBox="0 0 256 256">
                  <circle cx="60" cy="64" r="44" />
                  <ellipse cx="180" cy="64" rx="60" ry="36" />
                  <rect x="20" y="140" width="216" height="84" rx="28" ry="28" />
                </svg>
                """),
        ],
        "Gallery shapes");

    private readonly IClipboardTextWriter _clipboard;

    /// <summary>Coalesces keystrokes so the visual tree is rebuilt once, not once per character.</summary>
    private readonly DispatcherTimer _searchDebounce;

    /// <summary>Restores the status line after a copy confirmation.</summary>
    private readonly DispatcherTimer _copyConfirmation;

    private int _matchCount;

    /// <summary>Creates the ViewModel and applies the initial, unfiltered view.</summary>
    /// <param name="clipboard">Writes the generated XAML snippet to the system clipboard.</param>
    /// <exception cref="ArgumentNullException"><paramref name="clipboard"/> is null.</exception>
    public MainWindowViewModel(IClipboardTextWriter clipboard)
    {
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _searchDebounce.Tick += OnSearchDebounceTick;

        _copyConfirmation = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _copyConfirmation.Tick += OnCopyConfirmationTick;

        CopyXamlCommand = new AsyncRelayCommand<IconEntry>(OnCopyXamlAsync);

        ApplyFilter();
    }

    /// <summary>The search term. Matching is case-insensitive substring over the icon names.</summary>
    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Restart, do not extend: one shot, 200 ms after the last keystroke.
                _searchDebounce.Stop();
                _searchDebounce.Start();
            }
        }
    } = string.Empty;

    /// <summary>The weight every grid icon is drawn in. Defaults to <see cref="PhosphorWeight.Regular"/>.</summary>
    public PhosphorWeight SelectedWeight
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // The result set is unchanged — only the status line's weight name moves.
                UpdateStatus();
            }
        }
    } = PhosphorWeight.Regular;

    /// <summary>The size, in device-independent pixels, of every grid icon.</summary>
    public double IconSize
    {
        get;
        set => SetProperty(ref field, value);
    } = 32;

    /// <summary>The brush every grid icon paints with.</summary>
    public ColorPreset SelectedColor
    {
        get;
        set => SetProperty(ref field, value);
    } = ColorPresetList[0];

    /// <summary>The stretch mode of the non-square demo icon in the header.</summary>
    public Stretch SelectedStretch
    {
        get;
        set => SetProperty(ref field, value);
    } = Stretch.Uniform;

    /// <summary>The filtered catalog, chunked into rows of <see cref="ColumnsPerRow"/>.</summary>
    /// <remarks>
    /// Replaced wholesale, never mutated. An <c>ObservableCollection</c> cleared and refilled item by
    /// item would raise 1,512 notifications and defeat the virtualization this sample exists to show.
    /// </remarks>
    public IReadOnlyList<IconRow> Rows
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>The filtered count out of the full catalog, plus the current weight.</summary>
    public string StatusText
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>The message shown in place of the grid when nothing matches.</summary>
    public string EmptyMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>False when the filter matched nothing; hides the grid and shows
    /// <see cref="EmptyMessage"/>.</summary>
    public bool HasResults
    {
        get;
        private set => SetProperty(ref field, value);
    } = true;

    /// <summary>All six weights, in declaration order — an explicit array, not
    /// <c>Enum.GetValues</c> (SPEC §2.11).</summary>
    public IReadOnlyList<PhosphorWeight> Weights { get; } =
    [
        PhosphorWeight.Thin,
        PhosphorWeight.Light,
        PhosphorWeight.Regular,
        PhosphorWeight.Bold,
        PhosphorWeight.Fill,
        PhosphorWeight.Duotone,
    ];

    /// <summary>The colour presets offered by the selector.</summary>
    public IReadOnlyList<ColorPreset> ColorPresets => ColorPresetList;

    /// <summary>The four stretch modes, for the non-square demo.</summary>
    public IReadOnlyList<Stretch> Stretches { get; } =
        [Stretch.None, Stretch.Uniform, Stretch.UniformToFill, Stretch.Fill];

    /// <summary>The inline-SVG set backing the header's <c>shapes</c> glyph.</summary>
    public IIconSet ShapesIconSet => Shapes;

    /// <summary>Copies the clicked icon's XAML snippet to the clipboard.</summary>
    public AsyncRelayCommand<IconEntry> CopyXamlCommand { get; }

    /// <summary>
    /// Backs <see cref="ColorPresets"/>. A static field rather than a property initializer because
    /// <see cref="SelectedColor"/>'s initializer reads it, and instance initializers cannot see
    /// instance members.
    /// </summary>
    /// <remarks>
    /// Every brush is non-null on purpose — SPEC §10.2 makes a null <c>Foreground</c> paint nothing,
    /// which would read as a bug rather than as a colour choice.
    /// </remarks>
    private static IReadOnlyList<ColorPreset> ColorPresetList { get; } =
    [
        // First, and therefore the default. A mid tone on purpose: it has to stay legible against
        // both the light and the dark Fluent background, since the app follows the OS theme variant.
        new ColorPreset("Slate", new SolidColorBrush(Color.FromRgb(0x7A, 0x88, 0x99))),
        new ColorPreset("Black", Brushes.Black),
        new ColorPreset("White", Brushes.White),
        new ColorPreset("Blue", new SolidColorBrush(Color.FromRgb(0x21, 0x74, 0xD9))),
        new ColorPreset("Red", new SolidColorBrush(Color.FromRgb(0xD1, 0x3B, 0x3B))),
        new ColorPreset("Green", new SolidColorBrush(Color.FromRgb(0x2E, 0x8B, 0x57))),
    ];

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

    private void OnSearchDebounceTick(object? sender, EventArgs e)
    {
        // One shot: stop before applying, so a Tick never repeats.
        _searchDebounce.Stop();
        ApplyFilter();
    }

    private void OnCopyConfirmationTick(object? sender, EventArgs e)
    {
        _copyConfirmation.Stop();
        UpdateStatus();
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

        var rows = new List<IconRow>((matches.Count + ColumnsPerRow - 1) / ColumnsPerRow);
        for (int start = 0; start < matches.Count; start += ColumnsPerRow)
        {
            int length = Math.Min(ColumnsPerRow, matches.Count - start);
            var cells = new IconEntry[length];
            matches.CopyTo(start, cells, 0, length);
            rows.Add(new IconRow(cells));
        }

        _matchCount = matches.Count;
        Rows = rows;
        HasResults = matches.Count > 0;
        EmptyMessage = string.Format(CultureInfo.InvariantCulture, "No icons match '{0}'", term);

        // A pending confirmation is stale the moment the result set moves.
        _copyConfirmation.Stop();
        UpdateStatus();
    }

    private void UpdateStatus()
        => StatusText = string.Format(
            CultureInfo.InvariantCulture,
            "Showing {0:N0} of {1:N0} icons · {2}",
            _matchCount,
            Catalog.Length,
            WeightNames[(int)SelectedWeight]);

    private async Task OnCopyXamlAsync(IconEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        string snippet = string.Format(
            CultureInfo.InvariantCulture,
            "<ei:Icon Kind=\"{0}\" Weight=\"{1}\" Size=\"{2}\" />",
            // The XAML attribute value is the enum member name, which is what ToString returns. One
            // call per click is not the hot path SPEC §2.11 is about — that rule guards the library's
            // per-glyph lookup, which this sample never goes near (see BuildCatalog and WeightNames).
            entry.Kind,
            WeightNames[(int)SelectedWeight],
            IconSize.ToString("0.##", CultureInfo.InvariantCulture));

        // Foreground is deliberately absent: the consumer's brush is theirs, and SPEC §10.2 makes it
        // inherit from the enclosing text scope anyway.
        bool copied = await _clipboard.TryWriteAsync(snippet).ConfigureAwait(true);

        StatusText = copied
            ? string.Format(CultureInfo.InvariantCulture, "Copied {0}", snippet)
            : "Clipboard unavailable — nothing was copied.";

        _copyConfirmation.Stop();
        _copyConfirmation.Start();
    }
}
