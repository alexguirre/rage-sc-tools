namespace ScTools.ScriptLang
{
    using System;
    using System.Linq;

    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;

    /// <summary>
    /// Pretty-printer for <see cref="IType"/>s.
    /// </summary>
    public static class TypePrinter
    {
        public static string ToString(IType type, string? name)
            => type switch
            {
                AnyType => Simple("ANY", name),
                IArrayType arrayTy => Array(arrayTy, name),
                BoolType => Simple("BOOL", name),
                EnumType enumTy => Simple(enumTy.Declaration.Name, name),
                FloatType => Simple("FLOAT", name),
                FuncType funcTy => Func(funcTy, name),
                IntType => Simple("INT", name),
                NamedType namedTy => namedTy.ResolvedType is not null ? ToString(namedTy.ResolvedType, name) : Simple($"(unresolved type '{namedTy.Name}')", name),
                NullType => Simple("(null)", name),
                RefType refTy => Ref(refTy, name),
                StringType => Simple("STRING", name),
                StructType structTy => Simple(structTy.Declaration.Name, name),
                TextLabelType lblTy => Simple($"TEXT_LABEL{lblTy.Length}", name),
                VoidType => Simple("(void)", name),
                _ => Simple(type.ToString() ?? "<type.ToString() is null>", name),
            };

        private static string Simple(string typeName, string? name) => string.IsNullOrEmpty(name) ? typeName : $"{typeName} {name}";

        private static string Array(IArrayType arrayTy, string? name)
        {
            var lengths = "";
            while (true)
            {
                var length = arrayTy switch
                {
                    ArrayType a => a.Length.ToString(),
                    _ => "",
                };
                lengths += $"[{length}]";

                if (arrayTy.ItemType is IArrayType ty)
                {
                    arrayTy = ty;
                }
                else
                {
                    break;
                }
            }

            return string.IsNullOrEmpty(name) ?
                  $"{ToString(arrayTy.ItemType, null)}{lengths}" :
                  $"{ToString(arrayTy.ItemType, null)} {name}{lengths}";
        }

        private static string Func(FuncType funcTy, string? name)
        {
            var argsStr = $"({string.Join(", ", funcTy.Declaration.Parameters.Select(p => ToString(p.Type, p.Name)))})";
            var returnStr = funcTy.Declaration.IsProc ?
                                "PROC" :
                                $"FUNC {ToString(funcTy.Declaration.ReturnType, string.Empty)}";
            var nameStr = string.IsNullOrEmpty(name) ?
                                "" :
                                $" {name}";

            return returnStr + nameStr + argsStr;
        }

        private static string Ref(RefType refTy, string? name)
        {
            if (refTy.PointeeType is IArrayType arrTy)
            {
                return Array(arrTy, string.IsNullOrEmpty(name) ? $"(&)" : $"(&{name})");
            }
            else
            {
                return string.IsNullOrEmpty(name) ? $"{ToString(refTy.PointeeType, null)}&" : $"{ToString(refTy.PointeeType, null)}& { name}";
            }
        }
    }
}
