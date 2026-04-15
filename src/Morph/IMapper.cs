using System;

namespace Morph;

/// <summary>
/// Maps source instances to destination instances using a pre-built
/// <see cref="MapperConfiguration"/>. Not thread-safe for a single instance's state, but
/// stateless across calls — one <c>IMapper</c> per configuration is the intended use.
/// </summary>
public interface IMapper
{
    /// <summary>Map using runtime type discovery on <paramref name="source"/>.</summary>
    TDestination Map<TDestination>(object source);

    /// <summary>Map using compile-time <typeparamref name="TSource"/> for dispatch.</summary>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>Map into an existing destination instance. Returns <paramref name="destination"/>.</summary>
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>Non-generic map for when types are only known at runtime.</summary>
    object? Map(object? source, Type sourceType, Type destinationType);

    /// <summary>Configuration this mapper was built from.</summary>
    IConfigurationProvider ConfigurationProvider { get; }
}
