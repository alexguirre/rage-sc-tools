#nullable enable
namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

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

        /// <summary>
        /// If the <paramref name="name"/> is disallowed (i.e. is a keyword), add a suffix so it no longer collides.
        /// </summary>
        private static string SafeSymbol(string name)
            => Keywords.Contains(name) ? (name + "_") : name;

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

        private static readonly HashSet<string> Keywords = 
            typeof(Grammar.ScLangLexer)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.Name.StartsWith("K_")) // get all constants that start with 'K_' (prefix used for keywords in the grammar)
                .Select(f => f.Name[2..]) // remove prefix 'K_'
                .ToHashSet(Parser.CaseInsensitiveComparer);
    }
}
