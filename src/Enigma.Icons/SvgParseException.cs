using System;

namespace Enigma.Icons;

/// <summary>
/// Thrown when an SVG document is malformed, exceeds
/// <see cref="SvgIconParser.MaxDocumentBytes"/>, contains no paintable element, or cannot be read
/// from its source.
/// </summary>
/// <remarks>
/// There is deliberately no <c>(SerializationInfo, StreamingContext)</c> constructor: binary
/// serialization of exceptions is obsolete on <c>net8.0</c>/<c>net10.0</c> (SYSLIB0051), and with
/// <c>TreatWarningsAsErrors</c> that obsoletion is a build error.
/// </remarks>
public sealed class SvgParseException : Exception
{
    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">A description of what could not be parsed.</param>
    public SvgParseException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an underlying cause.</summary>
    /// <param name="message">A description of what could not be parsed.</param>
    /// <param name="innerException">The underlying cause, typically an <see cref="System.Xml.XmlException"/> or an <see cref="System.IO.IOException"/>.</param>
    public SvgParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
