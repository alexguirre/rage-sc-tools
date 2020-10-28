#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.ScriptLang.Semantics.Symbols;

    public abstract class Type : IEquatable<Type>
    {
        public abstract int SizeOf { get; }

        public abstract bool Equals(Type? other);

        protected abstract int DoGetHashCode();

        public override bool Equals(object? obj)
            => obj is Type t && Equals(t);

        public override int GetHashCode() => DoGetHashCode();

        public static bool operator ==(Type? lhs, Type? rhs) => ReferenceEquals(lhs, rhs) || (lhs?.Equals(rhs) ?? false);
        public static bool operator !=(Type? lhs, Type? rhs) => !(lhs == rhs);
    }

    public sealed class BasicType : Type
    {
        public BasicTypeCode TypeCode { get; }
        public override int SizeOf => 1;

        public BasicType(BasicTypeCode typeCode) => TypeCode = typeCode;

        public override bool Equals(Type? other)
            => other is BasicType b && b.TypeCode == TypeCode;

        protected override int DoGetHashCode()
            => HashCode.Combine(TypeCode);
    }

    public enum BasicTypeCode
    {
        Int,
        Float,
        Bool,
        String,
    }

    public sealed class StructType : Type
    {
        public string? Name { get; set; }
        public IList<Field> Fields { get; set; }
        public override int SizeOf => Fields.Sum(f => f.Type.SizeOf);

        public StructType(string? name, IEnumerable<Field> fields)
            => (Name, Fields) = (name, new List<Field>(fields));

        public StructType(string? name, params Field[] fields) : this(name, (IEnumerable<Field>)fields) { }

        public override bool Equals(Type? other)
        {
            if (!(other is StructType s))
            {
                return false;
            }

            if (s.Name != Name)
            {
                return false;
            }

            if (s.Fields.Count != Fields.Count)
            {
                return false;
            }

            for (int i = 0; i < Fields.Count; i++)
            {
                if (s.Fields[i] != Fields[i])
                {
                    return false;
                }
            }

            return true;
        }

        protected override int DoGetHashCode()
        {
            HashCode h = default;
            foreach (var f in Fields)
            {
                h.Add(f);
            }
            return h.ToHashCode();
        }
    }

    public readonly struct Field : IEquatable<Field>
    {
        public Type Type { get; }
        public string Name { get; }

        public Field(Type type, string name) => (Type, Name) = (type, name);

        public bool Equals(Field other)
            => other.Type == Type && other.Name == Name;

        public override bool Equals(object? obj)
            => obj is Field f && Equals(f);

        public override int GetHashCode()
            => HashCode.Combine(Type, Name);

        public static bool operator ==(Field lhs, Field rhs) => lhs.Equals(rhs);
        public static bool operator !=(Field lhs, Field rhs) => !(lhs == rhs);
    }

    public sealed class FunctionType : Type
    {
        public Type? ReturnType { get; set; }
        public IList<Type> Parameters { get; set; }
        public override int SizeOf => 1;

        public FunctionType(Type? returnType, IEnumerable<Type> parameters)
            => (ReturnType, Parameters) = (returnType, new List<Type>(parameters));

        public override bool Equals(Type? other)
        {
            if (!(other is FunctionType f))
            {
                return false;
            }

            if (f.ReturnType != ReturnType)
            {
                return false;
            }

            if (f.Parameters.Count != Parameters.Count)
            {
                return false;
            }

            for (int i = 0; i < Parameters.Count; i++)
            {
                if (f.Parameters[i] != Parameters[i])
                {
                    return false;
                }
            }

            return true;
        }

        protected override int DoGetHashCode()
        {
            HashCode h = default;
            h.Add(ReturnType);
            foreach (var f in Parameters)
            {
                h.Add(f);
            }
            return h.ToHashCode();
        }
    }

    public sealed class UnresolvedType : Type
    {
        public string TypeName { get; }
        public override int SizeOf => throw new InvalidOperationException("Unresolved type");

        public UnresolvedType(string typeName) => TypeName = typeName;

        public Type? Resolve(SymbolTable symbols)
        {
            var symbol = symbols.Lookup(TypeName);

            return (symbol as TypeSymbol)?.Type;
        }

        public override bool Equals(Type? other)
            => other is UnresolvedType t && t.TypeName == TypeName;

        protected override int DoGetHashCode()
            => HashCode.Combine(TypeName);
    }
}
