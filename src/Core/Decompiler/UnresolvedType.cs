namespace ScTools.Decompiler;

public class UnresolvedType
{
    public int Size { get; }

    public UnresolvedType(int size)
    {
        Size = size;
    }
}

public class UnresolvedTypeTuple
{
    public int TotalSize { get; }

    public UnresolvedTypeTuple(int totalSize)
    {
        TotalSize = totalSize;
    }
}
