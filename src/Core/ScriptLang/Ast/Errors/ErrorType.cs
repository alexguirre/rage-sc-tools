namespace ScTools.ScriptLang.Ast.Errors
{
    using System;

    using ScTools.ScriptLang.Ast.Types;

    public sealed class ErrorType : BaseType, IError
    {
        public override int SizeOf => throw new NotSupportedException($"Cannot get size of {nameof(ErrorType)}");

        public ErrorType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
