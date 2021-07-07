namespace ScTools.ScriptLang.Semantics
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;

    /// <summary>
    /// Replaces <see cref="NamedType"/>s with their resolved types.
    /// <para>
    /// It is executed in <see cref="IdentificationVisitor.Visit(Program, DiagnosticsReport, SymbolTables.GlobalSymbolTable)"/>
    /// when the <see cref="IdentificationVisitor"/> finishes.
    /// </para>
    /// <para>
    /// Note, <see cref="IExpression.Type"/>s are not replaced. This
    /// visitor should be used before <see cref="TypeChecker"/> is
    /// executed so expressions should not have any types yet.
    /// Only types from declarations and other types are replaced.
    /// </para>
    /// </summary>
    internal sealed class RemoveNamedTypesVisitor : DFSVisitor
    {
        public override Void Visit(EnumMemberDeclaration node, Void param)
        {
            node.Type = Clean(node.Type);
            return base.Visit(node, param);
        }

        public override Void Visit(FuncProtoDeclaration node, Void param)
        {
            node.ReturnType = Clean(node.ReturnType);
            return base.Visit(node, param);
        }

        public override Void Visit(VarDeclaration node, Void param)
        {
            node.Type = Clean(node.Type);
            return base.Visit(node, param);
        }

        public override Void Visit(StructField node, Void param)
        {
            node.Type = Clean(node.Type);
            return base.Visit(node, param);
        }

        public override Void Visit(IncompleteArrayType node, Void param)
        {
            node.ItemType = Clean(node.ItemType);
            return base.Visit(node, param);
        }

        public override Void Visit(ArrayType node, Void param)
        {
            node.ItemType = Clean(node.ItemType);
            return base.Visit(node, param);
        }

        public override Void Visit(NamedType node, Void param)
        {
            Debug.Assert(false, $"{nameof(NamedType)} reached but it should have been removed already");
            return base.Visit(node, param);
        }

        private static IType Clean(IType type)
            => type is NamedType namedType ? namedType.CheckedResolvedType : type;
    }
}
