using Xunit;

namespace Morph.Tests;

// I3 coverage: nested member mapping must find a base-class TypeMap when the declared
// member type is a subclass of that base. Pre-fix, `CoerceOrNestedMap` did an exact
// `(src, dst)` key lookup against `_config.TypeMaps`, so a `Dog` member with only an
// `Animal → AnimalDto` map registered fell through to `ValueCoercion` and threw.
// Elsewhere the mapper uses an `IsAssignableFrom` walk; this aligns the two paths.
public class SubtypeMappingTests
{
    public class Animal { public string Name { get; set; } = ""; }
    public class Dog : Animal { public string Breed { get; set; } = ""; }

    public class AnimalDto { public string Name { get; set; } = ""; }

    public class Owner { public Dog Pet { get; set; } = new(); }
    public class OwnerDto { public AnimalDto Pet { get; set; } = new(); }

    [Fact]
    public void Nested_member_declared_as_subclass_uses_base_TypeMap()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Animal, AnimalDto>();
            cfg.CreateMap<Owner, OwnerDto>();
        });
        var mapper = config.CreateMapper();

        var owner = new Owner { Pet = new Dog { Name = "Rex", Breed = "Lab" } };

        var result = mapper.Map<Owner, OwnerDto>(owner);

        Assert.NotNull(result.Pet);
        Assert.Equal("Rex", result.Pet.Name);
    }
}
