#nullable disable
namespace ScTools.GameFiles.GTA5;

using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

// From CodeWalker by dexyfex: https://github.com/dexyfex/CodeWalker

#region Data.cs

public enum Endianess
{
    LittleEndian,
    BigEndian
}

public enum DataType
{
    Byte = 0,
    Int16 = 1,
    Int32 = 2,
    Int64 = 3,
    Uint16 = 4,
    Uint32 = 5,
    Uint64 = 6,
    Float = 7,
    Double = 8,
    String = 9,
}

public class DataReader
{
    private Stream baseStream;

    /// <summary>
    /// Gets or sets the endianess of the underlying stream.
    /// </summary>
    public Endianess Endianess
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the length of the underlying stream.
    /// </summary>
    public virtual long Length
    {
        get
        {
            return baseStream.Length;
        }
    }

    /// <summary>
    /// Gets or sets the position within the underlying stream.
    /// </summary>
    public virtual long Position
    {
        get
        {
            return baseStream.Position;
        }
        set
        {
            baseStream.Position = value;
        }
    }

    /// <summary>
    /// Initializes a new data reader for the specified stream.
    /// </summary>
    public DataReader(Stream stream, Endianess endianess = Endianess.LittleEndian)
    {
        this.baseStream = stream;
        this.Endianess = endianess;
    }

    /// <summary>
    /// Reads data from the underlying stream. This is the only method that directly accesses
    /// the data in the underlying stream.
    /// </summary>
    protected virtual byte[] ReadFromStream(int count, bool ignoreEndianess = false)
    {
        var buffer = new byte[count];
        baseStream.Read(buffer, 0, count);

        // handle endianess
        if (!ignoreEndianess && (Endianess == Endianess.BigEndian))
        {
            Array.Reverse(buffer);
        }

        return buffer;
    }

    /// <summary>
    /// Reads a byte.
    /// </summary>
    public byte ReadByte()
    {
        return ReadFromStream(1)[0];
    }

    /// <summary>
    /// Reads a sequence of bytes.
    /// </summary>
    public byte[] ReadBytes(int count)
    {
        return ReadFromStream(count, true);
    }

    /// <summary>
    /// Reads a signed 16-bit value.
    /// </summary>
    public short ReadInt16()
    {
        return BitConverter.ToInt16(ReadFromStream(2), 0);
    }

    /// <summary>
    /// Reads a signed 32-bit value.
    /// </summary>
    public int ReadInt32()
    {
        return BitConverter.ToInt32(ReadFromStream(4), 0);
    }

    /// <summary>
    /// Reads a signed 64-bit value.
    /// </summary>
    public long ReadInt64()
    {
        return BitConverter.ToInt64(ReadFromStream(8), 0);
    }

    /// <summary>
    /// Reads an unsigned 16-bit value.
    /// </summary>
    public ushort ReadUInt16()
    {
        return BitConverter.ToUInt16(ReadFromStream(2), 0);
    }

    /// <summary>
    /// Reads an unsigned 32-bit value.
    /// </summary>
    public uint ReadUInt32()
    {
        return BitConverter.ToUInt32(ReadFromStream(4), 0);
    }

    /// <summary>
    /// Reads an unsigned 64-bit value.
    /// </summary>
    public ulong ReadUInt64()
    {
        return BitConverter.ToUInt64(ReadFromStream(8), 0);
    }

    /// <summary>
    /// Reads a single precision floating point value.
    /// </summary>
    public float ReadSingle()
    {
        return BitConverter.ToSingle(ReadFromStream(4), 0);
    }

    /// <summary>
    /// Reads a double precision floating point value.
    /// </summary>
    public double ReadDouble()
    {
        return BitConverter.ToDouble(ReadFromStream(8), 0);
    }

    /// <summary>
    /// Reads a string.
    /// </summary>
    public string ReadString()
    {
        var bytes = new List<byte>();
        var temp = ReadFromStream(1)[0];
        while (temp != 0)
        {
            bytes.Add(temp);
            temp = ReadFromStream(1)[0];
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }



    //TODO: put this somewhere else...
    public static uint SizeOf(DataType type)
    {
        switch (type)
        {
            default:
            case DataType.Byte: return 1;
            case DataType.Int16: return 2;
            case DataType.Int32: return 4;
            case DataType.Int64: return 8;
            case DataType.Uint16: return 2;
            case DataType.Uint32: return 4;
            case DataType.Uint64: return 8;
            case DataType.Float: return 4;
            case DataType.Double: return 8;
            case DataType.String: return 0; //how long is a string..?
        }
    }




}

public class DataWriter
{
    private Stream baseStream;

    /// <summary>
    /// Gets or sets the endianess of the underlying stream.
    /// </summary>
    public Endianess Endianess
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the length of the underlying stream.
    /// </summary>
    public virtual long Length
    {
        get
        {
            return baseStream.Length;
        }
    }

    /// <summary>
    /// Gets or sets the position within the underlying stream.
    /// </summary>
    public virtual long Position
    {
        get
        {
            return baseStream.Position;
        }
        set
        {
            baseStream.Position = value;
        }
    }

    /// <summary>
    /// Initializes a new data writer for the specified stream.
    /// </summary>
    public DataWriter(Stream stream, Endianess endianess = Endianess.LittleEndian)
    {
        this.baseStream = stream;
        this.Endianess = endianess;
    }

    /// <summary>
    /// Writes data to the underlying stream. This is the only method that directly accesses
    /// the data in the underlying stream.
    /// </summary>
    protected virtual void WriteToStream(byte[] value, bool ignoreEndianess = false)
    {
        if (!ignoreEndianess && (Endianess == Endianess.BigEndian))
        {
            var buffer = (byte[])value.Clone();
            Array.Reverse(buffer);
            baseStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            baseStream.Write(value, 0, value.Length);
        }
    }

    /// <summary>
    /// Writes a byte.
    /// </summary>
    public void Write(byte value)
    {
        WriteToStream(new byte[] { value });
    }

    /// <summary>
    /// Writes a sequence of bytes.
    /// </summary>
    public void Write(byte[] value)
    {
        WriteToStream(value, true);
    }

