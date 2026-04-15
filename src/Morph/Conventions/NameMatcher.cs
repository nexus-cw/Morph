using System.Reflection;

namespace Morph.Conventions;

/// <summary>
/// Default name-matching convention: exact-name, case-sensitive, properties-then-fields.
/// Deliberately strict to avoid silent mismatches. A future version may expose pluggable conventions.
/// </summary>
internal static class NameMatcher
{
    public static MemberInfo? Match(System.Type sourceType, string destMemberName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var prop = sourceType.GetProperty(destMemberName, flags);
        if (prop is not null && prop.CanRead) return prop;
        var field = sourceType.GetField(destMemberName, flags);
        return field;
    }
}
