using System;
using System.Collections.Generic;
using System.Linq;
using Morph.Execution;

namespace Morph;

/// <summary>
/// Entry point for building a Morph mapper. Construct with a configuration action; the resulting
/// instance is thread-safe and reusable — build once, reuse for the lifetime of the consumer.
/// </summary>
public sealed class MapperConfiguration : IConfigurationProvider
{
    internal IReadOnlyDictionary<(Type, Type), TypeMap> TypeMaps { get; }

    // Default recursion cap for nested maps. Guards against DoS via self-referential graphs
    // (cf. AutoMapper CVE-2026-32933). Legitimate object graphs rarely exceed ~10 levels;
    // 32 leaves headroom without allowing unbounded stack growth.
    public int MaxDepth { get; set; } = 32;

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var expr = new MapperConfigurationExpression();
        configure(expr);

        var allDefinitions = expr.Profiles.SelectMany(p => p.Definitions)
            .Concat(expr.InlineDefinitions);
        TypeMaps = MapPlanBuilder.Build(allDefinitions);
    }

    /// <summary>
    /// Validates the configuration. Throws if any map has a public settable destination member
    /// that isn't satisfied by a <c>MapFrom</c>, <c>UseValue</c>, convention match, or <c>Ignore</c>.
    /// </summary>
    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();
        foreach (var map in TypeMaps.Values)
        {
            // Full-type converter plans skip member validation.
            if (map.TypeConverterFunc is not null || map.TypeConverterType is not null)
                continue;

            foreach (var plan in map.MemberPlans.Values)
            {
                if (plan.Kind == MemberPlanKind.Ignored) continue;
                if (plan.Kind == MemberPlanKind.UseValue) continue;
                if (plan.Kind == MemberPlanKind.MapFromFunc) continue;
                if (plan.Kind == MemberPlanKind.MapFromResolver) continue;
                if (plan.Kind == MemberPlanKind.MapFromSourceMember) continue;
                if (plan.Kind == MemberPlanKind.ByConvention && plan.SourceMember is not null) continue;

                errors.Add(
                    $"Unmapped destination member: {map.DestinationType.Name}.{plan.DestinationMember.Name} " +
                    $"(from {map.SourceType.Name})");
            }
        }
        if (errors.Count > 0)
            throw new AutoMapperMappingException(
                "Morph configuration is invalid:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors));
    }

    public IMapper CreateMapper() => new Mapper(this);
}
