using System;
using System.Reflection;

namespace Morph;

/// <summary>
/// Top-level configuration builder. The action passed to <see cref="MapperConfiguration"/>
/// receives this; use it to register profiles or create inline maps.
/// </summary>
public interface IMapperConfigurationExpression
{
    /// <summary>
    /// Maximum recursion depth for nested maps. Default 32. Guards against DoS via
    /// self-referential graphs (cf. AutoMapper CVE-2026-32933). Set this inside the
    /// configure action; the value is snapshotted into the live mapper at
    /// <c>CreateMapper()</c> time and cannot be changed thereafter.
    /// </summary>
    int MaxDepth { get; set; }

    /// <summary>Register a profile type. A new instance is created.</summary>
    void AddProfile<TProfile>() where TProfile : Profile, new();

    /// <summary>Register a profile instance. Useful for profiles that take constructor arguments.</summary>
    void AddProfile(Profile profile);

    /// <summary>Scan <paramref name="assembly"/> for concrete <see cref="Profile"/> subclasses and register them.</summary>
    void AddProfiles(Assembly assembly);

    /// <summary>Register an inline map. Behaves identically to <c>CreateMap</c> inside a profile.</summary>
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
}
