using System;
using System.Collections.Generic;

namespace Morph.Execution;

/// <summary>
/// Resolved plan for a single (source, destination) pair. Built once by
/// <see cref="MapPlanBuilder"/> when the <see cref="MapperConfiguration"/> is finalized.
/// </summary>
internal sealed class TypeMap
{
    public TypeMap(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    public Type SourceType { get; }
    public Type DestinationType { get; }

    /// <summary>Per-destination-member plans. Keyed by MemberInfo.Name for fast lookup during plan build.</summary>
    public Dictionary<string, MemberPlan> MemberPlans { get; } = new();

    /// <summary>Optional custom constructor. Overrides <c>Activator.CreateInstance</c>.</summary>
    public Delegate? CustomConstructor { get; set; }
    public bool ConstructorTakesContext { get; set; }

    /// <summary>Optional full-type converter. When set, member plans are ignored entirely.</summary>
    public Delegate? TypeConverterFunc { get; set; }
    public bool TypeConverterTakesDestination { get; set; }
    public Type? TypeConverterType { get; set; }
}
