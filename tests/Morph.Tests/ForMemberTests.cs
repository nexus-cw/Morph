using System;
using Xunit;

namespace Morph.Tests;

public class ForMemberTests
{
    public class Src { public string First { get; set; } = ""; public string Last { get; set; } = ""; public int Age { get; set; } }
    public class Dst { public string FullName { get; set; } = ""; public int Age { get; set; } public string Secret { get; set; } = "keep-me"; }

    [Fact]
    public void MapFrom_expression_projects_source_member()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.First))
            .ForMember(d => d.Secret, opt => opt.Ignore()));
        var mapper = cfg.CreateMapper();

        var result = mapper.Map<Src, Dst>(new Src { First = "Ada", Last = "Lovelace", Age = 28 });

        Assert.Equal("Ada", result.FullName);
        Assert.Equal(28, result.Age);
        Assert.Equal("keep-me", result.Secret);
    }

    [Fact]
    public void MapFrom_func_has_access_to_source_and_destination()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ForMember(d => d.FullName, opt => opt.MapFrom((s, d) => s.First + " " + s.Last))
            .ForMember(d => d.Secret, opt => opt.Ignore()));
        var mapper = cfg.CreateMapper();

        var result = mapper.Map<Src, Dst>(new Src { First = "Ada", Last = "Lovelace" });

        Assert.Equal("Ada Lovelace", result.FullName);
    }

    public class UpperNameResolver : IValueResolver<Src, Dst, string>
    {
        public string Resolve(Src source, Dst destination, string destMember, ResolutionContext context)
            => (source.First + " " + source.Last).ToUpperInvariant();
    }

    [Fact]
    public void MapFrom_TResolver_uses_custom_resolver_type()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<UpperNameResolver>())
            .ForMember(d => d.Secret, opt => opt.Ignore()));
        var mapper = cfg.CreateMapper();

        var result = mapper.Map<Src, Dst>(new Src { First = "Ada", Last = "Lovelace" });

        Assert.Equal("ADA LOVELACE", result.FullName);
    }

    [Fact]
    public void Ignore_skips_a_member_that_has_no_source_match()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ForMember(d => d.FullName, opt => opt.Ignore())
            .ForMember(d => d.Secret, opt => opt.Ignore()));
        var mapper = cfg.CreateMapper();

        var result = mapper.Map<Src, Dst>(new Src { First = "x", Age = 1 });

        Assert.Equal("", result.FullName);
        Assert.Equal("keep-me", result.Secret);
    }

    [Fact]
    public void UseValue_writes_constant()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ForMember(d => d.FullName, opt => opt.UseValue("static"))
            .ForMember(d => d.Secret, opt => opt.Ignore()));
        var mapper = cfg.CreateMapper();

        var result = mapper.Map<Src, Dst>(new Src { First = "ignored" });

        Assert.Equal("static", result.FullName);
    }

    [Fact]
    public void Condition_skips_the_member_when_predicate_is_false()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<Src, Dst>()
            .ForMember(d => d.FullName, opt =>
            {
                opt.MapFrom(s => s.First);
                opt.Condition(s => s.Age > 18);
            })
            .ForMember(d => d.Secret, opt => opt.Ignore()));
        var mapper = cfg.CreateMapper();

        var adult = mapper.Map<Src, Dst>(new Src { First = "Alice", Age = 30 });
        var child = mapper.Map<Src, Dst>(new Src { First = "Bob", Age = 10 });

        Assert.Equal("Alice", adult.FullName);
        Assert.Equal("", child.FullName);
    }
}
