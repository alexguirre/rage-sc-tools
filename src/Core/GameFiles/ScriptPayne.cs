namespace ScTools.GameFiles;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

using CodeWalker.GameFiles;

/// <summary>
/// Version 16 and 17. Used in Max Payne 3.
/// </summary>
public class ScriptPayne
{
    public const uint MagicEncryptedV16 = 0x10726373,           // "scr\x10"
                      MagicEncryptedCompressedV16 = 0x10726353; // "Scr\x10"
    public const uint MagicEncryptedV17 = 0x11726373,           // "scr\x11"
                      MagicEncryptedCompressedV17 = 0x11726353; // "Scr\x11"

    public uint Magic { get; set; }
    public uint CodeLength { get; set; }
    public uint StaticsCount { get; set; }
    public uint GlobalsCount { get; set; }
    public uint ArgsCount { get; set; }
    public uint GlobalsSignature { get; set; }
    /// <summary>
    /// Added in version 17. In version 16 it is set to <c>0xFFFFFFFF</c>.
    /// </summary>
    public uint Unknown_18h { get; set; }
    public byte[] Code { get; set; } = Array.Empty<byte>();
    public ScriptValue32[] Statics { get; set; } = Array.Empty<ScriptValue32>();
    public ScriptValue32[] Globals { get; set; } = Array.Empty<ScriptValue32>();

    public void Read(DataReader reader, byte[] aesKey)
    {
        Aes.ThrowIfInvalidKey(aesKey);

        Magic = reader.ReadUInt32();
        CodeLength = reader.ReadUInt32();
        StaticsCount = reader.ReadUInt32();
        GlobalsCount = reader.ReadUInt32();
        ArgsCount = reader.ReadUInt32();
        GlobalsSignature = reader.ReadUInt32();

        if (Magic is MagicEncryptedV17 or MagicEncryptedCompressedV17)
        {
            Unknown_18h = reader.ReadUInt32();
        }
        else
        {
            Unknown_18h = 0xFFFFFFFF;
        }

        switch (Magic)
        {
            case MagicEncryptedV16:
            case MagicEncryptedV17:
                {
                    Code = reader.ReadBytes((int)CodeLength);
                    var statics = reader.ReadBytes((int)(4 * StaticsCount));
                    var globals = reader.ReadBytes((int)(4 * GlobalsCount));
                    Aes.Decrypt(Code, aesKey);
                    Aes.Decrypt(statics, aesKey);
                    Aes.Decrypt(globals, aesKey);
                    Statics = ScriptValue.FromBytes32(statics);
                    Globals = ScriptValue.FromBytes32(globals);
                }
                break;

            case MagicEncryptedCompressedV16:
            case MagicEncryptedCompressedV17:
                {
                    var compressedSize = reader.ReadUInt32();
                    var compressed = reader.ReadBytes((int)compressedSize);
                    Aes.Decrypt(compressed, aesKey);

                    using var zs = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);
                    using var decompressed = new MemoryStream();
                    zs.CopyTo(decompressed);

                    decompressed.Position = 0;
                    Code = new byte[CodeLength];
                    Statics = new ScriptValue32[StaticsCount];
                    Globals = new ScriptValue32[GlobalsCount];
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

    public void Write(DataWriter writer, byte[] aesKey)
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

        if (Magic is MagicEncryptedV17 or MagicEncryptedCompressedV17)
        {
            writer.Write(Unknown_18h);
        }

        switch (Magic)
        {
            case MagicEncryptedV16:
            case MagicEncryptedV17:
                {
                    var code = Code?.ToArray() ?? Array.Empty<byte>();
                    var statics = ScriptValue.ToBytes32(Statics);
                    var globals = ScriptValue.ToBytes32(Globals);
                    Aes.Encrypt(code, aesKey);
                    Aes.Encrypt(statics, aesKey);
                    Aes.Encrypt(globals, aesKey);
                    writer.Write(code);
                    writer.Write(statics);
                    writer.Write(globals);
                }
                break;

            case MagicEncryptedCompressedV16:
            case MagicEncryptedCompressedV17:
                {
                    using var compressedStream = new MemoryStream();
                    using (var zs = new ZLibStream(compressedStream, CompressionLevel.SmallestSize, leaveOpen: true))
                    {
                        zs.Write(Code.AsSpan());
                        zs.Write(MemoryMarshal.AsBytes(Statics.AsSpan()));
                        zs.Write(MemoryMarshal.AsBytes(Globals.AsSpan()));
                    }

                    var compressed = compressedStream.ToArray();
                    Aes.Encrypt(compressed, aesKey);

                    writer.Write(compressed.Length);
                    writer.Write(compressed);
                }
                break;

            default: throw new InvalidOperationException($"Unknown magic header 0x{Magic:X8}");
        }
    }
}
