//namespace ScTools.ScriptLang.Types;

//using System;
//using System.Collections.Immutable;
//using System.Linq;

///// <summary>
///// Represents a native object type.
///// </summary>
///// <param name="BaseClass"></param>
///// <param name="DerivedClass"></param>
//public readonly record struct HandleKind(int BaseClass, int DerivedClass)
//{
//    public bool IsBase => DerivedClass == 0;
//    public bool IsDerived => DerivedClass != 0;

//    public bool IsAssignableFrom(HandleKind source)
//        => (this.BaseClass == source.BaseClass) &&
//           ((this.DerivedClass == source.DerivedClass) || this.IsBase);
//}

///// <summary>
///// Strongly typed integer that represents a handle to a native object.
///// </summary>
///// <param name="Kind">Native object type identifier.</param>
//public sealed record HandleType(HandleKind Kind) : TypeInfo
//{
//    public override int SizeOf => 1;
//    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

//    public override string ToPrettyString() => KindToTypeName(Kind);
//    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

//    public static ImmutableArray<HandleType> All => Five.All;

//    public static string KindToTypeName(HandleKind kind) => Five.KindToTypeName(kind);

//    // TODO: move this handle type declarations somewhere else once we support more compilation targets/games
//    public static class Five
//    {
//        public static readonly HandleKind
//            PlayerIndex = new(1, 0),
//            EntityIndex = new(2, 0),
//                PedIndex = EntityIndex with { DerivedClass = 1 },
//                VehicleIndex = EntityIndex with { DerivedClass = 2 },
//                ObjectIndex = EntityIndex with { DerivedClass = 3 },
//            CameraIndex = new(3, 0),
//            PickupIndex = new(4, 0),
//            BlipInfoId = new(5, 0);

//        public static readonly ImmutableArray<HandleType> All = ImmutableArray.Create<HandleType>(
//            new(PlayerIndex), new(EntityIndex), new(PedIndex), new(VehicleIndex), new(ObjectIndex), new(CameraIndex), new(PickupIndex), new(BlipInfoId));

//        public static string KindToTypeName(HandleKind kind)
//            // keep synchronized with the declarations above
//            => kind.BaseClass switch
//            {
//                1 => "PLAYER_INDEX",
//                2 => kind.DerivedClass switch
//                {
//                    0 => "ENTITY_INDEX",
//                    1 => "PED_INDEX",
//                    2 => "VEHICLE_INDEX",
//                    3 => "OBJECT_INDEX",
//                    _ => throw new ArgumentOutOfRangeException(nameof(kind))
//                },
//                3 => "CAMERA_INDEX",
//                4 => "PICKUP_INDEX",
//                5 => "BLIP_INFO_ID",
//                _ => throw new ArgumentOutOfRangeException(nameof(kind))
//            };
//    }
//}
