using Xunit;

namespace Morph.Tests;

public class ConvertUsingTests
{
    public class Seconds { public int Value { get; set; } }
    public class Minutes { public int Value { get; set; } }

    public class SecondsToMinutes : ITypeConverter<Seconds, Minutes>
    {
        public Minutes Convert(Seconds source, Minutes destination, ResolutionContext context)
            => new() { Value = source.Value / 60 };
    }

    [Fact]
    public void ConvertUsing_TConverter_replaces_default_member_mapping()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Seconds, Minutes>().ConvertUsing<SecondsToMinutes>());
        var r = cfg.CreateMapper().Map<Seconds, Minutes>(new Seconds { Value = 180 });
        Assert.Equal(3, r.Value);
    }

    [Fact]
    public void ConvertUsing_func_one_arg_replaces_default_member_mapping()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Seconds, Minutes>()
            .ConvertUsing(s => new Minutes { Value = s.Value / 60 }));
        var r = cfg.CreateMapper().Map<Seconds, Minutes>(new Seconds { Value = 120 });
        Assert.Equal(2, r.Value);
    }

    [Fact]
    public void ConvertUsing_func_two_arg_has_access_to_existing_destination()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Seconds, Minutes>()
            .ConvertUsing((s, d) => { d.Value = s.Value / 60; return d; }));
        var dest = new Minutes { Value = -1 };
        var r = cfg.CreateMapper().Map(new Seconds { Value = 240 }, dest);
        Assert.Equal(4, r.Value);
        Assert.Same(dest, r);
    }
}
