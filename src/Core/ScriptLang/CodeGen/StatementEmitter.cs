namespace ScTools.ScriptLang.CodeGen;

using System;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Types;

/// <summary>
/// Emits code to execute statements.
/// </summary>
internal sealed class StatementEmitter : Visitor
{
    private readonly CodeEmitter _C;

    public StatementEmitter(CodeEmitter codeEmitter) => _C = codeEmitter;

    private void EmitAssignment(IExpression destination, IExpression source)
    {
        // TODO: handle special cases in assignment
        _C.EmitValue(source);
        _C.EmitStoreAt(destination);
    }
    public void EmitAssignment(VarDeclaration destination, IExpression source)
    {
        // TODO: handle special cases in assignment
        _C.EmitValue(source);
        _C.EmitStoreAt(destination);
    }

    public override void Visit(VarDeclaration node)
    {
        Debug.Assert(node.Kind is VarKind.Local);

        _C.AllocateFrameSpaceForLocal(node);

        if (node.Initializer is null && node.Semantics.ValueType!.IsDefaultInitialized())
        {
            _C.EmitDefaultInit(node);
        }
        else if (node.Initializer is not null)
        {
            EmitAssignment(node, node.Initializer);
        }
    }

    public override void Visit(AssignmentStatement node)
    {
        throw new NotImplementedException(nameof(AssignmentStatement));
        // TODO: AssignmentStatement consider compound assignments, no longer lowered to lhs = lhs binOp rhs
        //node.LHS.Type!.CGAssign(CG, node);
    }

    public override void Visit(BreakStatement node)
    {
        _C.EmitJump(node.Semantics.EnclosingStatement!.Semantics.ExitLabel!);
    }

    public override void Visit(ContinueStatement node)
    {
        _C.EmitJump(((ISemanticNode<LoopStatementSemantics>)node.Semantics.EnclosingLoop!).Semantics.ContinueLabel!);
    }

    public override void Visit(GotoStatement node)
    {
        _C.EmitJump(node.Semantics.Target!.Label!.Name);
    }

    public override void Visit(IfStatement node)
    {
        var sem = node.Semantics;
        // check condition
        _C.EmitValue(node.Condition);
        _C.EmitJumpIfZero(sem.ElseLabel!);

        // then body
        _C.EmitStatementBlock(node.Then);
        if (node.Else.Any())
        {
            // jump over the else body
            _C.EmitJump(sem.EndLabel!);
        }

        // else body
        _C.Label(sem.ElseLabel!);
        _C.EmitStatementBlock(node.Else);

        _C.Label(sem.EndLabel!);
    }

    public override void Visit(WhileStatement node)
    {
        var sem = node.Semantics;
        _C.Label(sem.BeginLabel!);

        // check condition
        _C.EmitValue(node.Condition);
        _C.EmitJumpIfZero(sem.ExitLabel!);

        // body
        _C.EmitStatementBlock(node.Body);

        // jump back to condition check
        _C.EmitJump(sem.BeginLabel!);

        _C.Label(sem.ExitLabel!);
    }

    public override void Visit(RepeatStatement node)
    {
        throw new NotImplementedException(nameof(RepeatStatement));
        //var intTy = BuiltInTypes.Int.CreateType(node.Location);
        //var constantZero = new IntLiteralExpression(Token.Integer(0, node.Location)) { Semantics = new(intTy, IsConstant: true, IsLValue: false) };
        //var constantOne = new IntLiteralExpression(Token.Integer(1, node.Location)) { Semantics = new(intTy, IsConstant: true, IsLValue: false) };

        //// set counter to 0
        //new AssignmentStatement(TokenKind.Equals.Create(node.Location), lhs: node.Counter, rhs: constantZero, label: null)
        //    .Accept(this, func);

        //var sem = node.Semantics;
        //CG.EmitLabel(sem.BeginLabel!);

        //// check counter < limit
        //CG.EmitValue(node.Counter);
        //CG.EmitValue(node.Limit);
        //CG.Emit(Opcode.ILT_JZ, sem.ExitLabel!);

        //// body
        //node.Body.ForEach(stmt => stmt.Accept(this));

        //CG.EmitLabel(sem.ContinueLabel!);

        //// increment counter
        //var counterPlusOne = new BinaryExpression(TokenKind.Plus.Create(node.Location), node.Counter, constantOne) { Semantics = new(intTy, IsConstant: false, IsLValue: false) };
        //new AssignmentStatement(TokenKind.Equals.Create(node.Location), lhs: node.Counter, rhs: counterPlusOne, label: null)
        //    .Accept(this, func);

        //// jump back to condition check
        //CG.EmitJump(sem.BeginLabel!);

        //CG.EmitLabel(sem.ExitLabel!);
    }

    public override void Visit(ReturnStatement node)
    {
        if (node.Expression is not null)
        {
            _C.EmitValue(node.Expression);
        }
        _C.EmitEpilogue();
    }

    public override void Visit(SwitchStatement node)
    {
        _C.EmitValue(node.Expression);

        _C.EmitSwitch(node.Cases.OfType<ValueSwitchCase>());

        var defaultCase = node.Cases.OfType<DefaultSwitchCase>().SingleOrDefault();
        _C.EmitJump(defaultCase?.Semantics.Label ?? node.Semantics.ExitLabel!);
        
        foreach (var @case in node.Cases)
        {
            _C.Label(@case.Semantics.Label!);
            _C.EmitStatementBlock(@case.Body);
        }

        _C.Label(node.Semantics.ExitLabel!);
    }

    public override void Visit(InvocationExpression node)
    {
        _C.EmitValueAndDrop(node);
    }
}
