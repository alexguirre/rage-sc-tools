namespace ScTools.GameFiles
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using CodeWalker.GameFiles;
    using System.Text;
    using System.Collections;

    internal class Script : ResourceFileBase
    {
        private const uint MaxPageLength = 0x4000;

        public override long BlockLength => 128;

        // structure data
        public ulong CodePagesPointer { get; set; }
        public uint Hash { get; set; }
        public uint CodeLength { get; set; }
        public uint ArgsCount { get; set; }
        public uint LocalsCount { get; set; }
        public uint GlobalsLengthAndBlock { get; set; }
        public uint NativesCount { get; set; }
        public ulong LocalsPointer { get; set; }
        public ulong GlobalsPagesPointer { get; set; }
        public ulong NativesPointer { get; set; }
        public long Unknown_48h { get; set; }
        public long Unknown_50h { get; set; }
        public uint NameHash { get; set; }
        public uint NumRefs { get; set; } // always 1
        public ulong NamePointer { get; set; }
        public ulong StringsPagesPointer { get; set; }
        public uint StringsLength { get; set; }
        public uint Unknown_74h { get; set; }
        public long Unknown_78h { get; set; }

        // reference data
        public byte[][] CodePages { get; set; }
        public ScriptValue[] Locals { get; set; }
        public ScriptValue[][] GlobalsPages { get; set; }
        public ulong[] Natives { get; set; }
        public string Name { get; set; }
        public byte[][] StringsPages { get; set; }

        private ResourceSystemStructBlock<byte>[] codePagesBlocks;
        private ResourceSystemStructBlock<ulong> codePagesPointersBlock;
        private ResourceSystemStructBlock<ScriptValue> localsBlock;
        private ResourceSystemStructBlock<ScriptValue>[] globalsPagesBlocks;
        private ResourceSystemStructBlock<ulong> globalsPagesPointersBlock;
        private ResourceSystemStructBlock<ulong> nativesBlock;
        private string_r nameBlock;
        private ResourceSystemStructBlock<byte>[] stringsPagesBlocks;
        private ResourceSystemStructBlock<ulong> stringsPagesPointersBlock;

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            base.Read(reader, parameters);

            // read structure data
            CodePagesPointer = reader.ReadUInt64();
            Hash = reader.ReadUInt32();
            CodeLength = reader.ReadUInt32();
            ArgsCount = reader.ReadUInt32();
            LocalsCount = reader.ReadUInt32();
            GlobalsLengthAndBlock = reader.ReadUInt32();
            NativesCount = reader.ReadUInt32();
            LocalsPointer = reader.ReadUInt64();
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
            ulong[] codePagesPtrs = reader.ReadUlongsAt(CodePagesPointer, codePagesCount, false);
            CodePages = new byte[codePagesCount][];
            for (int i = 0; i < codePagesCount; i++)
            {
                uint pageSize = i == codePagesCount - 1 ? (CodeLength & (MaxPageLength - 1)) : MaxPageLength;

                CodePages[i] = reader.ReadBytesAt(codePagesPtrs[i], pageSize, false);
            }

            Locals = reader.ReadStructsAt<ScriptValue>(LocalsPointer, LocalsCount);

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
            ulong[] stringPagesPtrs = reader.ReadUlongsAt(StringsPagesPointer, stringPagesCount, false);
            StringsPages = new byte[stringPagesCount][];
            for (int i = 0; i < stringPagesCount; i++)
            {
                uint pageSize = i == stringPagesCount - 1 ? (uint)(StringsLength - (i << 14)) : MaxPageLength;

                StringsPages[i] = reader.ReadBytesAt(stringPagesPtrs[i], pageSize, false);
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            CodePagesPointer = (ulong)(codePagesPointersBlock?.FilePosition ?? 0);
            CodeLength = 0;
            for (int i = 0; i < codePagesPointersBlock.Items.Length; i++)
            {
                codePagesPointersBlock.Items[i] = (ulong)codePagesBlocks[i].FilePosition;
                CodeLength += (uint)codePagesBlocks[i].ItemCount;
            }
            LocalsPointer = (ulong)(localsBlock?.FilePosition ?? 0);
            LocalsCount = (uint)(localsBlock?.ItemCount ?? 0);
            GlobalsPagesPointer = (ulong)(globalsPagesPointersBlock?.FilePosition ?? 0);
            Console.WriteLine("bef GlobalsLengthAndBlock = {0}", GlobalsLengthAndBlock);
            GlobalsLengthAndBlock &= 0xFFFC0000; // keep the global block index
            for (int i = 0; i < globalsPagesPointersBlock.Items.Length; i++)
            {
                globalsPagesPointersBlock.Items[i] = (ulong)globalsPagesBlocks[i].FilePosition;
                GlobalsLengthAndBlock += (uint)globalsPagesBlocks[i].ItemCount;
            }
            Console.WriteLine("aft GlobalsLengthAndBlock = {0}", GlobalsLengthAndBlock);
            NativesPointer = (ulong)(nativesBlock?.FilePosition ?? 0);
            NativesCount = (uint)(nativesBlock?.ItemCount ?? 0);
            NamePointer = (ulong)(nameBlock?.FilePosition ?? 0);
            StringsPagesPointer = (ulong)(stringsPagesPointersBlock?.FilePosition ?? 0);
            StringsLength = 0;
            for (int i = 0; i < stringsPagesPointersBlock.Items.Length; i++)
            {
                stringsPagesPointersBlock.Items[i] = (ulong)stringsPagesBlocks[i].FilePosition;
                StringsLength += (uint)stringsPagesBlocks[i].ItemCount;
            }

            // write structure data
            writer.Write(CodePagesPointer);
            writer.Write(Hash);
            writer.Write(CodeLength);
            writer.Write(ArgsCount);
            writer.Write(LocalsCount);
            writer.Write(GlobalsLengthAndBlock);
            writer.Write(NativesCount);
            writer.Write(LocalsPointer);
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

            if (CodePages != null)
            {
                codePagesBlocks = new ResourceSystemStructBlock<byte>[CodePages.Length];
                for (int i = 0; i < CodePages.Length; i++)
                {
                    codePagesBlocks[i] = new ResourceSystemStructBlock<byte>(CodePages[i]);
                    list.Add(codePagesBlocks[i]);
                }

                codePagesPointersBlock = new ResourceSystemStructBlock<ulong>(new ulong[CodePages.Length]);
                list.Add(codePagesPointersBlock);
            }
            else
            {
                codePagesBlocks = null;
                codePagesPointersBlock = null;
            }

            if (Locals != null)
            {
                localsBlock = new ResourceSystemStructBlock<ScriptValue>(Locals);
                list.Add(localsBlock);
            }
            else
            {
                localsBlock = null;
            }

            if (GlobalsPages != null)
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

            if (Natives != null)
            {
                nativesBlock = new ResourceSystemStructBlock<ulong>(Natives);
                list.Add(nativesBlock);
            }
            else
            {
                nativesBlock = null;
            }

            if (Name != null)
            {
                nameBlock = (string_r)Name;
                list.Add(nameBlock);
            }
            else
            {
                nameBlock = null;
            }

            if (StringsPages != null)
            {
                stringsPagesBlocks = new ResourceSystemStructBlock<byte>[StringsPages.Length];
                for (int i = 0; i < StringsPages.Length; i++)
                {
                    stringsPagesBlocks[i] = new ResourceSystemStructBlock<byte>(StringsPages[i]);
                    list.Add(stringsPagesBlocks[i]);
                }

                stringsPagesPointersBlock = new ResourceSystemStructBlock<ulong>(new ulong[StringsPages.Length]);
                list.Add(stringsPagesPointersBlock);
            }
            else
            {
                stringsPagesBlocks = null;
                stringsPagesPointersBlock = null;
            }

            return list.ToArray();
        }

        public ref byte IP(uint ip) => ref CodePages[ip >> 14][ip & 0x3FFF];
        public ref T IP<T>(uint ip) where T : unmanaged => ref Unsafe.As<byte, T>(ref IP(ip));

        public string String(uint id) => Encoding.UTF8.GetString(StringChars(id));
        public unsafe ReadOnlySpan<byte> StringChars(uint id)
        {
            byte[] page = StringsPages[id >> 14];
            uint pageOffset = id & 0x3FFF;
            
            void* start = Unsafe.AsPointer(ref page[pageOffset]);
            int length = 0;

            while (page[pageOffset + length] != 0) { length++; }

            return new ReadOnlySpan<byte>(start, length);
        }

        public IEnumerable<uint> StringIds()
        {
            uint pageIndex = 0;
            uint stringId = 0;
            bool inString = false;
            foreach (byte[] page in StringsPages)
            {
                uint pageOffset = 0;
                foreach (byte b in page)
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

    //public class ScriptStringPagesBlock : ResourceSystemBlock
    //{
    //    public override void Read(ResourceDataReader reader, params object[] parameters)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

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
