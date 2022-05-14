namespace ScTools;

using System;
using System.Runtime.CompilerServices;

internal static class EnumExtensions
{
    public static TInt AsInteger<TEnum, TInt>(this TEnum enumValue)
        where TEnum : struct, Enum
        where TInt : struct
    {
        // from https://github.com/dotnet/csharplang/discussions/1993#discussioncomment-104840
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>())
        {
            throw new InvalidOperationException($"Enums can only be converted to integers of the same size.");
        }
        
        return Unsafe.As<TEnum, TInt>(ref enumValue);
    }
}
