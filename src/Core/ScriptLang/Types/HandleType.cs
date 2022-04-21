namespace ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;
using System.Linq;

public enum HandleKind
{
    PlayerIndex,
    EntityIndex,
    PedIndex,
    VehicleIndex,
    ObjectIndex,
    CameraIndex,
    PickupIndex,
    BlipInfoId,
}

public sealed record HandleType(HandleKind Kind) : TypeInfo
{
    public override int SizeOf => 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override string ToPrettyString() => KindToTypeName(Kind);
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    public static ImmutableArray<HandleType> All { get; } = Enum.GetValues<HandleKind>().Select(h => new HandleType(h)).ToImmutableArray();

    public static string KindToTypeName(HandleKind kind)
        => kind switch
        {
            HandleKind.PlayerIndex => "PLAYER_INDEX",
            HandleKind.EntityIndex => "ENTITY_INDEX",
            HandleKind.PedIndex => "PED_INDEX",
            HandleKind.VehicleIndex => "VEHICLE_INDEX",
            HandleKind.ObjectIndex => "OBJECT_INDEX",
            HandleKind.CameraIndex => "CAMERA_INDEX",
            HandleKind.PickupIndex => "PICKUP_INDEX",
            HandleKind.BlipInfoId => "BLIP_INFO_ID",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}
