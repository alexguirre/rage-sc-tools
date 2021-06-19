namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    /// <summary>
    /// Represents an array type of constant size.
    /// </summary>
    public sealed class TextLabelType : BaseType
    {
        public const int MinLength = 8;
        public const int MaxLength = 248;

        private int length;
        public int Length
        {
            get => length;
            set => length = IsValidLength(value) ? value : throw new ArgumentException("Invalid length", nameof(value)); 
        }

        public override int SizeOf => Length / 8;

        public TextLabelType(SourceRange source, int length) : base(source)
            => Length = length;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is TextLabelType otherLbl && Length == otherLbl.Length;

        /// <returns><c>true</c> if <paramref name="length"/> is in the range [<see cref="MinLength"/>, <see cref="MaxLength"/>] and is a multiple of 8; otherwise, <c>false</c>.</returns>
        public static bool IsValidLength(int length)
            => length is >= MinLength and <= MaxLength && (length % 8) == 0;
    }
}
