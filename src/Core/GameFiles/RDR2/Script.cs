﻿namespace ScTools.GameFiles.RDR2;

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using ScTools.GameFiles.Crypto;
using ScTools.ScriptAssembly.Targets.RDR2;

/// <summary>
/// "Resource Era" Version 2. Used in Red Dead Redemption.
/// </summary>
public class Script : IScript
{
    public const uint MaxPageLength = 0x4000;

    public const uint MagicEncryptedCompressed = 0x53435202;           // "SCR\x02"
    public const int AesKeyId_None = 0;
    public const int AesKeyId_Default = -3;

    public uint Magic { get; set; }
    public uint GlobalsSignature { get; set; }
    public int AesKeyId { get; set; }
    public uint CodeLength { get; set; }
    public uint StaticsCount { get; set; }
    public uint GlobalsCount { get; set; }
    public uint ArgsCount { get; set; }
    public uint NativesCount { get; set; }
    public uint Unknown_24h { get; set; }
    public uint Unknown_28h { get; set; }
    public uint Unknown_2Ch { get; set; }
    public byte[][] CodePages { get; set; } = Array.Empty<byte[]>();
    public uint[] Natives { get; set; } = Array.Empty<uint>();
    public ScriptValue32[] Statics { get; set; } = Array.Empty<ScriptValue32>();
    public ScriptValue32[] Globals { get; set; } = Array.Empty<ScriptValue32>();

    public void Read(BinaryReader reader, byte[]? aesKey)
    {
        Debug.Assert(reader is BigEndianBinaryReader, "Expected BE reader");

        Magic = reader.ReadUInt32();
        GlobalsSignature = reader.ReadUInt32();
        var compressedSize = reader.ReadUInt32();
        AesKeyId = reader.ReadInt32();
        CodeLength = reader.ReadUInt32();
        StaticsCount = reader.ReadUInt32();
        GlobalsCount = reader.ReadUInt32();
        ArgsCount = reader.ReadUInt32();
        NativesCount = reader.ReadUInt32();
        Unknown_24h = reader.ReadUInt32();
        Unknown_28h = reader.ReadUInt32();
        Unknown_2Ch = reader.ReadUInt32();

        switch (Magic)
        {
            case MagicEncryptedCompressed:
                {
                    var compressed = reader.ReadBytes((int)compressedSize);
                    switch (AesKeyId)
                    {
                        case AesKeyId_None:
                            break;
                        case AesKeyId_Default:
                            Aes.ThrowIfInvalidKey(aesKey);
                            Aes.Decrypt(compressed, aesKey);
                            break;
                        default: throw new InvalidOperationException($"Unknown AES key ID {AesKeyId}");
                    }

                    using var decompressed = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);

                    uint numberOfCodePages = (CodeLength + (MaxPageLength - 1)) / MaxPageLength;
                    CodePages = new byte[numberOfCodePages][];
                    for (int i = 0; i < numberOfCodePages - 1; i++)
                    {
                        CodePages[i] = new byte[MaxPageLength];
                    }
                    CodePages[^1] = new byte[CodeLength & (MaxPageLength - 1)];
                    Natives = new uint[NativesCount];
                    Statics = new ScriptValue32[StaticsCount];
                    Globals = new ScriptValue32[GlobalsCount];
                    foreach (var codePage in CodePages)
                    {
                        decompressed.ReadExactly(codePage);
                    }
                    decompressed.ReadExactly(MemoryMarshal.AsBytes(Natives.AsSpan()));
                    decompressed.ReadExactly(MemoryMarshal.AsBytes(Statics.AsSpan()));
                    decompressed.ReadExactly(MemoryMarshal.AsBytes(Globals.AsSpan()));

                    ReverseEndiannessOfBytecode(CodePages);
                    ReverseEndianness(Natives);
                    ReverseEndianness(Statics);
                    ReverseEndianness(Globals);
                }
                break;

            default: throw new InvalidOperationException($"Unknown magic header 0x{Magic:X8}");
        }

