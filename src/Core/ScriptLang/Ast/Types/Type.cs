namespace ScTools.ScriptLang.Ast.Types
{
    public interface IType : INode
    {
        int SizeOf { get; }
    }

    public abstract class BaseType: BaseNode, IType
    {
        public abstract int SizeOf { get; }

        public BaseType(SourceRange source) : base(source) {}
    }
}
