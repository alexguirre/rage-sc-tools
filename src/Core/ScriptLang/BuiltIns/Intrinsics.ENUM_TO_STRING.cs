namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public static partial class Intrinsics
{
    /// <summary>
    /// Gets the string representation of an ENUM value.
    /// <br/>
    /// Signature: ENUM_TO_STRING (ENUM) -> STRING
    /// </summary>
    private sealed class IntrinsicENUM_TO_STRING : BaseIntrinsic
    {
        public new const string Name = "ENUM_TO_STRING";
        private const string EnumNotFound = "ENUM_NOT_FOUND";

        private readonly Dictionary<EnumDeclaration, FunctionDeclaration> enumToStringFunctionCache = new();

        public IntrinsicENUM_TO_STRING() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            UsagePrecondition(node);

            var argTypes = node.Arguments.Select(a => a.Accept(exprTypeChecker, semantics)).ToArray();

            // type-check arguments
            var args = node.Arguments;
            ExpressionTypeChecker.CheckArgumentCount(parameterCount: 1, node, semantics);

            // check that the argument is an ENUM value
            if (argTypes.Length > 0 && argTypes[0] is not EnumType)
            {
                ExpressionTypeChecker.ArgNotAnEnumError(semantics, 0, args[0], argTypes[0]);
            }

            return new(StringType.Instance, ValueKind.RValue | (args[0].ValueKind & ValueKind.Constant), ArgumentKind.None);
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var enumDecl = ((EnumType)node.Arguments[0].Type!).Declaration;
            var v = ConstantExpressionEvaluator.Eval(node.Arguments[0], semantics);
            var member = enumDecl.Members.FirstOrDefault(m => m.Semantics.ConstantValue!.IntValue == v.IntValue);
            return ConstantValue.String(member?.Name ?? EnumNotFound);
        }

        public override void CodeGen(InvocationExpression node, CodeEmitter c)
        {
            c.EmitValue(node.Arguments[0]);
            c.EmitCall(GetEnumToStringFunction((EnumType)node.Arguments[0].Type!));
        }

        private FunctionDeclaration GetEnumToStringFunction(EnumType enumType)
        {
            if (!enumToStringFunctionCache.TryGetValue(enumType.Declaration, out var func))
            {
                func = BuildEnumToStringFunction(enumType);
                enumToStringFunctionCache.Add(enumType.Declaration, func);
            }
            return func;
        }

        private static FunctionDeclaration BuildEnumToStringFunction(EnumType enumType)
        {
            var enumName = enumType.Declaration.Name;
            var param = new VarDeclaration(new(Token.Identifier(enumName)), new VarDeclarator(Token.Identifier("v")), VarKind.Parameter)
            {
                Semantics = new(ValueType: enumType, ConstantValue: null)
            };

            var body = new IStatement[]
            {
                MakeSwitch(enumType.Declaration, param),
                MakeReturn(EnumNotFound)
            };

            // TODO: add some special char valid in assembly but not in script source to prevent name collisions
            return new(TokenKind.FUNC.Create(), Token.Identifier($"__{enumName}_ENUM_TO_STRING"),
                       TokenKind.OpenParen.Create(), TokenKind.CloseParen.Create(), TokenKind.ENDFUNC.Create(),
                       new(Token.Identifier("STRING")), new[] { param }, body)
            {
                Semantics = new(ValueType: new FunctionType(StringType.Instance, ImmutableArray.Create(new ParameterInfo(enumType, IsReference: false))),
                                ConstantValue: null)
            };
            
            static SwitchStatement MakeSwitch(EnumDeclaration enumDeclaration, VarDeclaration param)
                => new(TokenKind.SWITCH.Create(), TokenKind.ENDSWITCH.Create(),
                       MakeName(param), enumDeclaration.Members.OrderBy(m => m.Semantics.ConstantValue!.IntValue).Select(MakeSwitchCase), label: null)
                {
                    Semantics = new(ExitLabel: $"__exit"),
                };
            static ValueSwitchCase MakeSwitchCase(EnumMemberDeclaration enumMember)
                => new(TokenKind.CASE.Create(),
                       MakeInt(enumMember.Semantics.ConstantValue!.IntValue), new[] { MakeReturn(enumMember.Name) })
                {
                    Semantics = new(Label: $"__case{unchecked((uint)enumMember.Semantics.ConstantValue!.IntValue):X}",
                                    Value: enumMember.Semantics.ConstantValue!.IntValue)
                };
            static ReturnStatement MakeReturn(string value)
                => new(TokenKind.RETURN.Create(), MakeString(value), label: null);
            static NameExpression MakeName(IValueDeclaration declaration)
                => new(Token.Identifier(declaration.Name)) { Semantics = new(declaration.Semantics.ValueType, ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable, ArgumentKind.None, declaration) };
            static StringLiteralExpression MakeString(string value)
                => new(Token.String(value)) { Semantics = new(StringType.Instance, ValueKind.RValue | ValueKind.Constant, ArgumentKind.None) };
            static IntLiteralExpression MakeInt(int value)
                => new(Token.Integer(value)) { Semantics = new(IntType.Instance, ValueKind.RValue | ValueKind.Constant, ArgumentKind.None) };
        }
    }
}
