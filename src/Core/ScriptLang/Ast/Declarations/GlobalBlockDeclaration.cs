namespace ScTools.ScriptLang.Ast.Declarations
{
    using System;
    using System.Collections.Generic;

    public sealed class GlobalBlockDeclaration : BaseNode, IDeclaration
    {
        public const int MaxBlockCount = 64; // limit hardcoded in the game .exe (and max value that fits in GLOBAL_U24* instructions)
        public const int MaxSize = 0x3FFFF;

        private int blockIndex;

        public string Name { get; set; }
        public int BlockIndex
        {
            get => blockIndex;
            set => blockIndex = value is >= 0 and < MaxBlockCount ? value : throw new ArgumentOutOfRangeException(nameof(value), $"Value is negative or greater than or equal to {MaxBlockCount}");
        }
        public IList<VarDeclaration> Vars { get; set; } = new List<VarDeclaration>();

        public GlobalBlockDeclaration(SourceRange source, string name, int blockIndex) : base(source)
            => (Name, BlockIndex) = (name, blockIndex);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}
