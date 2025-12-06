namespace FlowPack;

public static class TypeExtensions
{
    private static readonly Dictionary<Type, string> _csharpAliases = new()
    {
        { typeof(void), "void" },
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(char), "char" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(int), "int" },
        { typeof(uint), "uint" },
        { typeof(long), "long" },
        { typeof(ulong), "ulong" },
        { typeof(object), "object" },
        { typeof(short), "short" },
        { typeof(ushort), "ushort" },
        { typeof(string), "string" }
    };

    public static string GetCanonicalAiTypeName(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // Nullable<T> → T?
        var underlyingNullable = Nullable.GetUnderlyingType(type);
        if (underlyingNullable != null)
            return $"CSharp:{GetCanonicalAiTypeName(underlyingNullable)}?";

        // Arrays
        if (type.IsArray)
        {
            var elem = GetCanonicalAiTypeName(type.GetElementType()!);
            return $"CSharp:{elem}{new string('[', type.GetArrayRank())}{new string(']', type.GetArrayRank())}";
        }

        // Generic types
        if (type.IsGenericType)
        {
            string typeName = type.GetGenericTypeDefinition().FullName!;
            int backtick = typeName.IndexOf('`');
            if (backtick > 0)
                typeName = typeName[..backtick];

            var args = type.GetGenericArguments()
                .Select(GetCanonicalAiTypeName);

            return $"CSharp:{typeName}[{string.Join(", ", args)}]";
        }

        // Non-generic, non-nullable, non-array
        return $"CSharp:{type.FullName}";
    }

    public static string GetFriendlyTypeName(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // Handle arrays
        if (type.IsArray)
        {
            return $"{GetFriendlyTypeName(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
        }

        // Handle nullable types
        var underlyingNullable = Nullable.GetUnderlyingType(type);
        if (underlyingNullable != null)
        {
            return $"{GetFriendlyTypeName(underlyingNullable)}?";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            var typeDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments().Select(GetFriendlyTypeName).ToArray();

            var typeName = type.Name;
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex > 0)
                typeName = typeName.Substring(0, backtickIndex);

            return $"{typeName}<{string.Join(", ", genericArgs)}>";
        }

        // Map to C# alias if possible
        if (_csharpAliases.TryGetValue(type, out var alias))
            return alias;

        return type.Name; // fallback to CLR type name
    }
}