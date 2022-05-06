
namespace ScTools.ScriptLang.CodeGen;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public sealed class VarAllocator : IEnumerable<VarDeclaration>
{
    private readonly List<VarDeclaration> vars;
    private readonly Dictionary<VarDeclaration, int> offsets;

    public IReadOnlyList<VarDeclaration> Vars => vars;
    public int AllocatedSize { get; private set; }

    public VarAllocator()
    {
        vars = new();
        offsets = new();
        AllocatedSize = 0;
    }

    public VarAllocator(VarAllocator other)
    {
        vars = new(other.vars);
        offsets = new(other.offsets);
        AllocatedSize = other.AllocatedSize;
    }

    public void Clear()
    {
        vars.Clear();
        offsets.Clear();
        AllocatedSize = 0;
    }

    public int OffsetOf(VarDeclaration varDecl) => offsets[varDecl];

    public int Allocate(VarDeclaration varDecl)
    {
        Debug.Assert(varDecl.Semantics.ValueType is not null);

        if (offsets.ContainsKey(varDecl))
        {
            throw new ArgumentException($"Variable '{varDecl.Name}' already allocated", nameof(varDecl));
        }

        var type = varDecl.Semantics.ValueType!;
        var isReference = false;

        if (varDecl.Kind is VarKind.Parameter)
        {
            // special case for parameters passed by reference
            isReference = varDecl.IsReference || type is ArrayType;
        }

        var size = isReference ? 1 : type.SizeOf;
        var offset = Allocate(size);
        offsets.Add(varDecl, offset);
        vars.Add(varDecl);
        return offset;
    }

    public int Allocate(int size)
    {
        var offset = AllocatedSize;
        AllocatedSize += size;
        return offset;
    }

    public IEnumerator<VarDeclaration> GetEnumerator() => ((IEnumerable<VarDeclaration>)vars).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)vars).GetEnumerator();
}
