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
        var config = new MapperConfiguration(cfg => cfg.CreateMap<SrcNode, DstNode>());
        config.MaxDepth = 4;
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
    // GuardDepth never fired on list-element nesting and deep/cyclic graphs could blow the
    // stack (uncatchable StackOverflowException). We exercise this with a deep-but-finite
    // chain so the pre-fix failure is a missed-throw, not a test-host crash.
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
        Assert.Contains("Max recursion depth", ex.Message);
    }
}
