namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class ExpressionsTests : CodeGenTestsBase
{
    [Fact]
    public void BoolUnaryNOT()
    {
        CompileScript(
        scriptSource: @"
            BOOL b = NOT TRUE
        ",
        expectedAssembly: @"
            ENTER 0, 3
            PUSH_CONST_1
            INOT
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 3
            PUSH_CONST_1
            INOT
            {IntToLocalIV(2)}
            STORE
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntUnaryNOT()
    {
        CompileScript(
        scriptSource: @"
            BOOL b = NOT 1
        ",
        expectedAssembly: @"
            ENTER 0, 3
            PUSH_CONST_1
            INOT
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 3
            PUSH_CONST_1
            INOT
            {IntToLocalIV(2)}
            STORE
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void ShortCircuitAND()
    {
        CompileScript(
        scriptSource: @"
            BOOL b = TRUE AND FALSE
        ",
        expectedAssembly: @"
            ENTER 0, 3
            PUSH_CONST_1
            DUP
            JZ assign
            PUSH_CONST_0
            IAND
        assign:
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 3
            PUSH_CONST_1
            DUP
            JZ assign
            PUSH_CONST_0
            IAND
        assign:
            {IntToLocalIV(2)}
            STORE
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void ShortCircuitOR()
    {
        CompileScript(
        scriptSource: @"
            BOOL b = TRUE OR FALSE
        ",
        expectedAssembly: @"
            ENTER 0, 3
            PUSH_CONST_1
            DUP
            INOT
            JZ assign
            PUSH_CONST_0
            IOR
        assign:
            LOCAL_U8_STORE 2
            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 3
            PUSH_CONST_1
            DUP
            INOT
            JZ assign
            PUSH_CONST_0
            IOR
        assign:
            {IntToLocalIV(2)}
            STORE
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void LongShortCircuitAND()
    {
        CompileScript(
        scriptSource: @"
            INT n = 1
            IF n == 0 AND n == 3 AND n == 5
                n = 2
            ENDIF
        ",
        expectedAssembly: @"
            ENTER 0, 3
            PUSH_CONST_1
            LOCAL_U8_STORE 2
            LOCAL_U8_LOAD 2
            PUSH_CONST_0
            IEQ
            DUP
            JZ and
            LOCAL_U8_LOAD 2
            PUSH_CONST_3
            IEQ
            IAND
        and:
            DUP
            JZ if
            LOCAL_U8_LOAD 2
            PUSH_CONST_5
            IEQ
            IAND
        if:
            JZ endif
            PUSH_CONST_2
            LOCAL_U8_STORE 2
        endif:
            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 3
            PUSH_CONST_1
            {IntToLocalIV(2)}
            STORE
            {IntToLocalIV(2)}
            LOAD
            PUSH_CONST_0
            IEQ
            DUP
            JZ and
            {IntToLocalIV(2)}
            LOAD
            PUSH_CONST_3
            IEQ
            IAND
        and:
            DUP
            JZ if
            {IntToLocalIV(2)}
            LOAD
            PUSH_CONST_5
            IEQ
            IAND
        if:
            JZ endif
            PUSH_CONST_2
            {IntToLocalIV(2)}
            STORE
        endif:
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void LongShortCircuitOR()
    {
        CompileScript(
        scriptSource: @"
            INT n = 1
            IF n == 0 OR n == 3 OR n == 5
                n = 2
            ENDIF
        ",
        expectedAssembly: @"
            ENTER 0, 3
            PUSH_CONST_1
            LOCAL_U8_STORE 2
            LOCAL_U8_LOAD 2
            PUSH_CONST_0
            IEQ
            DUP
            INOT
            JZ or
            LOCAL_U8_LOAD 2
            PUSH_CONST_3
            IEQ
            IOR
        or:
            DUP
            INOT
            JZ if
            LOCAL_U8_LOAD 2
            PUSH_CONST_5
            IEQ
            IOR
        if:
            JZ endif
            PUSH_CONST_2
            LOCAL_U8_STORE 2
        endif:
            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 3
            PUSH_CONST_1
            {IntToLocalIV(2)}
            STORE
            {IntToLocalIV(2)}
            LOAD
            PUSH_CONST_0
            IEQ
            DUP
            INOT
            JZ or
            {IntToLocalIV(2)}
            LOAD
            PUSH_CONST_3
            IEQ
            IOR
        or:
            DUP
            INOT
            JZ if
            {IntToLocalIV(2)}
            LOAD
            PUSH_CONST_5
            IEQ
            IOR
        if:
            JZ endif
            PUSH_CONST_2
            {IntToLocalIV(2)}
            STORE
        endif:
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void BoolEquality()
    {
        const int b1 = 2, b2 = 3, b = 4; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            BOOL b1 = TRUE, b2 = FALSE
            BOOL b
            b = b1 == b2
            b = b1 <> b2
        ",
        expectedAssembly: $@"
            ENTER 0, 5
            ; b1 = TRUE
            PUSH_CONST_1
            LOCAL_U8_STORE {b1}
            ; b2 = FALSE
            PUSH_CONST_0
            LOCAL_U8_STORE {b2}

            ; b = b1 == b2
            LOCAL_U8_LOAD {b1}
            LOCAL_U8_LOAD {b2}
            IEQ
            LOCAL_U8_STORE {b}

            ; b = b1 <> b2
            LOCAL_U8_LOAD {b1}
            LOCAL_U8_LOAD {b2}
            INE
            LOCAL_U8_STORE {b}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 5
            ; b1 = TRUE
            PUSH_CONST_1
            {IntToLocalIV(b1)}
            STORE
            ; b2 = FALSE
            PUSH_CONST_0
            {IntToLocalIV(b2)}
            STORE

            ; b = b1 == b2
            {IntToLocalIV(b1)}
            LOAD
            {IntToLocalIV(b2)}
            LOAD
            IEQ
            {IntToLocalIV(b)}
            STORE

            ; b = b1 <> b2
            {IntToLocalIV(b1)}
            LOAD
            {IntToLocalIV(b2)}
            LOAD
            INE
            {IntToLocalIV(b)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntOperations()
    {
        const int n1 = 2, n2 = 3, n3 = 4; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            INT n1 = 1, n2 = 2, n3
            n3 = n1 + n2
            n3 = n1 - n2
            n3 = n1 * n2
            n3 = n1 / n2
            n3 = n1 & n2
            n3 = n1 ^ n2
            n3 = n1 | n2
            n3 = -n1
        ",
        expectedAssembly: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_1
            LOCAL_U8_STORE {n1}
            ; n1 = 2
            PUSH_CONST_2
            LOCAL_U8_STORE {n2}

            ; n3 = n1 + n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IADD
            LOCAL_U8_STORE {n3}

            ; n3 = n1 - n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            ISUB
            LOCAL_U8_STORE {n3}

            ; n3 = n1 * n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IMUL
            LOCAL_U8_STORE {n3}

            ; n3 = n1 / n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IDIV
            LOCAL_U8_STORE {n3}

            ; n3 = n1 & n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IAND
            LOCAL_U8_STORE {n3}

            ; n3 = n1 ^ n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IXOR
            LOCAL_U8_STORE {n3}

            ; n3 = n1 | n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IOR
            LOCAL_U8_STORE {n3}

            ; n3 = -n1
            LOCAL_U8_LOAD {n1}
            INEG
            LOCAL_U8_STORE {n3}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_1
            {IntToLocalIV(n1)}
            STORE
            ; n1 = 2
            PUSH_CONST_2
            {IntToLocalIV(n2)}
            STORE

            ; n3 = n1 + n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IADD
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 - n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            ISUB
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 * n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IMUL
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 / n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IDIV
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 & n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IAND
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 ^ n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IXOR
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 | n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IOR
            {IntToLocalIV(n3)}
            STORE

            ; n3 = -n1
            {IntToLocalIV(n1)}
            LOAD
            INEG
            {IntToLocalIV(n3)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntCompoundAssignments()
    {
        const int n1 = 2, n2 = 3; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            INT n1 = 1, n2 = 2
            n1 += n2
            n1 -= n2
            n1 *= n2
            n1 /= n2
            n1 &= n2
            n1 ^= n2
            n1 |= n2
        ",
        expectedAssembly: $@"
            ENTER 0, 4
            ; n1 = 1
            PUSH_CONST_1
            LOCAL_U8_STORE {n1}
            ; n1 = 2
            PUSH_CONST_2
            LOCAL_U8_STORE {n2}

            ; n1 += n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IADD
            LOCAL_U8_STORE {n1}

            ; n1 -= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            ISUB
            LOCAL_U8_STORE {n1}

            ; n1 *= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IMUL
            LOCAL_U8_STORE {n1}

            ; n1 /= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IDIV
            LOCAL_U8_STORE {n1}

            ; n1 &= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IAND
            LOCAL_U8_STORE {n1}

            ; n1 ^= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IXOR
            LOCAL_U8_STORE {n1}

            ; n1 |= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IOR
            LOCAL_U8_STORE {n1}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 4
            ; n1 = 1
            PUSH_CONST_1
            {IntToLocalIV(n1)}
            STORE
            ; n1 = 2
            PUSH_CONST_2
            {IntToLocalIV(n2)}
            STORE

            ; n1 += n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IADD
            {IntToLocalIV(n1)}
            STORE

            ; n1 -= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            ISUB
            {IntToLocalIV(n1)}
            STORE

            ; n1 *= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IMUL
            {IntToLocalIV(n1)}
            STORE

            ; n1 /= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IDIV
            {IntToLocalIV(n1)}
            STORE

            ; n1 &= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IAND
            {IntToLocalIV(n1)}
            STORE

            ; n1 ^= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IXOR
            {IntToLocalIV(n1)}
            STORE

            ; n1 |= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IOR
            {IntToLocalIV(n1)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntEqualityAndComparison()
    {
        const int n1 = 2, n2 = 3, b = 4; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            INT n1 = 1, n2 = 2
            BOOL b
            b = n1 == n2
            b = n1 <> n2
            b = n1 > n2
            b = n1 >= n2
            b = n1 < n2
            b = n1 <= n2
        ",
        expectedAssembly: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_1
            LOCAL_U8_STORE {n1}
            ; n1 = 2
            PUSH_CONST_2
            LOCAL_U8_STORE {n2}

            ; b = n1 == n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IEQ
            LOCAL_U8_STORE {b}

            ; b = n1 <> n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            INE
            LOCAL_U8_STORE {b}

            ; b = n1 > n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IGT
            LOCAL_U8_STORE {b}

            ; b = n1 >= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IGE
            LOCAL_U8_STORE {b}

            ; b = n1 < n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            ILT
            LOCAL_U8_STORE {b}

            ; b = n1 <= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            ILE
            LOCAL_U8_STORE {b}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_1
            {IntToLocalIV(n1)}
            STORE
            ; n1 = 2
            PUSH_CONST_2
            {IntToLocalIV(n2)}
            STORE

            ; b = n1 == n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IEQ
            {IntToLocalIV(b)}
            STORE

            ; b = n1 <> n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            INE
            {IntToLocalIV(b)}
            STORE

            ; b = n1 > n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IGT
            {IntToLocalIV(b)}
            STORE

            ; b = n1 >= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IGE
            {IntToLocalIV(b)}
            STORE

            ; b = n1 < n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            ILT
            {IntToLocalIV(b)}
            STORE

            ; b = n1 <= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            ILE
            {IntToLocalIV(b)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void FloatOperations()
    {
        const int n1 = 2, n2 = 3, n3 = 4; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            FLOAT n1 = 1.0, n2 = 2.0, n3
            n3 = n1 + n2
            n3 = n1 - n2
            n3 = n1 * n2
            n3 = n1 / n2
            n3 = -n1
        ",
        expectedAssembly: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_F1
            LOCAL_U8_STORE {n1}
            ; n1 = 2
            PUSH_CONST_F2
            LOCAL_U8_STORE {n2}

            ; n3 = n1 + n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FADD
            LOCAL_U8_STORE {n3}

            ; n3 = n1 - n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FSUB
            LOCAL_U8_STORE {n3}

            ; n3 = n1 * n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FMUL
            LOCAL_U8_STORE {n3}

            ; n3 = n1 / n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FDIV
            LOCAL_U8_STORE {n3}

            ; n3 = -n1
            LOCAL_U8_LOAD {n1}
            FNEG
            LOCAL_U8_STORE {n3}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_F 1.0
            {IntToLocalIV(n1)}
            STORE
            ; n1 = 2
            PUSH_CONST_F 2.0
            {IntToLocalIV(n2)}
            STORE

            ; n3 = n1 + n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FADD
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 - n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FSUB
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 * n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FMUL
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 / n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FDIV
            {IntToLocalIV(n3)}
            STORE

            ; n3 = -n1
            {IntToLocalIV(n1)}
            LOAD
            FNEG
            {IntToLocalIV(n3)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void FloatCompoundAssignments()
    {
        const int n1 = 2, n2 = 3; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            FLOAT n1 = 1.0, n2 = 2.0
            n1 += n2
            n1 -= n2
            n1 *= n2
            n1 /= n2
        ",
        expectedAssembly: $@"
            ENTER 0, 4
            ; n1 = 1
            PUSH_CONST_F1
            LOCAL_U8_STORE {n1}
            ; n2 = 2
            PUSH_CONST_F2
            LOCAL_U8_STORE {n2}

            ; n1 += n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FADD
            LOCAL_U8_STORE {n1}

            ; n1 -= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FSUB
            LOCAL_U8_STORE {n1}

            ; n1 *= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FMUL
            LOCAL_U8_STORE {n1}

            ; n1 /= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FDIV
            LOCAL_U8_STORE {n1}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 4
            ; n1 = 1
            PUSH_CONST_F 1.0
            {IntToLocalIV(n1)}
            STORE
            ; n2 = 2
            PUSH_CONST_F 2.0
            {IntToLocalIV(n2)}
            STORE

            ; n1 += n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FADD
            {IntToLocalIV(n1)}
            STORE

            ; n1 -= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FSUB
            {IntToLocalIV(n1)}
            STORE

            ; n1 *= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FMUL
            {IntToLocalIV(n1)}
            STORE

            ; n1 /= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FDIV
            {IntToLocalIV(n1)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void FloatEqualityAndComparison()
    {
        const int n1 = 2, n2 = 3, b = 4; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            FLOAT n1 = 1.0, n2 = 2.0
            BOOL b
            b = n1 == n2
            b = n1 <> n2
            b = n1 > n2
            b = n1 >= n2
            b = n1 < n2
            b = n1 <= n2
        ",
        expectedAssembly: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_F1
            LOCAL_U8_STORE {n1}
            ; n1 = 2
            PUSH_CONST_F2
            LOCAL_U8_STORE {n2}

            ; b = n1 == n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FEQ
            LOCAL_U8_STORE {b}

            ; b = n1 <> n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FNE
            LOCAL_U8_STORE {b}

            ; b = n1 > n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FGT
            LOCAL_U8_STORE {b}

            ; b = n1 >= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FGE
            LOCAL_U8_STORE {b}

            ; b = n1 < n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FLT
            LOCAL_U8_STORE {b}

            ; b = n1 <= n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            FLE
            LOCAL_U8_STORE {b}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_F 1.0
            {IntToLocalIV(n1)}
            STORE
            ; n2 = 2
            PUSH_CONST_F 2.0
            {IntToLocalIV(n2)}
            STORE

            ; b = n1 == n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FEQ
            {IntToLocalIV(b)}
            STORE

            ; b = n1 <> n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FNE
            {IntToLocalIV(b)}
            STORE

            ; b = n1 > n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FGT
            {IntToLocalIV(b)}
            STORE

            ; b = n1 >= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FGE
            {IntToLocalIV(b)}
            STORE

            ; b = n1 < n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FLT
            {IntToLocalIV(b)}
            STORE

            ; b = n1 <= n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            FLE
            {IntToLocalIV(b)}
            STORE

            LEAVE 0, 0
        ");
    }



    [Fact]
    public void EnumOperations()
    {
        const int n1 = 2, n2 = 3, n3 = 4; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            MY_ENUM n1 = A, n2 = B, n3
            n3 = n1 + n2
            n3 = n1 & n2
            n3 = n1 ^ n2
            n3 = n1 | n2
        ",
        declarationsSource: @"
            ENUM MY_ENUM
                A,
                B
            ENDENUM
        ",
        expectedAssembly: $@"
            ENTER 0, 5
            ; n1 = A
            PUSH_CONST_0
            LOCAL_U8_STORE {n1}
            ; n1 = B
            PUSH_CONST_1
            LOCAL_U8_STORE {n2}

            ; n3 = n1 + n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IADD
            LOCAL_U8_STORE {n3}

            ; n3 = n1 & n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IAND
            LOCAL_U8_STORE {n3}

            ; n3 = n1 ^ n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IXOR
            LOCAL_U8_STORE {n3}

            ; n3 = n1 | n2
            LOCAL_U8_LOAD {n1}
            LOCAL_U8_LOAD {n2}
            IOR
            LOCAL_U8_STORE {n3}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 5
            ; n1 = 0
            PUSH_CONST_0
            {IntToLocalIV(n1)}
            STORE
            ; n1 = 1
            PUSH_CONST_1
            {IntToLocalIV(n2)}
            STORE

            ; n3 = n1 + n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IADD
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 & n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IAND
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 ^ n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IXOR
            {IntToLocalIV(n3)}
            STORE

            ; n3 = n1 | n2
            {IntToLocalIV(n1)}
            LOAD
            {IntToLocalIV(n2)}
            LOAD
            IOR
            {IntToLocalIV(n3)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void VectorOperations()
    {
        const int v1 = 2, v2 = 5, v3 = 8; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            VECTOR v1 = <<1.0, 2.0, 3.0>>, v2 = <<4.0, 5.0, 6.0>>, v3
            v3 = v1 + v2
            v3 = v1 - v2
            v3 = v1 * v2
            v3 = v1 / v2
            v3 = -v1
        ",
        expectedAssembly: $@"
            ENTER 0, 11
            ; v1 = <<1.0, 2.0, 3.0>>
            PUSH_CONST_F1
            PUSH_CONST_F2
            PUSH_CONST_F3
            PUSH_CONST_3
            LOCAL_U8 {v1}
            STORE_N

            ; v2 = <<4.0, 5.0, 6.0>>
            PUSH_CONST_F4
            PUSH_CONST_F5
            PUSH_CONST_F6
            PUSH_CONST_3
            LOCAL_U8 {v2}
            STORE_N

            ; v3 = v1 + v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VADD
            PUSH_CONST_3
            LOCAL_U8 {v3}
            STORE_N

            ; v3 = v1 - v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VSUB
            PUSH_CONST_3
            LOCAL_U8 {v3}
            STORE_N

            ; v3 = v1 * v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VMUL
            PUSH_CONST_3
            LOCAL_U8 {v3}
            STORE_N

            ; v3 = v1 / v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VDIV
            PUSH_CONST_3
            LOCAL_U8 {v3}
            STORE_N

            ; v3 = -v1
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            VNEG
            PUSH_CONST_3
            LOCAL_U8 {v3}
            STORE_N

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 11
            ; v1 = <<1.0, 2.0, 3.0>>
            PUSH_CONST_F 1.0
            PUSH_CONST_F 2.0
            PUSH_CONST_F 3.0
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            STORE_N

            ; v2 = <<4.0, 5.0, 6.0>>
            PUSH_CONST_F 4.0
            PUSH_CONST_F 5.0
            PUSH_CONST_F 6.0
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            STORE_N

            ; v3 = v1 + v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VADD
            PUSH_CONST_3
            {IntToLocalIV(v3)}
            STORE_N

            ; v3 = v1 - v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VSUB
            PUSH_CONST_3
            {IntToLocalIV(v3)}
            STORE_N

            ; v3 = v1 * v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VMUL
            PUSH_CONST_3
            {IntToLocalIV(v3)}
            STORE_N

            ; v3 = v1 / v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VDIV
            PUSH_CONST_3
            {IntToLocalIV(v3)}
            STORE_N

            ; v3 = -v1
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            VNEG
            PUSH_CONST_3
            {IntToLocalIV(v3)}
            STORE_N

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void VectorCompoundAssignments()
    {
        const int v1 = 2, v2 = 5; // var stack positions in assembly
        CompileScript(
        scriptSource: @"
            VECTOR v1 = <<1.0, 2.0, 3.0>>, v2 = <<4.0, 5.0, 6.0>>
            v1 += v2
            v1 -= v2
            v1 *= v2
            v1 /= v2
        ",
        expectedAssembly: $@"
            ENTER 0, 8
            ; v1 = <<1.0, 2.0, 3.0>>
            PUSH_CONST_F1
            PUSH_CONST_F2
            PUSH_CONST_F3
            PUSH_CONST_3
            LOCAL_U8 {v1}
            STORE_N

            ; v2 = <<4.0, 5.0, 6.0>>
            PUSH_CONST_F4
            PUSH_CONST_F5
            PUSH_CONST_F6
            PUSH_CONST_3
            LOCAL_U8 {v2}
            STORE_N

            ; v1 += v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VADD
            PUSH_CONST_3
            LOCAL_U8 {v1}
            STORE_N

            ; v1 -= v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VSUB
            PUSH_CONST_3
            LOCAL_U8 {v1}
            STORE_N

            ; v1 *= v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VMUL
            PUSH_CONST_3
            LOCAL_U8 {v1}
            STORE_N

            ; v1 /= v2
            PUSH_CONST_3
            LOCAL_U8 {v1}
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 {v2}
            LOAD_N
            VDIV
            PUSH_CONST_3
            LOCAL_U8 {v1}
            STORE_N

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 8
            ; v1 = <<1.0, 2.0, 3.0>>
            PUSH_CONST_F 1.0
            PUSH_CONST_F 2.0
            PUSH_CONST_F 3.0
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            STORE_N

            ; v2 = <<4.0, 5.0, 6.0>>
            PUSH_CONST_F 4.0
            PUSH_CONST_F 5.0
            PUSH_CONST_F 6.0
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            STORE_N

            ; v1 += v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VADD
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            STORE_N

            ; v1 -= v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VSUB
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            STORE_N

            ; v1 *= v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VMUL
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            STORE_N

            ; v1 /= v2
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            LOAD_N
            PUSH_CONST_3
            {IntToLocalIV(v2)}
            LOAD_N
            VDIV
            PUSH_CONST_3
            {IntToLocalIV(v1)}
            STORE_N

            LEAVE 0, 0
        ");
    }

    [Theory]
    [InlineData("==", "IEQ")]
    [InlineData("<>", "INE")]
    public void NativeTypesSupportEqualityOperators(string equalityOperator, string opcode)
    {
        const int entity = 2, ped = 3, vehicle = 4, obj = 5, b = 6; // var stack positions in assembly
        CompileScript(
        scriptSource: $@"
            MY_ENTITY_INDEX entity
            MY_PED_INDEX ped
            MY_VEHICLE_INDEX vehicle
            MY_OBJECT_INDEX object

            BOOL b
            b = entity {equalityOperator} NULL
            b = entity {equalityOperator} entity
            b = ped {equalityOperator} entity
            b = ped {equalityOperator} NULL
            b = ped {equalityOperator} ped
            b = vehicle {equalityOperator} entity
            b = vehicle {equalityOperator} NULL
            b = vehicle {equalityOperator} vehicle
            b = object {equalityOperator} entity
            b = object {equalityOperator} NULL
            b = object {equalityOperator} object
        ",
        declarationsSource: $@"
            NATIVE MY_ENTITY_INDEX
            NATIVE MY_PED_INDEX : MY_ENTITY_INDEX
            NATIVE MY_VEHICLE_INDEX : MY_ENTITY_INDEX
            NATIVE MY_OBJECT_INDEX : MY_ENTITY_INDEX
        ",
        expectedAssembly: $@"
            ENTER 0, 7

            ; b = entity op NULL
            LOCAL_U8_LOAD {entity}
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = entity op entity
            LOCAL_U8_LOAD {entity}
            LOCAL_U8_LOAD {entity}
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = ped op entity
            LOCAL_U8_LOAD {ped}
            LOCAL_U8_LOAD {entity}
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = ped op NULL
            LOCAL_U8_LOAD {ped}
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = ped op ped
            LOCAL_U8_LOAD {ped}
            LOCAL_U8_LOAD {ped}
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = vehicle op entity
            LOCAL_U8_LOAD {vehicle}
            LOCAL_U8_LOAD {entity}
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = vehicle op NULL
            LOCAL_U8_LOAD {vehicle}
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = vehicle op vehicle
            LOCAL_U8_LOAD {vehicle}
            LOCAL_U8_LOAD {vehicle}
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = object op entity
            LOCAL_U8_LOAD {obj}
            LOCAL_U8_LOAD {entity}
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = object op NULL
            LOCAL_U8_LOAD {obj}
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE {b}

            ; b = object op object
            LOCAL_U8_LOAD {obj}
            LOCAL_U8_LOAD {obj}
            {opcode}
            LOCAL_U8_STORE {b}

            LEAVE 0, 0
        ",
        expectedAssemblyIV: $@"
            ENTER 0, 7

            ; b = entity op NULL
            {IntToLocalIV(entity)}
            LOAD
            NULL
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = entity op entity
            {IntToLocalIV(entity)}
            LOAD
            {IntToLocalIV(entity)}
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = ped op entity
            {IntToLocalIV(ped)}
            LOAD
            {IntToLocalIV(entity)}
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = ped op NULL
            {IntToLocalIV(ped)}
            LOAD
            NULL
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = ped op ped
            {IntToLocalIV(ped)}
            LOAD
            {IntToLocalIV(ped)}
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = vehicle op entity
            {IntToLocalIV(vehicle)}
            LOAD
            {IntToLocalIV(entity)}
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = vehicle op NULL
            {IntToLocalIV(vehicle)}
            LOAD
            NULL
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = vehicle op vehicle
            {IntToLocalIV(vehicle)}
            LOAD
            {IntToLocalIV(vehicle)}
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = object op entity
            {IntToLocalIV(obj)}
            LOAD
            {IntToLocalIV(entity)}
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = object op NULL
            {IntToLocalIV(obj)}
            LOAD
            NULL
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            ; b = object op object
            {IntToLocalIV(obj)}
            LOAD
            {IntToLocalIV(obj)}
            LOAD
            {opcode}
            {IntToLocalIV(b)}
            STORE

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void PostfixIncrement()
    {
        CompileScript(
            scriptSource: @$"
                INT n
                INT m = n++
            ",
            expectedAssembly: @$"
                ENTER 0, 4

                LOCAL_U8_LOAD 2
                DUP
                IADD_U8 1
                LOCAL_U8_STORE 2
                LOCAL_U8_STORE 3

                LEAVE 0, 0
            ",
            expectedAssemblyIV: @$"
                ENTER 0, 4

                {IntToLocalIV(2)}
                LOAD
                DUP
                PUSH_CONST_1
                IADD
                {IntToLocalIV(2)}
                STORE
                {IntToLocalIV(3)}
                STORE

                LEAVE 0, 0
            ");
    }

    [Fact]
    public void PostfixIncrementAsStatement()
    {
        CompileScript(
            scriptSource: @$"
                INT n
                n++
            ",
            expectedAssembly: @$"
                ENTER 0, 3

                LOCAL_U8_LOAD 2
                IADD_U8 1
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ",
            expectedAssemblyIV: @$"
                ENTER 0, 3

                {IntToLocalIV(2)}
                LOAD
                PUSH_CONST_1
                IADD
                {IntToLocalIV(2)}
                STORE

                LEAVE 0, 0
            ");
    }
    [Fact]
    public void PostfixDecrement()
    {
        CompileScript(
            scriptSource: @$"
                INT n
                INT m = n--
            ",
            expectedAssembly: @$"
                ENTER 0, 4

                LOCAL_U8_LOAD 2
                DUP
                PUSH_CONST_1
                ISUB
                LOCAL_U8_STORE 2
                LOCAL_U8_STORE 3

                LEAVE 0, 0
            ",
            expectedAssemblyIV: @$"
                ENTER 0, 4

                {IntToLocalIV(2)}
                LOAD
                DUP
                PUSH_CONST_1
                ISUB
                {IntToLocalIV(2)}
                STORE
                {IntToLocalIV(3)}
                STORE

                LEAVE 0, 0
            ");
    }

    [Fact]
    public void PostfixDecrementAsStatement()
    {
        CompileScript(
            scriptSource: @$"
                INT n
                n--
            ",
            expectedAssembly: @$"
                ENTER 0, 3

                LOCAL_U8_LOAD 2
                PUSH_CONST_1
                ISUB
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ",
            expectedAssemblyIV: @$"
                ENTER 0, 3

                {IntToLocalIV(2)}
                LOAD
                PUSH_CONST_1
                ISUB
                {IntToLocalIV(2)}
                STORE

                LEAVE 0, 0
            ");
    }
}
