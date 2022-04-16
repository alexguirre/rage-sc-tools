namespace ScTools.ScriptLang.Types;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public sealed class TypeRegistry
{
    private readonly Dictionary<string, TypeInfo> types = new();

    public TypeRegistry()
    {
        RegisterBuiltIns();
    }

    public void Register(string name, TypeInfo type) => types.Add(name, type);
    public bool TryRegister(string name, TypeInfo type) => types.TryAdd(name, type);
    public bool Find(string name, [MaybeNullWhen(false)] out TypeInfo type) => types.TryGetValue(name, out type);

    private void RegisterBuiltIns()
    {
        Register("ANY", AnyType.Instance);
        Register("INT", IntType.Instance);
        Register("FLOAT", FloatType.Instance);
        Register("BOOL", BoolType.Instance);
        Register("STRING", StringType.Instance);
        Register("VECTOR", VectorType.Instance);

        HandleType.All.ForEach(h => Register(HandleType.KindToTypeName(h.Kind), h));

        TextLabelType.All.ForEach(tl => Register(TextLabelType.GetTypeNameForLength(tl.Length), tl));
    }
}
