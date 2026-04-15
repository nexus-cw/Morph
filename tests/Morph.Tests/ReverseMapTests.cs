using Xunit;

namespace Morph.Tests;

public class ReverseMapTests
{
    public class Src { public int Id { get; set; } public string Name { get; set; } = ""; }
    public class Dst { public int Id { get; set; } public string Name { get; set; } = ""; }

    [Fact]
    public void ReverseMap_enables_inverse_mapping()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>().ReverseMap());
        var mapper = cfg.CreateMapper();

        var d = mapper.Map<Src, Dst>(new Src { Id = 1, Name = "a" });
        var s = mapper.Map<Dst, Src>(d);

        Assert.Equal(1, s.Id);
        Assert.Equal("a", s.Name);
    }
}
