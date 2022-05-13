namespace ScTools.GameFiles;

using System;

using CodeWalker.GameFiles;

/// <summary>
/// Version 13. Only appears in a GTA IV left-over script: `\common\data\cdimages\navgen_script.img\navgen_main.sco`.
/// </summary>
public class ScriptNYOld
{
    public const uint MagicUnencrypted = 0x0D524353;         // "SCR\x0D"

    public uint Magic { get; set; }
    public uint CodeLength { get; set; }
    public uint StaticsCount { get; set; }
    public uint GlobalsCount { get; set; }
    public uint ArgsCount { get; set; }
    public uint GlobalsSignature { get; set; }
    public byte[] Code { get; set; } = Array.Empty<byte>();
    public ScriptValue32[] Statics { get; set; } = Array.Empty<ScriptValue32>();
    public ScriptValue32[] Globals { get; set; } = Array.Empty<ScriptValue32>();

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
                    Statics = ScriptValue.FromBytes32(reader.ReadBytes((int)(4 * StaticsCount)));
                    Globals = ScriptValue.FromBytes32(reader.ReadBytes((int)(4 * GlobalsCount)));
                }
                break;

            default: throw new InvalidOperationException($"Unknown magic header 0x{Magic:X8}");
        }

        if (reader.Position != reader.Length)
        { } // TODO: navgen_main.sco gets here
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
                    writer.Write(ScriptValue.ToBytes32(Statics));
                    writer.Write(ScriptValue.ToBytes32(Globals));
                }
                break;

            default: throw new InvalidOperationException($"Unknown magic header 0x{Magic:X8}");
        }
    }
}