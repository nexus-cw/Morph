namespace Morph;

/// <summary>
/// Custom value resolver for a single destination member. Registered via
/// <c>ForMember(..., opt =&gt; opt.MapFrom&lt;TResolver&gt;())</c>.
/// </summary>
public interface IValueResolver<in TSource, in TDestination, TDestMember>
{
    TDestMember Resolve(TSource source, TDestination destination, TDestMember destMember, ResolutionContext context);
}
