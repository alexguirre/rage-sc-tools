namespace ScTools.ScriptLang.Types;

using System;

[Flags]
public enum ValueKind
{
    /// <summary>
    /// Expression can be the RHS of an assignment statement.
    /// </summary>
    RValue = 1 << 0,
    /// <summary>
    /// Expression can be assigned to.
    /// </summary>
    Assignable = 1 << 1,
    /// <summary>
    /// Expression can be passed by reference.
    /// </summary>
    Addressable = 1 << 2,
    /// <summary>
    /// Expression value is known at compile time.
    /// </summary>
    Constant = 1 << 3,
}

public static class ValueKindExtensions
{
    public static bool Is(this ValueKind value, ValueKind flag) => (value & flag) == flag;
}
