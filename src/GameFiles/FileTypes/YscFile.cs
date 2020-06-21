namespace ScTools.GameFiles
{
    using CodeWalker.GameFiles;

    internal class YscFile : GameFile, PackedFile
    {
        public const GameFileType FileType = (GameFileType)27;
        public const int FileVersion = 10;

        public Script Script { get; set; }

        public ResourceAnalyzer Analyzer { get; set; }

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

            System.Console.WriteLine("ResEntry: IsEncrypted={0}, Version={1}, SystemSize={2} ({3}, {4}), GraphicsSize={5} ({6})",
                resEntry.IsEncrypted, resEntry.Version, resEntry.SystemSize, resEntry.SystemFlags.Value, resEntry.SystemFlags.BaseShift, resEntry.GraphicsSize, resEntry.GraphicsFlags.Value);
            ResourceDataReader rd = new ResourceDataReader(resEntry, data);

            Script = rd.ReadBlock<Script>();

            Analyzer = new ResourceAnalyzer(rd);

            Loaded = true;
        }

        public byte[] Save() => ResourceBuilder.Build(Script, FileVersion);
    }
}
