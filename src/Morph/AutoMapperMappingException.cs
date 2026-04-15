using System;

namespace Morph;

/// <summary>
/// Thrown when a mapping operation fails. Name retained from AutoMapper for drop-in catch-block compatibility.
/// </summary>
public class AutoMapperMappingException : Exception
{
    public AutoMapperMappingException() { }
    public AutoMapperMappingException(string message) : base(message) { }
    public AutoMapperMappingException(string message, Exception inner) : base(message, inner) { }

    /// <summary>Source type involved in the failed map, when known.</summary>
    public Type? SourceType { get; set; }

    /// <summary>Destination type involved in the failed map, when known.</summary>
    public Type? DestinationType { get; set; }

    /// <summary>Member name within the destination type being mapped, when applicable.</summary>
    public string? MemberName { get; set; }
}
