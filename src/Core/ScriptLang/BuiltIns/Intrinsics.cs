namespace ScTools.ScriptLang.BuiltIns
{
    using System.Linq;

    using ScTools.ScriptLang.Ast.Declarations;

    public static class Intrinsics
    {
        public static FuncDeclaration F2V { get; } = CreateFunc("F2V", BuiltInTypes.Vector, (BuiltInTypes.Float, "value"));
        public static FuncDeclaration F2I { get; } = CreateFunc("F2I", BuiltInTypes.Int, (BuiltInTypes.Float, "value"));
        public static FuncDeclaration I2F { get; } = CreateFunc("I2F", BuiltInTypes.Float, (BuiltInTypes.Int, "value"));

        private static FuncDeclaration CreateFunc(string name, ITypeDeclaration returnType, params (ITypeDeclaration Type, string Name)[] parameters) =>
            new(SourceRange.Unknown, name, FuncKind.Intrinsic,
                new(SourceRange.Unknown, name + "@proto", returnType.CreateType(SourceRange.Unknown))
                {
                    Parameters = parameters.Select(p => new VarDeclaration(SourceRange.Unknown, p.Name, p.Type.CreateType(SourceRange.Unknown), VarKind.Parameter)).ToList()
                });
    }
}
