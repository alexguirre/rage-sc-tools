#nullable enable
namespace ScTools.ScriptLang
{
    using System;
    using System.IO;
    using System.Linq;

    public static class NativeCommandsGen
    {
        public static void Generate(TextWriter w, NativeDB db)
        {
            foreach (var cmd in db.Commands)
            {
                w.Write("NATIVE ");
                var returnTy = TypeToScriptType(cmd.ReturnType, true);
                if (returnTy == null)
                {
                    w.Write("PROC ");
                }
                else
                {
                    w.Write("FUNC ");
                    w.Write(returnTy);
                    w.Write(' ');
                }
                w.Write(cmd.Name);
                w.Write('(');
                w.Write(string.Join(", ", cmd.Parameters.Select(p => $"{TypeToScriptType(p.Type, false)} {SafeSymbol(p.Name)}")));
                w.Write(')');
                w.WriteLine();
            }
        }

        private static string SafeSymbol(string name)
        {
            // TODO: check for other keywords, REPEAT is the only one found currently in the nativeDB
            return string.Equals(name, "REPEAT", StringComparison.OrdinalIgnoreCase) ? (name + "_") : name;
        }

        private static string? TypeToScriptType(string type, bool returnType)
            => type switch
            {
                "void" => null,
                "Any" => "ANY",
                "Any*" when returnType => "INT",
                "Any*" => "ANY&",
                "BOOL" => "BOOL",
                "BOOL*" => "BOOL&",
                "int" => "INT",
                "int*" => "INT&",
                "float" => "FLOAT",
                "float*" => "FLOAT&",
                "const char*" => "STRING",
                "Hash" => "INT",
                "Hash*" => "INT&",
                "Entity" => "ENTITY_INDEX",
                "Entity*" => "ENTITY_INDEX&",
                "Ped" => "PED_INDEX",
                "Ped*" => "PED_INDEX&",
                "Vehicle" => "VEHICLE_INDEX",
                "Vehicle*" => "VEHICLE_INDEX&",
                "Object" => "OBJECT_INDEX",
                "Object*" => "OBJECT_INDEX&",
                "Cam" => "CAMERA_INDEX",
                "Cam*" => "CAMERA_INDEX&",
                "Player" => "PLAYER_INDEX",
                "Player*" => "PLAYER_INDEX&",
                "Pickup" => "PICKUP_INDEX",
                "Pickup*" => "PICKUP_INDEX&",
                "Blip" => "BLIP_INFO_ID",
                "Blip*" => "BLIP_INFO_ID&",
                "Vector3" => "VECTOR",
                "Vector3*" => "VECTOR&",
                "ScrHandle" => "INT",
                "ScrHandle*" => "INT&",
                "FireId" => "INT",
                "FireId*" => "INT&",
                "Interior" => "INT",
                "Interior*" => "INT&",
                "char*" => "ANY&", // TODO: only allow TEXT_LABEL somehow
                _ => throw new InvalidOperationException($"Cannot convert type '{type}'"),
            };

    }
}