    /// <summary>
    /// Writes a signed 16-bit value.
    /// </summary>
    public void Write(short value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a signed 32-bit value.
    /// </summary>
    public void Write(int value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a signed 64-bit value.
    /// </summary>
    public void Write(long value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes an unsigned 16-bit value.
    /// </summary>
    public void Write(ushort value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes an unsigned 32-bit value.
    /// </summary>
    public void Write(uint value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes an unsigned 64-bit value.
    /// </summary>
    public void Write(ulong value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a single precision floating point value.
    /// </summary>
    public void Write(float value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a double precision floating point value.
    /// </summary>
    public void Write(double value)
    {
        WriteToStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a string.
    /// </summary>
    public void Write(string value)
    {
        foreach (var c in value)
            Write((byte)c);
        Write((byte)0);
    }
}

#endregion // Data.cs

#region ResourceData.cs

/// <summary>
/// Represents a resource data reader.
/// </summary>
public class ResourceDataReader : DataReader
{
    private const long SYSTEM_BASE = 0x50000000;
    private const long GRAPHICS_BASE = 0x60000000;

    private Stream systemStream;
    private Stream graphicsStream;

    public RpfResourceFileEntry FileEntry { get; set; }

    // this is a dictionary that contains all the resource blocks
    // which were read from this resource reader
    public Dictionary<long, IResourceBlock> blockPool = new Dictionary<long, IResourceBlock>();
    public Dictionary<long, object> arrayPool = new Dictionary<long, object>();

    /// <summary>
    /// Gets the length of the underlying stream.
    /// </summary>
    public override long Length
    {
        get
        {
            return -1;
        }
    }

    /// <summary>
    /// Gets or sets the position within the underlying stream.
    /// </summary>
    public override long Position
    {
        get;
        set;
    }

    /// <summary>
    /// Initializes a new resource data reader for the specified system- and graphics-stream.
    /// </summary>
    public ResourceDataReader(Stream systemStream, Stream graphicsStream, Endianess endianess = Endianess.LittleEndian)
        : base((Stream)null, endianess)
    {
        this.systemStream = systemStream;
        this.graphicsStream = graphicsStream;
    }

    public ResourceDataReader(RpfResourceFileEntry resentry, byte[] data, Endianess endianess = Endianess.LittleEndian)
        : base((Stream)null, endianess)
    {
        FileEntry = resentry;
        var systemSize = resentry.SystemSize;
        var graphicsSize = resentry.GraphicsSize;

        //if (data != null)
        //{
        //    if (systemSize > data.Length)
        //    {
        //        systemSize = data.Length;
        //        graphicsSize = 0;
        //    }
        //    else if ((systemSize + graphicsSize) > data.Length)
        //    {
        //        graphicsSize = data.Length - systemSize;
        //    }
        //}

        this.systemStream = new MemoryStream(data, 0, systemSize);
        this.graphicsStream = new MemoryStream(data, systemSize, graphicsSize);
        Position = 0x50000000;
    }

    public ResourceDataReader(int systemSize, int graphicsSize, byte[] data, Endianess endianess = Endianess.LittleEndian)
        : base((Stream)null, endianess)
    {
        this.systemStream = new MemoryStream(data, 0, systemSize);
        this.graphicsStream = new MemoryStream(data, systemSize, graphicsSize);
        Position = 0x50000000;
    }



    /// <summary>
    /// Reads data from the underlying stream. This is the only method that directly accesses
    /// the data in the underlying stream.
    /// </summary>
    protected override byte[] ReadFromStream(int count, bool ignoreEndianess = false)
    {
        if ((Position & SYSTEM_BASE) == SYSTEM_BASE)
        {
            // read from system stream...

            systemStream.Position = Position & ~0x50000000;

            var buffer = new byte[count];
            systemStream.Read(buffer, 0, count);

            // handle endianess
            if (!ignoreEndianess && (Endianess == Endianess.BigEndian))
            {
                Array.Reverse(buffer);
            }

            Position = systemStream.Position | 0x50000000;
            return buffer;

        }
        if ((Position & GRAPHICS_BASE) == GRAPHICS_BASE)
        {
            // read from graphic stream...

            graphicsStream.Position = Position & ~0x60000000;

            var buffer = new byte[count];
            graphicsStream.Read(buffer, 0, count);

            // handle endianess
            if (!ignoreEndianess && (Endianess == Endianess.BigEndian))
            {
                Array.Reverse(buffer);
            }

            Position = graphicsStream.Position | 0x60000000;
            return buffer;
        }
        throw new Exception("illegal position!");
    }

    /// <summary>
    /// Reads a block.
    /// </summary>
    public T ReadBlock<T>(params object[] parameters) where T : IResourceBlock, new()
    {
        var usepool = !typeof(IResourceNoCacheBlock).IsAssignableFrom(typeof(T));
        if (usepool)
        {
            // make sure to return the same object if the same
            // block is read again...
            if (blockPool.ContainsKey(Position))
            {
                var block = blockPool[Position];
                if (block is T tblk)
                {
                    Position += block.BlockLength;
                    return tblk;
                }
                else
                {
                    usepool = false;
                }
            }
        }

        var result = new T();


        // replace with correct type...
        if (result is IResourceXXSystemBlock)
        {
            result = (T)((IResourceXXSystemBlock)result).GetType(this, parameters);
        }

        if (result == null)
        {
            return default(T);
        }

        if (usepool)
        {
            blockPool[Position] = result;
        }

        result.Read(this, parameters);

        return result;
    }

    /// <summary>
    /// Reads a block at a specified position.
    /// </summary>
    public T ReadBlockAt<T>(ulong position, params object[] parameters) where T : IResourceBlock, new()
    {
        if (position != 0)
        {
            var positionBackup = Position;

            Position = (long)position;
            var result = ReadBlock<T>(parameters);
            Position = positionBackup;

            return result;
        }
        else
        {
            return default(T);
        }
    }

    public T[] ReadBlocks<T>(ulong[] pointers) where T : IResourceBlock, new()
    {
        if (pointers == null) return null;
        var count = pointers.Length;
        var items = new T[count];
        for (int i = 0; i < count; i++)
        {
            items[i] = ReadBlockAt<T>(pointers[i]);
        }
        return items;
    }


    public byte[] ReadBytesAt(ulong position, uint count, bool cache = true)
    {
        long pos = (long)position;
        if ((pos <= 0) || (count == 0)) return null;
        var posbackup = Position;
        Position = pos;
        var result = ReadBytes((int)count);
        Position = posbackup;
        if (cache) arrayPool[(long)position] = result;
        return result;
    }
    public ushort[] ReadUshortsAt(ulong position, uint count, bool cache = true)
    {
        if ((position <= 0) || (count == 0)) return null;

        var result = new ushort[count];
        var length = count * 2;
        byte[] data = ReadBytesAt(position, length, false);
        Buffer.BlockCopy(data, 0, result, 0, (int)length);

        //var posbackup = Position;
        //Position = position;
        //var result2 = new ushort[count];
        //for (uint i = 0; i < count; i++)
        //{
        //    result2[i] = ReadUInt16();
        //}
        //Position = posbackup;

        if (cache) arrayPool[(long)position] = result;

        return result;
    }
    public short[] ReadShortsAt(ulong position, uint count, bool cache = true)
    {
        if ((position <= 0) || (count == 0)) return null;
        var result = new short[count];
        var length = count * 2;
        byte[] data = ReadBytesAt(position, length, false);
        Buffer.BlockCopy(data, 0, result, 0, (int)length);

        if (cache) arrayPool[(long)position] = result;

        return result;
    }
    public uint[] ReadUintsAt(ulong position, uint count, bool cache = true)
    {
        if ((position <= 0) || (count == 0)) return null;

        var result = new uint[count];
        var length = count * 4;
        byte[] data = ReadBytesAt(position, length, false);
        Buffer.BlockCopy(data, 0, result, 0, (int)length);

        //var posbackup = Position;
        //Position = position;
        //var result = new uint[count];
        //for (uint i = 0; i < count; i++)
        //{
        //    result[i] = ReadUInt32();
        //}
        //Position = posbackup;

        if (cache) arrayPool[(long)position] = result;

        return result;
    }
    public ulong[] ReadUlongsAt(ulong position, uint count, bool cache = true)
    {
        if ((position <= 0) || (count == 0)) return null;

        var result = new ulong[count];
        var length = count * 8;
        byte[] data = ReadBytesAt(position, length, false);
        Buffer.BlockCopy(data, 0, result, 0, (int)length);

        //var posbackup = Position;
        //Position = position;
        //var result = new ulong[count];
        //for (uint i = 0; i < count; i++)
        //{
        //    result[i] = ReadUInt64();
        //}
        //Position = posbackup;

        if (cache) arrayPool[(long)position] = result;

        return result;
    }
    public float[] ReadFloatsAt(ulong position, uint count, bool cache = true)
    {
        if ((position <= 0) || (count == 0)) return null;

        var result = new float[count];
        var length = count * 4;
        byte[] data = ReadBytesAt(position, length, false);
        Buffer.BlockCopy(data, 0, result, 0, (int)length);

        //var posbackup = Position;
        //Position = position;
        //var result = new float[count];
        //for (uint i = 0; i < count; i++)
        //{
        //    result[i] = ReadSingle();
        //}
        //Position = posbackup;

        if (cache) arrayPool[(long)position] = result;

        return result;
    }
    public T[] ReadStructsAt<T>(ulong position, uint count, bool cache = true)
    {
        if ((position <= 0) || (count == 0)) return null;

        uint structsize = (uint)Marshal.SizeOf(typeof(T));
        var length = count * structsize;
        byte[] data = ReadBytesAt(position, length, false);

        //var result2 = new T[count];
        //Buffer.BlockCopy(data, 0, result2, 0, (int)length); //error: "object must be an array of primitives" :(

        var result = new T[count];
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var h = handle.AddrOfPinnedObject();
        for (uint i = 0; i < count; i++)
        {
            result[i] = Marshal.PtrToStructure<T>(h + (int)(i * structsize));
        }
        handle.Free();

        if (cache) arrayPool[(long)position] = result;

        return result;
    }
    public T[] ReadStructs<T>(uint count)
    {
        uint structsize = (uint)Marshal.SizeOf(typeof(T));
        var result = new T[count];
        var length = count * structsize;
        byte[] data = ReadBytes((int)length);
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var h = handle.AddrOfPinnedObject();
        for (uint i = 0; i < count; i++)
        {
            result[i] = Marshal.PtrToStructure<T>(h + (int)(i * structsize));
        }
        handle.Free();
        return result;
    }

    public T ReadStruct<T>() where T : struct
    {
        uint structsize = (uint)Marshal.SizeOf(typeof(T));
        var length = structsize;
        byte[] data = ReadBytes((int)length);
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var h = handle.AddrOfPinnedObject();
        var result = Marshal.PtrToStructure<T>(h);
        handle.Free();
        return result;
    }

    public T ReadStructAt<T>(long position) where T : struct
    {
        if ((position <= 0)) return default(T);
        var posbackup = Position;
        Position = (long)position;
        var result = ReadStruct<T>();
        Position = posbackup;
        return result;
    }

    public string ReadStringAt(ulong position)
    {
        long newpos = (long)position;
        if ((newpos <= 0)) return null;
        var lastpos = Position;
        Position = newpos;
        var result = ReadString();
        Position = lastpos;
        arrayPool[newpos] = result;
        return result;
    }

}



/// <summary>
/// Represents a resource data writer.
/// </summary>
public class ResourceDataWriter : DataWriter
{
    private const long SYSTEM_BASE = 0x50000000;
    private const long GRAPHICS_BASE = 0x60000000;

    private Stream systemStream;
    private Stream graphicsStream;

    /// <summary>
    /// Gets the length of the underlying stream.
    /// </summary>
    public override long Length
    {
        get
        {
            return -1;
        }
    }

    /// <summary>
    /// Gets or sets the position within the underlying stream.
    /// </summary>
    public override long Position
    {
        get;
        set;
    }

    /// <summary>
    /// Initializes a new resource data reader for the specified system- and graphics-stream.
    /// </summary>
    public ResourceDataWriter(Stream systemStream, Stream graphicsStream, Endianess endianess = Endianess.LittleEndian)
        : base((Stream)null, endianess)
    {
        this.systemStream = systemStream;
        this.graphicsStream = graphicsStream;
    }

    /// <summary>
    /// Writes data to the underlying stream. This is the only method that directly accesses
    /// the data in the underlying stream.
    /// </summary>
    protected override void WriteToStream(byte[] value, bool ignoreEndianess = true)
    {
        if ((Position & SYSTEM_BASE) == SYSTEM_BASE)
        {
            // write to system stream...

            systemStream.Position = Position & ~SYSTEM_BASE;

            // handle endianess
            if (!ignoreEndianess && (Endianess == Endianess.BigEndian))
            {
                var buf = (byte[])value.Clone();
                Array.Reverse(buf);
                systemStream.Write(buf, 0, buf.Length);
            }
            else
            {
                systemStream.Write(value, 0, value.Length);
            }

            Position = systemStream.Position | 0x50000000;
            return;

        }
        if ((Position & GRAPHICS_BASE) == GRAPHICS_BASE)
        {
            // write to graphic stream...

            graphicsStream.Position = Position & ~GRAPHICS_BASE;

            // handle endianess
            if (!ignoreEndianess && (Endianess == Endianess.BigEndian))
            {
                var buf = (byte[])value.Clone();
                Array.Reverse(buf);
                graphicsStream.Write(buf, 0, buf.Length);
            }
            else
            {
                graphicsStream.Write(value, 0, value.Length);
            }

            Position = graphicsStream.Position | 0x60000000;
            return;
        }

        throw new Exception("illegal position!");
    }

    /// <summary>
    /// Writes a block.
    /// </summary>
    public void WriteBlock(IResourceBlock value)
    {
        value.Write(this);
    }




    public void WriteStruct<T>(T val) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(val, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        Write(arr);
    }
    public void WriteStructs<T>(T[] val) where T : struct
    {
        if (val == null) return;
        foreach (var v in val)
        {
            WriteStruct(v);
        }
    }



    /// <summary>
    /// Write enough bytes to the stream to get to the specified alignment.
    /// </summary>
    /// <param name="alignment">value to align to</param>
    public void WritePadding(int alignment)
    {
        var pad = ((alignment - (Position % alignment)) % alignment);
        if (pad > 0) Write(new byte[pad]);
    }

    public void WriteUlongs(ulong[] val)
    {
        if (val == null) return;
        foreach (var v in val)
        {
            Write(v);
        }
    }


}





/// <summary>
/// Represents a data block in a resource file.
/// </summary>
public interface IResourceBlock
{
    /// <summary>
    /// Gets or sets the position of the data block.
    /// </summary>
    long FilePosition { get; set; }

    /// <summary>
    /// Gets the length of the data block.
    /// </summary>
    long BlockLength { get; }

    /// <summary>
    /// Reads the data block.
    /// </summary>
    void Read(ResourceDataReader reader, params object[] parameters);

    /// <summary>
    /// Writes the data block.
    /// </summary>
    void Write(ResourceDataWriter writer, params object[] parameters);
}

/// <summary>
/// Represents a data block of the system segement in a resource file.
/// </summary>
public interface IResourceSystemBlock : IResourceBlock
{
    /// <summary>
    /// Returns a list of data blocks that are part of this block.
    /// </summary>
    Tuple<long, IResourceBlock>[] GetParts();

    /// <summary>
    /// Returns a list of data blocks that are referenced by this block.
    /// </summary>
    IResourceBlock[] GetReferences();
}

public interface IResourceXXSystemBlock : IResourceSystemBlock
{
    IResourceSystemBlock GetType(ResourceDataReader reader, params object[] parameters);
}

/// <summary>
/// Represents a data block of the graphics segmenet in a resource file.
/// </summary>
public interface IResourceGraphicsBlock : IResourceBlock
{ }


/// <summary>
/// Represents a data block that won't get cached while loading.
/// </summary>
public interface IResourceNoCacheBlock : IResourceBlock
{ }



/// <summary>
/// Represents a data block of the system segement in a resource file.
/// </summary>
public abstract class ResourceSystemBlock : IResourceSystemBlock
{
    private long position;

    /// <summary>
    /// Gets or sets the position of the data block.
    /// </summary>
    public virtual long FilePosition
    {
        get
        {
            return position;
        }
        set
        {
            position = value;
            foreach (var part in GetParts())
            {
                part.Item2.FilePosition = value + part.Item1;
            }
        }
    }

    /// <summary>
    /// Gets the length of the data block.
    /// </summary>
    public abstract long BlockLength
    {
        get;
    }

    /// <summary>
    /// Reads the data block.
    /// </summary>
    public abstract void Read(ResourceDataReader reader, params object[] parameters);

    /// <summary>
    /// Writes the data block.
    /// </summary>
    public abstract void Write(ResourceDataWriter writer, params object[] parameters);

    /// <summary>
    /// Returns a list of data blocks that are part of this block.
    /// </summary>
    public virtual Tuple<long, IResourceBlock>[] GetParts()
    {
        return new Tuple<long, IResourceBlock>[0];
    }

    /// <summary>
    /// Returns a list of data blocks that are referenced by this block.
    /// </summary>
    public virtual IResourceBlock[] GetReferences()
    {
        return new IResourceBlock[0];
    }
}

public abstract class ResourecTypedSystemBlock : ResourceSystemBlock, IResourceXXSystemBlock
{
    public abstract IResourceSystemBlock GetType(ResourceDataReader reader, params object[] parameters);
}

/// <summary>
/// Represents a data block of the graphics segmenet in a resource file.
/// </summary>
public abstract class ResourceGraphicsBlock : IResourceGraphicsBlock
{
    /// <summary>
    /// Gets or sets the position of the data block.
    /// </summary>
    public virtual long FilePosition
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the length of the data block.
    /// </summary>
    public abstract long BlockLength
    {
        get;
    }

    /// <summary>
    /// Reads the data block.
    /// </summary>
    public abstract void Read(ResourceDataReader reader, params object[] parameters);

    /// <summary>
    /// Writes the data block.
    /// </summary>
    public abstract void Write(ResourceDataWriter writer, params object[] parameters);
}

#endregion // ResourceData.cs

#region RpfFile.cs

    public class RpfFile
    {
        public string Name { get; set; } //name of this RPF file/package
        public string NameLower { get; set; }
        public string Path { get; set; } //path within the RPF structure
        public string FilePath { get; set; } //full file path of the RPF
        public long FileSize { get; set; }
        public string LastError { get; set; }
        public Exception LastException { get; set; }

        public RpfDirectoryEntry Root { get; set; }

        public bool IsAESEncrypted { get; set; }
        public bool IsNGEncrypted { get; set; }


        //offset in the current file
        public long StartPos { get; set; }

        //header data
        public uint Version { get; set; }
        public uint EntryCount { get; set; }
        public uint NamesLength { get; set; }
        public RpfEncryption Encryption { get; set; }

        //object linkage
        public List<RpfEntry> AllEntries { get; set; }
        public List<RpfFile> Children { get; set; }
        public RpfFile Parent { get; set; }
        public RpfBinaryFileEntry ParentFileEntry { get; set; }

        public BinaryReader CurrentFileReader { get; set; } //for temporary use while reading header



        public uint TotalFileCount { get; set; }
        public uint TotalFolderCount { get; set; }
        public uint TotalResourceCount { get; set; }
        public uint TotalBinaryFileCount { get; set; }
        public uint GrandTotalRpfCount { get; set; }
        public uint GrandTotalFileCount { get; set; }
        public uint GrandTotalFolderCount { get; set; }
        public uint GrandTotalResourceCount { get; set; }
        public uint GrandTotalBinaryFileCount { get; set; }
        public long ExtractedByteCount { get; set; }


        public RpfFile(string fpath, string relpath) //for a ROOT filesystem RPF
        {
            FileInfo fi = new FileInfo(fpath);
            Name = fi.Name;
            NameLower = Name.ToLowerInvariant();
            Path = relpath.ToLowerInvariant();
            FilePath = fpath;
            FileSize = fi.Length;
        }
        public RpfFile(string name, string path, long filesize) //for a child RPF
        {
            Name = name;
            NameLower = Name.ToLowerInvariant();
            Path = path.ToLowerInvariant();
            FilePath = path;
            FileSize = filesize;
        }

        // Returns string to new path
        public string CopyToModsFolder(out string status)
        {
            RpfFile parentFile = GetTopParent();
            string rel_parent_path = parentFile.Path;
            string full_parent_path = parentFile.FilePath;

            if(rel_parent_path.StartsWith(@"mods\"))
            {
                status = "already in mods folder";
                return null;
            }

            if(!full_parent_path.EndsWith(rel_parent_path))
            {
                throw new DirectoryNotFoundException("Expected full parent path to end with relative path");
            }

            string mods_base_path = full_parent_path.Replace(rel_parent_path, @"mods\");
            string dest_path = mods_base_path + rel_parent_path;

            try
            {
                File.Copy(full_parent_path, dest_path);
                status = $"copied \"{parentFile.Name}\" from \"{full_parent_path}\" to \"{dest_path}\"";
                return dest_path;
            } catch (IOException e)
            {
                status = $"unable to copy \"{parentFile.Name}\" from \"{full_parent_path}\" to \"{dest_path}\": {e.Message}";
                return null;
            } 
        }

        public bool IsInModsFolder()
        {
            return GetTopParent().Path.StartsWith(@"mods\");
        }

        public RpfFile GetTopParent()
        {
            RpfFile pfile = this;
            while (pfile.Parent != null)
            {
                pfile = pfile.Parent;
            }
            return pfile;
        }
        
        public string GetPhysicalFilePath()
        {
            return GetTopParent().FilePath;
        }




        private void ReadHeader(BinaryReader br)
        {
            CurrentFileReader = br;

            StartPos = br.BaseStream.Position;

            Version = br.ReadUInt32(); //RPF Version - GTAV should be 0x52504637 (1380992567)
            EntryCount = br.ReadUInt32(); //Number of Entries
            NamesLength = br.ReadUInt32();
            Encryption = (RpfEncryption)br.ReadUInt32(); //0x04E45504F (1313165391): none;  0x0ffffff9 (268435449): AES

            if (Version != 0x52504637)
            {
                throw new Exception("Invalid Resource - not GTAV!");
            }

            byte[] entriesdata = br.ReadBytes((int)EntryCount * 16); //4x uints each
            byte[] namesdata = br.ReadBytes((int)NamesLength);

            switch (Encryption)
            {
                case RpfEncryption.NONE: //no encryption
                case RpfEncryption.OPEN: //OpenIV style RPF with unencrypted TOC
                    break;
                case RpfEncryption.AES:
                    throw new NotImplementedException("AES encryption not implemented!");
                    // entriesdata = GTACrypto.DecryptAES(entriesdata);
                    // namesdata = GTACrypto.DecryptAES(namesdata);
                    IsAESEncrypted = true;
                    break;
                case RpfEncryption.NG:
                    throw new NotImplementedException("NG encryption not implemented!");
                    // entriesdata = GTACrypto.DecryptNG(entriesdata, Name, (uint)FileSize);
                    // namesdata = GTACrypto.DecryptNG(namesdata, Name, (uint)FileSize);
                    IsNGEncrypted = true;
                    break;
                default: //unknown encryption type? assume NG.. never seems to get here
                    throw new NotImplementedException("NG encryption not implemented!");
                    // entriesdata = GTACrypto.DecryptNG(entriesdata, Name, (uint)FileSize);
                    // namesdata = GTACrypto.DecryptNG(namesdata, Name, (uint)FileSize);
                    break;
            }


            var entriesrdr = new DataReader(new MemoryStream(entriesdata));
            var namesrdr = new DataReader(new MemoryStream(namesdata));
            AllEntries = new List<RpfEntry>();
            TotalFileCount = 0;
            TotalFolderCount = 0;
            TotalResourceCount = 0;
            TotalBinaryFileCount = 0;

            for (uint i = 0; i < EntryCount; i++)
            {
                //entriesrdr.Position += 4;
                uint y = entriesrdr.ReadUInt32();
                uint x = entriesrdr.ReadUInt32();
                entriesrdr.Position -= 8;

                RpfEntry e;

                if (x == 0x7fffff00) //directory entry
                {
                    e = new RpfDirectoryEntry();
                    TotalFolderCount++;
                }
                else if ((x & 0x80000000) == 0) //binary file entry
                {
                    e = new RpfBinaryFileEntry();
                    TotalBinaryFileCount++;
                    TotalFileCount++;
                }
                else //assume resource file entry
                {
                    e = new RpfResourceFileEntry();
                    TotalResourceCount++;
                    TotalFileCount++;
                }

                e.File = this;
                e.H1 = y;
                e.H2 = x;

                e.Read(entriesrdr);

                namesrdr.Position = e.NameOffset;
                e.Name = namesrdr.ReadString();
                e.NameLower = e.Name.ToLowerInvariant();

                if ((e is RpfFileEntry) && string.IsNullOrEmpty(e.Name))
                {
                }
                if ((e is RpfResourceFileEntry))// && string.IsNullOrEmpty(e.Name))
                {
                    var rfe = e as RpfResourceFileEntry;
                    rfe.IsEncrypted = rfe.NameLower.EndsWith(".ysc");//any other way to know..?
                }

                AllEntries.Add(e);
            }



            Root = (RpfDirectoryEntry)AllEntries[0];
            Root.Path = Path.ToLowerInvariant();// + "\\" + Root.Name;
            var stack = new Stack<RpfDirectoryEntry>();
            stack.Push(Root);
            while (stack.Count > 0)
            {
                var item = stack.Pop();

                int starti = (int)item.EntriesIndex;
                int endi = (int)(item.EntriesIndex + item.EntriesCount);

                for (int i = starti; i < endi; i++)
                {
                    RpfEntry e = AllEntries[i];
                    e.Parent = item;
                    if (e is RpfDirectoryEntry)
                    {
                        RpfDirectoryEntry rde = e as RpfDirectoryEntry;
                        rde.Path = item.Path + "\\" + rde.NameLower;
                        item.Directories.Add(rde);
                        stack.Push(rde);
                    }
                    else if (e is RpfFileEntry)
                    {
                        RpfFileEntry rfe = e as RpfFileEntry;
                        rfe.Path = item.Path + "\\" + rfe.NameLower;
                        item.Files.Add(rfe);
                    }
                }
            }

            br.BaseStream.Position = StartPos;

            CurrentFileReader = null;

        }





        public void ScanStructure(Action<string> updateStatus, Action<string> errorLog)
        {
            using (BinaryReader br = new BinaryReader(File.OpenRead(FilePath)))
            {
                try
                {
                    ScanStructure(br, updateStatus, errorLog);
                }
                catch (Exception ex)
                {
                    LastError = ex.ToString();
                    LastException = ex;
                    errorLog(FilePath + ": " + LastError);
                }
            }
        }
        private void ScanStructure(BinaryReader br, Action<string> updateStatus, Action<string> errorLog)
        {
            ReadHeader(br);

            GrandTotalRpfCount = 1; //count this file..
            GrandTotalFileCount = 1; //start with this one.
            GrandTotalFolderCount = 0;
            GrandTotalResourceCount = 0;
            GrandTotalBinaryFileCount = 0;

            Children = new List<RpfFile>();

            updateStatus?.Invoke("Scanning " + Path + "...");

            foreach (RpfEntry entry in AllEntries)
            {
                try
                {
                    if (entry is RpfBinaryFileEntry)
                    {
                        RpfBinaryFileEntry binentry = entry as RpfBinaryFileEntry;

                        //search all the sub resources for YSC files. (recurse!)
                        string lname = binentry.NameLower;
                        if (lname.EndsWith(".rpf"))
                        {
                            br.BaseStream.Position = StartPos + ((long)binentry.FileOffset * 512);

                            long l = binentry.GetFileSize();

                            RpfFile subfile = new RpfFile(binentry.Name, binentry.Path, l);
                            subfile.Parent = this;
                            subfile.ParentFileEntry = binentry;

                            subfile.ScanStructure(br, updateStatus, errorLog);

                            GrandTotalRpfCount += subfile.GrandTotalRpfCount;
                            GrandTotalFileCount += subfile.GrandTotalFileCount;
                            GrandTotalFolderCount += subfile.GrandTotalFolderCount;
                            GrandTotalResourceCount += subfile.GrandTotalResourceCount;
                            GrandTotalBinaryFileCount += subfile.GrandTotalBinaryFileCount;

                            Children.Add(subfile);
                        }
                        else
                        {
                            //binary file that's not an rpf...
                            GrandTotalBinaryFileCount++;
                            GrandTotalFileCount++;
                        }
                    }
                    else if (entry is RpfResourceFileEntry)
                    {
                        GrandTotalResourceCount++;
                        GrandTotalFileCount++;
                    }
                    else if (entry is RpfDirectoryEntry)
                    {
                        GrandTotalFolderCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorLog?.Invoke(entry.Path + ": " + ex.ToString());
                }
            }

        }


        public void ExtractScripts(string outputfolder, Action<string> updateStatus)
        {
            FileStream fs = File.OpenRead(FilePath);
            BinaryReader br = new BinaryReader(fs);

            try
            {
                ExtractScripts(br, outputfolder, updateStatus);
            }
            catch (Exception ex)
            {
                LastError = ex.ToString();
                LastException = ex;
            }

            br.Close();
            br.Dispose();
            fs.Dispose();
        }
        private void ExtractScripts(BinaryReader br, string outputfolder, Action<string> updateStatus)
        {
            updateStatus?.Invoke("Searching " + Name + "...");

            ReadHeader(br);

            //List<DataBlock> blocks = new List<DataBlock>();
            foreach (RpfEntry entry in AllEntries)
            {
                if (entry is RpfBinaryFileEntry)
                {
                    RpfBinaryFileEntry binentry = entry as RpfBinaryFileEntry;
                    long l = binentry.GetFileSize();

                    //search all the sub resources for YSC files. (recurse!)
                    string lname = binentry.NameLower;
                    if (lname.EndsWith(".rpf"))
                    {
                        br.BaseStream.Position = StartPos + ((long)binentry.FileOffset * 512);

                        RpfFile subfile = new RpfFile(binentry.Name, binentry.Path, l);
                        subfile.Parent = this;
                        subfile.ParentFileEntry = binentry;

                        subfile.ExtractScripts(br, outputfolder, updateStatus);
                    }

                }
                else if (entry is RpfResourceFileEntry)
                {

                    RpfResourceFileEntry resentry = entry as RpfResourceFileEntry;

                    string lname = resentry.NameLower;

                    if (lname.EndsWith(".ysc"))
                    {
                        updateStatus?.Invoke("Extracting " + resentry.Name + "...");

                        //found a YSC file. extract it!
                        string ofpath = outputfolder + "\\" + resentry.Name;

                        br.BaseStream.Position = StartPos + ((long)resentry.FileOffset * 512);

                        if (resentry.FileSize > 0)
                        {
                            uint offset = 0x10;
                            uint totlen = resentry.FileSize - offset;

                            byte[] tbytes = new byte[totlen];

                            br.BaseStream.Position += offset;

                            br.Read(tbytes, 0, (int)totlen);

                            byte[] decr;
                            if (IsAESEncrypted)
                            {
                                throw new NotImplementedException("AES encryption not implemented!");
                                // decr = GTACrypto.DecryptAES(tbytes);

                                //special case! probable duplicate pilot_school.ysc
                                ofpath = outputfolder + "\\" + Name + "___" + resentry.Name;
                            }
                            else
                            {
                                throw new NotImplementedException("NG encryption not implemented!");
                                // decr = GTACrypto.DecryptNG(tbytes, resentry.Name, resentry.FileSize);
                            }


                            try
                            {
                                MemoryStream ms = new MemoryStream(decr);
                                DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);

                                MemoryStream outstr = new MemoryStream();
                                ds.CopyTo(outstr);
                                byte[] deflated = outstr.GetBuffer();
                                byte[] outbuf = new byte[outstr.Length]; //need to copy to the right size buffer for File.WriteAllBytes().
                                Array.Copy(deflated, outbuf, outbuf.Length);

                                bool pathok = true;
                                if (File.Exists(ofpath))
                                {
                                    ofpath = outputfolder + "\\" + Name + "_" + resentry.Name;
                                    if (File.Exists(ofpath))
                                    {
                                        LastError = "Output file " + ofpath + " already exists!";
                                        pathok = false;
                                    }
                                }
                                if (pathok)
                                {
                                    File.WriteAllBytes(ofpath, outbuf);
                                }
                            }
                            catch (Exception ex)
                            {
                                LastError = ex.ToString();
                                LastException = ex;
                            }


                        }
                    }

                }
            }





        }





        public byte[] ExtractFile(RpfFileEntry entry)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(GetPhysicalFilePath())))
                {
                    if (entry is RpfBinaryFileEntry)
                    {
                        return ExtractFileBinary(entry as RpfBinaryFileEntry, br);
                    }
                    else if (entry is RpfResourceFileEntry)
                    {
                        return ExtractFileResource(entry as RpfResourceFileEntry, br);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex.ToString();
                LastException = ex;
                return null;
            }
        }
        public byte[] ExtractFileBinary(RpfBinaryFileEntry entry, BinaryReader br)
        {
            br.BaseStream.Position = StartPos + ((long)entry.FileOffset * 512);

            long l = entry.GetFileSize();

            if (l > 0)
            {
                uint offset = 0;// 0x10;
                uint totlen = (uint)l - offset;

                byte[] tbytes = new byte[totlen];

                br.BaseStream.Position += offset;
                br.Read(tbytes, 0, (int)totlen);

                byte[] decr = tbytes;

                if (entry.IsEncrypted)
                {
                    if (IsAESEncrypted)
                    {
                        throw new NotImplementedException("AES encryption not implemented!");
                        // decr = GTACrypto.DecryptAES(tbytes);
                    }
                    else //if (IsNGEncrypted) //assume the archive is set to NG encryption if not AES... (comment: fix for openIV modded files)
                    {
                        throw new NotImplementedException("NG encryption not implemented!");
                        // decr = GTACrypto.DecryptNG(tbytes, entry.Name, entry.FileUncompressedSize);
                    }
                    //else
                    //{ }
                }

                byte[] defl = decr;

                if (entry.FileSize > 0) //apparently this means it's compressed
                {
                    defl = DecompressBytes(decr);
                }
                else
                {
                }

                return defl;
            }

            return null;
        }
        public byte[] ExtractFileResource(RpfResourceFileEntry entry, BinaryReader br)
        {
            br.BaseStream.Position = StartPos + ((long)entry.FileOffset * 512);


            if (entry.FileSize > 0)
            {
                uint offset = 0x10;
                uint totlen = entry.FileSize - offset;

                byte[] tbytes = new byte[totlen];


                br.BaseStream.Position += offset;
                //byte[] hbytes = br.ReadBytes(16); //what are these 16 bytes actually used for?
                //if (entry.FileSize > 0xFFFFFF)
                //{ //(for huge files, the full file size is packed in 4 of these bytes... seriously wtf)
                //    var filesize = (hbytes[7] << 0) | (hbytes[14] << 8) | (hbytes[5] << 16) | (hbytes[2] << 24);
                //}


                br.Read(tbytes, 0, (int)totlen);

                byte[] decr = tbytes;
                if (entry.IsEncrypted)
                {
                    if (IsAESEncrypted)
                    {
                        throw new NotImplementedException("AES encryption not implemented!");
                        // decr = GTACrypto.DecryptAES(tbytes);
                    }
                    else //if (IsNGEncrypted) //assume the archive is set to NG encryption if not AES... (comment: fix for openIV modded files)
                    {
                        throw new NotImplementedException("NG encryption not implemented!");
                        // decr = GTACrypto.DecryptNG(tbytes, entry.Name, entry.FileSize);
                    }
                    //else
                    //{ }
                }

                byte[] deflated = DecompressBytes(decr);

                byte[] data = null;

                if (deflated != null)
                {
                    data = deflated;
                }
                else
                {
                    entry.FileSize -= offset;
                    data = decr;
                }


                return data;
            }

            return null;
        }

        public static T GetFile<T>(RpfEntry e) where T : class, PackedFile, new()
        {
            T file = null;
            byte[] data = null;
            RpfFileEntry entry = e as RpfFileEntry;
            if (entry != null)
            {
                data = entry.File.ExtractFile(entry);
            }
            if (data != null)
            {
                file = new T();
                file.Load(data, entry);
            }
            return file;
        }
        public static T GetFile<T>(RpfEntry e, byte[] data) where T : class, PackedFile, new()
        {
            T file = null;
            RpfFileEntry entry = e as RpfFileEntry;
            if ((data != null))
            {
                if (entry == null)
                {
                    entry = CreateResourceFileEntry(ref data, 0);
                }
                file = new T();
                file.Load(data, entry);
            }
            return file;
        }



        public static T GetResourceFile<T>(byte[] data) where T : class, PackedFile, new()
        {
            T file = null;
            RpfFileEntry entry = CreateResourceFileEntry(ref data, 0);
            if ((data != null) && (entry != null))
            {
                data = ResourceBuilder.Decompress(data);
                file = new T();
                file.Load(data, entry);
            }
            return file;
        }
        public static void LoadResourceFile<T>(T file, byte[] data, uint ver) where T : class, PackedFile
        {
            //direct load from a raw, compressed resource file (openIV-compatible format)

            RpfResourceFileEntry resentry = CreateResourceFileEntry(ref data, ver);

            if (file is GameFile)
            {
                GameFile gfile = file as GameFile;

                var oldresentry = gfile.RpfFileEntry as RpfResourceFileEntry;
                if (oldresentry != null) //update the existing entry with the new one
                {
                    oldresentry.SystemFlags = resentry.SystemFlags;
                    oldresentry.GraphicsFlags = resentry.GraphicsFlags;
                    resentry.Name = oldresentry.Name;
                    resentry.NameHash = oldresentry.NameHash;
                    resentry.NameLower = oldresentry.NameLower;
                    resentry.ShortNameHash = oldresentry.ShortNameHash;
                }
                else
                {
                    gfile.RpfFileEntry = resentry; //just stick it in there for later...
                }
            }

            data = ResourceBuilder.Decompress(data);

            file.Load(data, resentry);

        }
        public static RpfResourceFileEntry CreateResourceFileEntry(ref byte[] data, uint ver)
        {
            var resentry = new RpfResourceFileEntry();

            //hopefully this data has an RSC7 header...
            uint rsc7 = BitConverter.ToUInt32(data, 0);
            if (rsc7 == 0x37435352) //RSC7 header present!
            {
                int version = BitConverter.ToInt32(data, 4);//use this instead of what was given...
                resentry.SystemFlags = BitConverter.ToUInt32(data, 8);
                resentry.GraphicsFlags = BitConverter.ToUInt32(data, 12);
                if (data.Length > 16)
                {
                    int newlen = data.Length - 16; //trim the header from the data passed to the next step.
                    byte[] newdata = new byte[newlen];
                    Buffer.BlockCopy(data, 16, newdata, 0, newlen);
                    data = newdata;
                }
                //else
                //{
                //    data = null; //shouldn't happen... empty..
                //}
            }
            else
            {
                //direct load from file without the rpf header..
                //assume it's in resource meta format
                resentry.SystemFlags = RpfResourceFileEntry.GetFlagsFromSize(data.Length, 0);
                resentry.GraphicsFlags = RpfResourceFileEntry.GetFlagsFromSize(0, ver);
            }

            resentry.Name = "";
            resentry.NameLower = "";

            return resentry;
        }



        public string TestExtractAllFiles()
        {
            StringBuilder sb = new StringBuilder();
            ExtractedByteCount = 0;
            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(GetPhysicalFilePath())))
                {
                    foreach (RpfEntry entry in AllEntries)
                    {
                        try
                        {
                            LastError = string.Empty;
                            LastException = null;
                            if (!entry.NameLower.EndsWith(".rpf")) //don't try to extract rpf's, they will be done separately..
                            {
                                if (entry is RpfBinaryFileEntry)
                                {
                                    RpfBinaryFileEntry binentry = entry as RpfBinaryFileEntry;
                                    byte[] data = ExtractFileBinary(binentry, br);
                                    if (data == null)
                                    {
                                        if (binentry.FileSize == 0)
                                        {
                                            sb.AppendFormat("{0} : Binary FileSize is 0.", entry.Path);
                                            sb.AppendLine();
                                        }
                                        else
                                        {
                                            sb.AppendFormat("{0} : {1}", entry.Path, LastError);
                                            sb.AppendLine();
                                        }
                                    }
                                    else if (data.Length == 0)
                                    {
                                        sb.AppendFormat("{0} : Decompressed output was empty.", entry.Path);
                                        sb.AppendLine();
                                    }
                                    else
                                    {
                                        ExtractedByteCount += data.Length;
                                    }
                                }
                                else if (entry is RpfResourceFileEntry)
                                {
                                    RpfResourceFileEntry resentry = entry as RpfResourceFileEntry;
                                    byte[] data = ExtractFileResource(resentry, br);
                                    if (data == null)
                                    {
                                        if (resentry.FileSize == 0)
                                        {
                                            sb.AppendFormat("{0} : Resource FileSize is 0.", entry.Path);
                                            sb.AppendLine();
                                        }
                                        else
                                        {
                                            sb.AppendFormat("{0} : {1}", entry.Path, LastError);
                                            sb.AppendLine();
                                        }
                                    }
                                    else if (data.Length == 0)
                                    {
                                        sb.AppendFormat("{0} : Decompressed output was empty.", entry.Path);
                                        sb.AppendLine();
                                    }
                                    else
                                    {
                                        ExtractedByteCount += data.Length;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LastError = ex.ToString();
                            LastException = ex;
                            sb.AppendFormat("{0} : {1}", entry.Path, ex.Message);
                            sb.AppendLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex.ToString();
                LastException = ex;
                sb.AppendFormat("{0} : {1}", Path, ex.Message);
                sb.AppendLine();
                return null;
            }
            return sb.ToString();
        }




        public List<RpfFileEntry> GetFiles(string folder, bool recurse)
        {
            List<RpfFileEntry> result = new List<RpfFileEntry>();
            string[] parts = folder.ToLowerInvariant().Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            RpfDirectoryEntry dir = Root;
            for (int i = 0; i < parts.Length; i++)
            {
                if (dir == null) break;
                dir = FindSubDirectory(dir, parts[i]);
            }
            if (dir != null)
            {
                GetFiles(dir, result, recurse);
            }
            return result;
        }
        public void GetFiles(RpfDirectoryEntry dir, List<RpfFileEntry> result, bool recurse)
        {
            if (dir.Files != null)
            {
                result.AddRange(dir.Files);
            }
            if (recurse)
            {
                if (dir.Directories != null)
                {
                    for (int i = 0; i < dir.Directories.Count; i++)
                    {
                        GetFiles(dir.Directories[i], result, recurse);
                    }
                }
            }
        }

        private RpfDirectoryEntry FindSubDirectory(RpfDirectoryEntry dir, string name)
        {
            if (dir == null) return null;
            if (dir.Directories == null) return null;
            for (int i = 0; i < dir.Directories.Count; i++)
            {
                var cdir = dir.Directories[i];
                if (cdir.Name.ToLowerInvariant() == name)
                {
                    return cdir;
                }
            }
            return null;
        }




        public byte[] DecompressBytes(byte[] bytes)
        {
            try
            {
                using (DeflateStream ds = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress))
                {
                    MemoryStream outstr = new MemoryStream();
                    ds.CopyTo(outstr);
                    byte[] deflated = outstr.GetBuffer();
                    byte[] outbuf = new byte[outstr.Length]; //need to copy to the right size buffer for output.
                    Array.Copy(deflated, outbuf, outbuf.Length);

                    if (outbuf.Length <= bytes.Length)
                    {
                        LastError = "Warning: Decompressed data was smaller than compressed data...";
                        //return null; //could still be OK for tiny things!
                    }

                    return outbuf;
                }
            }
            catch (Exception ex)
            {
                LastError = "Could not decompress.";// ex.ToString();
                LastException = ex;
                return null;
            }
        }
        public static byte[] CompressBytes(byte[] data) //TODO: refactor this with ResourceBuilder.Compress/Decompress
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true);
                ds.Write(data, 0, data.Length);
                ds.Close();
                byte[] deflated = ms.GetBuffer();
                byte[] outbuf = new byte[ms.Length]; //need to copy to the right size buffer...
                Array.Copy(deflated, outbuf, outbuf.Length);
                return outbuf;
            }
        }














        private void WriteHeader(BinaryWriter bw)
        {
            var namesdata = GetHeaderNamesData();
            NamesLength = (uint)namesdata.Length;

            //ensure there's enough space for the new header, move things if necessary
            var headersize = GetHeaderBlockCount() * 512;
            EnsureSpace(bw, null, headersize);

            //entries may have been updated, so need to do this after ensuring header space
            var entriesdata = GetHeaderEntriesData();

            //FileSize = ... //need to make sure this is updated for NG encryption...
            switch (Encryption)
            {
                case RpfEncryption.NONE: //no encryption
                case RpfEncryption.OPEN: //OpenIV style RPF with unencrypted TOC
                    break;
                case RpfEncryption.AES:
                    throw new NotImplementedException("AES encryption not implemented!");
                    // entriesdata = GTACrypto.EncryptAES(entriesdata);
                    // namesdata = GTACrypto.EncryptAES(namesdata);
                    IsAESEncrypted = true;
                    break;
                case RpfEncryption.NG:
                    throw new NotImplementedException("NG encryption not implemented!");
                    // entriesdata = GTACrypto.EncryptNG(entriesdata, Name, (uint)FileSize);
                    // namesdata = GTACrypto.EncryptNG(namesdata, Name, (uint)FileSize);
                    IsNGEncrypted = true;
                    break;
                default: //unknown encryption type? assume NG.. should never get here!
                    throw new NotImplementedException("NG encryption not implemented!");
                    // entriesdata = GTACrypto.EncryptNG(entriesdata, Name, (uint)FileSize);
                    // namesdata = GTACrypto.EncryptNG(namesdata, Name, (uint)FileSize);
                    break;
            }

            //now there's enough space, it's safe to write the header data...
            bw.BaseStream.Position = StartPos;

            bw.Write(Version);
            bw.Write(EntryCount);
            bw.Write(NamesLength);
            bw.Write((uint)Encryption);
            bw.Write(entriesdata);
            bw.Write(namesdata);

            WritePadding(bw.BaseStream, StartPos + headersize); //makes sure the actual file can grow...
        }


        private static void WritePadding(Stream s, long upto)
        {
            int diff = (int)(upto - s.Position);
            if (diff > 0)
            {
                s.Write(new byte[diff], 0, diff);
            }
        }


        private void EnsureAllEntries()
        {
            if (AllEntries == null)
            {
                //assume this is a new RPF, create the root directory entry
                AllEntries = new List<RpfEntry>();
                Root = new RpfDirectoryEntry();
                Root.File = this;
                Root.Name = string.Empty;
                Root.NameLower = string.Empty;
                Root.Path = Path.ToLowerInvariant();
            }
            if (Children == null)
            {
                Children = new List<RpfFile>();
            }



            //re-build the AllEntries list from the root node.
            List<RpfEntry> temp = new List<RpfEntry>(); //for sorting
            AllEntries.Clear();
            AllEntries.Add(Root);
            Stack<RpfDirectoryEntry> stack = new Stack<RpfDirectoryEntry>();
            stack.Push(Root);
            while (stack.Count > 0)
            {
                var item = stack.Pop();

                item.EntriesCount = (uint)(item.Directories.Count + item.Files.Count);
                item.EntriesIndex = (uint)AllEntries.Count;

                //having items sorted by name is important for the game for some reason. (it crashes otherwise!)
                temp.Clear();
                temp.AddRange(item.Directories);
                temp.AddRange(item.Files);
                temp.Sort((a, b) => String.CompareOrdinal(a.Name, b.Name));

                foreach (var entry in temp)
                {
                    AllEntries.Add(entry);
                    RpfDirectoryEntry dir = entry as RpfDirectoryEntry;
                    if (dir != null)
                    {
                        stack.Push(dir);
                    }
                }
            }

            EntryCount = (uint)AllEntries.Count;

        }
        private byte[] GetHeaderNamesData()
        {
            MemoryStream namesstream = new MemoryStream();
            DataWriter nameswriter = new DataWriter(namesstream);
            var namedict = new Dictionary<string, uint>();
            foreach (var entry in AllEntries)
            {
                uint nameoffset;
                string name = entry.Name ?? "";
                if (namedict.TryGetValue(name, out nameoffset))
                {
                    entry.NameOffset = nameoffset;
                }
                else
                {
                    entry.NameOffset = (uint)namesstream.Length;
                    namedict.Add(name, entry.NameOffset);
                    nameswriter.Write(name);
                }
            }
            var buf = new byte[namesstream.Length];
            namesstream.Position = 0;
            namesstream.Read(buf, 0, buf.Length);
            return PadBuffer(buf, 16);
        }
        private byte[] GetHeaderEntriesData()
        {
            MemoryStream entriesstream = new MemoryStream();
            DataWriter entrieswriter = new DataWriter(entriesstream);
            foreach (var entry in AllEntries)
            {
                entry.Write(entrieswriter);
            }
            var buf = new byte[entriesstream.Length];
            entriesstream.Position = 0;
            entriesstream.Read(buf, 0, buf.Length);
            return buf;
        }
        private uint GetHeaderBlockCount()//make sure EntryCount and NamesLength are updated before calling this...
        {
            uint headerusedbytes = 16 + (EntryCount * 16) + NamesLength;
            uint headerblockcount = GetBlockCount(headerusedbytes);
            return headerblockcount;
        }
        private static byte[] PadBuffer(byte[] buf, uint n)//add extra bytes as necessary to nearest n
        {
            uint buflen = (uint)buf.Length;
            uint newlen = PadLength(buflen, n);
            if (newlen != buflen)
            {
                byte[] buf2 = new byte[newlen];
                Buffer.BlockCopy(buf, 0, buf2, 0, buf.Length);
                return buf2;
            }
            return buf;
        }
        private static uint PadLength(uint l, uint n)//round up to nearest n bytes
        {
            uint rem = l % n;
            return l + ((rem > 0) ? (n - rem) : 0);
        }
        private static uint GetBlockCount(long bytecount)
        {
            uint b0 = (uint)(bytecount & 0x1FF); //511;
            uint b1 = (uint)(bytecount >> 9);
            if (b0 == 0) return b1;
            return b1 + 1;
        }
        private RpfFileEntry FindFirstFileAfter(uint block)
        {
            RpfFileEntry nextentry = null;
            foreach (var entry in AllEntries)
            {
                RpfFileEntry fe = entry as RpfFileEntry;
                if ((fe != null) && (fe.FileOffset > block))
                {
                    if ((nextentry == null) || (fe.FileOffset < nextentry.FileOffset))
                    {
                        nextentry = fe;
                    }
                }
            }
            return nextentry;
        }
        private uint FindHole(uint reqblocks, uint ignorestart, uint ignoreend)
        {
            //find the block index of a hole that can fit the required number of blocks.
            //return 0 if no hole found (0 is the header block, it can't be used for files!)
            //make sure any found hole is not within the ignore range
            //(i.e. area where space is currently being made)

            //gather and sort the list of files to allow searching for holes
            List<RpfFileEntry> allfiles = new List<RpfFileEntry>();
            foreach (var entry in AllEntries)
            {
                RpfFileEntry rfe = entry as RpfFileEntry;
                if (rfe != null)
                {
                    allfiles.Add(rfe);
                }
            }
            allfiles.Sort((e1, e2) => e1.FileOffset.CompareTo(e2.FileOffset));

            //find the smallest available hole from the list.
            uint found = 0;
            uint foundsize = 0xFFFFFFFF;
            
            for (int i = 1; i < allfiles.Count(); i++)
            {
                RpfFileEntry e1 = allfiles[i - 1];
                RpfFileEntry e2 = allfiles[i];

                uint e1cnt = GetBlockCount(e1.GetFileSize());
                uint e1end = e1.FileOffset + e1cnt;
                uint e2beg = e2.FileOffset;
                if ((e2beg > ignorestart) && (e1end < ignoreend))
                {
                    continue; //this space is in the ignore area.
                }
                if (e1end < e2beg)
                {
                    uint space = e2beg - e1end;
                    if ((space >= reqblocks) && (space < foundsize))
                    {
                        found = e1end;
                        foundsize = space;
                    }
                }
            }

            return found;
        }
        private uint FindEndBlock()
        {
            //find the next available block after all other files (or after header if there's no files)
            uint endblock = 0;
            foreach (var entry in AllEntries)
            {
                RpfFileEntry e = entry as RpfFileEntry;
                if (e != null)
                {
                    uint ecnt = GetBlockCount(e.GetFileSize());
                    uint eend = e.FileOffset + ecnt;
                    if (eend > endblock)
                    {
                        endblock = eend;
                    }
                }
            }

            if (endblock == 0)
            {
                //must be no files present, end block comes directly after the header.
                endblock = GetHeaderBlockCount();
            }

            return endblock;
        }
        private void GrowArchive(BinaryWriter bw, uint newblockcount)
        {
            uint newsize = newblockcount * 512;
            if (newsize < FileSize)
            {
                return;//already bigger than it needs to be, can happen if last file got moved into a hole...
            }
            if (FileSize == newsize)
            {
                return;//nothing to do... correct size already
            }

            FileSize = newsize;


            //ensure enough space in the parent if there is one...
            if (Parent != null)
            {
                if (ParentFileEntry == null)
                {
                    throw new Exception("Can't grow archive " + Path + ": ParentFileEntry was null!");
                }


                //parent's header will be updated with these new values.
                ParentFileEntry.FileUncompressedSize = newsize;
                ParentFileEntry.FileSize = 0; //archives have FileSize==0 in parent...

                Parent.EnsureSpace(bw, ParentFileEntry, newsize);
            }
        }
        private void RelocateFile(BinaryWriter bw, RpfFileEntry f, uint newblock)
        {
            //directly move this file. does NOT update the header!
            //enough space should already be allocated for this move.

            uint flen = GetBlockCount(f.GetFileSize());
            uint fbeg = f.FileOffset;
            uint fend = fbeg + flen;
            uint nend = newblock + flen;
            if ((nend > fbeg) && (newblock < fend))//can't move to somewhere within itself!
            {
                throw new Exception("Unable to relocate file " + f.Path + ": new position was inside the original!");
            }

            var stream = bw.BaseStream;
            long origpos = stream.Position;
            long source = StartPos + ((long)fbeg * 512);
            long dest = StartPos + ((long)newblock * 512);
            long newstart = dest;
            long length = (long)flen * 512;
            long destend = dest + length;
            const int BUFFER_SIZE = 16384;//what buffer size is best for HDD copy?
            var buffer = new byte[BUFFER_SIZE];
            while (length > 0)
            {
                stream.Position = source;
                int i = stream.Read(buffer, 0, (int)Math.Min(length, BUFFER_SIZE));
                stream.Position = dest;
                stream.Write(buffer, 0, i);
                source += i;
                dest += i;
                length -= i;
            }

            WritePadding(stream, destend);//makes sure the stream can grow if necessary

            stream.Position = origpos;//reset this just to be nice

            f.FileOffset = newblock;

            //if this is a child RPF archive, need to update its StartPos...
            var child = FindChildArchive(f);
            if (child != null)
            {
                child.UpdateStartPos(newstart);
            }

        }
        private void EnsureSpace(BinaryWriter bw, RpfFileEntry e, long bytecount)
        {
            //(called with null entry for ensuring header space)

            uint blockcount = GetBlockCount(bytecount);
            uint startblock = e?.FileOffset ?? 0; //0 is always header block
            uint endblock = startblock + blockcount;

            RpfFileEntry nextentry = FindFirstFileAfter(startblock);

            while (nextentry != null) //just deal with relocating one entry at a time.
            {
                //move this nextentry to somewhere else... preferably into a hole otherwise at the end
                //if the RPF needs to grow, space needs to be ensured in the parent rpf (if there is one)...
                //keep moving further entries until enough space is gained.

                if (nextentry.FileOffset >= endblock)
                {
                    break; //already enough space for this entry, don't go further.
                }

                uint entryblocks = GetBlockCount(nextentry.GetFileSize());
                uint newblock = FindHole(entryblocks, startblock, endblock);
                if (newblock == 0)
                {
                    //no hole was found, move this entry to the end of the file.
                    newblock = FindEndBlock();
                    GrowArchive(bw, newblock + entryblocks);
                }

                //now move the file contents and update the entry's position.
                RelocateFile(bw, nextentry, newblock);

                //move on to the next file...
                nextentry = FindFirstFileAfter(startblock);
            }

            if (nextentry == null)
            {
                //last entry in the RPF, so just need to grow the RPF enough to fit.
                //this could be the header (for an empty RPF)...
                uint newblock = FindEndBlock();
                GrowArchive(bw, newblock + ((e != null) ? blockcount : 0));
            }

            //changing a file's size (not the header size!) - need to update the header..!
            //also, files could have been moved. so always update the header if we aren't already
            if (e != null)
            {
                WriteHeader(bw);
            }

        }
        private void InsertFileSpace(BinaryWriter bw, RpfFileEntry entry)
        {
            //to insert a new entry. find space in the archive for it and assign the FileOffset.

            uint blockcount = GetBlockCount(entry.GetFileSize());
            entry.FileOffset = FindHole(blockcount, 0, 0);
            if (entry.FileOffset == 0)
            {
                entry.FileOffset = FindEndBlock();
                GrowArchive(bw, entry.FileOffset + blockcount);
            }
            EnsureAllEntries();
            WriteHeader(bw);
        }

        private void WriteNewArchive(BinaryWriter bw, RpfEncryption encryption)
        {
            var stream = bw.BaseStream;
            Encryption = encryption;
            Version = 0x52504637; //'RPF7'
            IsAESEncrypted = (encryption == RpfEncryption.AES);
            IsNGEncrypted = (encryption == RpfEncryption.NG);
            StartPos = stream.Position;
            EnsureAllEntries();
            WriteHeader(bw);
            FileSize = stream.Position - StartPos;
        }

        private void UpdatePaths(RpfDirectoryEntry dir = null)
        {
            //recursively update paths, including in child RPFs.
            if (dir == null)
            {
                Root.Path = Path.ToLowerInvariant();
                dir = Root;
            }
            foreach (var file in dir.Files)
            {
                file.Path = dir.Path + "\\" + file.NameLower;

                RpfBinaryFileEntry binf = file as RpfBinaryFileEntry;
                if ((binf != null) && file.NameLower.EndsWith(".rpf"))
                {
                    RpfFile childrpf = FindChildArchive(binf);
                    if (childrpf != null)
                    {
                        childrpf.Path = binf.Path;
                        childrpf.FilePath = binf.Path;
                        childrpf.UpdatePaths();
                    }
                    else
                    { }//couldn't find child RPF! problem..!
                }

            }
            foreach (var subdir in dir.Directories)
            {
                subdir.Path = dir.Path + "\\" + subdir.NameLower;
                UpdatePaths(subdir);
            }
        }

        public RpfFile FindChildArchive(RpfFileEntry f)
        {
            RpfFile c = null;
            if (Children != null)
            {
                foreach (var child in Children)//kinda messy, but no other option really...
                {
                    if (child.ParentFileEntry == f)
                    {
                        c = child;
                        break;
                    }
                }
            }
            return c;
        }


        public long GetDefragmentedFileSize()
        {
            //this represents the size the file would be when fully defragmented.
            uint blockcount = GetHeaderBlockCount();

            foreach (var entry in AllEntries)
            {
                var fentry = entry as RpfFileEntry;
                if (fentry != null)
                {
                    blockcount += GetBlockCount(fentry.GetFileSize());
                }
            }

            return (long)blockcount * 512;
        }


        private void UpdateStartPos(long newpos)
        {
            StartPos = newpos;

            if (Children != null)
            {
                //make sure children also get their StartPos updated!
                foreach (var child in Children)
                {
                    if (child.ParentFileEntry == null) continue;//shouldn't really happen...
                    var cpos = StartPos + (long)child.ParentFileEntry.FileOffset * 512;
                    child.UpdateStartPos(cpos);
                }
            }
        }




        public static RpfFile CreateNew(string gtafolder, string relpath, RpfEncryption encryption = RpfEncryption.OPEN)
        {
            //create a new, empty RPF file in the filesystem
            //this will assume that the folder the file is going into already exists!

            string fpath = gtafolder;
            fpath = fpath.EndsWith("\\") ? fpath : fpath + "\\";
            fpath = fpath + relpath;

            if (File.Exists(fpath))
            {
                throw new Exception("File " + fpath + " already exists!");
            }

            File.Create(fpath).Dispose(); //just write a placeholder, will fill it out later

            RpfFile file = new RpfFile(fpath, relpath);

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    file.WriteNewArchive(bw, encryption);
                }
            }

            return file;
        }

        public static RpfFile CreateNew(RpfDirectoryEntry dir, string name, RpfEncryption encryption = RpfEncryption.OPEN)
        {
            //create a new empty RPF inside the given parent RPF directory.

            string namel = name.ToLowerInvariant();
            RpfFile parent = dir.File;
            string fpath = parent.GetPhysicalFilePath();
            string rpath = dir.Path + "\\" + namel;

            if (!File.Exists(fpath))
            {
                throw new Exception("Root RPF file " + fpath + " does not exist!");
            }


            RpfFile file = new RpfFile(name, rpath, 512);//empty RPF is 512 bytes...
            file.Parent = parent;
            file.ParentFileEntry = new RpfBinaryFileEntry();

            RpfBinaryFileEntry entry = file.ParentFileEntry;
            entry.Parent = dir;
            entry.FileOffset = 0;//InsertFileSpace will update this
            entry.FileSize = 0;
            entry.FileUncompressedSize = (uint)file.FileSize;
            entry.EncryptionType = 0;
            entry.IsEncrypted = false;
            entry.File = parent;
            entry.Path = rpath;
            entry.Name = name;
            entry.NameLower = namel;
            entry.NameHash = JenkHash.Hash(name);
            entry.ShortNameHash = JenkHash.Hash(entry.GetShortNameLower());

            dir.Files.Add(entry);

            parent.Children.Add(file);

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    parent.InsertFileSpace(bw, entry);

                    fstream.Position = parent.StartPos + entry.FileOffset * 512;

                    file.WriteNewArchive(bw, encryption);
                }
            }


            return file;
        }

        public static RpfDirectoryEntry CreateDirectory(RpfDirectoryEntry dir, string name)
        {
            //create a new directory inside the given parent dir

            RpfFile parent = dir.File;
            string namel = name.ToLowerInvariant();
            string fpath = parent.GetPhysicalFilePath();
            string rpath = dir.Path + "\\" + namel;

            if (!File.Exists(fpath))
            {
                throw new Exception("Root RPF file " + fpath + " does not exist!");
            }

            RpfDirectoryEntry entry = new RpfDirectoryEntry();
            entry.Parent = dir;
            entry.File = parent;
            entry.Path = rpath;
            entry.Name = name;
            entry.NameLower = namel;
            entry.NameHash = JenkHash.Hash(name);
            entry.ShortNameHash = JenkHash.Hash(entry.GetShortNameLower());

            foreach (var exdir in dir.Directories)
            {
                if (exdir.NameLower == entry.NameLower)
                {
                    throw new Exception("RPF Directory \"" + entry.Name + "\" already exists!");
                }
            }

            dir.Directories.Add(entry);

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    parent.EnsureAllEntries();
                    parent.WriteHeader(bw);
                }
            }

            return entry;
        }

        public static RpfFileEntry CreateFile(RpfDirectoryEntry dir, string name, byte[] data, bool overwrite = true)
        {
            string namel = name.ToLowerInvariant();
            if (overwrite)
            {
                foreach (var exfile in dir.Files)
                {
                    if (exfile.NameLower == namel)
                    {
                        //file already exists. delete the existing one first!
                        //this should probably be optimised to just replace the existing one...
                        //TODO: investigate along with ExploreForm.ReplaceSelected()
                        DeleteEntry(exfile);
                        break;
                    }
                }
            }
            //else fail if already exists..? items with the same name allowed?

            RpfFile parent = dir.File;
            string fpath = parent.GetPhysicalFilePath();
            string rpath = dir.Path + "\\" + namel;
            if (!File.Exists(fpath))
            {
                throw new Exception("Root RPF file " + fpath + " does not exist!");
            }


            RpfFileEntry entry = null;
            uint len = (uint)data.Length;


            bool isrpf = false;
            bool isawc = false;
            uint hdr = 0;
            if (len >= 16)
            {
                hdr = BitConverter.ToUInt32(data, 0);
            }

            if (hdr == 0x37435352) //'RSC7'
            {
                //RSC header is present... import as resource
                var rentry = new RpfResourceFileEntry();
                var version = BitConverter.ToUInt32(data, 4);
                rentry.SystemFlags = BitConverter.ToUInt32(data, 8);
                rentry.GraphicsFlags = BitConverter.ToUInt32(data, 12);
                rentry.FileSize = len;
                if (len >= 0xFFFFFF)
                {
                    //just....why
                    //FileSize = (buf[7] << 0) | (buf[14] << 8) | (buf[5] << 16) | (buf[2] << 24);
                    data[7] = (byte)((len >> 0) & 0xFF);
                    data[14] = (byte)((len >> 8) & 0xFF);
                    data[5] = (byte)((len >> 16) & 0xFF);
                    data[2] = (byte)((len >> 24) & 0xFF);
                }

                entry = rentry;
            }

            if (namel.EndsWith(".rpf") && (hdr == 0x52504637)) //'RPF7'
            {
                isrpf = true;
            }
            if (namel.EndsWith(".awc"))
            {
                isawc = true;
            }

            if (entry == null)
            {
                //no RSC7 header present, import as a binary file.
                var compressed = (isrpf||isawc) ? data : CompressBytes(data);
                var bentry = new RpfBinaryFileEntry();
                bentry.EncryptionType = 0;//TODO: binary encryption
                bentry.IsEncrypted = false;
                bentry.FileUncompressedSize = (uint)data.Length;
                bentry.FileSize = (isrpf||isawc) ? 0 : (uint)compressed.Length;
                if (bentry.FileSize > 0xFFFFFF)
                {
                    bentry.FileSize = 0;
                    compressed = data; 
                    //can't compress?? since apparently FileSize>0 means compressed...
                }
                data = compressed;
                entry = bentry;
            }

            entry.Parent = dir;
            entry.File = parent;
            entry.Path = rpath;
            entry.Name = name;
            entry.NameLower = name.ToLowerInvariant();
            entry.NameHash = JenkHash.Hash(name);
            entry.ShortNameHash = JenkHash.Hash(entry.GetShortNameLower());




            foreach (var exfile in dir.Files)
            {
                if (exfile.NameLower == entry.NameLower)
                {
                    throw new Exception("File \"" + entry.Name + "\" already exists!");
                }
            }



            dir.Files.Add(entry);


            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    parent.InsertFileSpace(bw, entry);
                    long bbeg = parent.StartPos + (entry.FileOffset * 512);
                    long bend = bbeg + (GetBlockCount(entry.GetFileSize()) * 512);
                    fstream.Position = bbeg;
                    fstream.Write(data, 0, data.Length);
                    WritePadding(fstream, bend); //write 0's until the end of the block.
                }
            }


            if (isrpf)
            {
                //importing a raw RPF archive. create the new RpfFile object, and read its headers etc.
                RpfFile file = new RpfFile(name, rpath, data.LongLength);
                file.Parent = parent;
                file.ParentFileEntry = entry as RpfBinaryFileEntry;
                file.StartPos = parent.StartPos + (entry.FileOffset * 512);
                parent.Children.Add(file);

                using (var fstream = File.OpenRead(fpath))
                {
                    using (var br = new BinaryReader(fstream))
                    {
                        fstream.Position = file.StartPos;
                        file.ScanStructure(br, null, null);
                    }
                }
            }

            return entry;
        }


        public static void RenameArchive(RpfFile file, string newname)
        {
            //updates all items in the RPF with the new path - no actual file changes made here
            //(since all the paths are generated at runtime and not stored)

            file.Name = newname;
            file.NameLower = newname.ToLowerInvariant();
            file.Path = GetParentPath(file.Path) + newname;
            file.FilePath = GetParentPath(file.FilePath) + newname;

            file.UpdatePaths();

        }

        public static void RenameEntry(RpfEntry entry, string newname)
        {
            //rename the entry in the RPF header... 
            //also make sure any relevant child paths are updated...

            string dirpath = GetParentPath(entry.Path);

            entry.Name = newname;
            entry.NameLower = newname.ToLowerInvariant();
            entry.Path = dirpath + newname;

            string sname = entry.GetShortNameLower();
            //JenkIndex.Ensure(sname);//could be anything... but it needs to be there
            entry.NameHash = JenkHash.Hash(newname);
            entry.ShortNameHash = JenkHash.Hash(sname);

            RpfFile parent = entry.File;
            string fpath = parent.GetPhysicalFilePath();

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    parent.EnsureAllEntries();
                    parent.WriteHeader(bw);
                }
            }

