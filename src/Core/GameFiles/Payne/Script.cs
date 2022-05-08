namespace ScTools.GameFiles.Payne;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

using CodeWalker.GameFiles;

public class Script
{
    public const uint MagicEncryptedV10 = 0x10726373,           // "scr\x10"
                      MagicEncryptedCompressedV10 = 0x10726353, // "Scr\x10"
                      MagicEncryptedV11 = 0x11726373,           // "scr\x11"
                      MagicEncryptedCompressedV11 = 0x11726353; // "Scr\x11"

    public uint Magic { get; set; }
    public uint CodeLength { get; set; }
    public uint StaticsCount { get; set; }
    public uint GlobalsCount { get; set; }
    public uint ArgsCount { get; set; }
    public uint GlobalsSignature { get; set; }
    public uint Unknown_V11 { get; set; }
    public byte[]? Code { get; set; }
    public ScriptValue[]? Statics { get; set; }
    public ScriptValue[]? Globals { get; set; }

    public void Read(DataReader reader)
    {
        Magic = reader.ReadUInt32();
        CodeLength = reader.ReadUInt32();
        StaticsCount = reader.ReadUInt32();
        GlobalsCount = reader.ReadUInt32();
        ArgsCount = reader.ReadUInt32();
        GlobalsSignature = reader.ReadUInt32();

        if (Magic is MagicEncryptedV11 or MagicEncryptedCompressedV11)
        {
            Unknown_V11 = reader.ReadUInt32();
        }
        else
        {
            Unknown_V11 = 0xFFFFFFFF;
        }

        var compressedSize = reader.ReadUInt32();
        switch (Magic)
        {
            case MagicEncryptedV10:
            case MagicEncryptedV11:
                {
                    if (compressedSize != 0)
                    { }

                    Code = reader.ReadBytes((int)CodeLength);
                    var statics = reader.ReadBytes((int)(4 * StaticsCount));
                    var globals = reader.ReadBytes((int)(4 * GlobalsCount));
                    Decrypt(Code);
                    Decrypt(statics);
                    Decrypt(globals);
                    Statics = BytesToScriptValues(statics);
                    Globals = BytesToScriptValues(globals);
                }
                break;

            case MagicEncryptedCompressedV10:
            case MagicEncryptedCompressedV11:
                {
                    var compressed = reader.ReadBytes((int)compressedSize);
                    Decrypt(compressed);

                    using var zs = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);
                    using var decompressed = new MemoryStream();
                    zs.CopyTo(decompressed);

                    decompressed.Position = 0;
                    Code = new byte[CodeLength];
                    Statics = new ScriptValue[StaticsCount];
                    Globals = new ScriptValue[GlobalsCount];
                    decompressed.Read(Code, 0, Code.Length);
                    decompressed.Read(MemoryMarshal.AsBytes(Statics.AsSpan()));
                    decompressed.Read(MemoryMarshal.AsBytes(Globals.AsSpan()));
                }
                break;

            default: throw new InvalidOperationException($"Unknown magic header 0x{Magic:X8}");
        }

        if (reader.Position != reader.Length)
        { }
    }

    public void Write(DataWriter writer)
    {
        // update structure data
        CodeLength = (uint)(Code?.Length ?? 0);
        StaticsCount = (uint)(Statics?.Length ?? 0);
        GlobalsCount = (uint)(Globals?.Length ?? 0);

        writer.Write(Magic);
        writer.Write(CodeLength);
        writer.Write(StaticsCount);
        writer.Write(GlobalsCount);
        writer.Write(ArgsCount);
        writer.Write(GlobalsSignature);

        if (Magic is MagicEncryptedV11 or MagicEncryptedCompressedV11)
        {
            writer.Write(Unknown_V11);
        }

        switch (Magic)
        {
            case MagicEncryptedV10:
            case MagicEncryptedV11:
                {
                    writer.Write(0u); // compressedSize
                    var code = Code?.ToArray() ?? Array.Empty<byte>();
                    var statics = ScriptValuesToBytes(Statics);
                    var globals = ScriptValuesToBytes(Globals);
                    Encrypt(code);
                    Encrypt(statics);
                    Encrypt(globals);
                    writer.Write(code);
                    writer.Write(statics);
                    writer.Write(globals);
                }
                break;

            case MagicEncryptedCompressedV10:
            case MagicEncryptedCompressedV11:
                {
                    using var compressedStream = new MemoryStream();
                    using (var zs = new ZLibStream(compressedStream, CompressionLevel.SmallestSize, leaveOpen: true))
                    {
                        zs.Write(Code.AsSpan());
                        zs.Write(MemoryMarshal.AsBytes(Statics.AsSpan()));
                        zs.Write(MemoryMarshal.AsBytes(Globals.AsSpan()));
                    }

                    var compressed = compressedStream.ToArray();
                    Encrypt(compressed);

                    writer.Write(compressed.Length);
                    writer.Write(compressed);
                }
                break;

            default: throw new InvalidOperationException($"Unknown magic header 0x{Magic:X8}");
        }
    }

    private static unsafe ScriptValue[] BytesToScriptValues(byte[]? buffer)
    {
        if (buffer is null) return Array.Empty<ScriptValue>();

        var result = new ScriptValue[buffer.Length / sizeof(ScriptValue)];
        fixed (void* bufferPtr = buffer)
        fixed (void* resultPtr = result)
        {
            Buffer.MemoryCopy(source: bufferPtr, destination: resultPtr, buffer.Length, buffer.Length);
        }
        return result;
    }

    private static unsafe byte[] ScriptValuesToBytes(ScriptValue[]? values)
    {
        if (values is null) return Array.Empty<byte>();

        var result = new byte[values.Length * sizeof(ScriptValue)];
        fixed (void* valuesPtr = values)
        fixed (void* resultPtr = result)
        {
            Buffer.MemoryCopy(source: valuesPtr, destination: resultPtr, result.Length, result.Length);
        }
        return result;
    }

    private static void Encrypt(byte[] data) => Aes.Encrypt(data, Keys.AesKey);
    private static void Decrypt(byte[] data) => Aes.Decrypt(data, Keys.AesKey);
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct ScriptValue
{
    [FieldOffset(0)] public float AsFloat;
    [FieldOffset(0)] public int AsInt32;
    [FieldOffset(0)] public uint AsUInt32;
}
