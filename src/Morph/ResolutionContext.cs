using System.Collections.Generic;

namespace Morph;

/// <summary>
/// Per-resolve state passed to custom resolvers and constructors. Carries per-operation
/// items from the caller and a back-reference to the mapper for nested Map calls.
/// </summary>
public sealed class ResolutionContext
{
    internal ResolutionContext(IMapper mapper)
    {
        Mapper = mapper;
        Items = new Dictionary<string, object?>();
    }

    /// <summary>Mapper running this resolve. Use for nested <c>Map&lt;T&gt;</c> calls from within a resolver.</summary>
    public IMapper Mapper { get; }

    /// <summary>Free-form per-operation items. Morph does not interpret these; consumer-set only.</summary>
    public IDictionary<string, object?> Items { get; }
}
