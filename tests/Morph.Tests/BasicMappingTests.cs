using Xunit;

namespace Morph.Tests;

public class BasicMappingTests
{
    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public class Dest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [Fact]
    public void Maps_primitive_properties_by_name()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<Source, Dest>());
        var mapper = config.CreateMapper();

        var result = mapper.Map<Source, Dest>(new Source { Id = 7, Name = "hero", Age = 42 });

        Assert.Equal(7, result.Id);
        Assert.Equal("hero", result.Name);
        Assert.Equal(42, result.Age);
    }

    [Fact]
    public void Maps_into_existing_destination_overwrites_members()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<Source, Dest>());
        var mapper = config.CreateMapper();

        var dest = new Dest { Id = 1, Name = "old", Age = 1 };
        var returned = mapper.Map(new Source { Id = 2, Name = "new", Age = 2 }, dest);

        Assert.Same(dest, returned);
        Assert.Equal(2, dest.Id);
        Assert.Equal("new", dest.Name);
        Assert.Equal(2, dest.Age);
    }

    [Fact]
    public void Map_via_runtime_type_discovery()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<Source, Dest>());
        var mapper = config.CreateMapper();

        object src = new Source { Id = 99, Name = "x", Age = 5 };
        var result = mapper.Map<Dest>(src);

        Assert.Equal(99, result.Id);
    }

    public class NestedSource
    {
        public string Name { get; set; } = "";
        public Source Inner { get; set; } = new();
    }

    public class NestedDest
    {
        public string Name { get; set; } = "";
        public Dest Inner { get; set; } = new();
    }

    [Fact]
    public void Maps_nested_objects_when_nested_map_is_configured()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Source, Dest>();
            cfg.CreateMap<NestedSource, NestedDest>();
        });
        var mapper = config.CreateMapper();

        var result = mapper.Map<NestedSource, NestedDest>(new NestedSource
        {
            Name = "outer",
            Inner = new Source { Id = 10, Name = "inner", Age = 3 }
        });

        Assert.Equal("outer", result.Name);
        Assert.Equal(10, result.Inner.Id);
        Assert.Equal("inner", result.Inner.Name);
    }
}
