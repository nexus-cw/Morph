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

    /// <summary>
    /// When true (default), a forward <c>ForMember(d => d.X, o => o.Ignore())</c> followed by
    /// <c>ReverseMap()</c> also Ignores the reverse-destination member of the same name.
    /// <para/>
    /// This is a deliberate hardening default that diverges from AutoMapper v14, which treats
    /// <c>Ignore()</c> as forward-only and lets the reverse convention-copy the field — a silent
    /// round-trip leak when the forward Ignore was hiding a sensitive value. Set to <c>false</c>
    /// for byte-for-byte v14 parity when migrating a codebase that deliberately relied on the
    /// old behavior.
    /// <para/>
    /// Snapshotted at <c>CreateMapper()</c> time like <see cref="MaxDepth"/>.
    /// </summary>
    bool MirrorIgnoreOnReverse { get; set; }

    /// <summary>Register a profile type. A new instance is created.</summary>
    void AddProfile<TProfile>() where TProfile : Profile, new();

    /// <summary>Register a profile instance. Useful for profiles that take constructor arguments.</summary>
    void AddProfile(Profile profile);

    /// <summary>Scan <paramref name="assembly"/> for concrete <see cref="Profile"/> subclasses and register them.</summary>
    void AddProfiles(Assembly assembly);

    /// <summary>Register an inline map. Behaves identically to <c>CreateMap</c> inside a profile.</summary>
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
}
