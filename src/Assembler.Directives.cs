namespace ScTools
{
    using System;

    internal partial class Assembler
    {
        private delegate void DirectiveCallback(Directive directive, TokenEnumerator tokens, Assembler assembler);

        private readonly struct Directive
        {
            public string Name { get; }
            public uint NameHash { get; }
            public DirectiveCallback Callback { get; }
            
            public Directive(string name, DirectiveCallback callback)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                NameHash = name.ToHash();
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            public static void NoMoreTokens(Directive d, TokenEnumerator tokens)
            {
                if (tokens.MoveNext())
                {
                    throw new ArgumentException($"Unknown token '{tokens.Current.ToString()}' after directive '{d.Name}'", nameof(tokens));
                }
            }

            public static Directive[] Sort(Directive[] directives)
            {
                // sort the directive based on NameHash so we can do binary search later
                Array.Sort(directives, (a, b) => a.NameHash.CompareTo(b.NameHash));
                return directives;
            }

            public static int Find(uint nameHash)
            {
                int left = 0;
                int right = Directives.Length - 1;

                while (left <= right)
                {
                    int middle = (left + right) / 2;
                    uint middleKey = Directives[middle].NameHash;
                    int cmp = middleKey.CompareTo(nameHash);
                    if (cmp == 0)
                    {
                        return middle;
                    }
                    else if (cmp < 0)
                    {
                        left = middle + 1;
                    }
                    else
                    {
                        right = middle - 1;
                    }
                }
                return -1;
            }
        }
        
        private static readonly Directive[] Directives = Directive.Sort(new[]
        {
            new Directive("NAME",
                (d, t, a) =>
                {
                    var name = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing name token in {d.Name} directive");
                    a.SetName(name.ToString());
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("STATICS",
                (d, t, a) =>
                {
                    var countStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing statics count token in {d.Name} directive");
                    uint count;
                    try
                    {
                        count = uint.Parse(countStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Statics count token in {d.Name} directive is not a valid uint32 value", e);
                    }
                    a.SetStaticsCount(count);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("STATIC_INT_INIT",
                (d, t, a) =>
                {
                    var indexStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing static index token in {d.Name} directive");
                    uint index;
                    try
                    {
                        index = uint.Parse(indexStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Static index token in {d.Name} directive is not a valid uint32 value", e);
                    }

                    var valueStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing value token in {d.Name} directive");
                    int value;
                    try
                    {
                        value = int.Parse(valueStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Value token in {d.Name} directive is not a valid int32 value", e);
                    }

                    a.SetStaticValue(index, value);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("STATIC_FLOAT_INIT",
                (d, t, a) =>
                {
                    var indexStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing static index token in {d.Name} directive");
                    uint index;
                    try
                    {
                        index = uint.Parse(indexStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Static index token in {d.Name} directive is not a valid uint32 value", e);
                    }

                    var valueStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing value token in {d.Name} directive");
                    float value;
                    try
                    {
                        value = float.Parse(valueStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Value token in {d.Name} directive is not a valid float32 value", e);
                    }

                    a.SetStaticValue(index, value);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("HASH", // tmp directive until we figure out how to calculate this hash automatically
                (d, t, a) =>
                {
                    var hashStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing hash token in {d.Name} directive");
                    uint hash;
                    try
                    {
                        hash = hashStr.StartsWith("0x".AsSpan()) ? uint.Parse(hashStr[2..], System.Globalization.NumberStyles.HexNumber) : throw new FormatException();
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Hash token in {d.Name} directive is not a valid uint32 hex value", e);
                    }
                    a.SetHash(hash);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("GLOBALS",
                (d, t, a) =>
                {
                    var blockStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing global block token in {d.Name} directive");
                    byte block;
                    try
                    {
                        block = byte.Parse(blockStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Global block token in {d.Name} directive is not a valid uint8 value", e);
                    }

                    var countStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing globals count token in {d.Name} directive");
                    uint length;
                    try
                    {
                        length = uint.Parse(countStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Globals count token in {d.Name} directive is not a valid uint32 value", e);
                    }
                    a.SetGlobals(block, length);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("GLOBAL_INT_INIT",
                (d, t, a) =>
                {
                    var idStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing global ID token in {d.Name} directive");
                    uint id;
                    try
                    {
                        id = uint.Parse(idStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Global ID token in {d.Name} directive is not a valid uint32 value", e);
                    }

                    var valueStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing value token in {d.Name} directive");
                    int value;
                    try
                    {
                        value = int.Parse(valueStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Value token in {d.Name} directive is not a valid int32 value", e);
                    }

                    a.SetGlobalValue(id, value);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("GLOBAL_FLOAT_INIT",
                (d, t, a) =>
                {
                    var idStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing global ID token in {d.Name} directive");
                    uint id;
                    try
                    {
                        id = uint.Parse(idStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Global ID token in {d.Name} directive is not a valid uint32 value", e);
                    }

                    var valueStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing value token in {d.Name} directive");
                    float value;
                    try
                    {
                        value = float.Parse(valueStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Value token in {d.Name} directive is not a valid float32 value", e);
                    }

                    a.SetGlobalValue(id, value);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("NATIVE_DEF",
                (d, t, a) =>
                {
                    var hashStr = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing native hash token in {d.Name} directive");
                    ulong hash;
                    try
                    {
                        hash = ulong.Parse(hashStr, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new ArgumentException($"Native hash token in {d.Name} directive is not a valid uint64 hex value", e);
                    }
                    a.AddNative(hash);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("STRING",
                (d, t, a) =>
                {
                    var str = t.MoveNext() ? t.Current : throw new ArgumentException($"Missing string token in {d.Name} directive");
                    if (!Token.IsString(str, out var contents))
                    {
                        throw new ArgumentException($"String token in {d.Name} directive is not a valid string");
                    }
                    a.AddString(contents.Unescape());
                    Directive.NoMoreTokens(d, t);
                }),
        });
    }
}
