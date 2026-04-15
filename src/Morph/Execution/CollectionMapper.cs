using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Morph.Execution;

/// <summary>
/// Maps <c>IEnumerable&lt;TSrc&gt;</c> source values into destination collection types
/// (<c>List&lt;TDest&gt;</c>, <c>TDest[]</c>, <c>ICollection&lt;TDest&gt;</c>, etc.) by
/// element-wise delegating to the parent <see cref="Mapper"/>.
/// </summary>
internal static class CollectionMapper
{
    public static bool TryGetElementTypes(Type collectionType, out Type elementType)
    {
        elementType = typeof(object);
        if (collectionType.IsArray)
        {
            elementType = collectionType.GetElementType()!;
            return true;
        }
        if (collectionType.IsGenericType)
        {
            var def = collectionType.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) || def == typeof(ICollection<>) || def == typeof(IList<>) || def == typeof(List<>))
            {
                elementType = collectionType.GetGenericArguments()[0];
                return true;
            }
        }
        foreach (var iface in collectionType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = iface.GetGenericArguments()[0];
                return true;
            }
        }
        return false;
    }

    public static object Map(IEnumerable source, Type sourceElemType, Type destinationType, Type destElemType, Mapper mapper, int depth)
    {
        var mapped = new List<object?>();
        // depth + 1: each collection element is one recursion level deeper, so the mapper's
        // GuardDepth sees list-element cycles the same way it sees property-nested cycles.
        // CVE-2026-32933 regression — C1.
        foreach (var item in source)
            mapped.Add(mapper.MapDynamic(item, item?.GetType() ?? sourceElemType, destElemType, depth + 1));

        if (destinationType.IsArray)
        {
            var arr = Array.CreateInstance(destElemType, mapped.Count);
            for (var i = 0; i < mapped.Count; i++) arr.SetValue(mapped[i], i);
            return arr;
        }

        // List<T>, ICollection<T>, IEnumerable<T>, IList<T> → List<T>
        var listType = typeof(List<>).MakeGenericType(destElemType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var m in mapped) list.Add(m);
        return list;
    }
}