        Debug.Assert(reader.BaseStream.Position == reader.BaseStream.Length, "Not all data was read");
    }

    public void Write(BinaryWriter writer, byte[] aesKey)
    {
        // TODO: ScriptRDR2
    }

    public byte[] MergeCodePages()
    {
        if (CodePages is null)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[CodeLength];
        var offset = 0;
        foreach (var page in CodePages)
        {
            page.CopyTo(buffer.AsSpan(offset));
            offset += page.Length;
        }
        return buffer;
    }

    private static void ReverseEndianness(Span<ScriptValue32> span) => ReverseEndianness(MemoryMarshal.Cast<ScriptValue32, uint>(span));
    private static void ReverseEndianness(Span<uint> span)
    {
        foreach (ref uint n in span)
        {
            n = BinaryPrimitives.ReverseEndianness(n);
        }
    }

    private static void ReverseEndiannessOfBytecode(byte[][] codePages)
    {
        foreach (var page in codePages)
        {
            for (int i = 0; i < page.Length;)
            {
                var opcode = (Opcode)page[i];
                if (opcode is Opcode.STRING_U32)
                {
                    // need to reverse the U32 string length to get the correct instruction length in GetInstructionSpan
                    // no other opcode needs correct endiannes for calculating the length
                    var s = page.AsSpan(i);
                    BinaryPrimitives.WriteUInt32LittleEndian(s[1..], BinaryPrimitives.ReverseEndianness(opcode.GetU32Operand(s)));
                }

                var inst = OpcodeTraits.GetInstructionSpan(page, i);
                switch (opcode)
                {
                    case Opcode.PUSH_CONST_S16:
                    case Opcode.IADD_S16:
                    case Opcode.IOFFSET_S16_LOAD:
                    case Opcode.IOFFSET_S16_STORE:
                    case Opcode.IMUL_S16:
                    case >= Opcode.J and <= Opcode.ILE_JZ:
                        var jumpOffset = opcode.GetS16Operand(inst);
                        BinaryPrimitives.WriteInt16LittleEndian(inst[1..], BinaryPrimitives.ReverseEndianness(jumpOffset));
                        break;

                    case Opcode.ARRAY_U16:
                    case Opcode.ARRAY_U16_LOAD:
                    case Opcode.ARRAY_U16_STORE:
                    case Opcode.LOCAL_U16:
                    case Opcode.LOCAL_U16_LOAD:
                    case Opcode.LOCAL_U16_STORE:
                    case Opcode.STATIC_U16:
                    case Opcode.STATIC_U16_LOAD:
                    case Opcode.STATIC_U16_STORE:
                    case Opcode.GLOBAL_U16:
                    case Opcode.GLOBAL_U16_LOAD:
                    case Opcode.GLOBAL_U16_STORE:
                    case >= Opcode.CALL_0 and <= Opcode.CALL_F:
                        var callOffset = opcode.GetU16Operand(inst);
                        BinaryPrimitives.WriteUInt16LittleEndian(inst[1..], BinaryPrimitives.ReverseEndianness(callOffset));
                        break;

                    case Opcode.PUSH_CONST_U24:
                    case Opcode.GLOBAL_U24:
                    case Opcode.GLOBAL_U24_LOAD:
                    case Opcode.GLOBAL_U24_STORE:
                        var hi = inst[1];
                        var lo = inst[3];
                        inst[1] = lo;
                        inst[3] = hi;
                        break;

                    case Opcode.PUSH_CONST_U32:
                    case Opcode.PUSH_CONST_F:
                        BinaryPrimitives.WriteUInt32LittleEndian(inst[1..], BinaryPrimitives.ReverseEndianness(opcode.GetU32Operand(inst)));
                        break;


                    case Opcode.SWITCH:
                        foreach (var (caseValue, caseJumpOffset, offsetWithinInstruction) in opcode.GetSwitchOperands(inst))
                        {
                            BinaryPrimitives.WriteUInt32LittleEndian(inst[offsetWithinInstruction..], BinaryPrimitives.ReverseEndianness(caseValue));
                            BinaryPrimitives.WriteInt16LittleEndian(inst[(offsetWithinInstruction + 4)..], BinaryPrimitives.ReverseEndianness(caseJumpOffset));
                        }
                        break;

                    case Opcode.ENTER:
                        var enter = opcode.GetEnterOperands(inst);
                        BinaryPrimitives.WriteUInt16LittleEndian(inst[2..], BinaryPrimitives.ReverseEndianness(enter.FrameSize));
                        break;
                }

                i += inst.Length;
            }
        }
    }

    public void Dump(TextWriter sink, DumpOptions options) => Dumper.Dump(this, sink, options);
}
