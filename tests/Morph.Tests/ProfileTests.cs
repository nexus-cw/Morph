using System.Reflection;
using Xunit;

namespace Morph.Tests;

public class ProfileTests
{
    public class A { public int X { get; set; } }
    public class B { public int X { get; set; } }

    public class MapAToB : Profile
    {
        public MapAToB() { CreateMap<A, B>(); }
    }

    [Fact]
    public void AddProfile_generic_registers_maps_from_profile_type()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MapAToB>());
        var r = cfg.CreateMapper().Map<A, B>(new A { X = 9 });
        Assert.Equal(9, r.X);
    }

    [Fact]
    public void AddProfile_instance_registers_maps_from_given_profile()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile(new MapAToB()));
        var r = cfg.CreateMapper().Map<A, B>(new A { X = 5 });
        Assert.Equal(5, r.X);
    }

    [Fact]
    public void AddProfiles_assembly_scans_and_registers()
    {
        var cfg = new MapperConfiguration(c => c.AddProfiles(typeof(ProfileTests).Assembly));
        // Proves the profile in this assembly was picked up.
        var r = cfg.CreateMapper().Map<A, B>(new A { X = 1 });
        Assert.Equal(1, r.X);
    }
}
