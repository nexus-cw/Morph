using System;
using Xunit;

namespace Morph.Tests;

// I4 coverage: when a custom IValueResolver uses `context.Mapper.Map<>()` to trigger a
// nested map, that nested call must inherit the current depth. Pre-fix, ExecuteTypeMap
// built a ResolutionContext holding a plain IMapper reference whose Map<> overloads
// re-enter MapDynamic at depth 0 — so a resolver that re-enters on every call can drive
// the stack past any MaxDepth setting. The fix hangs the current depth off the context
// and hands resolvers a depth-aware mapper wrapper so recursion keeps counting.
public class ResolverDepthTests
{
    public class Inner { public int Value { get; set; } }
    public class Outer { public Inner Pet { get; set; } = new(); }

    public class InnerDto { public int Value { get; set; } }
    public class OuterDto { public InnerDto Pet { get; set; } = new(); }

    // Resolver that calls context.Mapper.Map internally. Represents the real footgun:
    // a user writes a resolver that "just" delegates mapping for a nested member.
    public class InnerDtoResolver : IValueResolver<Outer, OuterDto, InnerDto>
    {
        public InnerDto Resolve(Outer source, OuterDto destination, InnerDto destMember, ResolutionContext context)
            => context.Mapper.Map<InnerDto>(source.Pet);
    }

    [Fact]
    public void Resolver_reentry_inherits_outer_depth_against_MaxDepth()
    {
        // MaxDepth 1: Outer→OuterDto itself is level 0, resolver's Map is level 1 which
        // is the last legal call (depth == MaxDepth throws). If a resolver that re-enters
        // the mapper were counted at depth 0, a cycle would sail past MaxDepth and blow
        // the stack. Here we only want to prove the depth *is threaded* — the easiest
        // assertion is that the throwing case throws at all.
        var config = new MapperConfiguration(cfg =>
        {
            cfg.MaxDepth = 1;
            cfg.CreateMap<Inner, InnerDto>();
            cfg.CreateMap<Outer, OuterDto>()
               .ForMember(d => d.Pet, opt => opt.MapFrom<InnerDtoResolver>());
        });
        var mapper = config.CreateMapper();

        var outer = new Outer { Pet = new Inner { Value = 42 } };

        // With MaxDepth = 1 and depth threaded, the resolver's re-entry lands at depth 1,
        // which equals MaxDepth — GuardDepth throws. Pre-fix it lands at depth 0 and the
        // map succeeds, so we assert the throw.
        Assert.Throws<AutoMapperMappingException>(() => mapper.Map<Outer, OuterDto>(outer));
    }

    [Fact]
    public void Resolver_reentry_still_works_at_normal_depths()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Inner, InnerDto>();
            cfg.CreateMap<Outer, OuterDto>()
               .ForMember(d => d.Pet, opt => opt.MapFrom<InnerDtoResolver>());
        });
        // Default MaxDepth = 32, well clear of the 2 levels we actually use.
        var mapper = config.CreateMapper();

        var outer = new Outer { Pet = new Inner { Value = 42 } };

        var result = mapper.Map<Outer, OuterDto>(outer);

        Assert.Equal(42, result.Pet.Value);
    }

    // The same ResolutionContext is reached by ConstructUsing((src, ctx) => ...) constructors,
    // so the depth-aware mapper wrapper should thread depth there too. Without this test, a
    // future refactor could swap to a plain IMapper on the constructor path and silently
    // bypass MaxDepth for anyone who maps from inside a custom constructor.
    public class OuterWithCtor
    {
        public Inner Pet { get; }
        public OuterWithCtor(Inner pet) { Pet = pet; }
    }
    public class OuterDtoWithCtor
    {
        public InnerDto Pet { get; }
        public OuterDtoWithCtor(InnerDto pet) { Pet = pet; }
    }

    [Fact]
    public void ConstructUsing_reentry_inherits_outer_depth_against_MaxDepth()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Inner, InnerDto>();
            cfg.CreateMap<OuterWithCtor, OuterDtoWithCtor>()
               .ConstructUsing((src, ctx) => new OuterDtoWithCtor(ctx.Mapper.Map<InnerDto>(src.Pet)));
        });
        config.MaxDepth = 1;
        var mapper = config.CreateMapper();

        var outer = new OuterWithCtor(new Inner { Value = 42 });

        // ConstructUsing runs through DynamicInvoke which wraps thrown exceptions in
        // TargetInvocationException — a separate ergonomics issue, but the depth guard
        // clearly fired inside the ctor: the inner exception is AutoMapperMappingException
        // naming the depth limit we actually set. That proves the re-entry inherited depth.
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => mapper.Map<OuterWithCtor, OuterDtoWithCtor>(outer));
        var inner = Assert.IsType<AutoMapperMappingException>(ex.InnerException);
        Assert.Contains("Max recursion depth (1)", inner.Message);
    }
}