            if (entry is RpfDirectoryEntry)
            {
                //a folder was renamed, make sure all its children's paths get updated
                parent.UpdatePaths(entry as RpfDirectoryEntry);
            }

        }


        public static void DeleteEntry(RpfEntry entry)
        {
            //delete this entry from the RPF header.
            //also remove any references to this item in its parent directory...
            //if this is a directory entry, this will delete the contents first

            RpfFile parent = entry.File;
            string fpath = parent.GetPhysicalFilePath();
            if (!File.Exists(fpath))
            {
                throw new Exception("Root RPF file " + fpath + " does not exist!");
            }

            RpfDirectoryEntry entryasdir = entry as RpfDirectoryEntry;
            RpfFileEntry entryasfile = entry as RpfFileEntry;//it has to be one or the other...

            if (entryasdir != null)
            {
                var deldirs = entryasdir.Directories.ToArray();
                var delfiles = entryasdir.Files.ToArray();
                foreach(var deldir in deldirs)
                {
                    DeleteEntry(deldir);
                }
                foreach (var delfile in delfiles)
                {
                    DeleteEntry(delfile);
                }
            }

            if (entry.Parent == null)
            {
                throw new Exception("Parent directory is null! This shouldn't happen - please refresh the folder!");
            }

            if (entryasdir != null)
            {
                entry.Parent.Directories.Remove(entryasdir);
            }
            if (entryasfile != null)
            {
                entry.Parent.Files.Remove(entryasfile);

                var child = parent.FindChildArchive(entryasfile);
                if (child != null)
                {
                    parent.Children.Remove(child); //RPF file being deleted...
                }
            }

            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    parent.EnsureAllEntries();
                    parent.WriteHeader(bw);
                }
            }

        }


        public static bool EnsureValidEncryption(RpfFile file, Func<RpfFile, bool> confirm)
        {
            if (file == null) return false;

            //currently assumes OPEN is the valid encryption type.
            //TODO: support other encryption types!

            bool needsupd = false;
            var f = file;
            List<RpfFile> files = new List<RpfFile>();
            while (f != null)
            {
                if (f.Encryption != RpfEncryption.OPEN)
                {
                    if (!confirm(f))
                    {
                        return false;
                    }
                    needsupd = true;
                }
                if (needsupd)
                {
                    files.Add(f);
                }
                f = f.Parent;
            }

            //change encryption types, starting from the root rpf.
            files.Reverse();
            foreach (var cfile in files)
            {
                SetEncryptionType(cfile, RpfEncryption.OPEN);
            }

            return true;
        }

        public static void SetEncryptionType(RpfFile file, RpfEncryption encryption)
        {
            file.Encryption = encryption;
            string fpath = file.GetPhysicalFilePath();
            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    file.WriteHeader(bw);
                }
            }
        }


        public static void Defragment(RpfFile file, Action<string, float> progress = null)
        {
            if (file?.AllEntries == null) return;

            string fpath = file.GetPhysicalFilePath();
            using (var fstream = File.Open(fpath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var bw = new BinaryWriter(fstream))
                {
                    uint destblock = file.GetHeaderBlockCount();

                    const int BUFFER_SIZE = 16384;//what buffer size is best for HDD copy?
                    var buffer = new byte[BUFFER_SIZE];

                    var allfiles = new List<RpfFileEntry>();
                    for (int i = 0; i < file.AllEntries.Count; i++)
                    {
                        var entry = file.AllEntries[i] as RpfFileEntry;
                        if (entry != null) allfiles.Add(entry);
                    }
                    //make sure we process everything in the current order that they are in the archive
                    allfiles.Sort((a, b) => { return a.FileOffset.CompareTo(b.FileOffset); });

                    for (int i = 0; i < allfiles.Count; i++)
                    {
                        var entry = allfiles[i];
                        float prog = (float)i / allfiles.Count;
                        string txt = "Relocating " + entry.Name + "...";
                        progress?.Invoke(txt, prog);

                        var sourceblock = entry.FileOffset;
                        var blockcount = GetBlockCount(entry.GetFileSize());

                        if (sourceblock > destblock) //should only be moving things toward the start
                        {
                            var source = file.StartPos + (long)sourceblock * 512;
                            var dest = file.StartPos + (long)destblock * 512;
                            var remlength = (long)blockcount * 512;
                            while (remlength > 0)
                            {
                                fstream.Position = source;
                                int n = fstream.Read(buffer, 0, (int)Math.Min(remlength, BUFFER_SIZE));
                                fstream.Position = dest;
                                fstream.Write(buffer, 0, n);
                                source += n;
                                dest += n;
                                remlength -= n;
                            }
                            entry.FileOffset = destblock;

                            var entryrpf = file.FindChildArchive(entry);
                            if (entryrpf != null)
                            {
                                entryrpf.UpdateStartPos(file.StartPos + (long)entry.FileOffset * 512);
                            }
                        }
                        else if (sourceblock != destblock)
                        { }//shouldn't get here...

                        destblock += blockcount;
                    }

                    file.FileSize = (long)destblock * 512;

                    file.WriteHeader(bw);

                    if (file.ParentFileEntry != null)
                    {
                        //make sure to also update the parent archive file entry, if there is one
                        file.ParentFileEntry.FileUncompressedSize = (uint)file.FileSize;
                        file.ParentFileEntry.FileSize = 0;
                        if (file.Parent != null)
                        {
                            file.Parent.WriteHeader(bw);
                        }
                    }
                    if (file.Parent == null)
                    {
                        //this is a root archive, so update the file's length to the new size.
                        fstream.SetLength(file.FileSize);
                    }
                }
            }
        }



        private static string GetParentPath(string path)
        {
            string dirpath = path.Replace('/', '\\');//just to make sure..
            int lidx = dirpath.LastIndexOf('\\');
            if (lidx > 0)
            {
                dirpath = dirpath.Substring(0, lidx + 1);
            }
            if (!dirpath.EndsWith("\\"))
            {
                dirpath = dirpath + "\\";
            }
            return dirpath;
        }


        public override string ToString()
        {
            return Path;
        }
    }


    public enum RpfEncryption : uint
    {
        NONE = 0, //some modded RPF's may use this
        OPEN = 0x4E45504F, //1313165391 "OPEN", ie. "no encryption"
        AES =  0x0FFFFFF9, //268435449
        NG =   0x0FEFFFFF, //267386879
    }


    public abstract class RpfEntry
    {
        public RpfFile File { get; set; }
        public RpfDirectoryEntry Parent { get; set; }

        public uint NameHash { get; set; }
        public uint ShortNameHash { get; set; }

        public uint NameOffset { get; set; }
        public string Name { get; set; }
        public string NameLower { get; set; }
        public string Path { get; set; }

        public uint H1; //first 2 header values from RPF table...
        public uint H2;

        public abstract void Read(DataReader reader);
        public abstract void Write(DataWriter writer);

        public override string ToString()
        {
            return Path;
        }

        public string GetShortName()
        {
            int ind = Name.LastIndexOf('.');
            if (ind > 0)
            {
                return Name.Substring(0, ind);
            }
            return Name;
        }
        public string GetShortNameLower()
        {
            if (NameLower == null)
            {
                NameLower = Name.ToLowerInvariant();
            }
            int ind = NameLower.LastIndexOf('.');
            if (ind > 0)
            {
                return NameLower.Substring(0, ind);
            }
            return NameLower;
        }
    }

    public class RpfDirectoryEntry : RpfEntry
    {
        public uint EntriesIndex { get; set; }
        public uint EntriesCount { get; set; }

        public List<RpfDirectoryEntry> Directories = new List<RpfDirectoryEntry>();
        public List<RpfFileEntry> Files = new List<RpfFileEntry>();

        public override void Read(DataReader reader)
        {
            NameOffset = reader.ReadUInt32();
            uint ident = reader.ReadUInt32();
            if (ident != 0x7FFFFF00u)
            {
                throw new Exception("Error in RPF7 directory entry.");
            }
            EntriesIndex = reader.ReadUInt32();
            EntriesCount = reader.ReadUInt32();
        }
        public override void Write(DataWriter writer)
        {
            writer.Write(NameOffset);
            writer.Write(0x7FFFFF00u);
            writer.Write(EntriesIndex);
            writer.Write(EntriesCount);
        }
        public override string ToString()
        {
            return "Directory: " + Path;
        }
    }

    public abstract class RpfFileEntry : RpfEntry
    {
        public uint FileOffset { get; set; }
        public uint FileSize { get; set; }
        public bool IsEncrypted { get; set; }

        public abstract long GetFileSize();
        public abstract void SetFileSize(uint s);
    }

    public class RpfBinaryFileEntry : RpfFileEntry
    {
        public uint FileUncompressedSize { get; set; }
        public uint EncryptionType { get; set; }

        public override void Read(DataReader reader)
        {
            ulong buf = reader.ReadUInt64();
            NameOffset = (uint)buf & 0xFFFF;
            FileSize = (uint)(buf >> 16) & 0xFFFFFF;
            FileOffset = (uint)(buf >> 40) & 0xFFFFFF;

            FileUncompressedSize = reader.ReadUInt32();

            EncryptionType = reader.ReadUInt32();

            switch (EncryptionType)
            {
                case 0: IsEncrypted = false; break;
                case 1: IsEncrypted = true; break;
                default:
                    throw new Exception("Error in RPF7 file entry.");
            }

        }
        public override void Write(DataWriter writer)
        {
            writer.Write((ushort)NameOffset);

            var buf1 = new byte[] {
                (byte)((FileSize >> 0) & 0xFF),
                (byte)((FileSize >> 8) & 0xFF),
                (byte)((FileSize >> 16) & 0xFF)
            };
            writer.Write(buf1);

            var buf2 = new byte[] {
                (byte)((FileOffset >> 0) & 0xFF),
                (byte)((FileOffset >> 8) & 0xFF),
                (byte)((FileOffset >> 16) & 0xFF)
            };
            writer.Write(buf2);

            writer.Write(FileUncompressedSize);

            if (IsEncrypted)
                writer.Write((uint)1);
            else
                writer.Write((uint)0);
        }
        public override string ToString()
        {
            return "Binary file: " + Path;
        }

        public override long GetFileSize()
        {
            return (FileSize == 0) ? FileUncompressedSize : FileSize;
        }
        public override void SetFileSize(uint s)
        {
            //FileUncompressedSize = s;
            FileSize = s;
        }
    }

    public class RpfResourceFileEntry : RpfFileEntry
    {
        public RpfResourcePageFlags SystemFlags { get; set; }
        public RpfResourcePageFlags GraphicsFlags { get; set; }


        public static int GetSizeFromFlags(uint flags)
        {
            //dexfx simplified version
            var s0 = ((flags >> 27) & 0x1)  << 0;   // 1 bit  - 27        (*1)
            var s1 = ((flags >> 26) & 0x1)  << 1;   // 1 bit  - 26        (*2)
            var s2 = ((flags >> 25) & 0x1)  << 2;   // 1 bit  - 25        (*4)
            var s3 = ((flags >> 24) & 0x1)  << 3;   // 1 bit  - 24        (*8)
            var s4 = ((flags >> 17) & 0x7F) << 4;   // 7 bits - 17 - 23   (*16)   (max 127 * 16)
            var s5 = ((flags >> 11) & 0x3F) << 5;   // 6 bits - 11 - 16   (*32)   (max 63  * 32)
            var s6 = ((flags >> 7)  & 0xF)  << 6;   // 4 bits - 7  - 10   (*64)   (max 15  * 64)
            var s7 = ((flags >> 5)  & 0x3)  << 7;   // 2 bits - 5  - 6    (*128)  (max 3   * 128)
            var s8 = ((flags >> 4)  & 0x1)  << 8;   // 1 bit  - 4         (*256)
            var ss = ((flags >> 0)  & 0xF);         // 4 bits - 0  - 3
            var baseSize = 0x200 << (int)ss;
            var size = baseSize * (s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8);
            return (int)size;


            #region dexyfex testing
            //var type = flags >> 28;
            //var test = GetFlagsFromSize((int)size, type);
            //s0 = ((test >> 27) & 0x1) << 0;   // 1 bit  - 27        (*1)
            //s1 = ((test >> 26) & 0x1) << 1;   // 1 bit  - 26        (*2)
            //s2 = ((test >> 25) & 0x1) << 2;   // 1 bit  - 25        (*4)
            //s3 = ((test >> 24) & 0x1) << 3;   // 1 bit  - 24        (*8)
            //s4 = ((test >> 17) & 0x7F) << 4;   // 7 bits - 17 - 23   (*16)   (max 127 * 16)
            //s5 = ((test >> 11) & 0x3F) << 5;   // 6 bits - 11 - 16   (*32)   (max 63  * 32)
            //s6 = ((test >> 7) & 0xF) << 6;   // 4 bits - 7  - 10   (*64)   (max 15  * 64)
            //s7 = ((test >> 5) & 0x3) << 7;   // 2 bits - 5  - 6    (*128)  (max 3   * 128)
            //s8 = ((test >> 4) & 0x1) << 8;   // 1 bit  - 4         (*256)
            //ss = ((test >> 0) & 0xF);         // 4 bits - 0  - 3
            //baseSize = 0x200 << (int)ss;
            //var tsize = baseSize * (s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8);
            //if (tsize != size)
            //{ }


            //if (s8 == 256)
            //{ }
            //if ((s0 != 0) || (s1 != 0) || (s2 != 0) || (s3 != 0))
            //{ }


            //return (int)size;

            //examples:
            //size:8192,    ss:0,             s4:1                              (ytd)
            //size:16384,   ss:0,                    s5:1                       (ytyp)
            //size:24576,   ss:0,             s4:1,  s5:1                       (ytyp)
            //size:40960,   ss:0,             s4:1,  s5:2                       (ytyp)
            //size:49152,   ss:0,             s4:2,  s5:2                       (ytyp)
            //size:237568,  ss:0,             s4:5,               s7:1, s8:1    (yft)
            //size:262144,  ss:1,                                       s8:1    (yft)
            //size:589824,  ss:1,                           s6:9                (ytd)
            //size:663552,  ss:1,       s3:1, s4:12,        s6:1, s7:3          (ydd) 
            //size:606208,  ss:2,       s3:1, s4:2,                     s8:1    (ydr)
            //size:958464,  ss:2, s2:1,       s4:1,         s6:3,       s8:1    (ydr)
            //size:966656,  ss:2,       s3:1, s4:1,         s6:3,       s8:1    (ydr)
            //size:1695744, ss:2, s2:1, s3:1, s4:5,  s5:3,        s7:3, s8:1    (ydr)
            //size:2768896, ss:3, s2:1,       s4:24, s5:1,  s6:4                (ydd)
            //size:4063232, ss:4,             s4:15,              s7:2          (ytd)
            //size:8650752, ss:5,             s4:13,        s6:5                (ytd)




            #endregion



            #region  original neo version (system)
            //const int RESOURCE_IDENT = 0x37435352;
            //const int BASE_SIZE = 0x2000;
            //var SystemPagesDiv16 = (int)(SystemFlags >> 27) & 0x1;
            //var SystemPagesDiv8 = (int)(SystemFlags >> 26) & 0x1;
            //var SystemPagesDiv4 = (int)(SystemFlags >> 25) & 0x1;
            //var SystemPagesDiv2 = (int)(SystemFlags >> 24) & 0x1;
            //var SystemPagesMul1 = (int)(SystemFlags >> 17) & 0x7F;
            //var SystemPagesMul2 = (int)(SystemFlags >> 11) & 0x3F;
            //var SystemPagesMul4 = (int)(SystemFlags >> 7) & 0xF;
            //var SystemPagesMul8 = (int)(SystemFlags >> 5) & 0x3;
            //var SystemPagesMul16 = (int)(SystemFlags >> 4) & 0x1;
            //var SystemPagesSizeShift = (int)(SystemFlags >> 0) & 0xF;
            //var systemBaseSize = BASE_SIZE << SystemPagesSizeShift;
            //return
            //    (systemBaseSize * SystemPagesDiv16) / 16 +
            //    (systemBaseSize * SystemPagesDiv8) / 8 +
            //    (systemBaseSize * SystemPagesDiv4) / 4 +
            //    (systemBaseSize * SystemPagesDiv2) / 2 +
            //    (systemBaseSize * SystemPagesMul1) * 1 +
            //    (systemBaseSize * SystemPagesMul2) * 2 +
            //    (systemBaseSize * SystemPagesMul4) * 4 +
            //    (systemBaseSize * SystemPagesMul8) * 8 +
            //    (systemBaseSize * SystemPagesMul16) * 16;
            #endregion


            #region  original neo version (graphics)
            //const int RESOURCE_IDENT = 0x37435352;
            //const int BASE_SIZE = 0x2000;
            //var GraphicsPagesDiv16 = (int)(GraphicsFlags >> 27) & 0x1;
            //var GraphicsPagesDiv8 = (int)(GraphicsFlags >> 26) & 0x1;
            //var GraphicsPagesDiv4 = (int)(GraphicsFlags >> 25) & 0x1;
            //var GraphicsPagesDiv2 = (int)(GraphicsFlags >> 24) & 0x1;
            //var GraphicsPagesMul1 = (int)(GraphicsFlags >> 17) & 0x7F;
            //var GraphicsPagesMul2 = (int)(GraphicsFlags >> 11) & 0x3F;
            //var GraphicsPagesMul4 = (int)(GraphicsFlags >> 7) & 0xF;
            //var GraphicsPagesMul8 = (int)(GraphicsFlags >> 5) & 0x3;
            //var GraphicsPagesMul16 = (int)(GraphicsFlags >> 4) & 0x1;
            //var GraphicsPagesSizeShift = (int)(GraphicsFlags >> 0) & 0xF;
            //var graphicsBaseSize = BASE_SIZE << GraphicsPagesSizeShift;
            //return
            //    graphicsBaseSize * GraphicsPagesDiv16 / 16 +
            //    graphicsBaseSize * GraphicsPagesDiv8 / 8 +
            //    graphicsBaseSize * GraphicsPagesDiv4 / 4 +
            //    graphicsBaseSize * GraphicsPagesDiv2 / 2 +
            //    graphicsBaseSize * GraphicsPagesMul1 * 1 +
            //    graphicsBaseSize * GraphicsPagesMul2 * 2 +
            //    graphicsBaseSize * GraphicsPagesMul4 * 4 +
            //    graphicsBaseSize * GraphicsPagesMul8 * 8 +
            //    graphicsBaseSize * GraphicsPagesMul16 * 16;
            #endregion

        }
        public static uint GetFlagsFromSize(int size, uint version)
        {
            //WIP - may make crashes :(
            //type: see SystemSize and GraphicsSize below

            //aim for s4: blocksize (0 remainder for 0x2000 block) 
            int origsize = size;
            int remainder = size & 0x1FF;
            int blocksize = 0x200;
            if (remainder != 0)
            {
                size = (size - remainder) + blocksize; //round up to the minimum blocksize
            }

            uint blockcount = (uint)size >> 9; //how many blocks of the minimum size (0x200)
            uint ss = 0;
            while (blockcount > 1024)
            {
                ss++;
                blockcount = blockcount >> 1;
            }
            if (ss > 0)
            {
                size = origsize;
                blocksize = blocksize << (int)ss; //adjust the block size to reduce the block count.
                remainder = size & blocksize;
                if(remainder!=0)
                {
                    size = (size - remainder) + blocksize; //readjust size with round-up
                }
            }

            var s0 = (blockcount >> 0) & 0x1;  //*1         X
            var s1 = (blockcount >> 1) & 0x1;  //*2          X
            var s2 = (blockcount >> 2) & 0x1;  //*4           X
            var s3 = (blockcount >> 3) & 0x1;  //*8            X
            var s4 = (blockcount >> 4) & 0x7F; //*16  7 bits    XXXXXXX
            var s5 = (blockcount >> 5) & 0x3F; //*32  6 bits           XXXXXX
            var s6 = (blockcount >> 6) & 0xF;  //*64  4 bits                 XXXX
            var s7 = (blockcount >> 7) & 0x3;  //*128 2 bits                     XX
            var s8 = (blockcount >> 8) & 0x1;  //*256                              X

            if (ss > 4)
            { }
            if (s4 > 0x7F)
            { } //too big...
            //needs more work to include higher bits..


            uint f = 0;
            f |= (version & 0xF) << 28;
            f |= (s0 & 0x1) << 27;
            f |= (s1 & 0x1) << 26;
            f |= (s2 & 0x1) << 25;
            f |= (s3 & 0x1) << 24;
            f |= (s4 & 0x7F) << 17;
            f |= (ss & 0xF);
            


            return f;


            //var s0 = ((flags >> 27) & 0x1) << 0;   // 1 bit  - 27        (*1)
            //var s1 = ((flags >> 26) & 0x1) << 1;   // 1 bit  - 26        (*2)
            //var s2 = ((flags >> 25) & 0x1) << 2;   // 1 bit  - 25        (*4)
            //var s3 = ((flags >> 24) & 0x1) << 3;   // 1 bit  - 24        (*8)
            //var s4 = ((flags >> 17) & 0x7F) << 4;   // 7 bits - 17 - 23   (*16)   (max 127 * 16)
            //var s5 = ((flags >> 11) & 0x3F) << 5;   // 6 bits - 11 - 16   (*32)   (max 63  * 32)
            //var s6 = ((flags >> 7) & 0xF) << 6;   // 4 bits - 7  - 10   (*64)   (max 15  * 64)
            //var s7 = ((flags >> 5) & 0x3) << 7;   // 2 bits - 5  - 6    (*128)  (max 3   * 128)
            //var s8 = ((flags >> 4) & 0x1) << 8;   // 1 bit  - 4         (*256)
            //var ss = ((flags >> 0) & 0xF);         // 4 bits - 0  - 3
            //var baseSize = 0x200 << (int)ss;
            //var size = baseSize * (s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8);


        }
        public static uint GetFlagsFromBlocks(uint blockCount, uint blockSize, uint version)
        {

            //dexfx test version - seems to work mostly...

            uint s0 = 0;
            uint s1 = 0;
            uint s2 = 0;
            uint s3 = 0;
            uint s4 = 0;
            uint s5 = 0;
            uint s6 = 0;
            uint s7 = 0;
            uint s8 = 0;
            uint ss = 0;

            uint bst = blockSize;
            if (blockCount > 0)
            {
                while (bst > 0x200) //ss is number of bits to shift 0x200 to get blocksize...
                {
                    ss++;
                    bst = bst >> 1;
                }
            }
            s0 = (blockCount >> 0) & 0x1;  //*1         X
            s1 = (blockCount >> 1) & 0x1;  //*2          X
            s2 = (blockCount >> 2) & 0x1;  //*4           X
            s3 = (blockCount >> 3) & 0x1;  //*8            X
            s4 = (blockCount >> 4) & 0x7F; //*16  7 bits    XXXXXXX
            //s5 = (blockCount >> 5) & 0x3F; //*32  6 bits           XXXXXX
            //s6 = (blockCount >> 6) & 0xF;  //*64  4 bits                 XXXX
            //s7 = (blockCount >> 7) & 0x3;  //*128 2 bits                     XX
            //s8 = (blockCount >> 8) & 0x1;  //*256                              X


            //if (blockCount > 0)
            //{
            //    var curblocksize = 0x2000u;
            //    var totsize = blockCount * blockSize;
            //    var totcount = totsize / curblocksize;
            //    if ((totsize % curblocksize) > 0) totcount++;
            //    ss = 4;
            //    while (totcount > 0x7f)
            //    {
            //        ss++;
            //        curblocksize = curblocksize << 1;
            //        totcount = totsize / curblocksize;
            //        if ((totsize % curblocksize) > 0) totcount++;
            //        if (ss >= 16)
            //        { break; }
            //    }
            //    s4 = totcount >> 4;
            //    s3 = (totcount >> 3) & 1;
            //    s2 = (totcount >> 2) & 1;
            //    s1 = (totcount >> 1) & 1;
            //    s0 = (totcount >> 0) & 1;
            //}



            if (ss > 0xF)
            { } //too big...
            if (s4 > 0x7F)
            { } //too big...
            //needs more work to include higher bits..


            uint f = 0;
            f |= (version & 0xF) << 28;
            f |= (s0 & 0x1) << 27;
            f |= (s1 & 0x1) << 26;
            f |= (s2 & 0x1) << 25;
            f |= (s3 & 0x1) << 24;
            f |= (s4 & 0x7F) << 17;
            f |= (s5 & 0x3F) << 11;
            f |= (s6 & 0xF) << 7;
            f |= (s7 & 0x3) << 5;
            f |= (s8 & 0x1) << 4;
            f |= (ss & 0xF);



            return f;
        }
        public static int GetVersionFromFlags(uint sysFlags, uint gfxFlags)
        {
            var sv = (sysFlags >> 28) & 0xF;
            var gv = (gfxFlags >> 28) & 0xF;
            return (int)((sv << 4) + gv);
        }


        public int Version
        {
            get
            {
                return GetVersionFromFlags(SystemFlags, GraphicsFlags);
            }
        }


        public int SystemSize
        {
            get
            {
                return (int)SystemFlags.Size;
            }
        }
        public int GraphicsSize
        {
            get
            {
                return (int)GraphicsFlags.Size;
            }
        }

        public override void Read(DataReader reader)
        {
            NameOffset = reader.ReadUInt16();

            var buf1 = reader.ReadBytes(3);
            FileSize = (uint)buf1[0] + (uint)(buf1[1] << 8) + (uint)(buf1[2] << 16);

            var buf2 = reader.ReadBytes(3);
            FileOffset = ((uint)buf2[0] + (uint)(buf2[1] << 8) + (uint)(buf2[2] << 16)) & 0x7FFFFF;

            SystemFlags = reader.ReadUInt32();
            GraphicsFlags = reader.ReadUInt32();

            // there are sometimes resources with length=0xffffff which actually
            // means length>=0xffffff
            if (FileSize == 0xFFFFFF)
            {
                BinaryReader cfr = File.CurrentFileReader;
                long opos = cfr.BaseStream.Position;
                cfr.BaseStream.Position = File.StartPos + ((long)FileOffset * 512); //need to use the base offset!!
                var buf = cfr.ReadBytes(16);
                FileSize = ((uint)buf[7] << 0) | ((uint)buf[14] << 8) | ((uint)buf[5] << 16) | ((uint)buf[2] << 24);
                cfr.BaseStream.Position = opos;
            }

        }
        public override void Write(DataWriter writer)
        {
            writer.Write((ushort)NameOffset);

            var fs = FileSize;
            if (fs > 0xFFFFFF) fs = 0xFFFFFF;//will also need to make sure the RSC header is updated...

            var buf1 = new byte[] {
                (byte)((fs >> 0) & 0xFF),
                (byte)((fs >> 8) & 0xFF),
                (byte)((fs >> 16) & 0xFF)
            };
            writer.Write(buf1);

            var buf2 = new byte[] {
                (byte)((FileOffset >> 0) & 0xFF),
                (byte)((FileOffset >> 8) & 0xFF),
                (byte)(((FileOffset >> 16) & 0xFF) | 0x80)
            };
            writer.Write(buf2);

            writer.Write(SystemFlags);
            writer.Write(GraphicsFlags);
        }
        public override string ToString()
        {
            return "Resource file: " + Path;
        }

        public override long GetFileSize()
        {
            return (FileSize == 0) ? (long)(SystemSize + GraphicsSize) : FileSize;
        }
        public override void SetFileSize(uint s)
        {
            FileSize = s;
        }
    }

    public struct RpfResourcePageFlags
    {
        public uint Value { get; set; }
        
        public RpfResourcePage[] Pages
        {
            get
            {
                var count = Count;
                if (count == 0) return null;
                var pages = new RpfResourcePage[count];
                var counts = PageCounts;
                var sizes = BaseSizes;
                int n = 0;
                uint o = 0;
                for (int i = 0; i < counts.Length; i++)
                {
                    var c = counts[i];
                    var s = sizes[i];
                    for (int p = 0; p < c; p++)
                    {
                        pages[n] = new RpfResourcePage() { Size = s, Offset = o };
                        o += s;
                        n++;
                    }
                }
                return pages;
            }
        }

        public uint TypeVal { get { return (Value >> 28) & 0xF; } }
        public uint BaseShift { get { return (Value & 0xF); } }
        public uint BaseSize { get { return (0x200u << (int)BaseShift); } }
        public uint[] BaseSizes
        {
            get
            {
                var baseSize = BaseSize;
                return new uint[]
                {
                    baseSize << 8,
                    baseSize << 7,
                    baseSize << 6,
                    baseSize << 5,
                    baseSize << 4,
                    baseSize << 3,
                    baseSize << 2,
                    baseSize << 1,
                    baseSize << 0,
                };
            }
        }
        public uint[] PageCounts
        {
            get
            {
                return new uint[]
                {
                    ((Value >> 4)  & 0x1),
                    ((Value >> 5)  & 0x3),
                    ((Value >> 7)  & 0xF),
                    ((Value >> 11) & 0x3F),
                    ((Value >> 17) & 0x7F),
                    ((Value >> 24) & 0x1),
                    ((Value >> 25) & 0x1),
                    ((Value >> 26) & 0x1),
                    ((Value >> 27) & 0x1),
                };
            }
        }
        public uint[] PageSizes
        {
            get
            {
                var counts = PageCounts;
                var baseSizes = BaseSizes;
                return new uint[]
                {
                    baseSizes[0] * counts[0],
                    baseSizes[1] * counts[1],
                    baseSizes[2] * counts[2],
                    baseSizes[3] * counts[3],
                    baseSizes[4] * counts[4],
                    baseSizes[5] * counts[5],
                    baseSizes[6] * counts[6],
                    baseSizes[7] * counts[7],
                    baseSizes[8] * counts[8],
                };
            }
        }
        public uint Count
        {
            get
            {
                var c = PageCounts;
                return c[0] + c[1] + c[2] + c[3] + c[4] + c[5] + c[6] + c[7] + c[8];
            }
        }
        public uint Size 
        { 
            get 
            {
                var flags = Value;
                var s0 = ((flags >> 27) & 0x1)  << 0;
                var s1 = ((flags >> 26) & 0x1)  << 1;
                var s2 = ((flags >> 25) & 0x1)  << 2;
                var s3 = ((flags >> 24) & 0x1)  << 3;
                var s4 = ((flags >> 17) & 0x7F) << 4;
                var s5 = ((flags >> 11) & 0x3F) << 5;
                var s6 = ((flags >> 7)  & 0xF)  << 6;
                var s7 = ((flags >> 5)  & 0x3)  << 7;
                var s8 = ((flags >> 4)  & 0x1)  << 8;
                var ss = ((flags >> 0)  & 0xF);
                var baseSize = 0x200u << (int)ss;
                return baseSize * (s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8);
            }
        }



        public RpfResourcePageFlags(uint v)
        {
            Value = v;
        }

        public RpfResourcePageFlags(uint[] pageCounts, uint baseShift)
        {
            var v = baseShift & 0xF;
            v += (pageCounts[0] & 0x1)  << 4;
            v += (pageCounts[1] & 0x3)  << 5;
            v += (pageCounts[2] & 0xF)  << 7;
            v += (pageCounts[3] & 0x3F) << 11;
            v += (pageCounts[4] & 0x7F) << 17;
            v += (pageCounts[5] & 0x1)  << 24;
            v += (pageCounts[6] & 0x1)  << 25;
            v += (pageCounts[7] & 0x1)  << 26;
            v += (pageCounts[8] & 0x1)  << 27;
            Value = v;
        }


        public static implicit operator uint(RpfResourcePageFlags f)
        {
            return f.Value;  //implicit conversion
        }
        public static implicit operator RpfResourcePageFlags(uint v)
        {
            return new RpfResourcePageFlags(v);
        }

        public override string ToString()
        {
            return "Size: " + Size.ToString() + ", Pages: " + Count.ToString();
        }
    }

    public struct RpfResourcePage
    {
        public uint Size { get; set; }
        public uint Offset { get; set; }

        public override string ToString()
        {
            return Size.ToString() + ": " + Offset.ToString();
        }
    }

    public interface PackedFile //interface for the different file types to use
    {
        void Load(byte[] data, RpfFileEntry entry);
    }

