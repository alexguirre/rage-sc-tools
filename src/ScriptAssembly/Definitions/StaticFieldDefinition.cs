namespace ScTools.ScriptAssembly.Definitions
{
    using System;
    using System.Diagnostics;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Types;

    public class StaticFieldDefinition : FieldDefinition, ISymbolDefinition
    {
        public uint Id { get; }
        public ScriptValue InitialValue { get; }

        public StaticFieldDefinition(string name, TypeBase type, ScriptValue initialValue) : base(name, type)
        {
            Id = Registry.NameToId(Name);
            InitialValue = initialValue;
        }

        public void InitializeValue(Span<ScriptValue> dest)
        {
            static void AutoInit(Span<ScriptValue> dest, ScriptValue initialValue)
            {
                dest[0] = initialValue;
            }

            static void ArrayInit(Span<ScriptValue> dest, ArrayType arr)
            {
                dest[0].AsUInt32 = arr.Length;

                dest = dest[1..];
                int itemSize = (int)arr.ItemType.SizeOf;
                if (!(arr.ItemType is AutoType))
                {
                    for (int i = 0; i < arr.Length; i++, dest = dest[itemSize..])
                    {
                        Init(arr.ItemType, dest[0..itemSize]);
                    }
                }
            }

            static void StructInit(Span<ScriptValue> dest, StructType struc)
            {
                for (int i = 0; i < struc.Fields.Length - 1; i++)
                {
                    int start = (int)struc.Offsets[i];
                    int end = (int)struc.Offsets[i + 1];
                    var f = struc.Fields[i];
                    Init(f.Type, dest[start..end], f.InitialValue);
                }

                var last = struc.Fields[^1];
                Init(last.Type, dest[(int)struc.Offsets[^1]..], last.InitialValue);
            }

            static void Init(TypeBase type, Span<ScriptValue> dest, ScriptValue? autoInitValue = null)
            {
                Debug.Assert(type.SizeOf == dest.Length);

                switch (type)
                {
                    case AutoType _ when autoInitValue.HasValue: AutoInit(dest, autoInitValue.Value); break;
                    case ArrayType arr: ArrayInit(dest, arr); break;
                    case StructType struc: StructInit(dest, struc); break;
                }
            }


            dest.Fill(default); // zero out the destination
            Init(Type, dest, InitialValue.AsUInt64 == default ? (ScriptValue?)null : InitialValue);
        }
    }

    public sealed class ArgDefinition : StaticFieldDefinition
    {
        public ArgDefinition(string name, TypeBase type, ScriptValue initialValue) : base(name, type, initialValue)
        {
        }
    }
}
