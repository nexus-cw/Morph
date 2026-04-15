using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Morph.Execution;

namespace Morph.Expressions;

/// <summary>
/// Accumulates per-map configuration. Stored inside the owning <see cref="Profile"/> (or the
/// inline registration) and transformed into a <see cref="TypeMap"/> by
/// <see cref="MapPlanBuilder"/> when the <see cref="MapperConfiguration"/> is built.
/// </summary>
internal sealed class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
{
    public Type SourceType => typeof(TSource);
    public Type DestinationType => typeof(TDestination);

    /// <summary>Explicit member configurations, keyed by destination member name.</summary>
    public Dictionary<string, MemberPlan> ExplicitMembers { get; } = new();

    public Delegate? CustomConstructor { get; private set; }
    public bool ConstructorTakesContext { get; private set; }

    public Delegate? TypeConverterFunc { get; private set; }
    public bool TypeConverterTakesDestination { get; private set; }
    public Type? TypeConverterType { get; private set; }

    /// <summary>Non-null once ReverseMap() has been called, so the builder knows to register the inverse.</summary>
    public object? ReverseMapExpression { get; private set; }

    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        var destMember = MemberPlan.ResolveMember(destinationMember);
        var plan = new MemberPlan(destMember);
        var cfg = new MemberConfigurationExpression<TSource, TDestination, TMember>(plan);
        memberOptions(cfg);
        ExplicitMembers[destMember.Name] = plan;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForMember(
        string destinationMemberName,
        Action<IMemberConfigurationExpression<TSource, TDestination, object?>> memberOptions)
    {
        var destMember = FindMember(typeof(TDestination), destinationMemberName)
            ?? throw new ArgumentException(
                $"Destination type {typeof(TDestination).Name} has no public member named '{destinationMemberName}'");
        var plan = new MemberPlan(destMember);
        var cfg = new MemberConfigurationExpression<TSource, TDestination, object?>(plan);
        memberOptions(cfg);
        ExplicitMembers[destMember.Name] = plan;
        return this;
    }

    public IMappingExpression<TDestination, TSource> ReverseMap()
    {
        var reverse = new MappingExpression<TDestination, TSource>();
        ReverseMapExpression = reverse;
        return reverse;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>()
        where TConverter : ITypeConverter<TSource, TDestination>, new()
    {
        TypeConverterType = typeof(TConverter);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter)
    {
        TypeConverterFunc = converter;
        TypeConverterTakesDestination = false;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination, TDestination> converter)
    {
        TypeConverterFunc = converter;
        TypeConverterTakesDestination = true;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor)
    {
        CustomConstructor = ctor;
        ConstructorTakesContext = false;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, ResolutionContext, TDestination> ctor)
    {
        CustomConstructor = ctor;
        ConstructorTakesContext = true;
        return this;
    }

    private static MemberInfo? FindMember(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        return (MemberInfo?)type.GetProperty(name, flags) ?? type.GetField(name, flags);
    }
}
