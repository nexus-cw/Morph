using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Morph.Execution;

/// <summary>
/// Ordered kinds match the resolution priority. The first matching kind wins when a
/// mapping expression is finalized into a <see cref="TypeMap"/>.
/// </summary>
internal enum MemberPlanKind
{
    /// <summary>Member is deliberately skipped. Do not write to it, and do not complain at validation.</summary>
    Ignored,
    /// <summary>Always write the given constant.</summary>
    UseValue,
    /// <summary>Call the user-supplied func with (source, dest).</summary>
    MapFromFunc,
    /// <summary>Instantiate a user resolver type and call Resolve.</summary>
    MapFromResolver,
    /// <summary>Read a source member (property or field).</summary>
    MapFromSourceMember,
    /// <summary>Fallback — default convention matched a source member by name.</summary>
    ByConvention,
}

/// <summary>
/// Plan for mapping a single destination member. One per public settable destination property
/// or field, plus one for each explicit Ignore/UseValue/MapFrom call.
/// </summary>
internal sealed class MemberPlan
{
    public MemberPlan(MemberInfo destinationMember)
    {
        DestinationMember = destinationMember;
    }

    public MemberInfo DestinationMember { get; }
    public MemberPlanKind Kind { get; set; } = MemberPlanKind.ByConvention;

    // One of these is populated per Kind:
    public object? ConstantValue { get; set; }                   // UseValue
    public Delegate? ResolverFunc { get; set; }                  // MapFromFunc — Func<TSrc,TDest,TMember>
    public Type? ResolverType { get; set; }                      // MapFromResolver — IValueResolver<,,> impl
    public MemberInfo? SourceMember { get; set; }                // MapFromSourceMember, ByConvention
    public Delegate? Condition { get; set; }                     // optional — Func<TSrc,bool>

    /// <summary>
    /// Type of the destination member — property or field type. Used by the mapper to coerce values.
    /// </summary>
    public Type DestinationType => DestinationMember switch
    {
        PropertyInfo p => p.PropertyType,
        FieldInfo f => f.FieldType,
        _ => throw new InvalidOperationException($"Unsupported member kind: {DestinationMember.GetType()}")
    };

    public void SetDestinationValue(object destination, object? value)
    {
        switch (DestinationMember)
        {
            case PropertyInfo p: p.SetValue(destination, value); break;
            case FieldInfo f: f.SetValue(destination, value); break;
            default: throw new InvalidOperationException($"Unsupported member kind: {DestinationMember.GetType()}");
        }
    }

    public static object? ReadMember(MemberInfo member, object instance)
        => member switch
        {
            PropertyInfo p => p.GetValue(instance),
            FieldInfo f => f.GetValue(instance),
            _ => throw new InvalidOperationException($"Unsupported member kind: {member.GetType()}")
        };

    public static Type MemberType(MemberInfo member) => member switch
    {
        PropertyInfo p => p.PropertyType,
        FieldInfo f => f.FieldType,
        _ => throw new InvalidOperationException($"Unsupported member kind: {member.GetType()}")
    };

    /// <summary>Resolve <c>d =&gt; d.Foo.Bar</c>-style expression to its terminal MemberInfo.</summary>
    public static MemberInfo ResolveMember(LambdaExpression expr)
    {
        var body = expr.Body;
        if (body is UnaryExpression u && u.NodeType == ExpressionType.Convert)
            body = u.Operand;
        if (body is MemberExpression me) return me.Member;
        throw new ArgumentException($"Expected a member-access expression, got: {expr}");
    }
}
