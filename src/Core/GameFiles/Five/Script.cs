namespace ScTools.GameFiles.Five;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeWalker.GameFiles;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Linq;

public class Script : ResourceFileBase
{
    public const uint MaxPageLength = 0x4000;

    public override long BlockLength => 128;

    // structure data
    public ulong CodePagesPointer { get; set; }
    public uint GlobalsSignature { get; set; }
    public uint CodeLength { get; set; }
    public uint ArgsCount { get; set; }
    public uint StaticsCount { get; set; }
    public uint GlobalsLengthAndBlock { get; set; }
    public uint NativesCount { get; set; }
    public ulong StaticsPointer { get; set; }
    public ulong GlobalsPagesPointer { get; set; }
    public ulong NativesPointer { get; set; }
    public long Unknown_48h { get; set; }
    public long Unknown_50h { get; set; }
    public uint NameHash { get; set; }
    public uint NumRefs { get; set; } = 1; // always 1, used by the game at runtime
    public ulong NamePointer { get; set; }
    public ulong StringsPagesPointer { get; set; }
    public uint StringsLength { get; set; }
    public uint Unknown_74h { get; set; }
    public long Unknown_78h { get; set; }

    // reference data
    public ScriptPageTable<byte>? CodePages { get; set; }
    public ScriptValue64[]? Statics { get; set; }
    public ScriptPageTable<ScriptValue64>? GlobalsPages { get; set; }
    public ulong[]? Natives { get; set; }
    public string? Name { get; set; }
    public ScriptPageTable<byte>? StringsPages { get; set; }

    private ResourceSystemStructBlock<ScriptValue64>? staticsBlock;
    private ResourceSystemStructBlock<ulong>? nativesBlock;
    private string_r? nameBlock;

    public uint GlobalsLength
    {
        get => GlobalsLengthAndBlock & 0x3FFFF;
        set => GlobalsLengthAndBlock = (GlobalsLengthAndBlock & 0xFFFC0000) | (value & 0x3FFFF);
    }

    public uint GlobalsBlock
    {
        get => GlobalsLengthAndBlock >> 18;
        set => GlobalsLengthAndBlock = GlobalsLength | (value << 18);
    }

    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        base.Read(reader, parameters);

        // read structure data
        CodePagesPointer = reader.ReadUInt64();
        GlobalsSignature = reader.ReadUInt32();
        CodeLength = reader.ReadUInt32();
        ArgsCount = reader.ReadUInt32();
        StaticsCount = reader.ReadUInt32();
        GlobalsLengthAndBlock = reader.ReadUInt32();
        NativesCount = reader.ReadUInt32();
        StaticsPointer = reader.ReadUInt64();
        GlobalsPagesPointer = reader.ReadUInt64();
        NativesPointer = reader.ReadUInt64();
        Unknown_48h = reader.ReadInt64();
        Unknown_50h = reader.ReadInt64();
        NameHash = reader.ReadUInt32();
        NumRefs = reader.ReadUInt32();
        NamePointer = reader.ReadUInt64();
        StringsPagesPointer = reader.ReadUInt64();
        StringsLength = reader.ReadUInt32();
        Unknown_74h = reader.ReadUInt32();
        Unknown_78h = reader.ReadInt64();

        // read reference data
        CodePages = reader.ReadBlockAt<ScriptPageTable<byte>>(CodePagesPointer, CodeLength);
        Statics = reader.ReadStructsAt<ScriptValue64>(StaticsPointer, StaticsCount);
        GlobalsPages = reader.ReadBlockAt<ScriptPageTable<ScriptValue64>>(GlobalsPagesPointer, GlobalsLength);
        Natives = reader.ReadUlongsAt(NativesPointer, NativesCount);
        Name = reader.ReadStringAt(NamePointer);
        StringsPages = reader.ReadBlockAt<ScriptPageTable<byte>>(StringsPagesPointer, StringsLength);
    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        base.Write(writer, parameters);

