namespace ScTools.ScriptLang.Types;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public sealed class TypeRegistry
{
    private readonly Dictionary<string, TypeInfo> types = new();

    public void Register(string name, TypeInfo type) => types.Add(name, type);
    public bool TryRegister(string name, TypeInfo type) => types.TryAdd(name, type);
    public bool Find(string name, [MaybeNullWhen(false)] out TypeInfo type) => types.TryGetValue(name, out type);
}
