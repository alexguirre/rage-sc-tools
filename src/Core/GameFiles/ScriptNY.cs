namespace ScTools.GameFiles;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

using CodeWalker.GameFiles;

/// <summary>
/// Version 14. Used in GTA IV and Midnight Club: LA.
/// </summary>
/// <remarks>
/// GTA IV does not support <see cref="MagicUnencrypted"/>.
/// </remarks>
public class ScriptNY
{
    public const uint MagicUnencrypted = 0x0E524353,         // "SCR\x0E"
                      MagicEncrypted = 0x0E726373,           // "scr\x0E"
                      MagicEncryptedCompressed = 0x0E726353; // "Scr\x0E"

    public uint Magic { get; set; }
    public uint CodeLength { get; set; }
    public uint StaticsCount { get; set; }
    public uint GlobalsCount { get; set; }
    public uint ArgsCount { get; set; }
    public uint GlobalsSignature { get; set; }
    public byte[]? Code { get; set; }
    public ScriptValue32[]? Statics { get; set; }
    public ScriptValue32[]? Globals { get; set; }

    public void Read(DataReader reader, byte[]? aesKey)
    {
        Magic = reader.ReadUInt32();
        CodeLength = reader.ReadUInt32();
        StaticsCount = reader.ReadUInt32();
        GlobalsCount = reader.ReadUInt32();
        ArgsCount = reader.ReadUInt32();
        GlobalsSignature = reader.ReadUInt32();

        switch (Magic)
        {
            case MagicUnencrypted:
                {
                    Code = reader.ReadBytes((int)CodeLength);
                    Statics = ScriptValue.FromBytes32(reader.ReadBytes((int)(4 * StaticsCount)));
                    Globals = ScriptValue.FromBytes32(reader.ReadBytes((int)(4 * GlobalsCount)));
                }
                break;

            case MagicEncrypted:
                {
                    Aes.ThrowIfInvalidKey(aesKey);

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

            case MagicEncryptedCompressed:
                {
                    Aes.ThrowIfInvalidKey(aesKey);

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

    public void Write(DataWriter writer, byte[]? aesKey)
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

        switch (Magic)
        {
            case MagicUnencrypted:
                {
                    writer.Write(Code);
                    writer.Write(ScriptValue.ToBytes32(Statics));
                    writer.Write(ScriptValue.ToBytes32(Globals));
                }
                break;

            case MagicEncrypted:
                {
                    Aes.ThrowIfInvalidKey(aesKey);

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

            case MagicEncryptedCompressed:
                {
                    Aes.ThrowIfInvalidKey(aesKey);

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