#endregion // Rpffile.cs

#region ResourceFile.cs

public class ResourceFileBase : ResourceSystemBlock
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public uint FileVFT { get; set; }
    public uint FileUnknown { get; set; } = 1;
    public ulong FilePagesInfoPointer { get; set; }

    // reference data
    public ResourcePagesInfo FilePagesInfo { get; set; }

    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.FileVFT = reader.ReadUInt32();
        this.FileUnknown = reader.ReadUInt32();
        this.FilePagesInfoPointer = reader.ReadUInt64();

        // read reference data
        this.FilePagesInfo = reader.ReadBlockAt<ResourcePagesInfo>(
            this.FilePagesInfoPointer // offset
        );
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data
        this.FilePagesInfoPointer = (ulong)(this.FilePagesInfo != null ? this.FilePagesInfo.FilePosition : 0);

        // write structure data
        writer.Write(this.FileVFT);
        writer.Write(this.FileUnknown);
        writer.Write(this.FilePagesInfoPointer);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();
        if (FilePagesInfo != null) list.Add(FilePagesInfo);
        return list.ToArray();
    }
}


public class ResourcePagesInfo : ResourceSystemBlock
{
    public override long BlockLength
    {
        get { return 20 + (256 * 16); }
    }

    // structure data
    public uint Unknown_0h { get; set; }
    public uint Unknown_4h { get; set; }
    public byte SystemPagesCount { get; set; }
    public byte GraphicsPagesCount { get; set; }
    public ushort Unknown_Ah { get; set; }
    public uint Unknown_Ch { get; set; }
    public uint Unknown_10h { get; set; }

    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.Unknown_0h = reader.ReadUInt32();
        this.Unknown_4h = reader.ReadUInt32();
        this.SystemPagesCount = reader.ReadByte();
        this.GraphicsPagesCount = reader.ReadByte();
        this.Unknown_Ah = reader.ReadUInt16();
        this.Unknown_Ch = reader.ReadUInt32();
        this.Unknown_10h = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // write structure data
        writer.Write(this.Unknown_0h);
        writer.Write(this.Unknown_4h);
        writer.Write(this.SystemPagesCount);
        writer.Write(this.GraphicsPagesCount);
        writer.Write(this.Unknown_Ah);
        writer.Write(this.Unknown_Ch);
        writer.Write(this.Unknown_10h);

