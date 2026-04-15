using System;
using System.Linq.Expressions;

namespace Morph;

/// <summary>
/// Per-member fluent configuration. Returned from inside the action passed to
/// <c>IMappingExpression.ForMember(...)</c>.
/// </summary>
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>Map the destination member from a source-member expression (e.g. <c>s =&gt; s.Name</c>).</summary>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);

    /// <summary>Map the destination member from an arbitrary function of source + current destination.</summary>
    void MapFrom(Func<TSource, TDestination, TMember> resolver);

    /// <summary>Map the destination member using a custom <see cref="IValueResolver{TSource,TDestination,TMember}"/>.</summary>
    void MapFrom<TResolver>() where TResolver : IValueResolver<TSource, TDestination, TMember>, new();

    /// <summary>Skip this destination member entirely during mapping and validation.</summary>
    void Ignore();

    /// <summary>Always set the destination member to <paramref name="value"/>.</summary>
    void UseValue(TMember value);

    /// <summary>Only perform the map when <paramref name="condition"/> returns <c>true</c>.</summary>
    void Condition(Func<TSource, bool> condition);
}
