using System;
using System.Linq.Expressions;
using Morph.Execution;

namespace Morph.Expressions;

/// <summary>
/// Records member-level options onto the underlying <see cref="MemberPlan"/>. Each method
/// overwrites the plan's kind, so the last-called method wins (matching AutoMapper's behavior).
/// </summary>
internal sealed class MemberConfigurationExpression<TSource, TDestination, TMember>
    : IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    private readonly MemberPlan _plan;
    public MemberConfigurationExpression(MemberPlan plan) { _plan = plan; }

    public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        _plan.Kind = MemberPlanKind.MapFromSourceMember;
        _plan.SourceMember = MemberPlan.ResolveMember(sourceMember);
    }

    public void MapFrom(Func<TSource, TDestination, TMember> resolver)
    {
        _plan.Kind = MemberPlanKind.MapFromFunc;
        _plan.ResolverFunc = resolver;
    }

    public void MapFrom<TResolver>() where TResolver : IValueResolver<TSource, TDestination, TMember>, new()
    {
        _plan.Kind = MemberPlanKind.MapFromResolver;
        _plan.ResolverType = typeof(TResolver);
    }

    public void Ignore() => _plan.Kind = MemberPlanKind.Ignored;

    public void UseValue(TMember value)
    {
        _plan.Kind = MemberPlanKind.UseValue;
        _plan.ConstantValue = value;
    }

    public void Condition(Func<TSource, bool> condition) => _plan.Condition = condition;
}
