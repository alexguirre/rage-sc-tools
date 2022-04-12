namespace ScTools.ScriptLang.CodeGen
{
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.BuiltIns;
    using ScTools.ScriptLang.Semantics;

    /// <summary>
    /// Emits code to execute statements.
    /// </summary>
    public sealed class StatementEmitter : EmptyVisitor<Void, FuncDeclaration>
    {
        public CodeGenerator CG { get; }

        public StatementEmitter(CodeGenerator cg) => CG = cg;

        public override Void Visit(VarDeclaration node, FuncDeclaration func)
        {
            if (node.Initializer is null && TypeHelper.IsDefaultInitialized(node.Type))
            {
                EmitDefaultInit(node.Address, node.Type);
            }
            else if (node.Initializer is not null)
            {
                var dest = new DeclarationRefExpression(Token.Identifier(node.Name, node.Location)) { Declaration = node, Type = node.Type!, IsLValue = true, IsConstant = false };
                new AssignmentStatement(node.Location, lhs: dest, rhs: node.Initializer)
                    .Accept(this, func);
            }

            return default;
        }

        /// <summary>
        /// Emits code to default initialize a local variable.
        /// </summary>
        private void EmitDefaultInit(int localAddress, IType type)
        {
            // push local address
            switch (localAddress)
            {
                case >= byte.MinValue and <= byte.MaxValue:
                    CG.Emit(Opcode.LOCAL_U8, localAddress);
                    break;

                case >= ushort.MinValue and <= ushort.MaxValue:
                    CG.Emit(Opcode.LOCAL_U16, localAddress);
                    break;

                default: Debug.Assert(false, "Local var address too big"); break;
            }

            EmitDefaultInitNoPushAddress(type);

            CG.Emit(Opcode.DROP); // drop local address
        }

        private void EmitDefaultInitNoPushAddress(IType type)
        {
            switch (type)
            {
                case StructType ty: EmitDefaultInitStruct(ty); break;
                case ArrayType ty: EmitDefaultInitArray(ty); break;
                default: throw new System.NotImplementedException();
            }
        }

        private void EmitDefaultInitStruct(StructType structTy)
        {
            foreach (var field in structTy.Declaration.Fields)
            {
                var hasInitializer = field.Initializer is not null;
                if (hasInitializer || TypeHelper.IsDefaultInitialized(field.Type))
                {
                    CG.Emit(Opcode.DUP); // duplicate struct address
                    if (field.Offset != 0)
                    {
                        CG.EmitOffset(field.Offset); // advance to field offset
                    }

                    // initialize field
                    if (hasInitializer)
                    {
                        switch (field.Type)
                        {
                            case IntType:
                                CG.EmitPushConstInt(ExpressionEvaluator.EvalInt(field.Initializer!, CG.Symbols));
                                CG.Emit(Opcode.STORE_REV);
                                break;
                            case FloatType:
                                // TODO: game scripts use PUSH_CONST_U32 to default initialize FLOAT fields, should we change it?
                                CG.EmitPushConstFloat(ExpressionEvaluator.EvalFloat(field.Initializer!, CG.Symbols));
                                CG.Emit(Opcode.STORE_REV);
                                break;
                            case BoolType:
                                CG.EmitPushConstInt(ExpressionEvaluator.EvalBool(field.Initializer!, CG.Symbols) ? 1 : 0);
                                CG.Emit(Opcode.STORE_REV); 
                                break;
                            // TODO: should VECTOR or STRING fields be allowed to be default initialized? it doesn't seem to happen in the game scripts
                            //case StructType sTy when BuiltInTypes.IsVectorType(sTy):
                            //    DefaultInitVector(field.Initializer!);
                            //    break;
                            default: throw new System.NotImplementedException();
                        }
                    }
                    else
                    {
                        Debug.Assert(TypeHelper.IsDefaultInitialized(field.Type));
                        EmitDefaultInitNoPushAddress(field.Type);
                    }

                    CG.Emit(Opcode.DROP); // drop duplicated address
                }
            }
        }

        private void EmitDefaultInitArray(ArrayType arrayTy)
        {
            // write array size
            CG.EmitPushConstInt(arrayTy.Rank);
            CG.Emit(Opcode.STORE_REV);

            if (TypeHelper.IsDefaultInitialized(arrayTy.ItemType))
            {
                CG.Emit(Opcode.DUP); // duplicate array address
                CG.EmitOffset(1); // advance duplicated address to the first item (skip array size)
                var itemSize = arrayTy.ItemType.SizeOf;
                for (int i = 0; i < arrayTy.Rank; i++)
                {
                    EmitDefaultInitNoPushAddress(arrayTy.ItemType); // initialize item
                    CG.EmitOffset(itemSize); // advance to the next item
                }
                CG.Emit(Opcode.DROP); // drop duplicated address
            }
        }

        public override Void Visit(AssignmentStatement node, FuncDeclaration func)
        {
            node.LHS.Type!.CGAssign(CG, node);
            return default;
        }

        public override Void Visit(BreakStatement node, FuncDeclaration func)
        {
            CG.EmitJump(node.EnclosingStatement!.ExitLabel!);
            return default;
        }

        public override Void Visit(ContinueStatement node, FuncDeclaration func)
        {
            CG.EmitJump(node.EnclosingLoop!.ContinueLabel!);
            return default;
        }

        public override Void Visit(GotoStatement node, FuncDeclaration func)
        {
            CG.EmitJump(node.Target!.Label!);
            return default;
        }

        public override Void Visit(IfStatement node, FuncDeclaration func)
        {
            // check condition
            CG.EmitValue(node.Condition);
            CG.EmitJumpIfZero(node.ElseLabel!);

            // then body
            node.Then.ForEach(stmt => stmt.Accept(this, func));
            if (node.Else.Any())
            {
                // jump over the else body
                CG.EmitJump(node.EndLabel!);
            }

            // else body
            CG.EmitLabel(node.ElseLabel!);
            node.Else.ForEach(stmt => stmt.Accept(this, func));

            CG.EmitLabel(node.EndLabel!);

            return default;
        }

        public override Void Visit(RepeatStatement node, FuncDeclaration func)
        {
            var intTy = BuiltInTypes.Int.CreateType(node.Location);
            var constantZero = new IntLiteralExpression(Token.Integer(0, node.Location)) { Type = intTy, IsConstant = true, IsLValue = false };
            var constantOne = new IntLiteralExpression(Token.Integer(1, node.Location)) { Type = intTy, IsConstant = true, IsLValue = false };

            // set counter to 0
            new AssignmentStatement(node.Location, lhs: node.Counter, rhs: constantZero)
                .Accept(this, func);

            CG.EmitLabel(node.BeginLabel!);

            // check counter < limit
            CG.EmitValue(node.Counter);
            CG.EmitValue(node.Limit);
            CG.Emit(Opcode.ILT_JZ, node.ExitLabel!);

            // body
            node.Body.ForEach(stmt => stmt.Accept(this, func));

            CG.EmitLabel(node.ContinueLabel!);

            // increment counter
            var counterPlusOne = new BinaryExpression(Token.Plus(node.Location), node.Counter, constantOne) { Type = intTy, IsConstant = false, IsLValue = false };
            new AssignmentStatement(node.Location, lhs: node.Counter, rhs: counterPlusOne)
                .Accept(this, func);

            // jump back to condition check
            CG.EmitJump(node.BeginLabel!);

            CG.EmitLabel(node.ExitLabel!);

            return default;
        }

        public override Void Visit(ReturnStatement node, FuncDeclaration func)
        {
            if (node.Expression is not null)
            {
                CG.EmitValue(node.Expression);
            }
            CG.Emit(Opcode.LEAVE, func.ParametersSize, func.Prototype.ReturnType.SizeOf);
            return default;
        }

        public override Void Visit(SwitchStatement node, FuncDeclaration func)
        {
            CG.EmitValue(node.Expression);

            CG.EmitSwitch(node.Cases.OfType<ValueSwitchCase>());

            var defaultCase = node.Cases.OfType<DefaultSwitchCase>().SingleOrDefault();
            CG.EmitJump(defaultCase?.Label ?? node.ExitLabel!);

            node.Cases.ForEach(c => c.Accept(this, func));
            CG.EmitLabel(node.ExitLabel!);
            return default;
        }

        public override Void Visit(ValueSwitchCase node, FuncDeclaration func)
        {
            CG.EmitLabel(node.Label!);
            node.Body.ForEach(stmt => stmt.Accept(this, func));
            return default;
        }

        public override Void Visit(DefaultSwitchCase node, FuncDeclaration func)
        {
            CG.EmitLabel(node.Label!);
            node.Body.ForEach(stmt => stmt.Accept(this, func));
            return default;
        }

        public override Void Visit(WhileStatement node, FuncDeclaration func)
        {
            CG.EmitLabel(node.BeginLabel!);

            // check condition
            CG.EmitValue(node.Condition);
            CG.EmitJumpIfZero(node.ExitLabel!);

            // body
            node.Body.ForEach(stmt => stmt.Accept(this, func));

            // jump back to condition check
            CG.EmitJump(node.BeginLabel!);

            CG.EmitLabel(node.ExitLabel!);

            return default;
        }

        public override Void Visit(InvocationExpression node, FuncDeclaration func)
        {
            CG.EmitValue(node);
            var returnValueSize = node.Type!.SizeOf;
            for (int i = 0; i < returnValueSize; i++)
            {
                CG.Emit(Opcode.DROP);
            }
            return default;
        }
    }
}
