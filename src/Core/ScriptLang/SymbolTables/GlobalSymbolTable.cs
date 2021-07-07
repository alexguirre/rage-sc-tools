namespace ScTools.ScriptLang.SymbolTables
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.BuiltIns;

    /// <summary>
    /// Table with symbols available anywhere in the script (i.e. built-ins, enums, structs, functions, procedures and non-local variables).
    /// </summary>
    public sealed class GlobalSymbolTable
    {
        private readonly Dictionary<string, ITypeDeclaration> typeDeclarations = new(Parser.CaseInsensitiveComparer);
        private readonly Dictionary<string, IValueDeclaration> valueDeclarations = new(Parser.CaseInsensitiveComparer);

        public IEnumerable<ITypeDeclaration> Types => typeDeclarations.Values;
        public IEnumerable<IValueDeclaration> Values => valueDeclarations.Values;

        public GlobalSymbolTable()
        {
            AddBuiltIns();
        }

        public bool AddType(ITypeDeclaration typeDeclaration) => typeDeclarations.TryAdd(typeDeclaration.Name, typeDeclaration);
        public bool AddValue(IValueDeclaration valueDeclaration) => valueDeclarations.TryAdd(valueDeclaration.Name, valueDeclaration);

        public ITypeDeclaration? FindType(string name) => typeDeclarations.TryGetValue(name, out var decl) ? decl : null;
        public IValueDeclaration? FindValue(string name) => valueDeclarations.TryGetValue(name, out var decl) ? decl : null;

        private void AddBuiltIns()
        {
            AddType(BuiltInTypes.Int);
            AddType(BuiltInTypes.Float);
            AddType(BuiltInTypes.Bool);
            AddType(BuiltInTypes.String);
            AddType(BuiltInTypes.Any);
            AddType(BuiltInTypes.Vector);
            AddType(BuiltInTypes.PlayerIndex);
            AddType(BuiltInTypes.EntityIndex);
            AddType(BuiltInTypes.PedIndex);
            AddType(BuiltInTypes.VehicleIndex);
            AddType(BuiltInTypes.ObjectIndex);
            AddType(BuiltInTypes.CameraIndex);
            AddType(BuiltInTypes.PickupIndex);
            AddType(BuiltInTypes.BlipInfoId);
            BuiltInTypes.TextLabels.ForEach(lbl => AddType(lbl));

            Intrinsics.AllIntrinsics.ForEach(intrin => AddValue(intrin));
        }
    }
}
