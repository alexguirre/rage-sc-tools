namespace ScTools.ScriptLang.Semantics;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

internal sealed class TypeFactory
{
    private readonly Visitor visitor;
    private readonly SemanticsAnalyzer semantics;

    public TypeFactory(SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
    {
        visitor = new(exprTypeChecker);
        this.semantics = semantics;
    }

    public TypeInfo GetFrom(TypeName typeName) => typeName.Accept(visitor, semantics);
    public TypeInfo GetFrom(ITypeDeclaration typeDecl) => typeDecl.Accept(visitor, semantics);
    public TypeInfo GetFrom(IValueDeclaration valueDecl) => valueDecl.Accept(visitor, semantics);

    private static TypeInfo Error => ErrorType.Instance;

    private sealed class Visitor : AstVisitor<TypeInfo, SemanticsAnalyzer>
    {
        private readonly ExpressionTypeChecker exprTypeChecker;

        public Visitor(ExpressionTypeChecker exprTypeChecker)
        {
            this.exprTypeChecker = exprTypeChecker;
        }

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
                var ty = BuildTypeForVarDeclaration(node, s);
                node.Semantics = node.Semantics with { ValueType = ty };
            }

            return node.Semantics.ValueType;
        }
        
        private TypeInfo BuildTypeForVarDeclaration(VarDeclaration node, SemanticsAnalyzer s)
        {
            var varType = node.Type.Accept(this, s);
            var declarator = node.Declarator;
            if (!declarator.IsArray)
            {
                return varType;
            }

            Debug.Assert(declarator.Lengths.All(l => l is not null), "Incomplete array types are not supported for now");
            return declarator.Lengths.Reverse().Aggregate(varType, (ty, lengthExpr) =>
            {
                var lengthType = lengthExpr?.Accept(exprTypeChecker, s) ?? ErrorType.Instance;

                ConstantValue? length = null;
                if (!lengthType.IsError)
                {
                    if (!lengthExpr!.ValueKind.Is(ValueKind.Constant))
                    {
                        s.ArrayLengthExpressionIsNotConstantError(node, lengthExpr);
                    }
                    else if (IntType.Instance.IsAssignableFrom(lengthType))
                    {
                        length = ConstantExpressionEvaluator.Eval(lengthExpr, s);
                    }
                    else
                    {
                        s.CannotConvertTypeError(lengthType, IntType.Instance, lengthExpr.Location);
                    }
                }

                return new ArrayType(ty, length?.IntValue ?? 0);
            });
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

        public override TypeInfo Visit(FunctionTypeDefDeclaration node, SemanticsAnalyzer s)
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
            var parametersTy = parameters.Select(p => new ParameterInfo(p.Accept(this, s), p.IsReference, p.Initializer));
            return new FunctionType(returnTy, parametersTy.ToImmutableArray());
        }

        public override TypeInfo Visit(NativeTypeDeclaration node, SemanticsAnalyzer s)
        {
            if (node.Semantics.DeclaredType is null)
            {
                NativeType? baseTy = null;
                if (node.BaseType is not null)
                {
                    var baseTyResolved = node.BaseType.Accept(this, s);
                    if (baseTyResolved.IsError)
                    {
                        baseTy = null;
                    }
                    else if (baseTyResolved is not NativeType)
                    {
                        s.ExpectedNativeTypeError(node.BaseType);
                        baseTy = null;
                    }
                    else
                    {
                        baseTy = (NativeType)baseTyResolved;
                    }
                }

                var ty = new NativeType(node, baseTy);
                node.Semantics = node.Semantics with { DeclaredType = ty };
            }
            
            return node.Semantics.DeclaredType;
        }
    }
}
