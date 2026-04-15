namespace Morph;

/// <summary>
/// Full-type custom conversion. When registered via
/// <c>CreateMap&lt;TSrc,TDest&gt;().ConvertUsing&lt;TConverter&gt;()</c>, the converter fully
/// replaces Morph's default member-by-member mapping for this type pair.
/// </summary>
public interface ITypeConverter<in TSource, TDestination>
{
    TDestination Convert(TSource source, TDestination destination, ResolutionContext context);
}
