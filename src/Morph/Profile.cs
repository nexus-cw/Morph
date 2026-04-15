using System.Collections.Generic;

namespace Morph;

/// <summary>
/// A logical grouping of map definitions. Subclass and call <see cref="CreateMap{TSource,TDestination}"/>
/// in the constructor. Register via <see cref="IMapperConfigurationExpression.AddProfile{TProfile}"/>
/// or <see cref="IMapperConfigurationExpression.AddProfile(Profile)"/>.
/// </summary>
public abstract class Profile
{
    internal readonly List<object> Definitions = new();

    /// <summary>Declare a map from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.</summary>
    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var expr = new Expressions.MappingExpression<TSource, TDestination>();
        Definitions.Add(expr);
        return expr;
    }
}
