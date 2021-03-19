namespace ScTools.ScriptAssembly
{
    using System;
    using System.IO;
    using System.Text;

    public class SegmentBuilder : IDisposable
    {
        public int Aligment { get; }
        public MemoryStream RawData { get; }
        public ReadOnlySpan<byte> RawDataBuffer => RawData.GetBuffer()[0..(int)ByteLength];
        public BinaryWriter RawWriter { get; }

        public long ByteLength => RawData.Length;

        public SegmentBuilder(int alignment)
        {
            Aligment = alignment;
            RawData = new MemoryStream();
            RawWriter = new BinaryWriter(RawData, Encoding.UTF8, leaveOpen: true);
        }

        public void Dispose()
        {
            RawWriter.Dispose();
            RawData.Dispose();
        }

        public void Int(int value)
        {
            Align();
            RawWriter.Write(value);
            Align();
        }

        public void UInt64(ulong value)
        {
            Align();
            RawWriter.Write(value);
            Align();
        }

        public void Float(float value)
        {
            Align();
            RawWriter.Write(value);
            Align();
        }

        public void String(string value)
        {
            Align();
            RawWriter.Write(value.AsSpan());
            RawWriter.Write((byte)0); // null-terminated
            Align();
        }

        private void Align()
        {
            var padding = (Aligment - (RawData.Length % Aligment)) % Aligment;
            if (padding > 0)
            {
                RawWriter.Write(new byte[padding]);
            }
        }
    }
}
