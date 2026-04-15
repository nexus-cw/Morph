using System;
using System.Collections.Generic;
using Morph;
using Xunit;

namespace Morph.Tests;

// Regression coverage for the AutoMapper CVE-2026-32933 pattern: self-referential graphs
// must not blow the stack. Morph caps recursion at MapperConfiguration.MaxDepth (default 32)
// and throws AutoMapperMappingException instead of StackOverflowException (which .NET cannot catch).
public class MaxDepthTests
{
    class SrcNode { public SrcNode? Self { get; set; } public int Value { get; set; } }
    class DstNode { public DstNode? Self { get; set; } public int Value { get; set; } }

    [Fact]
    public void Deeply_nested_self_referential_graph_throws_instead_of_stack_overflow()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<SrcNode, DstNode>());
        var mapper = config.CreateMapper();

        // Build a graph deeper than MaxDepth.
        var root = new SrcNode { Value = 0 };
        var current = root;
        for (int i = 1; i < 100; i++)
        {
            current.Self = new SrcNode { Value = i };
            current = current.Self;
        }

        var ex = Assert.Throws<AutoMapperMappingException>(() => mapper.Map<SrcNode, DstNode>(root));
        Assert.Contains("Max recursion depth", ex.Message);
    }

    [Fact]
    public void Graphs_within_max_depth_map_normally()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<SrcNode, DstNode>());
        var mapper = config.CreateMapper();

        // 3 levels — well under the default 32.
        var root = new SrcNode { Value = 0, Self = new SrcNode { Value = 1, Self = new SrcNode { Value = 2 } } };

        var result = mapper.Map<SrcNode, DstNode>(root);

        Assert.Equal(0, result.Value);
        Assert.Equal(1, result.Self.Value);
        Assert.Equal(2, result.Self.Self.Value);
        Assert.Null(result.Self.Self.Self);
    }

    [Fact]
    public void MaxDepth_is_configurable()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.MaxDepth = 4;
            cfg.CreateMap<SrcNode, DstNode>();
        });
        var mapper = config.CreateMapper();

        var root = new SrcNode { Value = 0 };
        var current = root;
        for (int i = 1; i < 10; i++)
        {
            current.Self = new SrcNode { Value = i };
            current = current.Self;
        }

        Assert.Throws<AutoMapperMappingException>(() => mapper.Map<SrcNode, DstNode>(root));
    }

    // C2 + P1: MaxDepth is configured inside the CreateMapper(cfg => ...) lambda and cannot
    // be mutated by a consumer post-construction. The setter on MapperConfiguration is
    // internal — unreachable from external consumer code — and the live mapper snapshots
    // the value at ctor time anyway. Both guarantees together close CVE-2026-32933: an
    // attacker-controlled reference to the MapperConfiguration can't raise the depth cap.
    [Fact]
    public void MaxDepth_setter_is_not_publicly_accessible()
    {
        var prop = typeof(MapperConfiguration).GetProperty(nameof(MapperConfiguration.MaxDepth))!;
        Assert.NotNull(prop.GetMethod);
        Assert.True(prop.GetMethod!.IsPublic, "getter should stay public for diagnostics / assertions");
        var setter = prop.SetMethod;
        Assert.NotNull(setter);
        Assert.False(setter!.IsPublic,
            "setter must not be publicly callable — consumers must configure MaxDepth inside the CreateMapper lambda");
    }

    // Belt-and-braces: even given access to the internal setter (in-assembly or via
    // reflection by a malicious caller), the *live mapper* must still ignore late changes
    // because it snapshotted MaxDepth at ctor time. This is the original C2 invariant.
    [Fact]
    public void MaxDepth_mutation_after_CreateMapper_is_ignored()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.MaxDepth = 4;
            cfg.CreateMap<SrcNode, DstNode>();
        });
        var mapper = config.CreateMapper();

        // Force a post-create mutation via the internal setter (reachable in-assembly).
        // An external consumer cannot hit this path; we use it here to prove the snapshot
        // holds even if the config object is later mutated.
        typeof(MapperConfiguration)
            .GetProperty(nameof(MapperConfiguration.MaxDepth))!
            .SetValue(config, int.MaxValue);

        var root = new SrcNode { Value = 0 };
        var current = root;
        for (int i = 1; i < 20; i++)
        {
            current.Self = new SrcNode { Value = i };
            current = current.Self;
        }

        var ex = Assert.Throws<AutoMapperMappingException>(() => mapper.Map<SrcNode, DstNode>(root));
        Assert.Contains("Max recursion depth (4)", ex.Message);
    }

    // Same-type self-reference case: current Morph behavior is reference-preserve (no clone),
    // so no recursion happens. That's a deliberate consequence of the "IsInstanceOfType → return
    // value" shortcut in CoerceOrNestedMap. Documenting the behavior so it doesn't silently change.
    [Fact]
    public void Same_type_self_reference_preserves_reference_does_not_recurse()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<SrcNode, SrcNode>());
        var mapper = config.CreateMapper();

        var root = new SrcNode { Value = 0 };
        root.Self = root; // cycle

        var result = mapper.Map<SrcNode, SrcNode>(root);

        Assert.Same(root.Self, result.Self);
    }

    class SrcTree { public int Value { get; set; } public List<SrcTree> Children { get; set; } = new(); }
    class DstTree { public int Value { get; set; } public List<DstTree> Children { get; set; } = new(); }

    // C1 regression: recursion through collection elements must tick the depth counter.
    // Pre-fix, CollectionMapper passed `depth` straight through instead of `depth + 1`, so
    // GuardDepth never fired on list-element nesting and deep/cyclic graphs blew the stack.
    //
    // DO NOT "fix" this into a true cycle (e.g. `root.Children.Add(root)`). On the pre-fix
    // code that variant throws StackOverflowException, which is uncatchable in .NET: the CLR
    // bypasses exception handling and terminates the test host, so Assert.Throws never runs
    // and the whole test run dies instead of failing cleanly. The deep-but-finite chain
    // exercises the same CollectionMapper path (pre-fix: depth never increments → guard
    // never fires → test fails on "no exception"; post-fix: guard trips at MaxDepth), which
    // is what the regression assertion actually needs.
    [Fact]
    public void Deep_collection_element_chain_trips_depth_guard()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<SrcTree, DstTree>());
        var mapper = config.CreateMapper();

        // 100 levels of single-child nesting, reached via List<SrcTree>.Children.
        var root = new SrcTree { Value = 0 };
        var current = root;
        for (int i = 1; i < 100; i++)
        {
            var next = new SrcTree { Value = i };
            current.Children.Add(next);
            current = next;
        }

        var ex = Assert.Throws<AutoMapperMappingException>(() => mapper.Map<SrcTree, DstTree>(root));
        // Pin the assertion: the guard must fire at the element-type boundary (SrcTree → DstTree)
        // with the configured MaxDepth (32). Without these the test would also pass if a
        // future refactor caused some other depth exception to fire earlier (e.g. at the outer
        // collection pair), masking a regression of the actual C1 fix.
        Assert.Contains("Max recursion depth (32)", ex.Message);
        Assert.Equal(typeof(SrcTree), ex.SourceType);
        Assert.Equal(typeof(DstTree), ex.DestinationType);
    }
}
