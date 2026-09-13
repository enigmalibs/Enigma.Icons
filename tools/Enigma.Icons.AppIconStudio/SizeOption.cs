using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.Icons.AppIconStudio;

/// <summary>One tick-box in an output size list.</summary>
/// <remarks>
/// Observable because the Generate button's <c>CanExecute</c> depends on at least one size being
/// ticked, so the ViewModel listens to each option's <see cref="ObservableObject.PropertyChanged"/>
/// rather than the view having to tell it something changed.
/// </remarks>
public sealed class SizeOption : ObservableObject
{
    /// <summary>Initializes a new option.</summary>
    /// <param name="sizePx">The square edge in pixels.</param>
    /// <param name="isSelected">Whether it starts ticked.</param>
    public SizeOption(int sizePx, bool isSelected)
    {
        SizePx = sizePx;
        IsSelected = isSelected;
    }

    /// <summary>The square edge in pixels.</summary>
    public int SizePx { get; }

    /// <summary>Whether this size is included in the next export.</summary>
    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    }
}
