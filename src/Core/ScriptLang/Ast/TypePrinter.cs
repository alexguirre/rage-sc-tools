namespace ScTools.ScriptLang
{
    using System.Linq;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    /// <summary>
    /// Pretty-printer for <see cref="IType"/>s.
    /// </summary>
    public static class TypePrinter
    {
        public static string ToString(IType type, string? name, bool isRef)
            => isRef ? Ref(type, name) : type switch
            {
                AnyType => Simple("ANY", name),
                IArrayType arrayTy => Array(arrayTy, name),
                BoolType => Simple("BOOL", name),
                EnumType enumTy => Simple(enumTy.Declaration.Name, name),
                FloatType => Simple("FLOAT", name),
                FuncType funcTy => Func(funcTy, name),
                HandleType handleTy => Simple(HandleType.KindToTypeName(handleTy.Kind), name),
                IntType => Simple("INT", name),
                NamedType namedTy => namedTy.ResolvedType is not null ? ToString(namedTy.ResolvedType, name, isRef) : Simple($"(unresolved type '{namedTy.Name}')", name),
                NullType => Simple("(null)", name),
                StringType => Simple("STRING", name),
                StructType structTy => Simple(structTy.Declaration.Name, name),
                TextLabelType lblTy => Simple($"TEXT_LABEL_{lblTy.Length-1}", name),
                TypeNameType => Simple("(type)", name),
                VectorType => Simple("VECTOR", name),
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
                  $"{ToString(arrayTy.ItemType, null, false)}{lengths}" :
                  $"{ToString(arrayTy.ItemType, null, false)} {name}{lengths}";
        }

        private static string Func(FuncType funcTy, string? name)
        {
            var prefixStr = funcTy.Declaration.Kind switch
            {
                FuncKind.Native => "NATIVE ",
                _ => "",
            };
            var argsStr = $"({string.Join(", ", funcTy.Declaration.Parameters.Select(p => ToString(p.Type, p.Name, p.IsReference)))})";
            var returnStr = funcTy.Declaration.IsProc ?
                                (funcTy.Declaration.Kind is FuncKind.Script ? "SCRIPT" : "PROC") :
                                $"FUNC {ToString(funcTy.Declaration.ReturnType, string.Empty, false)}";
            var nameStr = string.IsNullOrEmpty(name) ?
                                "" :
                                $" {name}";

            return prefixStr + returnStr + nameStr + argsStr;
        }

        private static string Ref(IType pointeeType, string? name)
        {
            if (pointeeType is IArrayType arrTy)
            {
                return Array(arrTy, name); // arrays are always passed by reference so don't need to include '&'
            }
            else
            {
                return string.IsNullOrEmpty(name) ? $"{ToString(pointeeType, null, false)}&" : $"{ToString(pointeeType, null, false)}& {name}";
            }
        }
    }
}
