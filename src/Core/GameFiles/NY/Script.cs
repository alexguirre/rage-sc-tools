namespace ScTools.GameFiles.NY;

using System;
using System.Linq;
using System.Runtime.InteropServices;

using CodeWalker.GameFiles;

public class Script
{
    public const uint MagicUnencrypted = 0x0D524353,         // "SCR\x0D"
                      MagicEncrypted = 0x0E726373,           // "scr\x0E"
                      MagicEncryptedCompressed = 0x0E726353; // "Scr\x0E"

    public uint Magic { get; set; }
    public uint CodeLength { get; set; }
    public uint StaticsCount { get; set; }
    public uint GlobalsCount { get; set; }
    public uint ArgsCount { get; set; }
    public uint GlobalsSignature { get; set; }
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

        switch (Magic)
        {
            case MagicUnencrypted:
                {
                    Code = reader.ReadBytes((int)CodeLength);
                    Statics = BytesToScriptValues(reader.ReadBytes((int)(4 * StaticsCount)));
                    Globals = BytesToScriptValues(reader.ReadBytes((int)(4 * GlobalsCount)));
                }
                break;

            case MagicEncrypted:
                {
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

            case MagicEncryptedCompressed:
                {
                    var compressedSize = reader.ReadUInt32();
                    var compressed = reader.ReadBytes((int)compressedSize);
                    Decrypt(compressed);


                    throw new NotImplementedException();
                }
                break;
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

        switch (Magic)
        {
            case MagicUnencrypted:
                {
                    writer.Write(Code);
                    writer.Write(ScriptValuesToBytes(Statics));
                    writer.Write(ScriptValuesToBytes(Globals));
                }
                break;

            case MagicEncrypted:
                {
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

            case MagicEncryptedCompressed:
                {
                    throw new NotImplementedException();
                }
                break;
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
