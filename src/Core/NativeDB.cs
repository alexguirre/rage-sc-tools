namespace ScTools
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public readonly struct NativeCommandParameter
    {
        public string Type { get; }
        public string Name { get; }

        public NativeCommandParameter(string type, string name) => (Type, Name) = (type, name);
    }

    public readonly struct NativeCommandDefinition : IEquatable<NativeCommandDefinition>
    {
        public ulong Hash { get; }
        public string Name { get; }
        public uint Build { get; }
        public ImmutableArray<NativeCommandParameter> Parameters { get; }
        public string ReturnType { get; }

        public NativeCommandDefinition(ulong hash, string name, uint build, IEnumerable<NativeCommandParameter> parameters, string returnType)
            => (Hash, Name, Build, Parameters, ReturnType) = (hash, name, build, parameters.ToImmutableArray(), returnType);

        public override int GetHashCode() => (Hash, Name, Parameters, ReturnType).GetHashCode();

        public override bool Equals(object? obj) => obj is NativeCommandDefinition n && Equals(n);
        public bool Equals(NativeCommandDefinition other) => Equals(in other);
        public bool Equals(in NativeCommandDefinition other)
            => Hash == other.Hash &&
               Parameters.Equals(other.Parameters) &&
               ReturnType == other.ReturnType &&
               Name == other.Name;

        public static bool operator ==(in NativeCommandDefinition a, in NativeCommandDefinition b) => a.Equals(in b);
        public static bool operator !=(in NativeCommandDefinition a, in NativeCommandDefinition b) => !a.Equals(in b);
    }

    public enum GameBuild
    {
        // the value refers to the column index of that version in the translationTable
        b323_335 = 0,
        b350 = 1,
        b372 = 2,
        b393 = 3,
        b463 = 4,
        b505 = 5,
        b573 = 6,
        b617 = 7,
        b678 = 8,
        b757 = 9,
        b791 = 10,
        b877 = 11,
        b944 = 12,
        b1011_1032 = 13,
        b1103 = 14,
        b1180 = 15,
        b1290 = 16,
        b1365 = 17,
        b1493 = 18,
        b1604 = 19,
        b1737 = 20,
        b1868 = 21,
        b2060 = 22,
        b2189_2215_2245 = 23,

        Latest = b2189_2215_2245,
    }

    public sealed class NativeDB
    {
        private readonly ulong[,] translationTable;
        private readonly Dictionary<ulong, ImmutableArray<int>> hashToRows;
        public ImmutableArray<NativeCommandDefinition> Commands { get; }

        private NativeDB(ulong[,] translationTable, Dictionary<ulong, ImmutableArray<int>> hashToRows, ImmutableArray<NativeCommandDefinition> commands)
            => (this.translationTable, this.hashToRows, Commands) = (translationTable, hashToRows, commands);

        public ulong TranslateHash(ulong origHash, GameBuild build)
        {
            if (!hashToRows.TryGetValue(origHash, out var rows))
            {
                return 0;
            }

            foreach (int row in rows)
            {
                var hash = translationTable[row, (int)build];
                if (hash != 0)
                {
                    return hash;
                }
            }

            return 0;
        }

        public ulong? FindOriginalHash(string name)
        {
            foreach (var def in Commands)
            {
                if (def.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return def.Hash;
                }
            }

            return null;
        }

        public ulong? FindOriginalHash(ulong hash)
        {
            for (int i = 0; i < translationTable.GetLength(0); i++)
            {
                for (int k = 0; k < translationTable.GetLength(1); k++)
                {
                    if (translationTable[i, k] == hash)
                    {
                        for (int n = 0; n < translationTable.GetLength(1); n++)
                        {
                            if (translationTable[i, n] != 0)
                            {
                                return translationTable[i, n];
                            }
                        }
                    }
                }
            }

            return null;
        }

        public NativeCommandDefinition? GetDefinition(ulong origHash)
        {
            foreach (var def in Commands)
            {
                if (def.Hash == origHash)
                {
                    return def;
                }
            }

            return null;
        }

        public string ToJson()
        {
            var model = new JsonModel
            {
                TranslationTable = ToJaggedArray(translationTable),
                HashToRows = hashToRows.Select(kvp => new JsonModel.HashToRowsEntry { Hash = kvp.Key, Rows = kvp.Value.ToArray() }).ToArray(),
                Commands = Commands.Select(cmd => new JsonModel.Command
                {
                    Hash = cmd.Hash,
                    Name = cmd.Name,
                    Build = cmd.Build,
                    Parameters = cmd.Parameters.Select(p => new JsonModel.Param
                    {
                        Type = p.Type,
                        Name = p.Name,
                    }).ToArray(),
                    ReturnType = cmd.ReturnType,
                }).ToArray(),
            };

            return JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
        }

        public static NativeDB FromJson(string json)
        {
            var model = JsonSerializer.Deserialize<JsonModel>(json);

            return new NativeDB(To2DArray(model.TranslationTable),
                                model.HashToRows.ToDictionary(e => e.Hash, e => e.Rows.ToImmutableArray()),
                                model.Commands.Select(cmd => new NativeCommandDefinition(
                                                                cmd.Hash,
                                                                cmd.Name,
                                                                cmd.Build,
                                                                cmd.Parameters.Select(p => new NativeCommandParameter(p.Type, p.Name)),
                                                                cmd.ReturnType))
                                              .ToImmutableArray());
        }

        private sealed class JsonModel
        {
            public ulong[][] TranslationTable { get; set; }
            public HashToRowsEntry[] HashToRows { get; set; }
            public Command[] Commands { get; set; }

            public sealed class HashToRowsEntry
            {
                public ulong Hash { get; set; }
                public int[] Rows { get; set; }
            }

            public sealed class Command
            {
                public ulong Hash { get; set; }
                public string Name { get; set; }
                public uint Build { get; set; }
                public Param[] Parameters { get; set; }
                public string ReturnType { get; set; }
            }

            public sealed class Param
            {
                public string Type { get; set; }
                public string Name { get; set; }
            }
        }

        /// <summary>
        /// JSON for https://github.com/gottfriedleibniz/GTA-V-Script-Decompiler
        /// </summary>
        public string ToDecompilerJson()
        {
            using var output = new MemoryStream();
            using var json = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });
            json.WriteStartObject();
            {
                json.WriteStartObject("ALL");
                {
                    foreach (var cmd in Commands)
                    {
                        json.WriteStartObject($"0x{cmd.Hash:X16}");
                        {
                            json.WriteString("name", cmd.Name);
                            json.WriteString("jhash", "");
                            json.WriteString("comment", "");
                            json.WriteStartArray("params");
                            foreach (var p in cmd.Parameters)
                            {
                                json.WriteStartObject();
                                {
                                    json.WriteString("type", p.Type);
                                    json.WriteString("name", p.Name);
                                }
                                json.WriteEndObject();
                            }
                            json.WriteEndArray();
                            json.WriteString("return_type", cmd.ReturnType);
                            json.WriteStartArray("hashes");
                            int versions = translationTable.GetLength(1);
                            for (int i = 0; i < versions; i++)
                            {
                                var h = TranslateHash(cmd.Hash, (GameBuild)i);
                                json.WriteStringValue($"0x{h:X16}");
                            }
                            json.WriteEndArray();
                            json.WriteString("build", cmd.Build.ToString());
                        }
                        json.WriteEndObject();
                    }
                }
                json.WriteEndObject();
            }
            json.WriteEndObject();
            json.Flush();
            return Encoding.UTF8.GetString(output.GetBuffer());
        }

        private static ulong[][] ToJaggedArray(ulong[,] array2d)
        {
            var arr = new ulong[array2d.GetLength(0)][];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = new ulong[array2d.GetLength(1)];
                for (int j = 0; j < arr[i].Length; j++)
                {
                    arr[i][j] = array2d[i, j];
                }
            }
            return arr;
        }

        private static ulong[,] To2DArray(ulong[][] jaggedArray)
        {
            var arr = new ulong[jaggedArray.Length, jaggedArray[0].Length];
            for (int i = 0; i < jaggedArray.Length; i++)
            {
                for (int j = 0; j < jaggedArray[0].Length; j++)
                {
                    arr[i, j] = jaggedArray[i][j];
                }
            }
            return arr;
        }

        public static async Task<NativeDB> Fetch(Uri nativeDbUrl, Uri shvZipUrl)
        {
            using var nativeDbClient = new WebClient();
            var nativeDbTask = nativeDbClient.DownloadStringTaskAsync(nativeDbUrl).ContinueWith(t => ParseNativeDb(t.Result));

            using var crossMapClient = new WebClient();
            crossMapClient.Headers.Add(HttpRequestHeader.Referer, shvZipUrl.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped));
            var crossMapTask = crossMapClient.DownloadDataTaskAsync(shvZipUrl).ContinueWith(t => ExtractCrossMapFromSHVZip(t.Result));

            var (translationTable, hashToRows) = await crossMapTask;
            var natives = await nativeDbTask;

            return new NativeDB(translationTable, hashToRows.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray()), natives);
        }

        private static ImmutableArray<NativeCommandDefinition> ParseNativeDb(string jsonStr)
        {
            using var json = JsonDocument.Parse(jsonStr);

            var nativeCount = json.RootElement.EnumerateObject().Sum(ns => ns.Value.EnumerateObject().Count());
            var natives = ImmutableArray.CreateBuilder<NativeCommandDefinition>(nativeCount);

            foreach (var ns in json.RootElement.EnumerateObject())
            {
                foreach (var native in ns.Value.EnumerateObject())
                {
                    var hash = ulong.Parse(native.Name.AsSpan()[2..], System.Globalization.NumberStyles.HexNumber);
                    var name = native.Value.GetProperty("name").GetString();
                    var build = uint.Parse(native.Value.GetProperty("build").GetString());
                    var parameters = native.Value.GetProperty("params")
                                                 .EnumerateArray()
                                                 .Select(p => new NativeCommandParameter(p.GetProperty("type").GetString(),
                                                                                         p.GetProperty("name").GetString()));
                    var returnType = native.Value.GetProperty("return_type").GetString();

                    natives.Add(new NativeCommandDefinition(hash, name, build, parameters, returnType));
                }
            }

            return natives.MoveToImmutable();
        }

        private static (ulong[,] TranslationTable, Dictionary<ulong, List<int>> HashToRows) ExtractCrossMapFromSHVZip(byte[] zipFile)
        {
            byte[] dllFile;

            using (var zip = new ZipArchive(new MemoryStream(zipFile, writable: false), ZipArchiveMode.Read, leaveOpen: false))
            using (var ms = new MemoryStream())
            {
                zip.GetEntry("bin/ScriptHookV.dll").Open().CopyTo(ms);
                dllFile = ms.ToArray();
            }

            return ExtractCrossMapFromSHV(dllFile);
        }

        private static (ulong[,] TranslationTable, Dictionary<ulong, List<int>> HashToRows) ExtractCrossMapFromSHV(Span<byte> dllFile)
        {
            const int Base = 0x400; // TODO: the offset of the .text section could change
            var code = dllFile[Base..];

            // 33 D2 4D 8B C2 8B CA 49 39 38
            int addr = FindPattern(code, new byte[] { 0x33, 0xD2, 0x4D, 0x8B, 0xC2, 0x8B, 0xCA, 0x49, 0x39, 0x38 });

            Console.WriteLine($"FindPattern => {addr}    0x{addr:X}");

            if (addr == -1)
            {
                return (new ulong[0,0], new Dictionary<ulong, List<int>>());
            }

            int translationTableRVA = ReadInt32(code, addr - 4);
            int translationTableAddr = addr + translationTableRVA - 0x600; // - 0xC00; // TODO: where does 0x600/0xC00 come from?

            uint tableEntrySize = ReadUInt32(code, addr + 0x11);
            uint versionCount = tableEntrySize / sizeof(ulong);
            uint nativeCount = ReadUInt32(code, addr + 0x1A);

            Console.WriteLine($"Translation Table Addr: {translationTableAddr:X8}");
            Console.WriteLine($"Entry Size:             {tableEntrySize:X8}");
            Console.WriteLine($"Version count:          {versionCount}");
            Console.WriteLine($"Native count:           {nativeCount}");

            var table = new ulong[nativeCount, versionCount];
            var hashToRows = new Dictionary<ulong, List<int>>((int)nativeCount);
            for (int i = 0; i < nativeCount; i++)
            {
                ulong origHash = 0;
                for (int k = 0; k < versionCount; k++)
                {
                    var hash = ReadUInt64(code, translationTableAddr + k * sizeof(ulong) + i * (int)tableEntrySize);

                    table[i, k] = hash;

                    if (origHash == 0 && hash != 0)
                    {
                        origHash = hash;
                    }
                }

                if (!hashToRows.TryGetValue(origHash, out var indices))
                {
                    indices = new List<int>(1);
                    hashToRows.Add(origHash, indices);
                }

                indices.Add(i);
            }

            return (table, hashToRows);

            static int ReadInt32(Span<byte> data, int offset) => BitConverter.ToInt32(data[offset..(offset + sizeof(int))]);
            static uint ReadUInt32(Span<byte> data, int offset) => BitConverter.ToUInt32(data[offset..(offset + sizeof(uint))]);
            static ulong ReadUInt64(Span<byte> data, int offset) => BitConverter.ToUInt64(data[offset..(offset + sizeof(ulong))]);

            static int FindPattern(Span<byte> data, Span<byte> pattern)
            {
                for (int i = 0; i < data.Length - pattern.Length; i++)
                {
                    if (data[i..(i + pattern.Length)].SequenceEqual(pattern))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }
    }
}
