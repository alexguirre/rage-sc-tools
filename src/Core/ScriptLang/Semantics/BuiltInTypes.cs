#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System.Collections.Immutable;

    public static class BuiltInTypes
    {
        public static readonly BasicType INT = new BasicType(BasicTypeCode.Int);
        public static readonly BasicType FLOAT = new BasicType(BasicTypeCode.Float);
        public static readonly BasicType BOOL = new BasicType(BasicTypeCode.Bool);
        public static readonly BasicType STRING = new BasicType(BasicTypeCode.String);

        public static readonly AnyType ANY = AnyType.Instance;

        public static readonly StructType VECTOR = new StructType(nameof(VECTOR), F(FLOAT, "x"), F(FLOAT, "y"), F(FLOAT, "z"));
        public static readonly StructType PLAYER_INDEX = new StructType(nameof(PLAYER_INDEX), F(INT, "value"));
        public static readonly StructType ENTITY_INDEX = new StructType(nameof(ENTITY_INDEX), F(INT, "value"));
        public static readonly StructType PED_INDEX = new StructType(nameof(PED_INDEX), F(INT, "value"));
        public static readonly StructType VEHICLE_INDEX = new StructType(nameof(VEHICLE_INDEX), F(INT, "value"));
        public static readonly StructType OBJECT_INDEX = new StructType(nameof(OBJECT_INDEX), F(INT, "value"));
        public static readonly StructType CAMERA_INDEX = new StructType(nameof(CAMERA_INDEX), F(INT, "value"));
        public static readonly StructType PICKUP_INDEX = new StructType(nameof(PICKUP_INDEX), F(INT, "value"));
        public static readonly StructType BLIP_INFO_ID = new StructType(nameof(BLIP_INFO_ID), F(INT, "value"));
        private static Field F(Type ty, string name) => new Field(ty, name);

        public static readonly ImmutableArray<StructType> STRUCTS;
        public static readonly ImmutableArray<TextLabelType> TEXT_LABELS;

        static BuiltInTypes()
        {
            STRUCTS = ImmutableArray.Create(VECTOR,
                                            PLAYER_INDEX,
                                            ENTITY_INDEX,
                                            PED_INDEX,
                                            VEHICLE_INDEX,
                                            OBJECT_INDEX,
                                            CAMERA_INDEX,
                                            PICKUP_INDEX,
                                            BLIP_INFO_ID);

            var textLabels = ImmutableArray.CreateBuilder<TextLabelType>(TextLabelType.MaxLength - TextLabelType.MinLength + 1);
            for (int len = TextLabelType.MinLength; len <= TextLabelType.MaxLength; len++)
            {
                textLabels.Add(new TextLabelType(len));
            }
            TEXT_LABELS = textLabels.MoveToImmutable();
        }
    }
}
