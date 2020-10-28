namespace ScTools
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    public readonly struct NativeCommand : IEquatable<NativeCommand>
    {
        public ulong Hash { get; }
        public ulong CurrentHash { get; }
        public string Name { get; }
        public byte ParameterCount { get; }
        public byte ReturnValueCount { get; }

        public NativeCommand(ulong hash, ulong currentHash, string name, byte parameterCount, byte returnValueCount)
        {
            Hash = hash;
            CurrentHash = currentHash;
            Name = name;
            ParameterCount = parameterCount;
            ReturnValueCount = returnValueCount;
        }

        public override int GetHashCode() => (Hash, CurrentHash, ParameterCount, ReturnValueCount, Name).GetHashCode();

        public override bool Equals(object obj) => obj is NativeCommand n && Equals(n);
        public bool Equals(NativeCommand other) => Equals(in other);
        public bool Equals(in NativeCommand other)
            => Hash == other.Hash &&
               CurrentHash == other.CurrentHash &&
               ParameterCount == other.ParameterCount &&
               ReturnValueCount == other.ReturnValueCount &&
               Name == other.Name;

        public static bool operator ==(in NativeCommand a, in NativeCommand b) => a.Equals(in b);
        public static bool operator !=(in NativeCommand a, in NativeCommand b) => !a.Equals(in b);
    }

    public sealed class NativeDBOld
    {
        public const uint HeaderMagic = 0x2042444E; // 'NDB '

        public ImmutableArray<NativeCommand> Natives { get; }

        private NativeDBOld(IEnumerable<NativeCommand> natives)
        {
            Natives = natives.ToImmutableArray();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(HeaderMagic);
            writer.Write(Natives.Length);
            foreach (ref readonly var native in Natives.AsSpan())
            {
                writer.Write(native.Hash);
                writer.Write(native.CurrentHash);
                writer.Write(native.ParameterCount);
                writer.Write(native.ReturnValueCount);
                writer.Write(native.Name);
            }
        }

        public static NativeDBOld Load(BinaryReader reader)
        {
            if (reader.BaseStream.Length < sizeof(uint) * 2)
            {
                throw new InvalidDataException("Incorrect size");
            }

            var magic = reader.ReadUInt32();

            if (magic != HeaderMagic)
            {
                throw new InvalidDataException("Incorrect header");
            }

            var nativeCount = reader.ReadInt32();
            var natives = new NativeCommand[nativeCount];
            try
            {
                for (int i = 0; i < nativeCount; i++)
                {
                    var hash = reader.ReadUInt64();
                    var currentHash = reader.ReadUInt64();
                    var paramCount = reader.ReadByte();
                    var returnValueCount = reader.ReadByte();
                    var name = reader.ReadString();

                    natives[i] = new NativeCommand(hash, currentHash, name, paramCount, returnValueCount);
                }
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataException("Incorrect native count", e);
            }

            return new NativeDBOld(natives);
        }

        public static async Task<NativeDBOld> Fetch(Uri crossMapUrl, Uri nativeDbUrl)
        {
            using var crossMapClient = new WebClient();
            var crossMapTask = crossMapClient.DownloadStringTaskAsync(crossMapUrl).ContinueWith(t => ParseCrossMap(t.Result));

            using var nativeDbClient = new WebClient();
            var nativeDbTask = nativeDbClient.DownloadStringTaskAsync(nativeDbUrl).ContinueWith(t => ParseNativeDb(t.Result));

            var crossMap = await crossMapTask;
            var natives = await nativeDbTask;

            return new NativeDBOld(natives.Select(n => new NativeCommand(n.Hash, crossMap.GetValueOrDefault(n.Hash, n.Hash), n.Name, n.ParameterCount, n.ReturnValueCount)));
        }

        private static Dictionary<ulong, ulong> ParseCrossMap(string crossMapStr)
        {
            static bool IsNewLine(char c) => c == '\r' || c == '\n';

            static bool NextLine(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> line)
            {
                if (text.Length == 0)
                {
                    line = default;
                    return false;
                }

                line = text;

                int length = 0;
                while (length < text.Length && !IsNewLine(text[length])) { length++; } // skip until we find a new line
                line = line[0..length];

                while (length < text.Length && IsNewLine(text[length])) { length++; } // in case we found "\r\n" or empty lines, skip
                text = text[length..];

                return true;
            }

            List<ulong[]> crossmap = new List<ulong[]>();
            List<ulong> currHashes = new List<ulong>();
            ReadOnlySpan<char> s = crossMapStr;
            int lineNumber = 1;
            while (NextLine(ref s, out var line))
            {
                line = line[4..^4]; // remove '{ { ' and '} },'

                bool allZeros = true;
                int hashStart = 0;
                int hashLength = -1;
                while (hashStart < line.Length)
                {
                    var remaining = line[hashStart..];
                    hashLength = remaining.IndexOf(", ".AsSpan());
                    hashLength = hashLength == -1 ? remaining.Length : hashLength;

                    var hashStr = line[hashStart..(hashStart + hashLength)];
                    var hash = ulong.Parse(hashStr[2..], System.Globalization.NumberStyles.HexNumber);

                    if (hash != 0)
                    {
                        allZeros = false;
                    }

                    currHashes.Add(hash);

                    hashStart += hashLength + 2;
                }

                if (crossmap.Count > 0 && currHashes.Count != crossmap[0].Length)
                {
                    throw new FormatException($"Incorrect number of hashes in line {lineNumber}");
                }

                if (!allZeros)
                {
                    crossmap.Add(currHashes.ToArray());
                }
                currHashes.Clear();

                lineNumber++;
            }

            return crossmap.Aggregate(new Dictionary<ulong, ulong>(), (dict, hashes) =>
            {
                var originalHash = hashes.First(h => h != 0);
                var currentHash = hashes.Last();
                if (currentHash == 0)
                {
                    currentHash = originalHash;
                }

                dict.TryAdd(originalHash, currentHash);
                return dict;
            });
        }

        private static NativeCommand[] ParseNativeDb(string jsonStr)
        {
            using var json = JsonDocument.Parse(jsonStr);

            var nativeCount = json.RootElement.EnumerateObject().Sum(ns => ns.Value.EnumerateObject().Count());
            var natives = new NativeCommand[nativeCount];

            int i = 0;
            foreach (var ns in json.RootElement.EnumerateObject())
            {
                foreach (var native in ns.Value.EnumerateObject())
                {
                    var hash = ulong.Parse(native.Name.AsSpan()[2..], System.Globalization.NumberStyles.HexNumber);
                    var name = native.Value.GetProperty("name").GetString();
                    byte paramCount = (byte)native.Value.GetProperty("params").GetArrayLength();
                    byte returnValueCount = native.Value.GetProperty("return_type").GetString() switch
                    {
                        "void" => 0,
                        "Vector3" => 3,
                        _ => 1
                    };

                    natives[i++] = new NativeCommand(hash, 0, name, paramCount, returnValueCount);
                }
            }

            return natives;
        }
    }
}
