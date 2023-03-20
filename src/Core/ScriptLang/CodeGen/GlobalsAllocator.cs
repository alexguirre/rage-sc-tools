namespace ScTools.ScriptLang.CodeGen;

using ScTools.ScriptLang.Ast.Declarations;
using System;
using System.Collections.Generic;

public sealed class GlobalsAllocator
{
    private readonly Dictionary<GlobalBlockDeclaration, VarAllocator> blocks;
    private readonly Dictionary<VarDeclaration, GlobalBlockDeclaration> varToBlock;
    private readonly bool indexed;

    public GlobalsAllocator(bool indexed)
    {
        blocks = new();
        varToBlock = new();
        this.indexed = indexed;
    }

    public GlobalsAllocator(GlobalsAllocator other)
    {
        blocks = new(other.blocks);
        varToBlock = new(other.varToBlock);
        indexed = other.indexed;
    }

    public void Clear()
    {
        blocks.Clear();
    }

    public GlobalBlockDeclaration? GetGlobalsBlockForScript(ScriptDeclaration script)
        => blocks.Keys.FirstOrDefault(b => Parser.CaseInsensitiveComparer.Equals(b.Name, script.Name));

    public int IndexOf(GlobalBlockDeclaration block) => block.BlockIndex;
    
    public int SizeOf(GlobalBlockDeclaration block) => blocks[block].AllocatedSize;

    public int OffsetOf(VarDeclaration varDecl)
    {
        var block = varToBlock[varDecl];
        var offset = blocks[block].OffsetOf(varDecl);
        if (indexed) { offset |= (IndexOf(block) << 18); }
        return offset;
    }

    public void Allocate(GlobalBlockDeclaration block)
    {
        if (blocks.ContainsKey(block))
        {
            throw new ArgumentException($"Global block '{block.Name}' already allocated", nameof(block));
        }

        var allocator = new VarAllocator();
        foreach (var varDecl in block.Vars)
        {
            allocator.Allocate(varDecl);
            varToBlock.Add(varDecl, block);
        }
        blocks.Add(block, allocator);
    }
}
