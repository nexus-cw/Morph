using Xunit;

namespace Morph.Tests;

public class ValidationTests
{
    public class Src { public int A { get; set; } }
    public class Dst { public int A { get; set; } public string Missing { get; set; } = ""; }

    [Fact]
    public void AssertConfigurationIsValid_throws_when_destination_member_unmapped()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>());
        var ex = Assert.Throws<AutoMapperMappingException>(() => cfg.AssertConfigurationIsValid());
        Assert.Contains("Missing", ex.Message);
    }

    [Fact]
    public void AssertConfigurationIsValid_passes_when_unmapped_member_is_ignored()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ForMember(d => d.Missing, opt => opt.Ignore()));
        cfg.AssertConfigurationIsValid(); // should not throw
    }

    [Fact]
    public void AssertConfigurationIsValid_passes_when_all_members_match_by_name()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Src>());
        cfg.AssertConfigurationIsValid();
    }
}
