namespace ScTools.ScriptLang.Ast.Types
{
    public interface IType : INode
    {
        int SizeOf { get; }

        public bool CanAssign(IType rhs);

        // Semantic Checks

        /// <summary>
        /// Checks if the type <paramref name="rhs"/> can be assigned to this type.
        /// </summary>
        public void Assign(IType rhs, SourceRange source, DiagnosticsReport diagnostics);
    }

    public abstract class BaseType: BaseNode, IType
    {
        public abstract int SizeOf { get; }

        public BaseType(SourceRange source) : base(source) {}

        public virtual bool CanAssign(IType rhs) => false;

        public virtual void Assign(IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (CanAssign(rhs))
            {
                return;
            }

            diagnostics.AddError($"{rhs} cannot be assigned to {this}", source);
        }
    }
}