        var pad = 256 * 16;
        writer.Write(new byte[pad]);
    }

    public override string ToString()
    {
        return SystemPagesCount.ToString() + ", " + GraphicsPagesCount.ToString();
    }
}

#endregion // ResourceFile.cs

#region ResourceBaseTypes.cs

public class string_r : ResourceSystemBlock
{
    // Represents a string that can be referenced in a resource structure.

    /// <summary>
    /// Gets the length of the string.
    /// </summary>
    public override long BlockLength
    {
        get { return Value.Length + 1; }
    }

    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    public string Value { get; set; }

    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        Value = reader.ReadString();
    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        writer.Write(Value);
    }

    public static explicit operator string(string_r value)
    {
        return value.Value;
    }

    public static explicit operator string_r(string value)
    {
        var x = new string_r();
        x.Value = value;
        return x;
    }
    public override string ToString()
    {
        return Value;
    }
}


public struct FlagsByte
{
    public byte Value { get; set; }

    public string Hex
    {
        get
        {
            return Convert.ToString(Value, 16).ToUpper().PadLeft(2, '0');
        }
    }

    public string Bin
    {
        get
        {
            return Convert.ToString(Value, 2).PadLeft(8, '0');
        }
    }


    public FlagsByte(byte v)
    {
        Value = v;
    }

