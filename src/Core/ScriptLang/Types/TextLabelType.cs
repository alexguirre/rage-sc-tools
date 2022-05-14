namespace ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;

public sealed record TextLabelType(int Length, int ValueByteSize) : TypeInfo
{
    public const int MinLength32 = 4;
    public const int MaxLength32 = 256 - 4;
    public const int MinLength64 = 8;
    public const int MaxLength64 = 256 - 8;

    public override int SizeOf => Length / ValueByteSize;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;
    public int ValueByteSize { get; init; } = ValueByteSize is 4 or 8 ? ValueByteSize : throw new ArgumentException("Value byte size must be 4 or 8", nameof(ValueByteSize));
    public int Length { get; init; } = IsValidLength(Length, ValueByteSize) ? Length : throw new ArgumentException("Invalid length", nameof(Length));

    public override string ToPrettyString() => GetTypeNameForLength(Length);
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    /// <summary>
    /// Gets all text TEXT_LABEL_* types available in 32-bit platforms.
    /// </summary>
    public static ImmutableArray<TextLabelType> All32 { get; }
    /// <summary>
    /// Gets all text TEXT_LABEL_* types available in 64-bit platforms.
    /// </summary>
    public static ImmutableArray<TextLabelType> All64 { get; }

    static TextLabelType()
    {
        All32 = CreateArray(MinLength32, MaxLength32, 4);
        All64 = CreateArray(MinLength64, MaxLength64, 8);

        static ImmutableArray<TextLabelType> CreateArray(int minLength, int maxLength, int valueByteSize)
        {
            var arr = ImmutableArray.CreateBuilder<TextLabelType>(initialCapacity: (maxLength - minLength) / valueByteSize + 1);
            for (int length = minLength; length <= maxLength; length += valueByteSize)
            {
                arr.Add(new TextLabelType(length, valueByteSize));
            }
            return arr.MoveToImmutable();
        }
    }

    public static bool IsValidLength(int length, int valueByteSize)
        => valueByteSize switch
        {
            4 => IsValidLength32(length),
            8 => IsValidLength64(length),
            _ => throw new ArgumentException("Value byte size must be 4 or 8", nameof(valueByteSize))
        };

    /// <returns>
    /// <c>true</c> if <paramref name="length"/> is in the range [<see cref="MinLength32"/>, <see cref="MaxLength32"/>]
    /// and is a multiple of 4; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidLength32(int length)
        => length is >= MinLength32 and <= MaxLength32 && (length % 4) == 0;
    /// <returns>
    /// <c>true</c> if <paramref name="length"/> is in the range [<see cref="MinLength64"/>, <see cref="MaxLength64"/>]
    /// and is a multiple of 8; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidLength64(int length)
        => length is >= MinLength64 and <= MaxLength64 && (length % 8) == 0;

    public static string GetTypeNameForLength(int length)
        => $"TEXT_LABEL_{length - 1}"; // real type name found in tty scripts from RDR3 (e.g. 'TEXT_LABEL_63 tlDebugName', 'TEXT_LABEL_31 tlPlaylist' or 'XML_LOADER_GET_TEXT_LABEL_127_RQ')
}
