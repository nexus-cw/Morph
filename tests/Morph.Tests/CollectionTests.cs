using System.Collections.Generic;
using Xunit;

namespace Morph.Tests;

public class CollectionTests
{
    public class Src { public int Id { get; set; } }
    public class Dst { public int Id { get; set; } }

    public class SrcWrapper { public List<Src> Items { get; set; } = new(); }
    public class DstListWrapper { public List<Dst> Items { get; set; } = new(); }
    public class DstArrayWrapper { public Dst[] Items { get; set; } = System.Array.Empty<Dst>(); }
    public class DstICollWrapper { public ICollection<Dst> Items { get; set; } = new List<Dst>(); }
    public class DstIEnumWrapper { public IEnumerable<Dst> Items { get; set; } = new List<Dst>(); }

    private static IMapper Build() =>
        new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Src, Dst>();
            cfg.CreateMap<SrcWrapper, DstListWrapper>();
            cfg.CreateMap<SrcWrapper, DstArrayWrapper>();
            cfg.CreateMap<SrcWrapper, DstICollWrapper>();
            cfg.CreateMap<SrcWrapper, DstIEnumWrapper>();
        }).CreateMapper();

    private static SrcWrapper Sample() => new()
    {
        Items = new List<Src> { new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 } }
    };

    [Fact] public void Maps_to_List()
    {
        var r = Build().Map<SrcWrapper, DstListWrapper>(Sample());
        Assert.Equal(new[] { 1, 2, 3 }, r.Items.ConvertAll(x => x.Id));
    }

    [Fact] public void Maps_to_Array()
    {
        var r = Build().Map<SrcWrapper, DstArrayWrapper>(Sample());
        Assert.Equal(3, r.Items.Length);
        Assert.Equal(2, r.Items[1].Id);
    }

    [Fact] public void Maps_to_ICollection()
    {
        var r = Build().Map<SrcWrapper, DstICollWrapper>(Sample());
        Assert.Equal(3, r.Items.Count);
    }

    [Fact] public void Maps_to_IEnumerable()
    {
        var r = Build().Map<SrcWrapper, DstIEnumWrapper>(Sample());
        var list = new List<Dst>(r.Items);
        Assert.Equal(3, list.Count);
    }

    [Fact] public void Empty_collection_round_trips_as_empty()
    {
        var r = Build().Map<SrcWrapper, DstListWrapper>(new SrcWrapper());
        Assert.Empty(r.Items);
    }
}
