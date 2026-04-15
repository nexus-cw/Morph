using System;

namespace Morph.Execution;

// IMapper wrapper handed to IValueResolver implementations via ResolutionContext.Mapper.
// Delegates to the underlying Mapper but pre-loads the re-entry depth so a resolver's
// `context.Mapper.Map<>()` call counts as one level deeper than the map that ran it.
// Without this, resolver re-entry restarts MapDynamic at depth 0, bypassing MaxDepth
// and reopening the stack-exhaustion window MaxDepth is there to close. See ticket #71 (I4).
internal sealed class DepthAwareMapper : IMapper
{
    private readonly Mapper _inner;
    private readonly int _outerDepth;

    internal DepthAwareMapper(Mapper inner, int outerDepth)
    {
        _inner = inner;
        _outerDepth = outerDepth;
    }

    public IConfigurationProvider ConfigurationProvider => _inner.ConfigurationProvider;

    public TDestination Map<TDestination>(object source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return (TDestination)_inner.MapDynamic(source, source.GetType(), typeof(TDestination), _outerDepth + 1)!;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
        => (TDestination)_inner.MapDynamic(source, typeof(TSource), typeof(TDestination), _outerDepth + 1)!;

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        _inner.MapInto(source, typeof(TSource), destination!, typeof(TDestination), _outerDepth + 1);
        return destination;
    }

    public object? Map(object? source, Type sourceType, Type destinationType)
        => _inner.MapDynamic(source, sourceType, destinationType, _outerDepth + 1);
}
