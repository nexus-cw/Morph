using System;
using System.Collections.Generic;
using System.Reflection;
using Morph.Expressions;

namespace Morph;

/// <summary>
/// Internal implementation of <see cref="IMapperConfigurationExpression"/>. Instances are
/// created by <see cref="MapperConfiguration"/> and passed to the caller-supplied config action.
/// </summary>
internal sealed class MapperConfigurationExpression : IMapperConfigurationExpression
{
    public List<Profile> Profiles { get; } = new();
    public List<object> InlineDefinitions { get; } = new();

    public void AddProfile<TProfile>() where TProfile : Profile, new()
        => Profiles.Add(new TProfile());

    public void AddProfile(Profile profile)
        => Profiles.Add(profile ?? throw new ArgumentNullException(nameof(profile)));

    public void AddProfiles(Assembly assembly)
    {
        if (assembly is null) throw new ArgumentNullException(nameof(assembly));
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(Profile).IsAssignableFrom(type)) continue;
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor is null) continue;
            Profiles.Add((Profile)ctor.Invoke(null));
        }
    }

    public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var expr = new MappingExpression<TSource, TDestination>();
        InlineDefinitions.Add(expr);
        return expr;
    }
}
