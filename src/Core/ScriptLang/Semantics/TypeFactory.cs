namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;
using System.Linq;

internal sealed class TypeFactory : EmptyVisitor<TypeInfo, SemanticAnalyzer>
{
    private readonly Visitor visitor;
    private readonly SemanticAnalyzer semantics;

    public TypeFactory(SemanticAnalyzer semantics)
    {
        visitor = new();
        this.semantics = semantics;
    }

    public TypeInfo GetFrom(TypeName typeName) => typeName.Accept(visitor, semantics);
    public TypeInfo GetFrom(ITypeDeclaration typeDecl) => typeDecl.Accept(visitor, semantics);
    public TypeInfo GetFrom(IValueDeclaration valueDecl) => valueDecl.Accept(visitor, semantics);

    private static TypeInfo Error => ErrorType.Instance;

    private sealed class Visitor : EmptyVisitor<TypeInfo, SemanticAnalyzer>
    {
        private readonly VarDeclaratorVisitor varDeclaratorVisitor;

        public Visitor()
        {
            varDeclaratorVisitor = new(this);
        }

        public override TypeInfo Visit(EnumDeclaration node, SemanticAnalyzer s)
        {
            if (node.Semantics.DeclaredType is null)
            {
                var enumTy = new EnumType(node.Name);
                node.Members.ForEach(m => m.Semantics = m.Semantics with { ValueType = enumTy });
                node.Semantics = node.Semantics with { DeclaredType = enumTy };
            }

            return node.Semantics.DeclaredType;
        }

        public override TypeInfo Visit(EnumMemberDeclaration node, SemanticAnalyzer s)
            => node.Semantics.ValueType ?? throw new InvalidOperationException($"Visited enum member '{node.Name}' before its enum declaration");

        public override TypeInfo Visit(StructDeclaration node, SemanticAnalyzer s)
        {
            if (node.Semantics.DeclaredType is null)
            {
                var fields = node.Fields.Select(f => new FieldInfo(f.Accept(this, s), f.Name));
                node.Semantics = node.Semantics with { DeclaredType = new StructType(node.Name, fields.ToImmutableArray()) };
            }

            return node.Semantics.DeclaredType;
        }

        public override TypeInfo Visit(TypeName node, SemanticAnalyzer s)
            => s.GetTypeSymbol(node, out var type) ? type : Error;

        public override TypeInfo Visit(VarDeclaration node, SemanticAnalyzer s)
            => node.Declarator.Accept(varDeclaratorVisitor, (node, s));

        public override TypeInfo Visit(FunctionDeclaration node, SemanticAnalyzer s)
            => MakeFunctionType(node.ReturnType, node.Parameters, s);

        public override TypeInfo Visit(FunctionPointerDeclaration node, SemanticAnalyzer s)
            => MakeFunctionType(node.ReturnType, node.Parameters, s);

        public override TypeInfo Visit(NativeFunctionDeclaration node, SemanticAnalyzer s)
            => MakeFunctionType(node.ReturnType, node.Parameters, s);

        private FunctionType MakeFunctionType(TypeName? returnType, ImmutableArray<VarDeclaration> parameters, SemanticAnalyzer s)
        {
            var returnTy = returnType?.Accept(this, s) ?? VoidType.Instance;
            var parametersTy = parameters.Select(p => p.Accept(this, s));
            return new FunctionType(returnTy, parametersTy.ToImmutableArray());
        }
    }

    private sealed class VarDeclaratorVisitor : EmptyVisitor<TypeInfo, (VarDeclaration Var, SemanticAnalyzer S)>
    {
        private readonly Visitor declarationVisitor;

        public VarDeclaratorVisitor(Visitor declarationVisitor)
        {
            this.declarationVisitor = declarationVisitor;
        }

        public override TypeInfo Visit(VarDeclarator node, (VarDeclaration Var, SemanticAnalyzer S) param)
            => param.Var.Type.Accept(declarationVisitor, param.S);

        public override TypeInfo Visit(VarRefDeclarator node, (VarDeclaration Var, SemanticAnalyzer S) param)
            => new RefType(param.Var.Type.Accept(declarationVisitor, param.S));

        public override TypeInfo Visit(VarArrayDeclarator node, (VarDeclaration Var, SemanticAnalyzer S) param)
        {
            TypeInfo itemType = param.Var.Type.Accept(declarationVisitor, param.S);
            // TODO: array type error checking
            return Error;
        }
    }

}