        // update structure data
        CodePagesPointer = (ulong)(CodePages?.FilePosition ?? 0);
        CodeLength = CodePages?.Length ?? 0;
        StaticsPointer = (ulong)(staticsBlock?.FilePosition ?? 0);
        StaticsCount = (uint)(staticsBlock?.ItemCount ?? 0);
        GlobalsPagesPointer = (ulong)(GlobalsPages?.FilePosition ?? 0);
        GlobalsLength = GlobalsPages?.Length ?? 0;
        NativesPointer = (ulong)(nativesBlock?.FilePosition ?? 0);
        NativesCount = (uint)(nativesBlock?.ItemCount ?? 0);
        NamePointer = (ulong)(nameBlock?.FilePosition ?? 0);
        StringsPagesPointer = (ulong)(StringsPages?.FilePosition ?? 0);
        StringsLength = StringsPages?.Length ?? 0;

        // write structure data
        writer.Write(CodePagesPointer);
        writer.Write(GlobalsSignature);
        writer.Write(CodeLength);
        writer.Write(ArgsCount);
        writer.Write(StaticsCount);
        writer.Write(GlobalsLengthAndBlock);
        writer.Write(NativesCount);
        writer.Write(StaticsPointer);
        writer.Write(GlobalsPagesPointer);
        writer.Write(NativesPointer);
        writer.Write(Unknown_48h);
        writer.Write(Unknown_50h);
        writer.Write(NameHash);
        writer.Write(NumRefs);
        writer.Write(NamePointer);
        writer.Write(StringsPagesPointer);
        writer.Write(StringsLength);
        writer.Write(Unknown_74h);
        writer.Write(Unknown_78h);
    }

    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>(base.GetReferences());

        if (CodePages != null && CodePages.NumberOfPages > 0)
        {
            list.Add(CodePages);
        }

        if (Statics != null && Statics.Length > 0)
        {
            staticsBlock = new ResourceSystemStructBlock<ScriptValue64>(Statics);
            list.Add(staticsBlock);
        }
        else
        {
            staticsBlock = null;
        }

        if (GlobalsPages != null && GlobalsPages.NumberOfPages > 0)
        {
            list.Add(GlobalsPages);
        }

        if (Natives != null && Natives.Length > 0)
        {
            nativesBlock = new ResourceSystemStructBlock<ulong>(Natives);
            list.Add(nativesBlock);
        }
        else
        {
            nativesBlock = null;
        }

        if (!string.IsNullOrEmpty(Name))
        {
            nameBlock = (string_r)Name;
            list.Add(nameBlock);
        }
        else
        {
            nameBlock = null;
        }

        if (StringsPages != null && StringsPages.NumberOfPages > 0)
        {
            list.Add(StringsPages);
        }

        return list.ToArray();
    }

    public ref byte IP(uint ip) => ref CodePages[(int)(ip >> 14)][(int)(ip & 0x3FFF)];
    public ref T IP<T>(uint ip) where T : unmanaged => ref Unsafe.As<byte, T>(ref IP(ip));

    public ulong NativeHash(int index)
    {
        if (index < 0 ||index >= NativesCount)
        {
            throw new IndexOutOfRangeException();
        }

        return DecodeNativeHash(Natives[index], index, CodeLength);
    }

    public static ulong DecodeNativeHash(ulong hash, int index, uint codeLength)
    {
        // from: https://gtamods.com/wiki/Script_Container#Native_Functions
        var rotate = (index + (int)codeLength) & 0x3F;
        return hash << rotate | hash >> (64 - rotate);
    }

    public static ulong EncodeNativeHash(ulong hash, int index, uint codeLength)
    {
        var rotate = (index + (int)codeLength) & 0x3F;
        return hash >> rotate | hash << (64 - rotate);
    }

    public string String(uint id) => Encoding.UTF8.GetString(StringChars(id));
    public unsafe ReadOnlySpan<byte> StringChars(uint id)
    {
        var page = StringsPages[(int)(id >> 14)];
        int pageOffset = (int)(id & 0x3FFF);
            
        void* start = Unsafe.AsPointer(ref page[pageOffset]);
        int length = 0;

        while (page[pageOffset + length] != 0) { length++; }

        return new ReadOnlySpan<byte>(start, length);
    }

    public IEnumerable<uint> StringIds()
    {
        if (StringsPages == null)
        {
            yield break;
        }

        uint pageIndex = 0;
        uint stringId = 0;
        bool inString = false;
        foreach (var page in StringsPages)
        {
            uint pageOffset = 0;
            foreach (byte b in page.Data)
            {
                if (inString)
                {
                    if (b == 0) // null terminator
                    {
                        yield return stringId;
                        inString = false;
                    }
                }
                else
                {
                    stringId = (pageIndex << 14) | (pageOffset & 0x3FFF);
                    if (b == 0) // found empty string
                    {
                        yield return stringId;
                    }
                    else
                    {
                        inString = true;
                    }
                }

                pageOffset++;
            }

            pageIndex++;
        }
    }
}

