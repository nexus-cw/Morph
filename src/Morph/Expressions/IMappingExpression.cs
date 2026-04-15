using System;
using System.Linq.Expressions;

namespace Morph;

/// <summary>
/// Per-map fluent configuration returned from <c>CreateMap&lt;TSrc,TDest&gt;()</c>.
/// </summary>
public interface IMappingExpression<TSource, TDestination>
{
    /// <summary>Configure a single destination member by expression (e.g. <c>d =&gt; d.FullName</c>).</summary>
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

    /// <summary>Configure a destination member by name. Use when the name isn't known at compile time.</summary>
    IMappingExpression<TSource, TDestination> ForMember(
        string destinationMemberName,
        Action<IMemberConfigurationExpression<TSource, TDestination, object?>> memberOptions);

    /// <summary>Create the inverse map. Member-level configuration does not automatically reverse.</summary>
    IMappingExpression<TDestination, TSource> ReverseMap();

    /// <summary>Bypass default member-by-member mapping; run <typeparamref name="TConverter"/> instead.</summary>
    IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>()
        where TConverter : ITypeConverter<TSource, TDestination>, new();

    /// <summary>Bypass default member-by-member mapping; run the supplied function instead.</summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter);

    /// <summary>Bypass default member-by-member mapping; run the supplied function instead.</summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination, TDestination> converter);

    /// <summary>Custom destination construction. Morph uses this to create the destination instead of <c>new TDestination()</c>.</summary>
    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor);

    /// <summary>Custom destination construction with access to <see cref="ResolutionContext"/>.</summary>
    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, ResolutionContext, TDestination> ctor);
}
