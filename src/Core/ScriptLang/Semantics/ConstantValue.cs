namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Types;

using System;

public abstract record ConstantValue
{
    public abstract TypeInfo Type { get; }
    public abstract int IntValue { get; }
    public abstract float FloatValue { get; }
    public abstract bool BoolValue { get; }
    public abstract string? StringValue { get; }
    public abstract (float X, float Y, float Z) VectorValue { get; }

    public static ConstantValue Null => ConstantValueNull.Instance;
    public static ConstantValue Int(int value) => new ConstantValueInt(value);
    public static ConstantValue Float(float value) => new ConstantValueFloat(value);
    public static ConstantValue Bool(bool value) => new ConstantValueBool(value);
    public static ConstantValue String(string value) => new ConstantValueString(value);
    public static ConstantValue Vector(float x, float y, float z) => new ConstantValueVector(x, y, z);

    private sealed record ConstantValueInt(int Value) : ConstantValue
    {
        public override TypeInfo Type => IntType.Instance;
        public override int IntValue => Value;
        public override float FloatValue => throw new InvalidOperationException("Cannot implicity converted INT to FLOAT");
        public override bool BoolValue => Value != 0;
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity converted INT to STRING");
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity converted INT to VECTOR");
    }

    private sealed record ConstantValueFloat(float Value) : ConstantValue
    {
        public override TypeInfo Type => FloatType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity converted FLOAT to INT");
        public override float FloatValue => Value;
        public override bool BoolValue => throw new InvalidOperationException("Cannot implicity converted FLOAT to BOOL");
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity converted FLOAT to STRING");
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity converted FLOAT to VECTOR");
    }

    private sealed record ConstantValueBool(bool Value) : ConstantValue
    {
        public override TypeInfo Type => BoolType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity converted BOOL to INT");
        public override float FloatValue => throw new InvalidOperationException("Cannot implicity converted BOOL to FLOAT");
        public override bool BoolValue => Value;
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity converted BOOL to STRING");
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity converted BOOL to VECTOR");
    }

    private sealed record ConstantValueString(string Value) : ConstantValue
    {
        public override TypeInfo Type => StringType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity converted STRING to INT");
        public override float FloatValue => throw new InvalidOperationException("Cannot implicity converted STRING to FLOAT");
        public override bool BoolValue => throw new InvalidOperationException("Cannot implicity converted STRING to BOOL");
        public override string? StringValue => Value;
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity converted STRING to VECTOR");
    }

    private sealed record ConstantValueVector(float X, float Y, float Z) : ConstantValue
    {
        public override TypeInfo Type => VectorType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity converted VECTOR to INT");
        public override float FloatValue => throw new InvalidOperationException("Cannot implicity converted VECTOR to FLOAT");
        public override bool BoolValue => throw new InvalidOperationException("Cannot implicity converted VECTOR to BOOL");
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity converted VECTOR to STRING");
        public override (float X, float Y, float Z) VectorValue => (X, Y, Z);
    }

    private sealed record ConstantValueNull : ConstantValue
    {
        public override TypeInfo Type => NullType.Instance;
        public override int IntValue => 0;
        public override float FloatValue => 0.0f;
        public override bool BoolValue => false;
        public override string? StringValue => null;
        public override (float X, float Y, float Z) VectorValue => (0.0f, 0.0f, 0.0f);

        private ConstantValueNull() { }

        public static readonly ConstantValueNull Instance = new();
    }
}
