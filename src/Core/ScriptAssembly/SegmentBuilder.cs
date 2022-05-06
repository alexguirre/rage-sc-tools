namespace ScTools.ScriptAssembly
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    using ScTools.GameFiles.Five;

    public class SegmentBuilder : IDisposable
    {
        private bool disposed;

        public int AddressingUnitByteSize { get; }
        public bool IsPaged { get; }
        public MemoryStream RawData { get; }
        public Span<byte> RawDataBuffer => RawData.GetBuffer().AsSpan(0, ByteLength);
        public BinaryWriter RawWriter { get; }

        public int ByteLength => (int)RawData.Length;
        public int Length => (ByteLength - 1 + AddressingUnitByteSize) / AddressingUnitByteSize;

        public SegmentBuilder(int addressingUnitByteSize, bool isPaged)
        {
            if (addressingUnitByteSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(addressingUnitByteSize), "Alignment must be at least 1");
            }

            AddressingUnitByteSize = addressingUnitByteSize;
            IsPaged = isPaged;
            RawData = new MemoryStream();
            RawWriter = new BinaryWriter(RawData, Encoding.UTF8, leaveOpen: true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    RawData.Dispose();
                    RawWriter.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Bytes(ReadOnlySpan<byte> data)
        {
            Align();
            CheckPaging(data.Length);
            RawWriter.Write(data);
            Align();
        }

        public void Byte(byte value)
        {
            Align();
            CheckPaging(sizeof(byte));
            RawWriter.Write(value);
            Align();
        }

        public void Int16(short value)
        {
            Align();
            CheckPaging(sizeof(short));
            RawWriter.Write(value);
            Align();
        }

        public void UInt16(ushort value)
        {
            Align();
            CheckPaging(sizeof(ushort));
            RawWriter.Write(value);
            Align();
        }

        public void Int(int value)
        {
            Align();
            CheckPaging(sizeof(int));
            RawWriter.Write(value);
            Align();
        }

        public void UInt(uint value)
        {
            Align();
            CheckPaging(sizeof(uint));
            RawWriter.Write(value);
            Align();
        }

        public void Int64(long value)
        {
            Align();
            CheckPaging(sizeof(long));
            RawWriter.Write(value);
            Align();
        }

        public void UInt64(ulong value)
        {
            Align();
            CheckPaging(sizeof(ulong));
            RawWriter.Write(value);
            Align();
        }

        public void Float(float value)
        {
            Align();
            CheckPaging(sizeof(float));
            RawWriter.Write(value);
            Align();
        }

        public void String(string value)
        {
            Align();
            CheckPaging(Encoding.UTF8.GetByteCount(value) + 1);
            RawWriter.Write(value.AsSpan());
            RawWriter.Write((byte)0); // null-terminated
            Align();
        }

        private void Align()
        {
            if (AddressingUnitByteSize == 1)
            {
                return;
            }

            var padding = (AddressingUnitByteSize - (RawData.Length % AddressingUnitByteSize)) % AddressingUnitByteSize;
            if (padding > 0)
            {
                RawWriter.Write(new byte[padding]);
            }
        }

        public bool FitsCurrentPage(int dataByteSize)
        {
            if (!IsPaged)
            {
                return true;
            }

            var dataLength = (dataByteSize - 1 + AddressingUnitByteSize) / AddressingUnitByteSize;
            var offset = Length & (Script.MaxPageLength - 1);
            var lengthAfterWrite = offset + dataLength;
            return lengthAfterWrite <= Script.MaxPageLength;
        }

        /// <summary>
        /// Check if the new data does not fit the current page, if so add padding until the next page.
        /// </summary>
        private void CheckPaging(int dataByteSize)
        {
            if (!IsPaged)
            {
                return;
            }

            var dataLength = (dataByteSize - 1 + AddressingUnitByteSize) / AddressingUnitByteSize;
            var offset = Length & (Script.MaxPageLength - 1);
            var lengthAfterWrite = offset + dataLength;
            if (lengthAfterWrite > Script.MaxPageLength)
            {
                // the data doesn't fit in the current page, skip until the next one (zeroed out)
                var len = Script.MaxPageLength - offset;
                RawWriter.Write(new byte[len * AddressingUnitByteSize]);
            }
        }

        public ScriptPageArray<T> ToPages<T>() where T : struct
        {
            if (!IsPaged)
            {
                throw new InvalidOperationException("This segment is not paged");
            }

            var data = MemoryMarshal.Cast<byte, T>(RawDataBuffer);

            // create pages
            var pageCount = (data.Length + Script.MaxPageLength) / Script.MaxPageLength;
            var p = new ScriptPage<T>[pageCount];
            if (pageCount > 0)
            {
                for (int i = 0; i < pageCount - 1; i++)
                {
                    p[i] = new ScriptPage<T> { Data = new T[Script.MaxPageLength] };
                    data.Slice(i * (int)Script.MaxPageLength, (int)Script.MaxPageLength).CopyTo(p[i].Data);
                }

                p[^1] = new ScriptPage<T> { Data = new T[data.Length & 0x3FFF] };
                data.Slice((int)((pageCount - 1) * Script.MaxPageLength), p[^1].Data.Length).CopyTo(p[^1].Data);
            }

            return new ScriptPageArray<T> { Items = p };
        }
    }
}
