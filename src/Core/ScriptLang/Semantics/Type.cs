#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Checks whether <paramref name="dest"/> and <paramref name="src"/> are the same type;
        /// or, if both are <see cref="StructType"/>, whether <paramref name="src"/> is an aggregate type with the same field layout as <paramref name="dest"/>.
        /// </summary>
        public bool IsAssignableFrom(Type src, bool considerReferences)
        {
            var dest = this;
            bool assignable = dest == src;

            if (!assignable)
            {
                if (dest is StructType destStructType && src is StructType srcStructType)
                {
                    bool srcIsAggregate = srcStructType.Name == null;
                    assignable = srcIsAggregate && srcStructType.HasSameFieldLayout(destStructType);
                }
                else if (considerReferences)
                {
                    assignable = (dest is RefType destRefType && destRefType.ElementType.IsAssignableFrom(src, considerReferences: false)) ||
                                 (src is RefType srcRefType && dest.IsAssignableFrom(srcRefType.ElementType, considerReferences: false));
                }
            }

            return assignable;
        }

        /// <summary>
        /// Gets this <see cref="Type"/>, except for <see cref="RefType"/>s, in which it returns its <see cref="RefType.ElementType"/>.
        /// </summary>
        public Type UnderlyingType => this is RefType refType ? refType.ElementType : this;
    }

    public sealed class RefType : Type
    {
        public override int SizeOf => 1;
        public Type ElementType { get; }

        public RefType(Type elementType)
            => ElementType = elementType is RefType ? throw new ArgumentException("References to references are not allowed") :
                                                      elementType;

        public override bool Equals(Type? other)
            => other is RefType r && r.ElementType == ElementType;

        protected override int DoGetHashCode()
            => HashCode.Combine(ElementType);
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

        public bool HasField(string name)
        {
            foreach (var f in Fields)
            {
                if (f.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public Type TypeOfField(string name)
        {
            foreach (var f in Fields)
            {
                if (f.Name == name)
                {
                    return f.Type;
                }
            }

            throw new ArgumentException($"No field with name '{name}' exists in struct '{Name}'");
        }

        public int OffsetOfField(string name)
        {
            int offset = 0;
            foreach (var f in Fields)
            {
                if (f.Name == name)
                {
                    return offset;
                }
                else
                {
                    offset += f.Type.SizeOf;
                }
            }

            throw new ArgumentException($"No field with name '{name}' exists in struct '{Name}'");
        }

        public bool Equals(Type? other, bool ignoreNames)
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

        public bool HasSameFieldLayout(StructType other)
        {
            if (other.Fields.Count != Fields.Count)
            {
                return false;
            }

            for (int i = 0; i < Fields.Count; i++)
            {
                if (!SameTypeIgnoreFieldNames(other.Fields[i].Type, Fields[i].Type))
                {
                    return false;
                }
            }

            return true;

            static bool SameTypeIgnoreFieldNames(Type left, Type right)
            {
                if (left == right)
                {
                    return true;
                }

                if (left is StructType leftStruct && right is StructType rightStruct)
                {
                    return leftStruct.HasSameFieldLayout(rightStruct);
                }

                return false;
            }
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

        public static Type NewAggregate(IEnumerable<Type> fieldTypes)
            => new StructType(null, fieldTypes.Select((t, i) => new Field(t, $"_item{i}")));
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
        public bool IsReference { get; }
        public override int SizeOf => throw new InvalidOperationException("Unresolved type");

        public UnresolvedType(string typeName, bool isReference)
            => (TypeName, IsReference) = (typeName, isReference);

        public Type? Resolve(SymbolTable symbols)
        {
            var symbol = symbols.Lookup(TypeName);
            var type = (symbol as TypeSymbol)?.Type;
            return type != null ?
                (IsReference ? new RefType(type) : type) :
                null;
        }

        public override bool Equals(Type? other)
            => other is UnresolvedType t && t.TypeName == TypeName && t.IsReference == IsReference;

        protected override int DoGetHashCode()
            => HashCode.Combine(TypeName, IsReference);
    }
}
