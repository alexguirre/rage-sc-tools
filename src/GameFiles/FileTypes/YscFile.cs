namespace ScTools.GameFiles
{
    using CodeWalker.GameFiles;

    internal class YscFile : GameFile, PackedFile
    {
        public const GameFileType FileType = (GameFileType)27;
        public const int FileVersion = 10;

        public Script Script { get; set; }

        public YscFile() : base(null, FileType)
        {
        }

        public YscFile(RpfFileEntry entry) : base(entry, FileType)
        {
        }

        public void Load(byte[] data) => RpfFile.LoadResourceFile(this, data, FileVersion);

        public void Load(byte[] data, RpfFileEntry entry)
        {
            Name = entry.Name;
            RpfFileEntry = entry;

            if (!(entry is RpfResourceFileEntry resEntry))
            {
                throw new System.Exception("File entry wasn't a resource! (is it binary data?)");
            }

            ResourceDataReader rd = new ResourceDataReader(resEntry, data);

            Script = rd.ReadBlock<Script>();

            Loaded = true;
        }

        public byte[] Save() => ResourceBuilder.Build(Script, FileVersion);
    }
}
