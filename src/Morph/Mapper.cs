using System;
using System.Collections;
using Morph.Execution;

namespace Morph;

/// <summary>
/// Runtime mapper. Created via <see cref="MapperConfiguration.CreateMapper"/>. Stateless across
/// calls apart from configuration — safe to hold a single instance for the life of the consumer.
/// </summary>
internal sealed class Mapper : IMapper
{
    private readonly MapperConfiguration _config;
    public Mapper(MapperConfiguration config) { _config = config; }

    public IConfigurationProvider ConfigurationProvider => _config;

    public TDestination Map<TDestination>(object source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return (TDestination)MapDynamic(source, source.GetType(), typeof(TDestination), depth: 0)!;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
    {
        var result = MapDynamic(source, typeof(TSource), typeof(TDestination), depth: 0);
        return (TDestination)result!;
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        MapInto(source, typeof(TSource), destination!, typeof(TDestination), depth: 0);
        return destination;
    }

    public object? Map(object? source, Type sourceType, Type destinationType)
        => MapDynamic(source, sourceType, destinationType, depth: 0);

    /// <summary>
    /// Core entry: resolve the TypeMap, construct the destination, run the plan. Exposed
    /// internally because <see cref="CollectionMapper"/> calls back in for element mapping.
    /// </summary>
    internal object? MapDynamic(object? source, Type sourceType, Type destinationType, int depth)
    {
        if (source is null) return DefaultFor(destinationType);
        GuardDepth(depth, sourceType, destinationType);

        // Collections first — they route through CollectionMapper which recurses into MapDynamic per element.
        if (source is IEnumerable enumerable && sourceType != typeof(string) &&
            CollectionMapper.TryGetElementTypes(destinationType, out var destElemType))
        {
            CollectionMapper.TryGetElementTypes(sourceType, out var srcElemType);
            return CollectionMapper.Map(enumerable, srcElemType, destinationType, destElemType, this, depth);
        }

        // Exact type map lookup.
        if (_config.TypeMaps.TryGetValue((sourceType, destinationType), out var typeMap))
            return ExecuteTypeMap(typeMap, source, destination: null, depth);

        // Assignable-from source types — handles runtime types that are subclasses of the declared source.
        foreach (var kv in _config.TypeMaps)
        {
            if (kv.Key.Item2 != destinationType) continue;
            if (kv.Key.Item1.IsAssignableFrom(sourceType))
                return ExecuteTypeMap(kv.Value, source, destination: null, depth);
        }

        // Trivial assignment.
        if (destinationType.IsAssignableFrom(sourceType)) return source;

        // Last resort: coerce.
        return ValueCoercion.Convert(source, destinationType);
    }

    private void MapInto(object? source, Type sourceType, object destination, Type destinationType, int depth)
    {
        if (source is null) return;
        GuardDepth(depth, sourceType, destinationType);
        if (!_config.TypeMaps.TryGetValue((sourceType, destinationType), out var typeMap))
        {
            // Assignable source fallback.
            foreach (var kv in _config.TypeMaps)
            {
                if (kv.Key.Item2 != destinationType) continue;
                if (kv.Key.Item1.IsAssignableFrom(sourceType))
                {
                    typeMap = kv.Value;
                    break;
                }
            }
            if (typeMap is null)
                throw new AutoMapperMappingException(
                    $"No map configured from {sourceType.FullName} to {destinationType.FullName}")
                {
                    SourceType = sourceType,
                    DestinationType = destinationType
                };
        }
        ExecuteTypeMap(typeMap, source, destination, depth);
    }

    private void GuardDepth(int depth, Type sourceType, Type destinationType)
    {
        if (depth >= _config.MaxDepth)
            throw new AutoMapperMappingException(
                $"Max recursion depth ({_config.MaxDepth}) exceeded mapping {sourceType.Name} → {destinationType.Name}. " +
                $"Raise MapperConfiguration.MaxDepth if the graph is legitimately deep; otherwise check for cycles.")
            {
                SourceType = sourceType,
                DestinationType = destinationType
            };
    }

    private object ExecuteTypeMap(TypeMap typeMap, object source, object? destination, int depth)
    {
        var context = new ResolutionContext(this);

        // Full-type converter short-circuit.
        if (typeMap.TypeConverterType is not null)
        {
            var converter = Activator.CreateInstance(typeMap.TypeConverterType)!;
            var convertMethod = typeMap.TypeConverterType.GetMethod("Convert")!;
            destination ??= DefaultFor(typeMap.DestinationType);
            return convertMethod.Invoke(converter, new[] { source, destination, context })!;
        }
        if (typeMap.TypeConverterFunc is not null)
        {
            if (typeMap.TypeConverterTakesDestination)
            {
                destination ??= DefaultFor(typeMap.DestinationType);
                return typeMap.TypeConverterFunc.DynamicInvoke(source, destination)!;
            }
            return typeMap.TypeConverterFunc.DynamicInvoke(source)!;
        }

        // Construct destination if not supplied.
        if (destination is null)
        {
            if (typeMap.CustomConstructor is not null)
            {
                destination = typeMap.ConstructorTakesContext
                    ? typeMap.CustomConstructor.DynamicInvoke(source, context)!
                    : typeMap.CustomConstructor.DynamicInvoke(source)!;
            }
            else
            {
                destination = Activator.CreateInstance(typeMap.DestinationType)
                    ?? throw new AutoMapperMappingException(
                        $"Could not create an instance of {typeMap.DestinationType.FullName}")
                    {
                        DestinationType = typeMap.DestinationType
                    };
            }
        }

        // Apply each member plan.
        foreach (var plan in typeMap.MemberPlans.Values)
        {
            try
            {
                ApplyPlan(plan, source, destination, context, depth);
            }
            catch (AutoMapperMappingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AutoMapperMappingException(
                    $"Mapping {typeMap.SourceType.Name} → {typeMap.DestinationType.Name}, " +
                    $"member {plan.DestinationMember.Name}: {ex.Message}", ex)
                {
                    SourceType = typeMap.SourceType,
                    DestinationType = typeMap.DestinationType,
                    MemberName = plan.DestinationMember.Name
                };
            }
        }

        return destination;
    }

    private void ApplyPlan(MemberPlan plan, object source, object destination, ResolutionContext context, int depth)
    {
        if (plan.Condition is not null)
        {
            var ok = (bool)plan.Condition.DynamicInvoke(source)!;
            if (!ok) return;
        }

        switch (plan.Kind)
        {
            case MemberPlanKind.Ignored:
                return;

            case MemberPlanKind.UseValue:
                plan.SetDestinationValue(destination, plan.ConstantValue);
                return;

            case MemberPlanKind.MapFromFunc:
            {
                var value = plan.ResolverFunc!.DynamicInvoke(source, destination);
                plan.SetDestinationValue(destination, CoerceIfNeeded(value, plan.DestinationType));
                return;
            }

            case MemberPlanKind.MapFromResolver:
            {
                var resolver = Activator.CreateInstance(plan.ResolverType!)!;
                var resolveMethod = plan.ResolverType!.GetMethod("Resolve")!;
                var current = MemberPlan.ReadMember(plan.DestinationMember, destination);
                var value = resolveMethod.Invoke(resolver, new[] { source, destination, current, context });
                plan.SetDestinationValue(destination, CoerceIfNeeded(value, plan.DestinationType));
                return;
            }

            case MemberPlanKind.MapFromSourceMember:
            case MemberPlanKind.ByConvention:
            {
                if (plan.SourceMember is null) return;
                var srcValue = MemberPlan.ReadMember(plan.SourceMember, source);
                var srcType = MemberPlan.MemberType(plan.SourceMember);
                plan.SetDestinationValue(destination, CoerceOrNestedMap(srcValue, srcType, plan.DestinationType, depth));
                return;
            }
        }
    }

    private object? CoerceOrNestedMap(object? value, Type sourceType, Type destinationType, int depth)
    {
        if (value is null) return DefaultFor(destinationType);
        if (destinationType.IsInstanceOfType(value)) return value;

        // Collection dispatch.
        if (value is IEnumerable enumerable && sourceType != typeof(string) &&
            CollectionMapper.TryGetElementTypes(destinationType, out var destElemType))
        {
            CollectionMapper.TryGetElementTypes(sourceType, out var srcElemType);
            return CollectionMapper.Map(enumerable, srcElemType, destinationType, destElemType, this, depth);
        }

        // Nested map dispatch — bump depth before recursing into another type map.
        if (_config.TypeMaps.ContainsKey((sourceType, destinationType)))
            return MapDynamic(value, sourceType, destinationType, depth + 1);

        return ValueCoercion.Convert(value, destinationType);
    }

    private object? CoerceIfNeeded(object? value, Type destinationType)
    {
        if (value is null) return DefaultFor(destinationType);
        if (destinationType.IsInstanceOfType(value)) return value;
        return ValueCoercion.Convert(value, destinationType);
    }

    private static object? DefaultFor(Type t)
        => t.IsValueType && Nullable.GetUnderlyingType(t) is null ? Activator.CreateInstance(t) : null;
}
