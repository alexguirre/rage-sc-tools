namespace ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;

public sealed record TextLabelType(int Length) : TypeInfo
{
    public const int MinLength = 8;
    public const int MaxLength = 248;

    public override int SizeOf => Length / 8;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;
    public int Length { get; init; } = IsValidLength(Length) ? Length : throw new ArgumentException("Invalid length", nameof(Length));

    public override string ToPrettyString() => GetTypeNameForLength(Length);
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    public static ImmutableArray<TextLabelType> All { get; }

    static TextLabelType()
    {
        var arr = ImmutableArray.CreateBuilder<TextLabelType>(initialCapacity: (MaxLength - MinLength) / 8 + 1);
        for (int length = MinLength; length <= MaxLength; length += 8)
        {
            arr.Add(new TextLabelType(length));
        }
        All = arr.MoveToImmutable();
    }

    /// <returns>
    /// <c>true</c> if <paramref name="length"/> is in the range [<see cref="MinLength"/>, <see cref="MaxLength"/>]
    /// and is a multiple of 8; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidLength(int length)
        => length is >= MinLength and <= MaxLength && (length % 8) == 0;

    public static string GetTypeNameForLength(int length)
    {
        if (!IsValidLength(length))
        {
            throw new ArgumentException("Invalid length", nameof(length));
        }

        return $"TEXT_LABEL_{length - 1}"; // real type name found in tty scripts from RDR3 (e.g. 'TEXT_LABEL_63 tlDebugName', 'TEXT_LABEL_31 tlPlaylist' or 'XML_LOADER_GET_TEXT_LABEL_127_RQ')
    }
}
