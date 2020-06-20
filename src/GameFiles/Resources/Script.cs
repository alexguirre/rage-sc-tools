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
        public uint GlobalsCount { get; set; }
        public uint NativesCount { get; set; }
        public ulong LocalsInitialValuesPointer { get; set; }
        public ulong GlobalsInitialValuesPointer { get; set; }
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
        public ScriptValue[] LocalsInitialValues { get; set; }
        public ulong[] Natives { get; set; }
        public string Name { get; set; }
        public byte[][] StringsPages { get; set; }

        private ResourceSystemStructBlock<ScriptValue> localsInitialValuesBlock;
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
            LocalsCount = reader.ReadUInt32();
            GlobalsCount = reader.ReadUInt32();
            NativesCount = reader.ReadUInt32();
            LocalsInitialValuesPointer = reader.ReadUInt64();
            GlobalsInitialValuesPointer = reader.ReadUInt64();
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

            LocalsInitialValues = reader.ReadStructsAt<ScriptValue>(LocalsInitialValuesPointer, LocalsCount);
            // TODO: GlobalsInitialValuesPointer
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
            throw new NotImplementedException();

            // update structure data
            LocalsInitialValuesPointer = (ulong)(localsInitialValuesBlock?.FilePosition ?? 0);
            NativesPointer = (ulong)(nativesBlock?.FilePosition ?? 0);
            NamePointer = (ulong)(nameBlock?.FilePosition ?? 0);

            // write structure data
        }

        public override IResourceBlock[] GetReferences()
        {
            throw new NotImplementedException();

            var list = new List<IResourceBlock>(base.GetReferences());

            if (LocalsInitialValues != null)
            {
                localsInitialValuesBlock = new ResourceSystemStructBlock<ScriptValue>(LocalsInitialValues);
                list.Add(nativesBlock);
            }

            if (Natives != null)
            {
                nativesBlock = new ResourceSystemStructBlock<ulong>(Natives);
                list.Add(nativesBlock);
            }

            if (Name != null)
            {
                nameBlock = (string_r)Name;
                list.Add(nameBlock);
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
