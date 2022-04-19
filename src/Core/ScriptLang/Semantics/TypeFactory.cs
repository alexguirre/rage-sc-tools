namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;
using System.Linq;

internal sealed class TypeFactory : EmptyVisitor<TypeInfo, SemanticsAnalyzer>
{
    private readonly Visitor visitor;
    private readonly SemanticsAnalyzer semantics;

    public TypeFactory(SemanticsAnalyzer semantics)
    {
        visitor = new();
        this.semantics = semantics;
    }

    public TypeInfo GetFrom(TypeName typeName) => typeName.Accept(visitor, semantics);
    public TypeInfo GetFrom(ITypeDeclaration typeDecl) => typeDecl.Accept(visitor, semantics);
    public TypeInfo GetFrom(IValueDeclaration valueDecl) => valueDecl.Accept(visitor, semantics);

    private static TypeInfo Error => ErrorType.Instance;

    private sealed class Visitor : EmptyVisitor<TypeInfo, SemanticsAnalyzer>
    {
        private readonly VarDeclaratorVisitor varDeclaratorVisitor;

        public Visitor()
        {
            varDeclaratorVisitor = new(this);
        }

        public override TypeInfo Visit(EnumDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.DeclaredType is null)
            {
                var enumTy = new EnumType(node.Name);
                node.Members.ForEach(m => m.Semantics = m.Semantics with { ValueType = enumTy });
                node.Semantics = node.Semantics with { DeclaredType = enumTy };
            }

            return node.Semantics.DeclaredType;
        }

        public override TypeInfo Visit(EnumMemberDeclaration node, SemanticsAnalyzer s)
            => node.Semantics.ValueType ?? throw new InvalidOperationException($"Visited enum member '{node.Name}' before its enum declaration");

        public override TypeInfo Visit(StructDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.DeclaredType is null)
            {
                var fields = node.Fields.Select(f => new FieldInfo(f.Accept(this, s), f.Name));
                node.Semantics = node.Semantics with { DeclaredType = new StructType(node.Name, fields.ToImmutableArray()) };
            }

            return node.Semantics.DeclaredType;
        }

        public override TypeInfo Visit(TypeName node, SemanticsAnalyzer s)
            => s.GetTypeSymbol(node, out var type) ? type : Error;

        public override TypeInfo Visit(VarDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.ValueType is null)
            {
                var ty = node.Declarator.Accept(varDeclaratorVisitor, (node, s));
                node.Semantics = node.Semantics with { ValueType = ty };
            }

            return node.Semantics.ValueType;
        }

        public override TypeInfo Visit(FunctionDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.ValueType is null)
            {
                var ty = MakeFunctionType(node.ReturnType, node.Parameters, s);
                node.Semantics = node.Semantics with { ValueType = ty };
            }

            return node.Semantics.ValueType;
        }

        public override TypeInfo Visit(FunctionPointerDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.DeclaredType is null)
            {
                var ty = MakeFunctionType(node.ReturnType, node.Parameters, s);
                node.Semantics = node.Semantics with { DeclaredType = ty };
            }

            return node.Semantics.DeclaredType;
        }

        public override TypeInfo Visit(NativeFunctionDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.ValueType is null)
            {
                var ty = MakeFunctionType(node.ReturnType, node.Parameters, s);
                node.Semantics = node.Semantics with { ValueType = ty };
            }

            return node.Semantics.ValueType;
        }

        private FunctionType MakeFunctionType(TypeName? returnType, ImmutableArray<VarDeclaration> parameters, SemanticsAnalyzer s)
        {
            var returnTy = returnType?.Accept(this, s) ?? VoidType.Instance;
            var parametersTy = parameters.Select(p => p.Accept(this, s));
            return new FunctionType(returnTy, parametersTy.ToImmutableArray());
        }
    }

    private sealed class VarDeclaratorVisitor : EmptyVisitor<TypeInfo, (VarDeclaration Var, SemanticsAnalyzer S)>
    {
        private readonly Visitor declarationVisitor;

        public VarDeclaratorVisitor(Visitor declarationVisitor)
        {
            this.declarationVisitor = declarationVisitor;
        }

        public override TypeInfo Visit(VarDeclarator node, (VarDeclaration Var, SemanticsAnalyzer S) param)
            => param.Var.Type.Accept(declarationVisitor, param.S);

        public override TypeInfo Visit(VarRefDeclarator node, (VarDeclaration Var, SemanticsAnalyzer S) param)
            => new RefType(param.Var.Type.Accept(declarationVisitor, param.S));

        public override TypeInfo Visit(VarArrayDeclarator node, (VarDeclaration Var, SemanticsAnalyzer S) param)
        {
            var itemType = param.Var.Type.Accept(declarationVisitor, param.S);
            // TODO: evaluate length expression of arrays
            var arrayType = node.Lengths.Reverse().Aggregate(itemType, (ty, lengthExpr) => new ArrayType(ty, 999));
            return arrayType;
        }
    }

}
