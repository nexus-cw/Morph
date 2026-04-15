using System;

namespace Morph.Execution;

/// <summary>
/// Best-effort conversion between primitive/string types. Used by the mapper as a fallback
/// when source and destination member types differ but no nested TypeMap exists.
/// </summary>
internal static class ValueCoercion
{
    public static object? Convert(object? value, Type destinationType)
    {
        if (value is null) return DefaultFor(destinationType);

        var destType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        if (destType.IsInstanceOfType(value)) return value;

        if (destType.IsEnum)
        {
            if (value is string s) return Enum.Parse(destType, s, ignoreCase: false);
            return Enum.ToObject(destType, value);
        }

        if (value is IConvertible)
        {
            try { return System.Convert.ChangeType(value, destType); }
            catch { /* fall through to throw */ }
        }

        if (destType == typeof(string))
            return value.ToString();

        throw new AutoMapperMappingException(
            $"Cannot convert value of type {value.GetType().FullName} to {destinationType.FullName}")
        {
            SourceType = value.GetType(),
            DestinationType = destinationType
        };
    }

    private static object? DefaultFor(Type t)
        => t.IsValueType && Nullable.GetUnderlyingType(t) is null ? Activator.CreateInstance(t) : null;
}
