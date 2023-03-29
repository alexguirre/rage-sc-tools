namespace ScTools.GameFiles;

using System.IO;
using System.Buffers.Binary;

public class BigEndianBinaryReader : BinaryReader
{
    public BigEndianBinaryReader(Stream input) : base(input) { }
    public BigEndianBinaryReader(Stream input, System.Text.Encoding encoding) : base(input, encoding) { }
    public BigEndianBinaryReader(Stream input, System.Text.Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

    public override short ReadInt16() => BinaryPrimitives.ReverseEndianness(base.ReadInt16());
    public override ushort ReadUInt16() => BinaryPrimitives.ReverseEndianness(base.ReadUInt16());
    public override int ReadInt32() => BinaryPrimitives.ReverseEndianness(base.ReadInt32());
    public override uint ReadUInt32() => BinaryPrimitives.ReverseEndianness(base.ReadUInt32());
    public override long ReadInt64() => BinaryPrimitives.ReverseEndianness(base.ReadInt64());
    public override ulong ReadUInt64() => BinaryPrimitives.ReverseEndianness(base.ReadUInt64());
    public override System.Half ReadHalf() => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReverseEndianness(BitConverter.HalfToUInt16Bits(base.ReadHalf())));
    public override float ReadSingle() => BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToUInt32Bits(base.ReadSingle())));
    public override double ReadDouble() => BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToUInt64Bits(base.ReadDouble())));
    public override decimal ReadDecimal() => throw new NotSupportedException($"{nameof(BigEndianBinaryReader)} doesn't support decimal");
}