    public override string ToString()
    {
        return Bin + " | 0x" + Hex + " | " + Value.ToString();
    }
    public string ToShortString()
    {
        return Bin + " | 0x" + Hex;
    }
    public string ToHexString()
    {
        return "0x" + Hex;
    }

    public static implicit operator FlagsByte(byte v)
    {
        return new FlagsByte(v);
    }
    public static implicit operator byte(FlagsByte v)
    {
        return v.Value;  //implicit conversion
    }
}

public struct FlagsUshort
{
    public ushort Value { get; set; }

    public string Hex
    {
        get
        {
            return Convert.ToString(Value, 16).ToUpper().PadLeft(4, '0');
        }
    }

    public string Bin
    {
        get
        {
            return Convert.ToString(Value, 2).PadLeft(16, '0');
        }
    }

    public FlagsUshort(ushort v)
    {
        Value = v;
    }

    public override string ToString()
    {
        return Bin + " | 0x" + Hex + " | " + Value.ToString();
    }
    public string ToShortString()
    {
        return Bin + " | 0x" + Hex;
    }

    public static implicit operator FlagsUshort(ushort v)
    {
        return new FlagsUshort(v);
    }
    public static implicit operator ushort(FlagsUshort v)
    {
        return v.Value;  //implicit conversion
    }

}

