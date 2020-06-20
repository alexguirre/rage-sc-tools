namespace ScTools.GameFiles
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using CodeWalker.GameFiles;

    internal class Script : ResourceFileBase
    {
        public const uint CodePageMaxLength = 0x4000;

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
        public ulong StringsPointer { get; set; }
        public uint StringsCount { get; set; }
        public uint Unknown_74h { get; set; }
        public long Unknown_78h { get; set; }

        // reference data
        public byte[][] CodePages { get; set; }
        public ScriptValue[] LocalsInitialValues { get; set; }
        public ulong[] Natives { get; set; }
        public string Name { get; set; }

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
            StringsPointer = reader.ReadUInt64();
            StringsCount = reader.ReadUInt32();
            Unknown_74h = reader.ReadUInt32();
            Unknown_78h = reader.ReadInt64();

            // read reference data
            uint codePagesCount = (CodeLength + 0x3FFF) >> 14;
            ulong[] codePagesPtrs = reader.ReadUlongsAt(CodePagesPointer, codePagesCount, false);

            CodePages = new byte[codePagesCount][];
            for (int i = 0; i < codePagesCount; i++)
            {
                uint pageSize = i == codePagesCount - 1 ? (CodeLength & (CodePageMaxLength - 1)) : CodePageMaxLength;

                CodePages[i] = reader.ReadBytesAt(codePagesPtrs[i], pageSize, false);
            }

            LocalsInitialValues = reader.ReadStructsAt<ScriptValue>(LocalsInitialValuesPointer, LocalsCount);
            // TODO: GlobalsInitialValuesPointer
            Natives = reader.ReadUlongsAt(NativesPointer, NativesCount);
            Name = reader.ReadStringAt(NamePointer);
            // TODO: StringsPointer
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters) => throw new NotImplementedException();
        public override IResourceBlock[] GetReferences() => throw new NotImplementedException();

        public ref byte IP(uint ip) => ref CodePages[ip >> 14][ip & 0x3FFF];
        public ref T IP<T>(uint ip) where T : unmanaged => ref Unsafe.As<byte, T>(ref IP(ip));
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
