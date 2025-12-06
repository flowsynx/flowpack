using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowPack.UnitTests;

public class TypeExtensions_ExhaustiveTests
{
    // ============================================================
    //       PRIMITIVES & SPECIAL TYPES
    // ============================================================

    [Fact]
    public void Canonical_Primitives_And_CoreTypes()
    {
        Assert.Equal("CSharp:System.Int32", TypeExtensions.GetCanonicalAiTypeName(typeof(int)));
        Assert.Equal("CSharp:System.Boolean", TypeExtensions.GetCanonicalAiTypeName(typeof(bool)));
        Assert.Equal("CSharp:System.Char", TypeExtensions.GetCanonicalAiTypeName(typeof(char)));
        Assert.Equal("CSharp:System.Byte", TypeExtensions.GetCanonicalAiTypeName(typeof(byte)));
        Assert.Equal("CSharp:System.SByte", TypeExtensions.GetCanonicalAiTypeName(typeof(sbyte)));
        Assert.Equal("CSharp:System.Decimal", TypeExtensions.GetCanonicalAiTypeName(typeof(decimal)));
        Assert.Equal("CSharp:System.Double", TypeExtensions.GetCanonicalAiTypeName(typeof(double)));
        Assert.Equal("CSharp:System.Single", TypeExtensions.GetCanonicalAiTypeName(typeof(float)));
        Assert.Equal("CSharp:System.Int64", TypeExtensions.GetCanonicalAiTypeName(typeof(long)));
        Assert.Equal("CSharp:System.Int16", TypeExtensions.GetCanonicalAiTypeName(typeof(short)));
        Assert.Equal("CSharp:System.UInt32", TypeExtensions.GetCanonicalAiTypeName(typeof(uint)));
        Assert.Equal("CSharp:System.UInt64", TypeExtensions.GetCanonicalAiTypeName(typeof(ulong)));
        Assert.Equal("CSharp:System.UInt16", TypeExtensions.GetCanonicalAiTypeName(typeof(ushort)));

        Assert.Equal("CSharp:System.Object", TypeExtensions.GetCanonicalAiTypeName(typeof(object)));
        Assert.Equal("CSharp:System.String", TypeExtensions.GetCanonicalAiTypeName(typeof(string)));
        Assert.Equal("CSharp:System.Void", TypeExtensions.GetCanonicalAiTypeName(typeof(void)));
    }

    // ============================================================
    //       NINT / NUINT
    // ============================================================

    [Fact]
    public void Canonical_Nint_And_Nuint()
    {
        Assert.Equal("CSharp:System.IntPtr", TypeExtensions.GetCanonicalAiTypeName(typeof(nint)));
        Assert.Equal("CSharp:System.UIntPtr", TypeExtensions.GetCanonicalAiTypeName(typeof(nuint)));
    }

    // ============================================================
    //       NULLABLE
    // ============================================================

    [Fact]
    public void Canonical_Nullable_Allowed()
    {
        Assert.Equal("CSharp:System.Int32?", TypeExtensions.GetCanonicalAiTypeName(typeof(int?)));
        Assert.Equal("CSharp:System.DateTime?", TypeExtensions.GetCanonicalAiTypeName(typeof(DateTime?)));
    }

    // ============================================================
    //       ARRAY TYPES
    // ============================================================

    [Fact]
    public void Canonical_Arrays_Simple_And_MultiDim()
    {
        Assert.Equal("CSharp:System.Int32[]", TypeExtensions.GetCanonicalAiTypeName(typeof(int[])));
        Assert.Equal("CSharp:System.Int32[][]", TypeExtensions.GetCanonicalAiTypeName(typeof(int[][])));
        Assert.Equal("CSharp:System.Int32[,,]", TypeExtensions.GetCanonicalAiTypeName(typeof(int[,,])));
        Assert.Equal("CSharp:System.String[,,,]", TypeExtensions.GetCanonicalAiTypeName(typeof(string[,,,])));
    }

    [Fact]
    public void Canonical_Arrays_OfGenerics()
    {
        Assert.Equal(
            "CSharp:System.Collections.Generic.List[System.String][]",
            TypeExtensions.GetCanonicalAiTypeName(typeof(List<string>[]))
        );

        Assert.Equal(
            "CSharp:System.Collections.Generic.Dictionary[System.String, System.Int32][,,]",
            TypeExtensions.GetCanonicalAiTypeName(typeof(Dictionary<string, int>[,,]))
        );
    }

    // ============================================================
    //       POINTERS
    // ============================================================

    [Fact]
    public void Canonical_PointerTypes()
    {
        Assert.Contains("System.Int32*", TypeExtensions.GetCanonicalAiTypeName(typeof(int*)));
        Assert.Contains("System.Byte**", TypeExtensions.GetCanonicalAiTypeName(typeof(byte**)));
    }

    // ============================================================
    //       BYREF TYPES
    // ============================================================

    [Fact]
    public void Canonical_ByRef_Types()
    {
        Assert.Contains("System.Int32&", TypeExtensions.GetCanonicalAiTypeName(typeof(int).MakeByRefType()));
        Assert.Contains("System.String&", TypeExtensions.GetCanonicalAiTypeName(typeof(string).MakeByRefType()));
    }

    // ============================================================
    //       GENERIC PARAMETER TYPES
    // ============================================================