public struct FlagsUint
{
    public uint Value { get; set; }

    public string Hex
    {
        get
        {
            return Convert.ToString(Value, 16).ToUpper().PadLeft(8, '0');
        }
    }

    public string Bin
    {
        get
        {
            return Convert.ToString(Value, 2).PadLeft(32, '0');
        }
    }

    public FlagsUint(uint v)
    {
        Value = v;
    }

    public override string ToString()
    {
        return Bin + " | 0x" + Hex + " | " + Value.ToString();
    }
    public string ToShortString()
    {
        return Bin + " | 0x" + Hex;
    }

    public static implicit operator FlagsUint(uint v)
    {
        return new FlagsUint(v);
    }
    public static implicit operator uint(FlagsUint v)
    {
        return v.Value;  //implicit conversion
    }

}




public abstract class ListBase<T> : ResourceSystemBlock, ICustomTypeDescriptor, IList<T> where T : IResourceSystemBlock, new()
{

    // this is the data...
    public List<T> Data { get; set; }





    public T this[int index]
    {
        get
        {
            return Data[index];
        }
        set
        {
            Data[index] = value;
        }
    }

    public int Count
    {
        get
        {
            return Data?.Count ?? 0;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            return false;
        }
    }






    public ListBase()
    {
        //Data = new List<T>();
    }





    public void Add(T item)
    {
        if (Data == null)
        {
            Data = new List<T>();
        }
        Data.Add(item);
    }

    public void Clear()
    {
        Data.Clear();
    }

    public bool Contains(T item)
    {
        return Data.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Data.CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Data.GetEnumerator();
    }

    public int IndexOf(T item)
    {
        return Data.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        Data.Insert(index, item);
    }

    public bool Remove(T item)
    {
        return Data.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Data.RemoveAt(index);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return Data.GetEnumerator();
    }




    public String GetClassName()
    {
        return TypeDescriptor.GetClassName(this, true);
    }

    public AttributeCollection GetAttributes()
    {
        return TypeDescriptor.GetAttributes(this, true);
    }

    public String GetComponentName()
    {
        return TypeDescriptor.GetComponentName(this, true);
    }

    public TypeConverter GetConverter()
    {
        return TypeDescriptor.GetConverter(this, true);
    }

    public EventDescriptor GetDefaultEvent()
    {
        return TypeDescriptor.GetDefaultEvent(this, true);
    }

    public PropertyDescriptor GetDefaultProperty()
    {
        return TypeDescriptor.GetDefaultProperty(this, true);
    }

    public object GetEditor(Type editorBaseType)
    {
        return TypeDescriptor.GetEditor(this, editorBaseType, true);
    }

    public EventDescriptorCollection GetEvents(Attribute[] attributes)
    {
        return TypeDescriptor.GetEvents(this, attributes, true);
    }

    public EventDescriptorCollection GetEvents()
    {
        return TypeDescriptor.GetEvents(this, true);
    }

    public object GetPropertyOwner(PropertyDescriptor pd)
    {
        return this;
    }


    public PropertyDescriptorCollection GetProperties()
    {
        var pds = new PropertyDescriptorCollection(null);
        for (int i = 0; i < Data.Count; i++)
        {
            var pd = new ListBasePropertyDescriptor(this, i);
            pds.Add(pd);
        }
        return pds;
    }

    public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
    {
        return GetProperties();
    }


    public class ListBasePropertyDescriptor : PropertyDescriptor
    {
        private ListBase<T> collection = null;
        private int index = -1;

        public ListBasePropertyDescriptor(ListBase<T> coll, int i) : base("#" + i.ToString(), null)
        {
            collection = coll;
            index = i;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                return new AttributeCollection(null);
            }
        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get
            {
                return this.collection.GetType();
            }
        }

        public override string DisplayName
        {
            get
            {
                return "[" + index.ToString() + "]";
            }
        }

        public override string Description
        {
            get
            {
                return collection[index].ToString();
            }
        }

        public override object GetValue(object component)
        {
            return this.collection[index];
        }

        public override bool IsReadOnly
        {
            get { return true; }
        }

        public override string Name
        {
            get { return "#" + index.ToString(); }
        }

        public override Type PropertyType
        {
            get { return this.collection[index].GetType(); }
        }

        public override void ResetValue(object component) { }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        public override void SetValue(object component, object value)
        {
            // this.collection[index] = value;
        }
    }

}




public class ResourceSimpleArray<T> : ListBase<T>, IResourceNoCacheBlock where T : IResourceSystemBlock, new()
{
    /// <summary>
    /// Gets the length of the data block.
    /// </summary>
    public override long BlockLength
    {
        get
        {
            long length = 0;
            if (Data != null)
            {
                foreach (var x in Data)
                    length += x.BlockLength;
            }
            return length;
        }
    }


    public ResourceSimpleArray()
    {
        //Data = new List<T>();
    }







    /// <summary>
    /// Reads the data block.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        int numElements = Convert.ToInt32(parameters[0]);

        Data = new List<T>(numElements);
        for (int i = 0; i < numElements; i++)
        {
            T item = reader.ReadBlock<T>();
            Data.Add(item);
        }
    }

    /// <summary>
    /// Writes the data block.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        foreach (var f in Data)
            f.Write(writer);
    }




    public override Tuple<long, IResourceBlock>[] GetParts()
    {
        var list = new List<Tuple<long, IResourceBlock>>();

        long length = 0;

        if (Data != null)
        {
            foreach (var x in Data)
            {
                list.Add(new Tuple<long, IResourceBlock>(length, x));
                length += x.BlockLength;
            }
        }
        

        return list.ToArray();
    }




    public override string ToString()
    {
        return "(Count: " + Count.ToString() + ")";
    }
}

public class ResourceSimpleList64<T> : ResourceSystemBlock, IResourceNoCacheBlock where T : IResourceSystemBlock, new()
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; private set; }
    public ushort EntriesCapacity { get; private set; }

    // reference data
    //public ResourceSimpleArray<T> Entries;
    public T[] data_items { get; set; }

    private ResourceSimpleArray<T> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        // read reference data
        //this.Entries = reader.ReadBlockAt<ResourceSimpleArray<T>>(
        //    this.EntriesPointer, // offset
        //    this.EntriesCount
        //);

        //TODO: NEEDS TO BE TESTED!!!
        data_items = new T[EntriesCount];
        var posbckp = reader.Position;
        reader.Position = (long)EntriesPointer;
        for (int i = 0; i < EntriesCount; i++)
        {
            data_items[i] = reader.ReadBlock<T>();
        }
        reader.Position = posbckp;

    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.Count : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.Count : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSimpleArray<T>();
            data_block.Data = new List<T>();
            data_block.Data.AddRange(data_items);
            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}
public class ResourceSimpleList64_s<T> : ResourceSystemBlock, IResourceNoCacheBlock where T : struct
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; private set; }
    public ushort EntriesCapacity { get; private set; }

    // reference data
    public T[] data_items { get; set; }

    private ResourceSystemStructBlock<T> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        // read reference data

        //TODO: NEEDS TO BE TESTED!!!
        data_items = reader.ReadStructsAt<T>(EntriesPointer, EntriesCount);

        if (EntriesCount != EntriesCapacity)
        { }
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSystemStructBlock<T>(data_items);

            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}
public class ResourceSimpleList64b_s<T> : ResourceSystemBlock, IResourceNoCacheBlock where T : struct
{
    //this version uses uints for the count/cap!

    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public uint EntriesCount { get; set; } //this needs to be set manually for this type! make sure it's <= capacity
    public uint EntriesCapacity { get; private set; }

    // reference data
    public T[] data_items { get; set; }

    private ResourceSystemStructBlock<T> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt32();
        this.EntriesCapacity = reader.ReadUInt32();
        //reader.Position += 4;

        // read reference data

        //TODO: NEEDS TO BE TESTED!!!
        data_items = reader.ReadStructsAt<T>(EntriesPointer, EntriesCapacity);
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        //this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        //writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSystemStructBlock<T>(data_items);

            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}
public class ResourceSimpleList64_byte : ResourceSystemBlock, IResourceNoCacheBlock
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; private set; }
    public ushort EntriesCapacity { get; private set; }

    // reference data
    public byte[] data_items { get; private set; }

    private ResourceSystemStructBlock<byte> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        // read reference data

        //TODO: NEEDS TO BE TESTED!!!
        data_items = reader.ReadBytesAt(EntriesPointer, EntriesCount);
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSystemStructBlock<byte>(data_items);

            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}
public class ResourceSimpleList64_ushort : ResourceSystemBlock, IResourceNoCacheBlock
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; private set; }
    public ushort EntriesCapacity { get; private set; }

    // reference data
    public ushort[] data_items { get; set; }

    private ResourceSystemStructBlock<ushort> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        // read reference data

        //TODO: NEEDS TO BE TESTED!!!
        data_items = reader.ReadUshortsAt(EntriesPointer, EntriesCount);
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSystemStructBlock<ushort>(data_items);

            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}
public class ResourceSimpleList64_uint : ResourceSystemBlock, IResourceNoCacheBlock
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; private set; }
    public ushort EntriesCapacity { get; private set; }

    // reference data
    public uint[] data_items { get; set; }

    private ResourceSystemStructBlock<uint> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        // read reference data

        //TODO: NEEDS TO BE TESTED!!!
        data_items = reader.ReadUintsAt(EntriesPointer, EntriesCount);
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSystemStructBlock<uint>(data_items);

            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}
public class ResourceSimpleList64_ulong : ResourceSystemBlock, IResourceNoCacheBlock
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; private set; }
    public ushort EntriesCapacity { get; private set; }

    // reference data
    public ulong[] data_items { get; private set; }

    private ResourceSystemStructBlock<ulong> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        // read reference data

        //TODO: NEEDS TO BE TESTED!!!
        data_items = reader.ReadUlongsAt(EntriesPointer, EntriesCount);
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSystemStructBlock<ulong>(data_items);

            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}
public class ResourceSimpleList64_float : ResourceSystemBlock, IResourceNoCacheBlock
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; private set; }
    public ushort EntriesCapacity { get; private set; }

    // reference data
    public float[] data_items { get; set; }

    private ResourceSystemStructBlock<float> data_block;//used for saving.


    /// <summary>
    /// Reads the data-block from a stream.
    /// </summary>
    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        // read structure data
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        // read reference data

        //TODO: NEEDS TO BE TESTED!!!
        data_items = reader.ReadFloatsAt(EntriesPointer, EntriesCount);
    }

    /// <summary>
    /// Writes the data-block to a stream.
    /// </summary>
    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update structure data //TODO: fix
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);
        this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.ItemCount : 0);

        // write structure data
        writer.Write(this.EntriesPointer);
        writer.Write(this.EntriesCount);
        writer.Write(this.EntriesCapacity);
        writer.Write((uint)0x00000000);
    }

    /// <summary>
    /// Returns a list of data blocks which are referenced by this block.
    /// </summary>
    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourceSystemStructBlock<float>(data_items);

            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }

    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}


public class ResourcePointerArray64<T> : ResourceSystemBlock, IList<T> where T : IResourceSystemBlock, new()
{

    public int GetNonEmptyNumber()
    {
        int i = 0;
        foreach (var q in data_items)
            if (q != null)
                i++;
        return i;
    }

    public override long BlockLength
    {
        get
        {
            return (data_items != null) ? 8 * data_items.Length : 0;
        }
    }


    public ulong[] data_pointers { get; set; }
    public T[] data_items { get; set; }

    public bool ManualReferenceOverride = false;//use this if the items are embedded in something else


    public ResourcePointerArray64()
    {
    }

    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        int numElements = Convert.ToInt32(parameters[0]);


        data_pointers = reader.ReadUlongsAt((ulong)reader.Position, (uint)numElements, false);


        data_items = new T[numElements];
        for (int i = 0; i < numElements; i++)
        {
            data_items[i] = reader.ReadBlockAt<T>(data_pointers[i]);
        }


    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update...
        var list = new List<ulong>();
        foreach (var x in data_items)
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
        data_pointers = list.ToArray();


