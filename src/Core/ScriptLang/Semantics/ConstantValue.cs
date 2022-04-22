namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Types;

using System;
using System.Diagnostics;

[DebuggerDisplay("{ToString(),nq}")]
public abstract record ConstantValue
{
    public abstract TypeInfo Type { get; }
    public abstract int IntValue { get; }
    public abstract float FloatValue { get; }
    public abstract bool BoolValue { get; }
    public abstract string? StringValue { get; }
    public abstract (float X, float Y, float Z) VectorValue { get; }

    public void Match(
        Action caseNull,
        Action<int> caseInt,
        Action<float> caseFloat,
        Action<bool> caseBool,
        Action<string> caseString,
        Action<float, float, float> caseVector)
    {
        switch (Type)
        {
            case NullType: caseNull(); break;
            case IntType: caseInt(IntValue); break;
            case FloatType: caseFloat(FloatValue); break;
            case BoolType: caseBool(BoolValue); break;
            case StringType: caseString(StringValue!); break;
            case VectorType:
                var (x, y, z) = VectorValue;
                caseVector(x, y, z);
                break;

            default: Debug.Assert(false, $"Unsupported type '{Type.ToPrettyString()}'"); break;
        }
    }

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
        public override float FloatValue => Value;
        public override bool BoolValue => Value != 0;
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity convert INT to STRING");
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity convert INT to VECTOR");

        public override string ToString() => $"{nameof(ConstantValueInt)}({Value})";
    }

    private sealed record ConstantValueFloat(float Value) : ConstantValue
    {
        public override TypeInfo Type => FloatType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity convert FLOAT to INT");
        public override float FloatValue => Value;
        public override bool BoolValue => throw new InvalidOperationException("Cannot implicity convert FLOAT to BOOL");
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity convert FLOAT to STRING");
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity convert FLOAT to VECTOR");

        public override string ToString() => $"{nameof(ConstantValueFloat)}({Value})";
    }

    private sealed record ConstantValueBool(bool Value) : ConstantValue
    {
        public override TypeInfo Type => BoolType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity convert BOOL to INT");
        public override float FloatValue => throw new InvalidOperationException("Cannot implicity convert BOOL to FLOAT");
        public override bool BoolValue => Value;
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity convert BOOL to STRING");
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity convert BOOL to VECTOR");

        public override string ToString() => $"{nameof(ConstantValueBool)}({Value})";
    }

    private sealed record ConstantValueString(string Value) : ConstantValue
    {
        public override TypeInfo Type => StringType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity convert STRING to INT");
        public override float FloatValue => throw new InvalidOperationException("Cannot implicity convert STRING to FLOAT");
        public override bool BoolValue => throw new InvalidOperationException("Cannot implicity convert STRING to BOOL");
        public override string? StringValue => Value;
        public override (float X, float Y, float Z) VectorValue => throw new InvalidOperationException("Cannot implicity convert STRING to VECTOR");

        public override string ToString() => $"{nameof(ConstantValueString)}({Value})";
    }

    private sealed record ConstantValueVector(float X, float Y, float Z) : ConstantValue
    {
        public override TypeInfo Type => VectorType.Instance;
        public override int IntValue => throw new InvalidOperationException("Cannot implicity convert VECTOR to INT");
        public override float FloatValue => throw new InvalidOperationException("Cannot implicity convert VECTOR to FLOAT");
        public override bool BoolValue => throw new InvalidOperationException("Cannot implicity convert VECTOR to BOOL");
        public override string? StringValue => throw new InvalidOperationException("Cannot implicity convert VECTOR to STRING");
        public override (float X, float Y, float Z) VectorValue => (X, Y, Z);

        public override string ToString() => $"{nameof(ConstantValueVector)}({X}, {Y}, {Z})";
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

        public override string ToString() => nameof(ConstantValueString);

        public static readonly ConstantValueNull Instance = new();
    }
}
