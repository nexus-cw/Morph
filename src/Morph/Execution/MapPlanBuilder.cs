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
    public static Dictionary<(Type, Type), TypeMap> Build(IEnumerable<object> expressions)
    {
        var result = new Dictionary<(Type, Type), TypeMap>();

        // First pass: collect all expressions (including reverses) into a flat list.
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
