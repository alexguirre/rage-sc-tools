namespace ScTools.ScriptLang.BuiltIns
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.SymbolTables;

    public interface IIntrinsicDeclaration : IValueDeclaration
    {
        int EvalInt(InvocationExpression expr, GlobalSymbolTable symbols);
        float EvalFloat(InvocationExpression expr, GlobalSymbolTable symbols);
        bool EvalBool(InvocationExpression expr, GlobalSymbolTable symbols);
        string EvalString(InvocationExpression expr, GlobalSymbolTable symbols);
        (float X, float Y, float Z) EvalVector(InvocationExpression expr, GlobalSymbolTable symbols);
    }

    public static partial class Intrinsics
    {
        private static readonly StringOrIntDecl StringOrInt = new();
        private static readonly GenericTextLabelDecl GenericTextLabel = new();

        public static IIntrinsicDeclaration F2V { get; } = new BasicIntrinsic(new F2VIntrinsicType());
        public static IIntrinsicDeclaration F2I { get; } = new BasicIntrinsic(new F2IIntrinsicType());
        public static IIntrinsicDeclaration I2F { get; } = new BasicIntrinsic(new I2FIntrinsicType());
        public static IIntrinsicDeclaration Append { get; } = new BasicIntrinsic(new AppendIntrinsicType());
        public static IIntrinsicDeclaration CountOf { get; } = new CountOfIntrinsic();
        public static IIntrinsicDeclaration EnumToInt { get; } = new EnumToIntIntrinsic();
        public static IIntrinsicDeclaration IntToEnum { get; } = new IntToEnumIntrinsic();

        public static ImmutableArray<IIntrinsicDeclaration> AllIntrinsics { get; } = ImmutableArray.Create(F2V, F2I, I2F, Append, CountOf, EnumToInt, IntToEnum);

        public static IIntrinsicDeclaration? FindIntrinsic(string name) => AllIntrinsics.FirstOrDefault(i => ParserNew.CaseInsensitiveComparer.Equals(name, i.Name));


        private sealed class BasicIntrinsic : BaseValueDeclaration, IIntrinsicDeclaration
        {
            public BasicIntrinsic(BasicIntrinsicType type) : base(SourceRange.Unknown, type.Name, type) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();

            public int EvalInt(InvocationExpression expr, GlobalSymbolTable symbols) => ((BasicIntrinsicType)Type).EvalInt(expr, symbols);
            public float EvalFloat(InvocationExpression expr, GlobalSymbolTable symbols) => ((BasicIntrinsicType)Type).EvalFloat(expr, symbols);
            public bool EvalBool(InvocationExpression expr, GlobalSymbolTable symbols) => ((BasicIntrinsicType)Type).EvalBool(expr, symbols);
            public string EvalString(InvocationExpression expr, GlobalSymbolTable symbols) => ((BasicIntrinsicType)Type).EvalString(expr, symbols);
            public (float X, float Y, float Z) EvalVector(InvocationExpression expr, GlobalSymbolTable symbols) => ((BasicIntrinsicType)Type).EvalVector(expr, symbols);
        }

        private abstract class BasicIntrinsicType : BaseType
        {
            public override int SizeOf => throw new NotImplementedException();

            public string Name { get; }
            public IType ReturnType { get; }
            public IList<VarDeclaration> Parameters { get; }
            public bool CanBeConstantIfAllArgsAreConstant { get; set; }

            public BasicIntrinsicType(string name, params (ITypeDeclaration Type, string Name, bool IsRef)[] parameters)
                : this(name, new VoidType(SourceRange.Unknown), parameters)
            {
            }

            public BasicIntrinsicType(string name, ITypeDeclaration returnType, params (ITypeDeclaration Type, string Name, bool IsRef)[] parameters)
                : this(name, returnType.CreateType(SourceRange.Unknown), parameters)
            {
            }

            public BasicIntrinsicType(string name, IType returnType, params (ITypeDeclaration Type, string Name, bool IsRef)[] parameters) : base(SourceRange.Unknown)
            {
                Name = name;
                ReturnType = returnType;
                Parameters = parameters.Select(p => new VarDeclaration(SourceRange.Unknown, p.Name, p.Type.CreateType(SourceRange.Unknown), VarKind.Parameter, p.IsRef)).ToList();
            }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();

            public override bool Equivalent(IType other)
            => other is BasicIntrinsicType otherIntrin &&
               Parameters.Count == otherIntrin.Parameters.Count &&
               ReturnType.Equivalent(otherIntrin.ReturnType) &&
               Parameters.Zip(otherIntrin.Parameters).All(p => p.First.Type.Equivalent(p.Second.Type));

            public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType;

            public override (IType ReturnType, bool IsConstant) Invocation(IExpression[] args, SourceRange source, DiagnosticsReport diagnostics)
            {
                // TODO: this was copied from FuncType.Invocation, refactor?
                var parameters = Parameters;
                if (args.Length != parameters.Count)
                {
                    diagnostics.AddError($"Expected {parameters.Count} arguments, found {args.Length}", source);
                }

                var n = Math.Min(args.Length, parameters.Count);
                for (int i = 0; i < n; i++)
                {
                    var param = parameters[i];
                    var arg = args[i];
                    if (param.IsReference)
                    {
                        if (!arg.IsLValue)
                        {
                            diagnostics.AddError($"Argument {i + 1}: cannot bind parameter '{TypePrinter.ToString(param.Type, param.Name, param.IsReference)}' to non-lvalue", arg.Location);
                        }
                        else if (!param.Type.CanBindRefTo(arg.Type!))
                        {
                            diagnostics.AddError($"Argument {i + 1}: cannot bind parameter '{TypePrinter.ToString(param.Type, param.Name, param.IsReference)}' to reference of type '{arg.Type}'", arg.Location);
                        }
                    }
                    else
                    {
                        if ((param.Type is TextLabelType && !param.Type.Equivalent(arg.Type!)) ||
                            !param.Type.CanAssign(arg.Type!, arg.IsLValue))
                        {
                            diagnostics.AddError($"Argument {i + 1}: cannot pass '{arg.Type}' as parameter '{TypePrinter.ToString(param.Type, param.Name, param.IsReference)}'", arg.Location);
                        }
                    }
                }

                if (CanBeConstantIfAllArgsAreConstant)
                {
                    return (ReturnType, IsConstant: args.All(a => a.IsConstant));
                }
                else
                {
                    return (ReturnType, false);
                }
            }

            public abstract override void CGInvocation(CodeGenerator cg, InvocationExpression expr);

            public virtual int EvalInt(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public virtual float EvalFloat(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public virtual bool EvalBool(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public virtual string EvalString(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public virtual (float X, float Y, float Z) EvalVector(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();

            public override string ToString()
            {
                var argsStr = $"({string.Join(", ", Parameters.Select(p => TypePrinter.ToString(p.Type, p.Name, p.IsReference)))})";
                var returnStr = ReturnType is VoidType ?
                                    "" :
                                    $" {TypePrinter.ToString(ReturnType, string.Empty, false)}";
                return $"INTRINSIC{returnStr} {Name}{argsStr}";
            }
        }

        private sealed class F2VIntrinsicType : BasicIntrinsicType
        {
            public F2VIntrinsicType() : base("F2V", BuiltInTypes.Vector, (BuiltInTypes.Float, "value", false))
                => CanBeConstantIfAllArgsAreConstant = true;

            public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
            {
                cg.EmitValue(expr.Arguments[0]);
                cg.Emit(Opcode.F2V);
            }

            public override (float X, float Y, float Z) EvalVector(InvocationExpression expr, GlobalSymbolTable symbols)
            {
                var value = ExpressionEvaluator.EvalFloat(expr.Arguments[0], symbols);
                return (value, value, value);
            }
        }

        private sealed class F2IIntrinsicType : BasicIntrinsicType
        {
            public F2IIntrinsicType() : base("F2I", BuiltInTypes.Int, (BuiltInTypes.Float, "value", false))
                => CanBeConstantIfAllArgsAreConstant = true;

            public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
            {
                cg.EmitValue(expr.Arguments[0]);
                cg.Emit(Opcode.F2I);
            }

            public override int EvalInt(InvocationExpression expr, GlobalSymbolTable symbols)
            {
                return (int)ExpressionEvaluator.EvalFloat(expr.Arguments[0], symbols);
            }
        }

        private sealed class I2FIntrinsicType : BasicIntrinsicType
        {
            public I2FIntrinsicType() : base("I2F", BuiltInTypes.Float, (BuiltInTypes.Int, "value", false))
                => CanBeConstantIfAllArgsAreConstant = true;

            public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
            {
                cg.EmitValue(expr.Arguments[0]);
                cg.Emit(Opcode.I2F);
            }

            public override float EvalFloat(InvocationExpression expr, GlobalSymbolTable symbols)
            {
                return ExpressionEvaluator.EvalInt(expr.Arguments[0], symbols);
            }
        }

        private sealed class AppendIntrinsicType : BasicIntrinsicType
        {
            public AppendIntrinsicType() : base("APPEND", (GenericTextLabel, "tl", true), (StringOrInt, "value", false)) { }

            public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
            {
                var args = expr.Arguments;
                Debug.Assert(args[0].Type is TextLabelType && args[0].IsLValue);
                Debug.Assert((args[1].Type is StringType or IntType) ||
                             (args[1].Type is TextLabelType && args[1].IsLValue));

                var destTextLabel = args[0];
                var destTextLabelTy = (TextLabelType)destTextLabel.Type!;
                var argToAppend = args[1];

                if (argToAppend.Type is TextLabelType)
                {
                    // convert to string by taking the address of the TEXT_LABEL
                    cg.EmitAddress(argToAppend);
                }
                else
                {
                    cg.EmitValue(argToAppend);
                }
                cg.EmitAddress(destTextLabel);
                cg.Emit(argToAppend.Type is IntType ? Opcode.TEXT_LABEL_APPEND_INT :
                                                      Opcode.TEXT_LABEL_APPEND_STRING,
                        destTextLabelTy.Length);
            }
        }



        /// <summary>
        /// Used for parameters that can be STRING or INT.
        /// </summary>
        private sealed class StringOrIntDecl : BaseTypeDeclaration
        {
            public StringOrIntDecl() : base(SourceRange.Unknown, "STRING|INT") { }
            public override StringOrIntType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class StringOrIntType : BaseType
        {
            public override int SizeOf => 0;

            public StringType String { get; }
            public IntType Int { get; }

            public StringOrIntType(SourceRange source) : base(source)
                => (String, Int) = (new(source), new(source));

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
            public override bool Equivalent(IType other) => other is StringOrIntType;

            public override bool CanAssign(IType rhs, bool rhsIsLValue)
                => String.CanAssign(rhs, rhsIsLValue) || Int.CanAssign(rhs, rhsIsLValue);

            public override string ToString() => "STRING|INT";
        }

        /// <summary>
        /// Used for parameters that can be a reference to any TEXT_LABEL_*.
        /// </summary>
        private sealed class GenericTextLabelDecl : BaseTypeDeclaration
        {
            public GenericTextLabelDecl() : base(SourceRange.Unknown, "TEXT_LABEL_*") { }
            public override GenericTextLabelType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class GenericTextLabelType : BaseType
        {
            public override int SizeOf => 0;

            public GenericTextLabelType(SourceRange source) : base(source) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
            public override bool Equivalent(IType other) => other is GenericTextLabelType;

            public override bool CanBindRefTo(IType other)
                => other is TextLabelType;

            public override string ToString() => "TEXT_LABEL_*";
        }
    }
}
