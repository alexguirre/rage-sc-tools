namespace ScTools.GameFiles
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using CodeWalker.GameFiles;
    using System.Text;
    using System.Diagnostics;

    internal class Script : ResourceFileBase
    {
        public const uint MaxPageLength = 0x4000;

        public override long BlockLength => 128;

        // structure data
        public ulong CodePagesPointer { get; set; }
        public uint Hash { get; set; }
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
        public uint NumRefs { get; set; } = 1; // always 1
        public ulong NamePointer { get; set; }
        public ulong StringsPagesPointer { get; set; }
        public uint StringsLength { get; set; }
        public uint Unknown_74h { get; set; }
        public long Unknown_78h { get; set; }

        // reference data
        public ResourcePointerArray64<ScriptPage> CodePages { get; set; }
        public ScriptValue[] Statics { get; set; }
        public ScriptValue[][] GlobalsPages { get; set; }
        public ulong[] Natives { get; set; }
        public string Name { get; set; }
        public ResourcePointerArray64<ScriptPage> StringsPages { get; set; }

        private ResourceSystemStructBlock<ScriptValue> staticsBlock;
        private ResourceSystemStructBlock<ScriptValue>[] globalsPagesBlocks;
        private ResourceSystemStructBlock<ulong> globalsPagesPointersBlock;
        private ResourceSystemStructBlock<ulong> nativesBlock;
        private string_r nameBlock;

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            CodePagesPointer = reader.ReadUInt64();
            Hash = reader.ReadUInt32();
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
            uint codePagesCount = (CodeLength + 0x3FFF) >> 14;
            CodePages = reader.ReadBlockAt<ResourcePointerArray64<ScriptPage>>(CodePagesPointer, codePagesCount);

            Statics = reader.ReadStructsAt<ScriptValue>(StaticsPointer, StaticsCount);

            uint globalsPagesCount = ((GlobalsLengthAndBlock & 0x3FFFF) + 0x3FFF) >> 14;
            ulong[] globalsPagesPtrs = reader.ReadUlongsAt(GlobalsPagesPointer, globalsPagesCount, false);
            GlobalsPages = new ScriptValue[globalsPagesCount][];
            for (int i = 0; i < globalsPagesCount; i++)
            {
                uint pageSize = i == globalsPagesCount - 1 ? (uint)((GlobalsLengthAndBlock & 0x3FFFF) - (i << 14)) : MaxPageLength;

                GlobalsPages[i] = reader.ReadStructsAt<ScriptValue>(globalsPagesPtrs[i], pageSize, false);
            }

            Natives = reader.ReadUlongsAt(NativesPointer, NativesCount);
            Name = reader.ReadStringAt(NamePointer);
            
            uint stringPagesCount = (StringsLength + 0x3FFF) >> 14;
            StringsPages = reader.ReadBlockAt<ResourcePointerArray64<ScriptPage>>(StringsPagesPointer, stringPagesCount);
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            CodePagesPointer = (ulong)(CodePages?.FilePosition ?? 0);
            StaticsPointer = (ulong)(staticsBlock?.FilePosition ?? 0);
            StaticsCount = (uint)(staticsBlock?.ItemCount ?? 0);
            GlobalsPagesPointer = (ulong)(globalsPagesPointersBlock?.FilePosition ?? 0);
            GlobalsLengthAndBlock &= 0xFFFC0000; // keep the global block index
            if (globalsPagesPointersBlock != null && globalsPagesBlocks != null)
            {
                for (int i = 0; i < globalsPagesPointersBlock.Items.Length; i++)
                {
                    globalsPagesPointersBlock.Items[i] = (ulong)globalsPagesBlocks[i].FilePosition;
                    GlobalsLengthAndBlock += (uint)globalsPagesBlocks[i].ItemCount;
                }
            }
            NativesPointer = (ulong)(nativesBlock?.FilePosition ?? 0);
            NativesCount = (uint)(nativesBlock?.ItemCount ?? 0);
            NamePointer = (ulong)(nameBlock?.FilePosition ?? 0);
            StringsPagesPointer = (ulong)(StringsPages?.FilePosition ?? 0);

            // write structure data
            writer.Write(CodePagesPointer);
            writer.Write(Hash);
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

            if (CodePages != null && CodePages.Count > 0)
            {
                list.Add(CodePages);
            }

            if (Statics != null && Statics.Length > 0)
            {
                staticsBlock = new ResourceSystemStructBlock<ScriptValue>(Statics);
                list.Add(staticsBlock);
            }
            else
            {
                staticsBlock = null;
            }

            if (GlobalsPages != null && GlobalsPages.Length > 0)
            {
                globalsPagesBlocks = new ResourceSystemStructBlock<ScriptValue>[GlobalsPages.Length];
                for (int i = 0; i < GlobalsPages.Length; i++)
                {
                    globalsPagesBlocks[i] = new ResourceSystemStructBlock<ScriptValue>(GlobalsPages[i]);
                    list.Add(globalsPagesBlocks[i]);
                }

                globalsPagesPointersBlock = new ResourceSystemStructBlock<ulong>(new ulong[GlobalsPages.Length]);
                list.Add(globalsPagesPointersBlock);
            }
            else
            {
                globalsPagesBlocks = null;
                globalsPagesPointersBlock = null;
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

            if (StringsPages != null && StringsPages.Count > 0)
            {
                list.Add(StringsPages);
            }

            return list.ToArray();
        }

        public ref byte IP(uint ip) => ref CodePages[(int)(ip >> 14)].Data[ip & 0x3FFF];
        public ref T IP<T>(uint ip) where T : unmanaged => ref Unsafe.As<byte, T>(ref IP(ip));

        public string String(uint id) => Encoding.UTF8.GetString(StringChars(id));
        public unsafe ReadOnlySpan<byte> StringChars(uint id)
        {
            ScriptPage page = StringsPages[(int)(id >> 14)];
            uint pageOffset = id & 0x3FFF;
            
            void* start = Unsafe.AsPointer(ref page.Data[pageOffset]);
            int length = 0;

            while (page.Data[pageOffset + length] != 0) { length++; }

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
            foreach (ScriptPage page in StringsPages.data_items)
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
                        if (b != 0) // null terminator
                        {
                            inString = true;
                            stringId = (pageIndex << 14) | (pageOffset & 0x3FFF);
                        }
                    }

                    pageOffset++;
                }

                pageIndex++;
            }
        }
    }

    public class ScriptPage : ResourceSystemBlock
    {
        public override long BlockLength => Script.MaxPageLength;

        public byte[] Data { get; set; }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            Data = reader.ReadBytes((int)Script.MaxPageLength);
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            Debug.Assert(Data != null && Data.Length == Script.MaxPageLength);

            writer.Write(Data);
        }
    }

    public struct ScriptValue
    {
        private ValueUnion union;

        public float AsFloat
        {
            get => union.AsFloat;
            set => union.AsFloat = value;
        }

        public int AsInt32
        {
            get => union.AsInt32;
            set => union.AsInt32 = value;
        }

        public long AsInt64
        {
            get => union.AsInt64;
            set => union.AsInt64 = value;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct ValueUnion
        {
            [FieldOffset(0)] public float AsFloat;
            [FieldOffset(0)] public int AsInt32;
            [FieldOffset(0)] public long AsInt64;
        }
    }
}
