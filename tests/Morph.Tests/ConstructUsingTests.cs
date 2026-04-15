using Xunit;

namespace Morph.Tests;

public class ConstructUsingTests
{
    public class Src { public string Name { get; set; } = ""; public int Age { get; set; } }
    public class Dst
    {
        public string Name { get; }
        public int Age { get; set; }
        public Dst(string name) { Name = name; }
    }

    [Fact]
    public void ConstructUsing_func_supplies_destination_instance()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ConstructUsing(s => new Dst(s.Name)));
        var r = cfg.CreateMapper().Map<Src, Dst>(new Src { Name = "Ada", Age = 28 });
        Assert.Equal("Ada", r.Name);
        Assert.Equal(28, r.Age);
    }

    [Fact]
    public void ConstructUsing_with_context_receives_resolution_context()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ConstructUsing((s, ctx) =>
            {
                Assert.NotNull(ctx);
                Assert.NotNull(ctx.Mapper);
                return new Dst(s.Name);
            }));
        var r = cfg.CreateMapper().Map<Src, Dst>(new Src { Name = "Grace", Age = 85 });
        Assert.Equal("Grace", r.Name);
    }
}
