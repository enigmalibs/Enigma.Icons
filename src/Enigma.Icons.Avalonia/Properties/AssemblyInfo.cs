using Avalonia.Metadata;

// SPEC §10.3. Avalonia's `using:` mapping covers one CLR namespace and NOT its sub-namespaces, so a
// lone xmlns:ei="using:Enigma.Icons.Avalonia" would reach <ei:Icon> but not {ei:IconGeometry ...}.
// Mapping both CLR namespaces onto one XML namespace URI — the repository URL, per the ecosystem
// convention and SPEC §10.5 — is what lets a consumer declare a single prefix and get the control
// AND both markup extensions. The per-namespace `using:` forms keep working as the documented
// fallback.
[assembly: XmlnsDefinition("https://github.com/josueclement/Enigma.Icons", "Enigma.Icons.Avalonia")]
[assembly: XmlnsDefinition("https://github.com/josueclement/Enigma.Icons", "Enigma.Icons.Avalonia.Markup")]
