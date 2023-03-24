namespace ScTools.GameFiles.GTA5;

using System.IO;
using System;
using System.Collections.Generic;
using CodeWalker.GameFiles;

public class YscFile : GameFile, PackedFile
{
    public const GameFileType FileType = (GameFileType)27;
    /// <summary>
    /// v10: release version
    /// v11: IS_BIT_SET native replaced with IBITTEST opcode, since b2612
    /// v12: added STATIC_U24, STATIC_U24_LOAD and STATIC_U24_STORE, since b2802
    /// </summary>
    public const int FileVersion = 12;

    public Script Script { get; set; }

    public YscFile() : base(null, FileType)
    {
    }

    public YscFile(RpfFileEntry entry) : base(entry, FileType)
    {
    }

    // TODO: support for loading NG encrypted data
    public void Load(byte[] data) => RpfFile.LoadResourceFile(this, data, FileVersion);

    public void Load(byte[] data, RpfFileEntry entry)
    {
        Name = entry.Name;
        RpfFileEntry = entry;

        if (!(entry is RpfResourceFileEntry resEntry))
        {
            throw new Exception("File entry wasn't a resource! (is it binary data?)");
        }

        ResourceDataReader rd = new ResourceDataReader(resEntry, data);

        Script = rd.ReadBlock<Script>();

        Loaded = true;
    }

    public byte[] Save(string fileName = null) => Build(Script, FileVersion, fileName, encrypt: fileName != null);

    // same as ResourceBuilder.Build but with support NG encrypting after compressing
    private static byte[] Build(ResourceFileBase fileBase, int version, string name, bool compress = true, bool encrypt = true)
    {

        fileBase.FilePagesInfo = new ResourcePagesInfo();

        IList<IResourceBlock> systemBlocks;
        IList<IResourceBlock> graphicBlocks;
        ResourceBuilder.GetBlocks(fileBase, out systemBlocks, out graphicBlocks);

        RpfResourcePageFlags systemPageFlags;
        ResourceBuilder.AssignPositions(systemBlocks, 0x50000000, out systemPageFlags);

        RpfResourcePageFlags graphicsPageFlags;
        ResourceBuilder.AssignPositions(graphicBlocks, 0x60000000, out graphicsPageFlags);


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


        var cdata = compress ? ResourceBuilder.Compress(tdata) : tdata;
        cdata = encrypt ? GTACrypto.EncryptNG(cdata, name, (uint)cdata.Length + 0x10) : cdata;

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
}
