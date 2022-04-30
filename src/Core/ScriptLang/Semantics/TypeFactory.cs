namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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

        public override TypeInfo Visit(BuiltInTypeDeclaration node, SemanticsAnalyzer param)
            => node.BuiltInType;

        public override TypeInfo Visit(EnumDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.DeclaredType is null)
            {
                var enumTy = new EnumType(node);
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
                var offset = 0;
                var fields = node.Fields.Select(f =>
                {
                    var field = new FieldInfo(f.Accept(this, s), f.Name, offset);
                    offset += field.Type.SizeOf;
                    return field;
                }).ToImmutableArray();
                node.Semantics = node.Semantics with { DeclaredType = new StructType(node, fields) };
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

        public override TypeInfo Visit(FunctionPointerTypeDeclaration node, SemanticsAnalyzer s)
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
            var parametersTy = parameters.Select(p => new ParameterInfo(p.Accept(this, s), p.IsReference));
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
            => param.Var.Type.Accept(declarationVisitor, param.S);

        public override TypeInfo Visit(VarArrayDeclarator node, (VarDeclaration Var, SemanticsAnalyzer S) param)
        {
            var itemType = param.Var.Type.Accept(declarationVisitor, param.S);
            // TODO: check that the length expression is constant
            Debug.Assert(node.Lengths.All(l => l is not null), "Incomplete array types are not supported for now");
            var arrayType = node.Lengths.Reverse().Aggregate(itemType, (ty, lengthExpr) => new ArrayType(ty, ConstantExpressionEvaluator.Eval(lengthExpr!, param.S).IntValue));
            return arrayType;
        }
    }

}
