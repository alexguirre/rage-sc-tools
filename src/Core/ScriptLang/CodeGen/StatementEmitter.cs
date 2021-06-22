﻿namespace ScTools.ScriptLang.CodeGen
{
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;

    public sealed class StatementEmitter : EmptyVisitor<Void, FuncDeclaration>
    {
        public CodeGenerator CG { get; }

        public StatementEmitter(CodeGenerator cg) => CG = cg;

        public override Void Visit(LabelDeclaration node, FuncDeclaration func)
        {
            // TODO: support local labels in assembler to prevent conflicts in assembly generated by the compiler
            CG.Sink.WriteLine(" {0}:", node.Name);
            return default;
        }

        public override Void Visit(VarDeclaration node, FuncDeclaration func) => default;
        public override Void Visit(AssignmentStatement node, FuncDeclaration func) => default;

        public override Void Visit(BreakStatement node, FuncDeclaration func)
        {
            CG.Sink.WriteLine("\tJ {0}", node.EnclosingStatement!.ExitLabel);
            return default;
        }

        public override Void Visit(ContinueStatement node, FuncDeclaration func)
        {
            CG.Sink.WriteLine("\tJ {0}", node.EnclosingLoop!.BeginLabel);
            return default;
        }

        public override Void Visit(GotoStatement node, FuncDeclaration func)
        {
            CG.Sink.WriteLine("\tJ {0}", node.Label!.Name);
            return default;
        }

        public override Void Visit(IfStatement node, FuncDeclaration func) => default;
        public override Void Visit(RepeatStatement node, FuncDeclaration func) => default;

        public override Void Visit(ReturnStatement node, FuncDeclaration func)
        {
            if (node.Expression is not null)
            {
                // TODO: push value of expression
            }
            CG.Sink.WriteLine("\tLEAVE {0}, {1}", func.Prototype.ParametersSize, func.Prototype.ReturnType.SizeOf);
            return default;
        }

        public override Void Visit(SwitchStatement node, FuncDeclaration func) => default;
        public override Void Visit(ValueSwitchCase node, FuncDeclaration func) => default;
        public override Void Visit(DefaultSwitchCase node, FuncDeclaration func) => default;
        public override Void Visit(WhileStatement node, FuncDeclaration func) => default;
        public override Void Visit(InvocationExpression node, FuncDeclaration func) => default;
    }
}
