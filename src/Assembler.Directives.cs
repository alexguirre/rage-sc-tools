namespace ScTools
{
    using System;
    using ScTools.GameFiles;

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
                    throw new AssemblerSyntaxException($"Unknown token '{tokens.Current.ToString()}' after directive '{d.Name}'");
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
                    var name = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"Missing name token in {d.Name} directive");
                    a.SetName(name.ToString());
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("STATICS",
                (d, t, a) =>
                {
                    var countStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"Missing statics count token in {d.Name} directive");
                    uint count;
                    try
                    {
                        count = uint.Parse(countStr);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException($"Statics count token in {d.Name} directive is not a valid uint32 value", e);
                    }
                    a.SetStaticsCount(count);
                    Directive.NoMoreTokens(d, t);
                }),
            new Directive("NATIVE_DEF",
                (d, t, a) =>
                {
                    var hashStr = t.MoveNext() ? t.Current : throw new AssemblerSyntaxException($"Missing native hash token in {d.Name} directive");
                    ulong hash;
                    try
                    {
                        hash = ulong.Parse(hashStr, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException)
                    {
                        throw new AssemblerSyntaxException($"Native hash token in {d.Name} directive is not a valid uint64 hex value", e);
                    }
                    a.AddNative(hash);
                    Directive.NoMoreTokens(d, t);
                }),
        });
    }
}
