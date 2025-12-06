namespace FlowPack;

public static class TypeExtensions
{
    private const string Prefix = "CSharp:";

    /// <summary>
    /// Returns the canonical AI type name with prefix "CSharp:", avoiding nested prefixes.
    /// </summary>
    public static string GetCanonicalAiTypeName(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        return Prefix + GetCanonicalInternal(type);
    }

    private static string GetCanonicalInternal(Type type)
    {
        // Nullable<T> → T?
        if (Nullable.GetUnderlyingType(type) is Type underlyingNullable)
            return $"{GetCanonicalInternal(underlyingNullable)}?";

        // Arrays
        if (type.IsArray)
        {
            string elem = GetCanonicalInternal(type.GetElementType()!);
            // C#-style multi-dim array: [,,]
            string commas = new string(',', type.GetArrayRank() - 1);
            //string brackets = new string('[', type.GetArrayRank()) + new string(']', type.GetArrayRank());
            return $"{elem}[{commas}]";
        }

        // Generic types
        if (type.IsGenericType)
        {
            string fullName = type.GetGenericTypeDefinition().FullName!;
            int idx = fullName.IndexOf('`');
            if (idx > 0)
                fullName = fullName[..idx];

            var args = type.GetGenericArguments()
                           .Select(GetCanonicalInternal);

            return $"{fullName}[{string.Join(", ", args)}]";
        }

        // Simple type
        return type.FullName!;
    }
}