public class ScriptPage<T> : ResourceSystemBlock where T : unmanaged
{
    public override unsafe long BlockLength => (Data?.Length ?? 0) * sizeof(T);

    public T[] Data { get; set; } = Array.Empty<T>();
    
    public ref T this[int index] => ref Data[index];

    public override unsafe void Read(ResourceDataReader reader, params object[] parameters)
    {
        uint length = Convert.ToUInt32(parameters[0]);

        var bytes = reader.ReadBytes((int)(length * sizeof(T)));
        Data = new T[length];
        bytes.CopyTo(MemoryMarshal.AsBytes(Data.AsSpan()));
    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        Debug.Assert(Data != null && Data.Length <= Script.MaxPageLength);

        writer.Write(MemoryMarshal.AsBytes(Data.AsSpan()).ToArray());
    }
}

public class ScriptPageTable<T> : ResourceSystemBlock, IEnumerable<ScriptPage<T>> where T : unmanaged
{
    public override long BlockLength => 8 * NumberOfPages;

    public ulong[] Pointers { get; set; } = Array.Empty<ulong>();
    public ScriptPage<T>[] Pages { get; set; } = Array.Empty<ScriptPage<T>>();

    public int NumberOfPages => Pages?.Length ?? 0;

    public ref ScriptPage<T> this[int index] => ref Pages[index];

    /// <summary>
    /// Gets the total length of all the pages combined.
    /// </summary>
    public uint Length => (uint)(Pages?.Sum(i => i.Data?.Length ?? 0) ?? 0);

    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        uint length = Convert.ToUInt32(parameters[0]);

        uint numberOfPages = (length + 0x3FFF) >> 14;
        Pointers = reader.ReadUlongsAt((ulong)reader.Position, numberOfPages, false);


        Pages = new ScriptPage<T>[numberOfPages];
        for (int i = 0; i < numberOfPages; i++)
        {
            uint pageLength = i == numberOfPages - 1 ? (length & (Script.MaxPageLength - 1)) : Script.MaxPageLength;
            Pages[i] = reader.ReadBlockAt<ScriptPage<T>>(Pointers[i], pageLength);
        }
    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update...
        var list = new List<ulong>();
        foreach (var x in Pages)
        {
            if (x != null)
            {
                list.Add((ulong)x.FilePosition);
            }
            else
            {
                list.Add(0);
            }
        }
        Pointers = list.ToArray();


        // write...
        foreach (var x in Pointers)
            writer.Write(x);
    }

    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();
            
        foreach (var x in Pages)
        {
            list.Add(x);
        }

        return list.ToArray();
    }

    public IEnumerator<ScriptPage<T>> GetEnumerator() => ((IEnumerable<ScriptPage<T>>)Pages).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Pages.GetEnumerator();
}