    [Fact]
    public void Canonical_GenericParameter()
    {
        var tParam = typeof(List<>).GetGenericArguments()[0];

        var result = TypeExtensions.GetCanonicalAiTypeName(tParam);

        Assert.StartsWith("CSharp:", result);
        Assert.True(result.Length >= "CSharp:".Length);
    }

    // ============================================================
    //       OPEN GENERICS & GENERIC DEFINITIONS
    // ============================================================

    [Fact]
    public void Canonical_OpenGenericDefinitions()
    {
        var def = typeof(Dictionary<,>);

        var result = TypeExtensions.GetCanonicalAiTypeName(def);

        Assert.StartsWith("CSharp:System.Collections.Generic.Dictionary", result);
        Assert.DoesNotContain("`", result);
    }

    [Fact]
    public void Canonical_ClosedGenerics_DeepNested()
    {
        var type = typeof(Dictionary<string, List<Dictionary<int, List<string>>>>);

        var result = TypeExtensions.GetCanonicalAiTypeName(type);

        Assert.Contains("Dictionary", result);
        Assert.Contains("List", result);
        Assert.Contains("System.Int32", result);
    }

    // ============================================================
    //       TUPLES
    // ============================================================

    [Fact]
    public void Canonical_ValueTuple()
    {
        var t = typeof((int, string));

        var result = TypeExtensions.GetCanonicalAiTypeName(t);

        Assert.StartsWith("CSharp:System.ValueTuple", result);
        Assert.Contains("System.Int32", result);
        Assert.Contains("System.String", result);
    }

    [Fact]
    public void Canonical_Large_ValueTuple()
    {
        var t = typeof((int, int, int, int, int, int, int, int));

        var result = TypeExtensions.GetCanonicalAiTypeName(t);

        Assert.Contains("System.ValueTuple", result);
    }

    // ============================================================
    //       ANONYMOUS TYPES
    // ============================================================

    [Fact]
    public void Canonical_AnonymousType()
    {
        var anon = new { A = 1, B = "two" }.GetType();

        var result = TypeExtensions.GetCanonicalAiTypeName(anon);

        Assert.StartsWith("CSharp:", result);
        Assert.Contains("AnonymousType", result);
    }

    // ============================================================
    //       ENUMS / STRUCTS / RECORDS / REF STRUCTS
    // ============================================================

    private enum MyEnum { A, B }
    private struct MyStruct { }
    private readonly struct MyReadonlyStruct { }
    private ref struct MyRefStruct { }
    private record MyRecord(int X, string Y);

    [Fact]
    public void Canonical_Enum_Struct_Record_RefStruct()
    {
        Assert.Equal("CSharp:FlowPack.UnitTests.TypeExtensions_ExhaustiveTests+MyEnum",
            TypeExtensions.GetCanonicalAiTypeName(typeof(MyEnum)));

        Assert.Contains("MyStruct", TypeExtensions.GetCanonicalAiTypeName(typeof(MyStruct)));
        Assert.Contains("MyReadonlyStruct", TypeExtensions.GetCanonicalAiTypeName(typeof(MyReadonlyStruct)));

        // ref struct (no IL full name)
        var refStructType = typeof(MyRefStruct);
        var result = TypeExtensions.GetCanonicalAiTypeName(refStructType);
        Assert.Contains("MyRefStruct", result);

        Assert.Contains("MyRecord", TypeExtensions.GetCanonicalAiTypeName(typeof(MyRecord)));
    }

    // ============================================================
    //       INTERFACES / ABSTRACT CLASSES
    // ============================================================

    private interface IMyInterface { }
    private abstract class MyAbstractClass { }

    [Fact]
    public void Canonical_Interface_And_AbstractClass()
    {
        Assert.Contains("IMyInterface", TypeExtensions.GetCanonicalAiTypeName(typeof(IMyInterface)));
        Assert.Contains("MyAbstractClass", TypeExtensions.GetCanonicalAiTypeName(typeof(MyAbstractClass)));
    }

    // ============================================================
    //       NESTED CLASSES
    // ============================================================

    private class Outer { public class Inner { } }

    [Fact]
    public void Canonical_NestedClasses()
    {
        var t = typeof(Outer.Inner);

        var result = TypeExtensions.GetCanonicalAiTypeName(t);

        Assert.Contains("Outer+Inner", result);
    }

    // ============================================================
    //       CLASSES WITHOUT NAMESPACE
    // ============================================================

    class NoNamespaceClass { }

    [Fact]
    public void Canonical_NoNamespace()
    {
        var t = new NoNamespaceClass().GetType();

        var result = TypeExtensions.GetCanonicalAiTypeName(t);

        Assert.StartsWith("CSharp:", result);
        Assert.Contains("NoNamespaceClass", result);
    }

    // ============================================================
    //       DELEGATE TYPES
    // ============================================================

    private delegate int MyDelegate(string x);

    [Fact]
    public void Canonical_DelegateType()
    {
        var result = TypeExtensions.GetCanonicalAiTypeName(typeof(MyDelegate));

        Assert.Contains("MyDelegate", result);
    }

    // ============================================================
    //       NULL ARGUMENT HANDLING
    // ============================================================

    [Fact]
    public void Canonical_Throws_OnNull()
    {
        Assert.Throws<ArgumentNullException>(() => TypeExtensions.GetCanonicalAiTypeName(null!));
    }
}
