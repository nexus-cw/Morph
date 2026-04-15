using System;

namespace Morph;

/// <summary>
/// Resolved configuration snapshot. Exposed via <see cref="IMapper.ConfigurationProvider"/>.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Validates the configuration. Throws if any declared map has an unmapped public settable
    /// destination member that isn't <c>Ignore()</c>d, <c>UseValue</c>d, or satisfied by a
    /// <c>MapFrom</c> or convention-matched source member.
    /// </summary>
    void AssertConfigurationIsValid();

    /// <summary>Creates a fresh <see cref="IMapper"/> bound to this configuration.</summary>
    IMapper CreateMapper();
}
