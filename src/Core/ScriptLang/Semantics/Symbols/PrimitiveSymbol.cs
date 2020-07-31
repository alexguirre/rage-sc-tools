#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    public abstract class PrimitiveSymbol : ISymbol
    {
        public string Name { get; }
        public PrimitiveType Primitive { get; }

        private PrimitiveSymbol(string name, PrimitiveType primitive) => (Name, Primitive) = (name, primitive);

        public static PrimitiveSymbol Int { get; } = new IntSymbol();
        public static PrimitiveSymbol Float { get; } = new FloatSymbol();
        public static PrimitiveSymbol Bool { get; } = new BoolSymbol();
        public static PrimitiveSymbol Vec3 { get; } = new Vec3Symbol();

        private sealed class IntSymbol : PrimitiveSymbol { public IntSymbol() : base("INT", PrimitiveType.Int) { } }
        private sealed class FloatSymbol : PrimitiveSymbol { public FloatSymbol() : base("FLOAT", PrimitiveType.Float) { } }
        private sealed class BoolSymbol : PrimitiveSymbol { public BoolSymbol() : base("BOOL", PrimitiveType.Bool) { } }
        private sealed class Vec3Symbol : PrimitiveSymbol { public Vec3Symbol() : base("VEC3", PrimitiveType.Bool) { } }
    }

    public enum PrimitiveType
    {
        Int,
        Float,
        Bool,
        Vec3,
    }
}