        // write...
        foreach (var x in data_pointers)
            writer.Write(x);
    }


    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (ManualReferenceOverride == false)
        {
            foreach (var x in data_items)
            {
                list.Add(x);
            }
        }

        return list.ToArray();
    }





    public int IndexOf(T item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, T item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        //data_items.RemoveAt(index);
        throw new NotImplementedException();
    }

    public T this[int index]
    {
        get
        {
            return data_items[index];
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public void Add(T item)
    {
        //data_items.Add(item);
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public int Count
    {
        //get { return data_items.Count; }
        get { return (data_items != null) ? data_items.Length : 0; }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    public bool Remove(T item)
    {
        //return data_items.Remove(item);
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        //return data_items.GetEnumerator();
        throw new NotImplementedException();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }




    public override string ToString()
    {
        return "(Count: " + ((data_items != null) ? data_items.Length : 0).ToString() + ")";
    }

}

public class ResourcePointerList64<T> : ResourceSystemBlock, IList<T> where T : IResourceSystemBlock, new()
{
    public override long BlockLength
    {
        get { return 16; }
    }

    // structure data
    public ulong EntriesPointer { get; private set; }
    public ushort EntriesCount { get; set; }
    public ushort EntriesCapacity { get; set; }

    // reference data
    //public ResourcePointerArray64<T> Entries;

    public ulong[] data_pointers { get; private set; }
    public T[] data_items { get; set; }

    public bool ManualCountOverride = false; //use this to manually specify the count
    public bool ManualReferenceOverride = false; //use this if the items are embedded in something else

    private ResourcePointerArray64<T> data_block;//used for saving.


    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        this.EntriesPointer = reader.ReadUInt64();
        this.EntriesCount = reader.ReadUInt16();
        this.EntriesCapacity = reader.ReadUInt16();
        reader.Position += 4;

        //this.Entries = reader.ReadBlockAt<ResourcePointerArray64<T>>(
        //    this.EntriesPointer, // offset
        //    this.EntriesCount
        //);

        data_pointers = reader.ReadUlongsAt(EntriesPointer, EntriesCapacity);
        data_items = new T[EntriesCount];
        for (int i = 0; i < EntriesCount; i++)
        {
            data_items[i] = reader.ReadBlockAt<T>(data_pointers[i]);
        }


    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        // update...
        this.EntriesPointer = (ulong)(this.data_block != null ? this.data_block.FilePosition : 0);
        if (ManualCountOverride == false)
        {
            this.EntriesCapacity = (ushort)(this.data_block != null ? this.data_block.Count : 0);
            this.EntriesCount = (ushort)(this.data_block != null ? this.data_block.Count : 0);
        }


        // write...
        writer.Write(EntriesPointer);
        writer.Write(EntriesCount);
        writer.Write(EntriesCapacity);
        writer.Write((uint)0x0000000);
    }

    public override IResourceBlock[] GetReferences()
    {
        var list = new List<IResourceBlock>();

        if (data_items?.Length > 0)
        {
            data_block = new ResourcePointerArray64<T>();
            data_block.data_items = data_items;
            data_block.ManualReferenceOverride = ManualReferenceOverride;
            list.Add(data_block);
        }
        else
        {
            data_block = null;
        }

        return list.ToArray();
    }




    public int IndexOf(T item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, T item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    public T this[int index]
    {
        get
        {
            return data_items[index];
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public void Add(T item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public int Count
    {
        get { return EntriesCount; }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }


    public IResourceBlock CheckForCast(ResourceDataReader reader, params object[] parameters)
    {
        throw new NotImplementedException();
    }


    public override string ToString()
    {
        return "(Count: " + EntriesCount.ToString() + ")";
    }
}


public struct ResourcePointerListHeader
{
    public ulong Pointer { get; set; }
    public ushort Count { get; set; }
    public ushort Capacity { get; set; }
    public uint Unknown { get; set; }
}










public class ResourceSystemDataBlock : ResourceSystemBlock //used for writing resources.
{
    public byte[] Data { get; set; }
    public int DataLength { get; set; }

    public override long BlockLength
    {
        get
        {
            return (Data != null) ? Data.Length : DataLength;
        }
    }


    public ResourceSystemDataBlock(byte[] data)
    {
        Data = data;
        DataLength = (Data != null) ? Data.Length : 0;
    }


    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        Data = reader.ReadBytes(DataLength);
    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {
        writer.Write(Data);
    }
}

public class ResourceSystemStructBlock<T> : ResourceSystemBlock where T : struct //used for writing resources.
{
    public T[] Items { get; set; }
    public int ItemCount { get; set; }
    public int StructureSize { get; set; }

    public override long BlockLength
    {
        get
        {
            return ((Items != null) ? Items.Length : ItemCount) * StructureSize;
        }
    }

    public ResourceSystemStructBlock(T[] items)
    {
        Items = items;
        ItemCount = (Items != null) ? Items.Length : 0;
        StructureSize = Marshal.SizeOf(typeof(T));
    }

    public override void Read(ResourceDataReader reader, params object[] parameters)
    {
        int datalength = ItemCount * StructureSize;
        byte[] data = reader.ReadBytes(datalength);
        Items = ConvertDataArray(data, 0, ItemCount);
        
        static T ConvertData(byte[] data, int offset)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var h = handle.AddrOfPinnedObject();
            var r = Marshal.PtrToStructure<T>(h + offset);
            handle.Free();
            return r;
        }
        static T[] ConvertDataArray(byte[] data, int offset, int count)
        {
            T[] items = new T[count];
            int itemsize = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < count; i++)
            {
                int off = offset + i * itemsize;
                items[i] = ConvertData(data, off);
            }
            return items;
        }
    }

    public override void Write(ResourceDataWriter writer, params object[] parameters)
    {

        byte[] data = ConvertArrayToBytes(Items);
        if (data != null)
        {
            writer.Write(data);
        }
        
        static byte[] ConvertArrayToBytes(params T[] items)
        {
            if (items == null) return null;
            int size = Marshal.SizeOf(typeof(T));
            int sizetot = size * items.Length;
            byte[] arrout = new byte[sizetot];
            int offset = 0;
            for (int i = 0; i < items.Length; i++)
            {
                byte[] arr = new byte[size];
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(items[i], ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                Marshal.FreeHGlobal(ptr);
                Buffer.BlockCopy(arr, 0, arrout, offset, size);
                offset += size;
            }
            return arrout;
        }
    }
}

#endregion // ResourceBaseTypes.cs

#region ResourceBuilder.cs

public class ResourceBuilder
{
    protected const int RESOURCE_IDENT = 0x37435352;
    protected const int BASE_SIZE = 0x2000;
    private const int SKIP_SIZE = 16;//512;//256;//64;
    private const int ALIGN_SIZE = 16;//512;//64;

    public class ResourceBuilderBlock
    {
        public IResourceBlock Block;
        public long Length;

        public ResourceBuilderBlock(IResourceBlock block)
        {
            Block = block;
            Length = block?.BlockLength ?? 0;
        }
    }
    public class ResourceBuilderBlockSet
    {
        public bool IsSystemSet = false;
        public ResourceBuilderBlock RootBlock = null;
        public LinkedList<ResourceBuilderBlock> BlockList = new LinkedList<ResourceBuilderBlock>();
        public Dictionary<ResourceBuilderBlock, LinkedListNode<ResourceBuilderBlock>> BlockDict = new Dictionary<ResourceBuilderBlock, LinkedListNode<ResourceBuilderBlock>>();

        public int Count => BlockList.Count;

        public ResourceBuilderBlockSet(IList<IResourceBlock> blocks, bool sys)
        {
            IsSystemSet = sys;
            if (sys && (blocks.Count > 0))
            {
                RootBlock = new ResourceBuilderBlock(blocks[0]);
            }
            var list = new List<ResourceBuilderBlock>();
            int start = sys ? 1 : 0;
            for (int i = start; i < blocks.Count; i++)
            {
                var bb = new ResourceBuilderBlock(blocks[i]);
                list.Add(bb);
            }
            list.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (var bb in list)
            {
                var ln = BlockList.AddLast(bb);
                BlockDict[bb] = ln;
            }
        }

        public ResourceBuilderBlock FindBestBlock(long maxSize)
        {
            var n = BlockList.First;
            while ((n != null) && (n.Value.Length > maxSize))
            {
                n = n.Next;
            }
            return n?.Value;
        }

        public ResourceBuilderBlock TakeBestBlock(long maxSize)
        {
            var r = FindBestBlock(maxSize);
            if (r != null)
            {
                if (BlockDict.TryGetValue(r, out LinkedListNode<ResourceBuilderBlock> ln))
                {
                    BlockList.Remove(ln);
                    BlockDict.Remove(r);
                }
            }
            return r;
        }

    }

    public static void GetBlocks(IResourceBlock rootBlock, out IList<IResourceBlock> sys, out IList<IResourceBlock> gfx)
    {
        var systemBlocks = new HashSet<IResourceBlock>();
        var graphicBlocks = new HashSet<IResourceBlock>();
        var processed = new HashSet<IResourceBlock>();


        void addBlock(IResourceBlock block)
        {
            if (block is IResourceSystemBlock)
            {
                if (!systemBlocks.Contains(block)) systemBlocks.Add(block);
            }
            else if(block is IResourceGraphicsBlock)
            {
                if (!graphicBlocks.Contains(block)) graphicBlocks.Add(block);
            }
        }
        void addChildren(IResourceBlock block)
        {
            if (block is IResourceSystemBlock sblock)
            {
                var references = sblock.GetReferences();
                foreach (var reference in references)
                {
                    if (!processed.Contains(reference))
                    {
                        processed.Add(reference);
                        addBlock(reference);
                        addChildren(reference);
                    }
                }
                var parts = sblock.GetParts();
                foreach (var part in parts)
                {
                    addChildren(part.Item2);
                }
            }
        }

        addBlock(rootBlock);
        addChildren(rootBlock);


        sys = new List<IResourceBlock>();
        foreach (var s in systemBlocks)
        {
            sys.Add(s);
        }
        gfx = new List<IResourceBlock>();
        foreach (var s in graphicBlocks)
        {
            gfx.Add(s);
        }
    }

    public static void AssignPositions(IList<IResourceBlock> blocks, uint basePosition, out RpfResourcePageFlags pageFlags)
    {
        // if ((blocks.Count > 0) && (blocks[0] is Meta))
        // {
        //     //use naive packing strategy for Meta resources, due to crashes caused by the improved packing
        //     AssignPositionsForMeta(blocks, basePosition, out pageFlags);
        //     return;
        // }

        var sys = (basePosition == 0x50000000);

        long pad(long p)
        {
            return ((ALIGN_SIZE - (p % ALIGN_SIZE)) % ALIGN_SIZE);
        }

        long largestBlockSize = 0; // find largest structure
        long startPageSize = BASE_SIZE;// 0x2000; // find starting page size
        long totalBlockSize = 0;
        foreach (var block in blocks)
        {
            var blockLength = block.BlockLength;
            totalBlockSize += blockLength;
            totalBlockSize += pad(totalBlockSize);
            if (largestBlockSize < blockLength)
            {
                largestBlockSize = blockLength;
            }
        }
        while (startPageSize < largestBlockSize)
        {
            startPageSize *= 2;
        }


        pageFlags = new RpfResourcePageFlags();
        var pageSizeMult = 1;

        while (true)
        {
            if (blocks.Count == 0) break;

            var blockset = new ResourceBuilderBlockSet(blocks, sys);
            var rootblock = blockset.RootBlock;
            var currentPosition = 0L;
            var currentPageSize = startPageSize;
            var currentPageStart = 0L;
            var currentPageSpace = startPageSize;
            var currentRemainder = totalBlockSize;
            var pageCount = 1;
            var pageCounts = new uint[9];
            var pageCountIndex = 0;
            var targetPageSize = Math.Max(65536 * pageSizeMult, startPageSize >> (sys ? 5 : 2));
            var minPageSize = Math.Max(512 * pageSizeMult, Math.Min(targetPageSize, startPageSize) >> 4);
            var baseShift = 0u;
            var baseSize = 512;
            while (baseSize < minPageSize)
            {
                baseShift++;
                baseSize *= 2;
                if (baseShift >= 0xF) break;
            }
            var baseSizeMax = baseSize << 8;
            var baseSizeMaxTest = startPageSize;
            while (baseSizeMaxTest < baseSizeMax)
            {
                pageCountIndex++;
                baseSizeMaxTest *= 2;
            }
            pageCounts[pageCountIndex] = 1;

            while (true)
            {
                var isroot = sys && (currentPosition == 0);
                var block = isroot ? rootblock : blockset.TakeBestBlock(currentPageSpace);
                var blockLength = block?.Length ?? 0;
                if (block != null)
                {
                    //add this block to the current page.
                    block.Block.FilePosition = basePosition + currentPosition;
                    var opos = currentPosition;
                    currentPosition += blockLength;
                    currentPosition += pad(currentPosition);
                    var usedspace = currentPosition - opos;
                    currentPageSpace -= usedspace;
                    currentRemainder -= usedspace;//blockLength;// 

                }
                else if (blockset.Count > 0)
                {
                    //allocate a new page
                    currentPageStart += currentPageSize;
                    currentPosition = currentPageStart;
                    block = blockset.FindBestBlock(long.MaxValue); //just find the biggest block
                    blockLength = block?.Length ?? 0;
                    while (blockLength <= (currentPageSize >> 1))//determine best new page size
                    {
                        if (currentPageSize <= minPageSize) break;
                        if (pageCountIndex >= 8) break;
                        if ((currentPageSize <= targetPageSize) && (currentRemainder >= (currentPageSize - minPageSize))) break;

                        currentPageSize = currentPageSize >> 1;
                        pageCountIndex++;
                    }
                    currentPageSpace = currentPageSize;
                    pageCounts[pageCountIndex]++;
                    pageCount++;
                }
                else
                {
                    break;
                }
            }


            pageFlags = new RpfResourcePageFlags(pageCounts, baseShift);

            if ((pageCount == pageFlags.Count) && (pageFlags.Size >= currentPosition)) //make sure page counts fit in the flags value
            {
                break;
            }

            startPageSize *= 2;
            pageSizeMult *= 2;
        }

    }

    public static void AssignPositionsForMeta(IList<IResourceBlock> blocks, uint basePosition, out RpfResourcePageFlags pageFlags)
    {
        // find largest structure
        long largestBlockSize = 0;
        foreach (var block in blocks)
        {
            if (largestBlockSize < block.BlockLength)
                largestBlockSize = block.BlockLength;
        }

        // find minimum page size
        long currentPageSize = 0x2000;
        while (currentPageSize < largestBlockSize)
            currentPageSize *= 2;

        long currentPageCount;
        long currentPosition;
        while (true)
        {
            currentPageCount = 0;
            currentPosition = 0;

            // reset all positions
            foreach (var block in blocks)
                block.FilePosition = -1;

            foreach (var block in blocks)
            {
                if (block.FilePosition != -1)
                    throw new Exception("Block was already assigned a position!");

                // check if new page is necessary...
                // if yes, add a new page and align to it
                long maxSpace = currentPageCount * currentPageSize - currentPosition;
                if (maxSpace < (block.BlockLength + SKIP_SIZE))
                {
                    currentPageCount++;
                    currentPosition = currentPageSize * (currentPageCount - 1);
                }

                // set position
                block.FilePosition = basePosition + currentPosition;
                currentPosition += block.BlockLength; // + SKIP_SIZE; //is padding everywhere really necessary??

                // align...
                if ((currentPosition % ALIGN_SIZE) != 0)
                    currentPosition += (ALIGN_SIZE - (currentPosition % ALIGN_SIZE));
            }

            // break if everything fits...
            if (currentPageCount < 128)
                break;

            currentPageSize *= 2;
        }

        pageFlags = new RpfResourcePageFlags(RpfResourceFileEntry.GetFlagsFromBlocks((uint)currentPageCount, (uint)currentPageSize, 0));

    }


    public static byte[] Build(ResourceFileBase fileBase, int version, bool compress = true)
    {

        fileBase.FilePagesInfo = new ResourcePagesInfo();

        IList<IResourceBlock> systemBlocks;
        IList<IResourceBlock> graphicBlocks;
        GetBlocks(fileBase, out systemBlocks, out graphicBlocks);
        
        RpfResourcePageFlags systemPageFlags;
        AssignPositions(systemBlocks, 0x50000000, out systemPageFlags);
        
        RpfResourcePageFlags graphicsPageFlags;
        AssignPositions(graphicBlocks, 0x60000000, out graphicsPageFlags);


        fileBase.FilePagesInfo.SystemPagesCount = (byte)systemPageFlags.Count;
        fileBase.FilePagesInfo.GraphicsPagesCount = (byte)graphicsPageFlags.Count;


        var systemStream = new MemoryStream();
        var graphicsStream = new MemoryStream();
        var resourceWriter = new ResourceDataWriter(systemStream, graphicsStream);

        resourceWriter.Position = 0x50000000;
        foreach (var block in systemBlocks)
        {
            resourceWriter.Position = block.FilePosition;

            var pos_before = resourceWriter.Position;
            block.Write(resourceWriter);
            var pos_after = resourceWriter.Position;

            if ((pos_after - pos_before) != block.BlockLength)
            {
                throw new Exception("error in system length");
            }
        }

        resourceWriter.Position = 0x60000000;
        foreach (var block in graphicBlocks)
        {
            resourceWriter.Position = block.FilePosition;

            var pos_before = resourceWriter.Position;
            block.Write(resourceWriter);
            var pos_after = resourceWriter.Position;

            if ((pos_after - pos_before) != block.BlockLength)
            {
                throw new Exception("error in graphics length");
            }
        }




        var sysDataSize = (int)systemPageFlags.Size;
        var sysData = new byte[sysDataSize];
        systemStream.Flush();
        systemStream.Position = 0;
        systemStream.Read(sysData, 0, (int)systemStream.Length);


        var gfxDataSize = (int)graphicsPageFlags.Size;
        var gfxData = new byte[gfxDataSize];
        graphicsStream.Flush();
        graphicsStream.Position = 0;
        graphicsStream.Read(gfxData, 0, (int)graphicsStream.Length);



        uint uv = (uint)version;
        uint sv = (uv >> 4) & 0xF;
        uint gv = (uv >> 0) & 0xF;
        uint sf = systemPageFlags.Value + (sv << 28);
        uint gf = graphicsPageFlags.Value + (gv << 28);


        var tdatasize = sysDataSize + gfxDataSize;
        var tdata = new byte[tdatasize];
        Buffer.BlockCopy(sysData, 0, tdata, 0, sysDataSize);
        Buffer.BlockCopy(gfxData, 0, tdata, sysDataSize, gfxDataSize);


        var cdata = compress ? Compress(tdata) : tdata;


        var dataSize = 16 + cdata.Length;
        var data = new byte[dataSize];

        byte[] h1 = BitConverter.GetBytes((uint)0x37435352);
        byte[] h2 = BitConverter.GetBytes((int)version);
        byte[] h3 = BitConverter.GetBytes(sf);
        byte[] h4 = BitConverter.GetBytes(gf);
        Buffer.BlockCopy(h1, 0, data, 0, 4);
        Buffer.BlockCopy(h2, 0, data, 4, 4);
        Buffer.BlockCopy(h3, 0, data, 8, 4);
        Buffer.BlockCopy(h4, 0, data, 12, 4);
        Buffer.BlockCopy(cdata, 0, data, 16, cdata.Length);

        return data;
    }






    public static byte[] AddResourceHeader(RpfResourceFileEntry entry, byte[] data)
    {
        if (data == null) return null;
        byte[] newdata = new byte[data.Length + 16];
        byte[] h1 = BitConverter.GetBytes((uint)0x37435352);
        byte[] h2 = BitConverter.GetBytes(entry.Version);
        byte[] h3 = BitConverter.GetBytes(entry.SystemFlags);
        byte[] h4 = BitConverter.GetBytes(entry.GraphicsFlags);
        Buffer.BlockCopy(h1, 0, newdata, 0, 4);
        Buffer.BlockCopy(h2, 0, newdata, 4, 4);
        Buffer.BlockCopy(h3, 0, newdata, 8, 4);
        Buffer.BlockCopy(h4, 0, newdata, 12, 4);
        Buffer.BlockCopy(data, 0, newdata, 16, data.Length);
        return newdata;
    }


    public static byte[] Compress(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true);
            ds.Write(data, 0, data.Length);
            ds.Close();
            byte[] deflated = ms.GetBuffer();
            byte[] outbuf = new byte[ms.Length]; //need to copy to the right size buffer...
            Array.Copy(deflated, outbuf, outbuf.Length);
            return outbuf;
        }
    }
    public static byte[] Decompress(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress);
            MemoryStream outstr = new MemoryStream();
            ds.CopyTo(outstr);
            byte[] deflated = outstr.GetBuffer();
            byte[] outbuf = new byte[outstr.Length]; //need to copy to the right size buffer...
            Array.Copy(deflated, outbuf, outbuf.Length);
            return outbuf;
        }
    }

}

#endregion // ResourceBuilder.cs

#region GameFile.cs

public abstract class GameFile// : Cacheable<GameFileCacheKey>
{
    public volatile bool Loaded = false;
    public volatile bool LoadQueued = false;
    public RpfFileEntry RpfFileEntry { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; } //used by the project form.
    public GameFileType Type { get; set; }



    public GameFile(RpfFileEntry entry, GameFileType type)
    {
        RpfFileEntry = entry;
        Type = type;
        //MemoryUsage = (entry != null) ? entry.GetFileSize() : 0;
        if (entry is RpfResourceFileEntry)
        {
            var resent = entry as RpfResourceFileEntry;
            var newuse = resent.SystemSize + resent.GraphicsSize;
            //MemoryUsage = newuse;
        }
        else if (entry is RpfBinaryFileEntry)
        {
            var binent = entry as RpfBinaryFileEntry;
            var newuse = binent.FileUncompressedSize;
            // if (newuse > MemoryUsage)
            // {
            //     MemoryUsage = newuse;
            // }
        }
        else
        {
        }
    }

    public override string ToString()
    {
        return Name ?? string.Empty; // (string.IsNullOrEmpty(Name)) ? JenkIndex.GetString(Key.Hash) : Name;
    }


}


public enum GameFileType : int
{
    Ydd = 0,
    Ydr = 1,
    Yft = 2,
    Ymap = 3,
    Ymf = 4,
    Ymt = 5,
    Ytd = 6,
    Ytyp = 7,
    Ybn = 8,
    Ycd = 9,
    Ypt = 10,
    Ynd = 11,
    Ynv = 12,
    Rel = 13,
    Ywr = 14,
    Yvr = 15,
    Gtxd = 16,
    Vehicles = 17,
    CarCols = 18,
    CarModCols = 19,
    CarVariations = 20,
    VehicleLayouts = 21,
    Peds = 22,
    Ped = 23,
    Yed = 24,
    Yld = 25,
    Yfd = 26,
    Heightmap = 27,
    Watermap = 28,
    Mrf = 29,
}

#endregion // GameFile.cs

