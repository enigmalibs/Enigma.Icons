using Enigma.Icons.AppIconStudio.Design;

namespace Enigma.Icons.AppIconStudio;

/// <summary>One entry of the plate fill selector.</summary>
/// <param name="Name">The label shown in the combo box.</param>
/// <param name="Mode">The fill mode it selects.</param>
public sealed record FillModeOption(string Name, PlateFillMode Mode);
