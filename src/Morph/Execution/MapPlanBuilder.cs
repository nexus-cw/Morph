using System;
using System.Collections.Generic;
using System.Reflection;
using Morph.Conventions;
using Morph.Expressions;

namespace Morph.Execution;

/// <summary>
/// Resolves accumulated <see cref="MappingExpression{TSrc,TDest}"/> records into executable
/// <see cref="TypeMap"/>s. Runs once at <see cref="MapperConfiguration"/> construction; the
/// resulting dictionary is then read-only.
/// </summary>
internal static class MapPlanBuilder
{
    public static Dictionary<(Type, Type), TypeMap> Build(
        IEnumerable<object> expressions,
        bool mirrorIgnoreOnReverse = true)
    {
        var result = new Dictionary<(Type, Type), TypeMap>();

        // First pass: collect all expressions (including reverses) into a flat list.
        // While walking, mirror Ignored forward plans onto the reverse expression's
        // ExplicitMembers if enabled — see MirrorIgnoreOnReverse docs on
        // MapperConfiguration. Done here (not inside ReverseMap()) because the flag
        // lives on the top-level config, not on the individual mapping expression.
        var flat = new List<(Type, Type, object)>();
        foreach (var expr in expressions)
        {
            var exprType = expr.GetType();
            var src = (Type)exprType.GetProperty("SourceType")!.GetValue(expr)!;
            var dst = (Type)exprType.GetProperty("DestinationType")!.GetValue(expr)!;
            flat.Add((src, dst, expr));

            var reverse = exprType.GetProperty("ReverseMapExpression")!.GetValue(expr);
            if (reverse is not null)
            {
                var rType = reverse.GetType();
                var rSrc = (Type)rType.GetProperty("SourceType")!.GetValue(reverse)!;
                var rDst = (Type)rType.GetProperty("DestinationType")!.GetValue(reverse)!;

                if (mirrorIgnoreOnReverse)
                    MirrorIgnoredPlans(expr, exprType, reverse, rType, reverseDestType: rDst);

                flat.Add((rSrc, rDst, reverse));
            }
        }

        // Second pass: build a TypeMap per (src,dst).
        foreach (var (src, dst, expr) in flat)
        {
            var typeMap = new TypeMap(src, dst);
            BuildOne(typeMap, expr);
            result[(src, dst)] = typeMap;
        }

        return result;
    }

    // For each forward ExplicitMembers entry of Kind == Ignored, add a matching Ignored
    // plan to the reverse expression's ExplicitMembers, keyed by member name on the
    // reverse destination type. Silently skips members that don't exist on the reverse
    // destination (e.g. if the forward destination has an extra field the source lacks).
    // Preserves anything the caller already set on the reverse — explicit wins over
    // mirrored default.
    private static void MirrorIgnoredPlans(
        object forwardExpr, Type forwardExprType,
        object reverseExpr, Type reverseExprType,
        Type reverseDestType)
    {
        var forwardMembers =
            (Dictionary<string, MemberPlan>)forwardExprType.GetProperty("ExplicitMembers")!.GetValue(forwardExpr)!;
        var reverseMembers =
            (Dictionary<string, MemberPlan>)reverseExprType.GetProperty("ExplicitMembers")!.GetValue(reverseExpr)!;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        foreach (var kv in forwardMembers)
        {
            if (kv.Value.Kind != MemberPlanKind.Ignored) continue;
            var name = kv.Value.DestinationMember.Name;
            if (reverseMembers.ContainsKey(name)) continue; // caller already configured this member

            MemberInfo? reverseMember =
                (MemberInfo?)reverseDestType.GetProperty(name, flags) ?? reverseDestType.GetField(name, flags);
            if (reverseMember is null) continue; // no matching member on reverse destination

            reverseMembers[name] = new MemberPlan(reverseMember) { Kind = MemberPlanKind.Ignored };
        }
    }

    private static void BuildOne(TypeMap typeMap, object expression)
    {
        var exprType = expression.GetType();

        // Pull the raw settings off the generic MappingExpression<,>.
        typeMap.CustomConstructor = (Delegate?)exprType.GetProperty("CustomConstructor")!.GetValue(expression);
        typeMap.ConstructorTakesContext = (bool)exprType.GetProperty("ConstructorTakesContext")!.GetValue(expression)!;
        typeMap.TypeConverterFunc = (Delegate?)exprType.GetProperty("TypeConverterFunc")!.GetValue(expression);
        typeMap.TypeConverterTakesDestination = (bool)exprType.GetProperty("TypeConverterTakesDestination")!.GetValue(expression)!;
        typeMap.TypeConverterType = (Type?)exprType.GetProperty("TypeConverterType")!.GetValue(expression);

        var explicitMembers = (Dictionary<string, MemberPlan>)exprType.GetProperty("ExplicitMembers")!.GetValue(expression)!;

        // If a full-type converter is set, member plans aren't needed — the converter produces the destination outright.
        if (typeMap.TypeConverterFunc is not null || typeMap.TypeConverterType is not null)
            return;

        // Enumerate public settable destination members. Start from convention; overlay explicit configs.
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        foreach (var prop in typeMap.DestinationType.GetProperties(flags))
        {
            if (!prop.CanWrite) continue;
            if (explicitMembers.TryGetValue(prop.Name, out var explicitPlan))
            {
                typeMap.MemberPlans[prop.Name] = explicitPlan;
                continue;
            }
            var match = NameMatcher.Match(typeMap.SourceType, prop.Name);
            if (match is not null)
            {
                typeMap.MemberPlans[prop.Name] = new MemberPlan(prop)
                {
                    Kind = MemberPlanKind.ByConvention,
                    SourceMember = match
                };
            }
            else
            {
                // Unmatched: leave out of plan. Validation catches it at Assert time.
                typeMap.MemberPlans[prop.Name] = new MemberPlan(prop)
                {
                    Kind = MemberPlanKind.ByConvention,
                    SourceMember = null
                };
            }
        }

        foreach (var field in typeMap.DestinationType.GetFields(flags))
        {
            if (field.IsInitOnly) continue;
            if (explicitMembers.TryGetValue(field.Name, out var explicitPlan))
            {
                typeMap.MemberPlans[field.Name] = explicitPlan;
                continue;
            }
            var match = NameMatcher.Match(typeMap.SourceType, field.Name);
            if (match is not null)
            {
                typeMap.MemberPlans[field.Name] = new MemberPlan(field)
                {
                    Kind = MemberPlanKind.ByConvention,
                    SourceMember = match
                };
            }
        }

        // Any explicit configs for members that don't exist on the destination — surface loudly.
        foreach (var kv in explicitMembers)
        {
            if (!typeMap.MemberPlans.ContainsKey(kv.Key))
                typeMap.MemberPlans[kv.Key] = kv.Value;
        }
    }
}
