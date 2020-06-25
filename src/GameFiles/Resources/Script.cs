namespace ScTools.GameFiles
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using CodeWalker.GameFiles;
    using System.Text;
    using System.Diagnostics;
    using System.Collections;

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
        public ScriptPageArray<byte> CodePages { get; set; }
        public ScriptValue[] Statics { get; set; }
        public ScriptPageArray<ScriptValue> GlobalsPages { get; set; }
        public ulong[] Natives { get; set; }
        public string Name { get; set; }
        public ScriptPageArray<byte> StringsPages { get; set; }

        private ResourceSystemStructBlock<ScriptValue> staticsBlock;
        private ResourceSystemStructBlock<ulong> nativesBlock;
        private string_r nameBlock;

        public uint GlobalsLength => GlobalsLengthAndBlock & 0x3FFFF;
        public uint GlobalsBlock => GlobalsLengthAndBlock >> 18;

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
            CodePages = reader.ReadBlockAt<ScriptPageArray<byte>>(CodePagesPointer, CodeLength);
            Statics = reader.ReadStructsAt<ScriptValue>(StaticsPointer, StaticsCount);
            GlobalsPages = reader.ReadBlockAt<ScriptPageArray<ScriptValue>>(GlobalsPagesPointer, GlobalsLength);
            Natives = reader.ReadUlongsAt(NativesPointer, NativesCount);
            Name = reader.ReadStringAt(NamePointer);
            StringsPages = reader.ReadBlockAt<ScriptPageArray<byte>>(StringsPagesPointer, StringsLength);
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            base.Write(writer, parameters);

            // update structure data
            CodePagesPointer = (ulong)(CodePages?.FilePosition ?? 0);
            StaticsPointer = (ulong)(staticsBlock?.FilePosition ?? 0);
            StaticsCount = (uint)(staticsBlock?.ItemCount ?? 0);
            GlobalsPagesPointer = (ulong)(GlobalsPages?.FilePosition ?? 0);
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

            if (GlobalsPages != null && GlobalsPages.Count > 0)
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

            if (StringsPages != null && StringsPages.Count > 0)
            {
                list.Add(StringsPages);
            }

            return list.ToArray();
        }

        public ref byte IP(uint ip) => ref CodePages[(int)(ip >> 14)][ip & 0x3FFF];
        public ref T IP<T>(uint ip) where T : unmanaged => ref Unsafe.As<byte, T>(ref IP(ip));

        public ulong NativeHash(int index)
        {
            if (index < 0 ||index >= NativesCount)
            {
                throw new IndexOutOfRangeException();
            }

            // from: https://gtamods.com/wiki/Script_Container
            byte rotate = (byte)(((uint)index + CodeLength) & 0x3F);
            return Natives[index] << rotate | Natives[index] >> (64 - rotate);
        }

        public string String(uint id) => Encoding.UTF8.GetString(StringChars(id));
        public unsafe ReadOnlySpan<byte> StringChars(uint id)
        {
            var page = StringsPages[(int)(id >> 14)];
            uint pageOffset = id & 0x3FFF;
            
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

    public class ScriptPage<T> : ResourceSystemBlock where T : struct
    {
        public override long BlockLength => (Data?.Length ?? 0) * Marshal.SizeOf<T>();

        public T[] Data { get; set; }
        public ref T this[int index] => ref Data[index];
        public ref T this[uint index] => ref Data[index];
        public ref T this[long index] => ref Data[index];
        public ref T this[ulong index] => ref Data[index];

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            uint length = Convert.ToUInt32(parameters[0]);

            // special cases for faster reading
            if (typeof(T) == typeof(byte))
            {
                Data = reader.ReadBytes((int)length) as T[];
            }
            else if (typeof(T) == typeof(ScriptValue))
            {
                byte[] d = reader.ReadBytes((int)length * Marshal.SizeOf<ScriptValue>());
                ScriptValue[] v = new ScriptValue[length];
                unsafe
                {
                    fixed (byte* src = d)
                    fixed (ScriptValue* dest = v)
                    {
                        Buffer.MemoryCopy(src, dest, d.Length, d.Length);
                    }
                }

                Data = v as T[];
            }
            else
            {
                // default case
                Data = reader.ReadStructs<T>(length);
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            Debug.Assert(Data != null && Data.Length <= Script.MaxPageLength);

            // special cases for faster writing
            if (typeof(T) == typeof(byte))
            {
                writer.Write(Data as byte[]);
            }
            else if (typeof(T) == typeof(ScriptValue))
            {
                byte[] d = new byte[Data.Length * Marshal.SizeOf<ScriptValue>()];
                ScriptValue[] v = Data as ScriptValue[];
                unsafe
                {
                    fixed (ScriptValue* src = v)
                    fixed (byte* dest = d)
                    {
                        Buffer.MemoryCopy(src, dest, d.Length, d.Length);
                    }
                }

                writer.Write(d);
            }
            else
            {
                // default case
                writer.WriteStructs(Data);
            }
        }
    }

    public class ScriptPageArray<T> : ResourceSystemBlock, IEnumerable<ScriptPage<T>> where T : struct
    {
        public override long BlockLength => 8 * Count;

        public ulong[] Pointers { get; set; }
        public ScriptPage<T>[] Items { get; set; }

        public int Count => Items?.Length ?? 0;
        public ref ScriptPage<T> this[int index] => ref Items[index];
        public ref ScriptPage<T> this[uint index] => ref Items[index];
        public ref ScriptPage<T> this[long index] => ref Items[index];
        public ref ScriptPage<T> this[ulong index] => ref Items[index];

        public ScriptPageArray()
        {
        }

        public override void Read(ResourceDataReader reader, params object[] parameters)
        {
            uint length = Convert.ToUInt32(parameters[0]);

            uint pageCount = (length + 0x3FFF) >> 14;
            Pointers = reader.ReadUlongsAt((ulong)reader.Position, pageCount, false);


            Items = new ScriptPage<T>[pageCount];
            for (int i = 0; i < pageCount; i++)
            {
                uint pageSize = i == pageCount - 1 ? (length & (Script.MaxPageLength - 1)) : Script.MaxPageLength;
                Items[i] = reader.ReadBlockAt<ScriptPage<T>>(Pointers[i], pageSize);
            }
        }

        public override void Write(ResourceDataWriter writer, params object[] parameters)
        {
            // update...
            var list = new List<ulong>();
            foreach (var x in Items)
            {
                if (x != null)
                {
                    list.Add((uint)x.FilePosition);
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
            
            foreach (var x in Items)
            {
                list.Add(x);
            }

            return list.ToArray();
        }

        public IEnumerator<ScriptPage<T>> GetEnumerator()
        {
            if (Items == null)
            {
                yield break;
            }

            for (int i = 0; i < Items.Length; i++)
            {
                yield return Items[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
