using System.Collections.Generic;

namespace Morph;

/// <summary>
/// Per-resolve state passed to custom resolvers and constructors. Carries per-operation
/// items from the caller and a back-reference to the mapper for nested Map calls.
/// </summary>
public sealed class ResolutionContext
{
    internal ResolutionContext(IMapper mapper)
        : this(mapper, currentDepth: 0) { }

    internal ResolutionContext(IMapper mapper, int currentDepth)
    {
        Mapper = mapper;
        Items = new Dictionary<string, object?>();
        CurrentDepth = currentDepth;
    }

    /// <summary>Mapper running this resolve. Use for nested <c>Map&lt;T&gt;</c> calls from within a resolver.</summary>
    public IMapper Mapper { get; }

    /// <summary>Free-form per-operation items. Morph does not interpret these; consumer-set only.</summary>
    public IDictionary<string, object?> Items { get; }

    // Recursion depth of the map that owns this context. Handed to resolvers via the
    // depth-aware mapper wrapper so that `context.Mapper.Map<>()` re-entry inherits the
    // outer depth instead of restarting at 0 and bypassing MaxDepth. See ticket #71 (I4).
    internal int CurrentDepth { get; }
}
