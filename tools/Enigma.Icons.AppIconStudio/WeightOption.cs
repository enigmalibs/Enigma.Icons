using Enigma.Icons.Phosphor;

namespace Enigma.Icons.AppIconStudio;

/// <summary>One entry of the weight selector.</summary>
/// <param name="Name">The label shown in the combo box.</param>
/// <param name="Weight">The weight it selects.</param>
/// <remarks>
/// A record carrying its own label, rather than binding the enum and letting the default template
/// call <c>ToString</c> on it — SPEC §2.11's no-<c>Enum.ToString</c> rule, and the only way to give a
/// member a label that is not its identifier should one ever need one.
/// </remarks>
public sealed record WeightOption(string Name, PhosphorWeight Weight);
