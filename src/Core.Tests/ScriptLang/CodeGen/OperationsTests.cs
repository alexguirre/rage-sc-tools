namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class OperationsTests : CodeGenTestsBase
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
        ");
    }

    [Fact]
    public void BoolEquality()
    {
        CompileScript(
        scriptSource: @"
            BOOL b1 = TRUE, b2 = FALSE
            BOOL b
            b = b1 == b2
            b = b1 <> b2
        ",
        expectedAssembly: @"
        #define b1 2
        #define b2 3
        #define b 4
            ENTER 0, 5
            ; b1 = TRUE
            PUSH_CONST_1
            LOCAL_U8_STORE b1
            ; b2 = FALSE
            PUSH_CONST_0
            LOCAL_U8_STORE b2

            ; b = b1 == b2
            LOCAL_U8_LOAD b1
            LOCAL_U8_LOAD b2
            IEQ
            LOCAL_U8_STORE b

            ; b = b1 <> b2
            LOCAL_U8_LOAD b1
            LOCAL_U8_LOAD b2
            INE
            LOCAL_U8_STORE b

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntOperations()
    {
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
        expectedAssembly: @"
        #define n1 2
        #define n2 3
        #define n3 4
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_1
            LOCAL_U8_STORE n1
            ; n1 = 2
            PUSH_CONST_2
            LOCAL_U8_STORE n2

            ; n3 = n1 + n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IADD
            LOCAL_U8_STORE n3

            ; n3 = n1 - n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            ISUB
            LOCAL_U8_STORE n3

            ; n3 = n1 * n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IMUL
            LOCAL_U8_STORE n3

            ; n3 = n1 / n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IDIV
            LOCAL_U8_STORE n3

            ; n3 = n1 & n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IAND
            LOCAL_U8_STORE n3

            ; n3 = n1 ^ n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IXOR
            LOCAL_U8_STORE n3

            ; n3 = n1 | n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IOR
            LOCAL_U8_STORE n3

            ; n3 = -n1
            LOCAL_U8_LOAD n1
            INEG
            LOCAL_U8_STORE n3

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntCompoundAssignments()
    {
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
        expectedAssembly: @"
        #define n1 2
        #define n2 3
            ENTER 0, 4
            ; n1 = 1
            PUSH_CONST_1
            LOCAL_U8_STORE n1
            ; n1 = 2
            PUSH_CONST_2
            LOCAL_U8_STORE n2

            ; n1 += n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IADD
            LOCAL_U8_STORE n1

            ; n1 -= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            ISUB
            LOCAL_U8_STORE n1

            ; n1 *= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IMUL
            LOCAL_U8_STORE n1

            ; n1 /= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IDIV
            LOCAL_U8_STORE n1

            ; n1 &= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IAND
            LOCAL_U8_STORE n1

            ; n1 ^= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IXOR
            LOCAL_U8_STORE n1

            ; n1 |= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IOR
            LOCAL_U8_STORE n1

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntEqualityAndComparison()
    {
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
        expectedAssembly: @"
        #define n1 2
        #define n2 3
        #define b 4
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_1
            LOCAL_U8_STORE n1
            ; n1 = 2
            PUSH_CONST_2
            LOCAL_U8_STORE n2

            ; b = n1 == n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IEQ
            LOCAL_U8_STORE b

            ; b = n1 <> n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            INE
            LOCAL_U8_STORE b

            ; b = n1 > n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IGT
            LOCAL_U8_STORE b

            ; b = n1 >= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            IGE
            LOCAL_U8_STORE b

            ; b = n1 < n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            ILT
            LOCAL_U8_STORE b

            ; b = n1 <= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            ILE
            LOCAL_U8_STORE b

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void FloatOperations()
    {
        CompileScript(
        scriptSource: @"
            FLOAT n1 = 1.0, n2 = 2.0, n3
            n3 = n1 + n2
            n3 = n1 - n2
            n3 = n1 * n2
            n3 = n1 / n2
            n3 = -n1
        ",
        expectedAssembly: @"
        #define n1 2
        #define n2 3
        #define n3 4
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_F1
            LOCAL_U8_STORE n1
            ; n1 = 2
            PUSH_CONST_F2
            LOCAL_U8_STORE n2

            ; n3 = n1 + n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FADD
            LOCAL_U8_STORE n3

            ; n3 = n1 - n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FSUB
            LOCAL_U8_STORE n3

            ; n3 = n1 * n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FMUL
            LOCAL_U8_STORE n3

            ; n3 = n1 / n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FDIV
            LOCAL_U8_STORE n3

            ; n3 = -n1
            LOCAL_U8_LOAD n1
            FNEG
            LOCAL_U8_STORE n3

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void FloatCompoundAssignments()
    {
        CompileScript(
        scriptSource: @"
            FLOAT n1 = 1.0, n2 = 2.0
            n1 += n2
            n1 -= n2
            n1 *= n2
            n1 /= n2
        ",
        expectedAssembly: @"
        #define n1 2
        #define n2 3
            ENTER 0, 4
            ; n1 = 1
            PUSH_CONST_F1
            LOCAL_U8_STORE n1
            ; n1 = 2
            PUSH_CONST_F2
            LOCAL_U8_STORE n2

            ; n1 += n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FADD
            LOCAL_U8_STORE n1

            ; n1 -= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FSUB
            LOCAL_U8_STORE n1

            ; n1 *= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FMUL
            LOCAL_U8_STORE n1

            ; n1 /= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FDIV
            LOCAL_U8_STORE n1

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void FloatEqualityAndComparison()
    {
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
        expectedAssembly: @"
        #define n1 2
        #define n2 3
        #define b 4
            ENTER 0, 5
            ; n1 = 1
            PUSH_CONST_F1
            LOCAL_U8_STORE n1
            ; n1 = 2
            PUSH_CONST_F2
            LOCAL_U8_STORE n2

            ; b = n1 == n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FEQ
            LOCAL_U8_STORE b

            ; b = n1 <> n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FNE
            LOCAL_U8_STORE b

            ; b = n1 > n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FGT
            LOCAL_U8_STORE b

            ; b = n1 >= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FGE
            LOCAL_U8_STORE b

            ; b = n1 < n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FLT
            LOCAL_U8_STORE b

            ; b = n1 <= n2
            LOCAL_U8_LOAD n1
            LOCAL_U8_LOAD n2
            FLE
            LOCAL_U8_STORE b

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void VectorOperations()
    {
        CompileScript(
        scriptSource: @"
            VECTOR v1 = <<1.0, 2.0, 3.0>>, v2 = <<4.0, 5.0, 6.0>>, v3
            v3 = v1 + v2
            v3 = v1 - v2
            v3 = v1 * v2
            v3 = v1 / v2
            v3 = -v1
        ",
        expectedAssembly: @"
        #define v1 2
        #define v2 5
        #define v3 8
            ENTER 0, 11
            ; v1 = <<1.0, 2.0, 3.0>>
            PUSH_CONST_F1
            PUSH_CONST_F2
            PUSH_CONST_F3
            PUSH_CONST_3
            LOCAL_U8 v1
            STORE_N

            ; v2 = <<4.0, 5.0, 6.0>>
            PUSH_CONST_F4
            PUSH_CONST_F5
            PUSH_CONST_F6
            PUSH_CONST_3
            LOCAL_U8 v2
            STORE_N

            ; v3 = v1 + v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VADD
            PUSH_CONST_3
            LOCAL_U8 v3
            STORE_N

            ; v3 = v1 - v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VSUB
            PUSH_CONST_3
            LOCAL_U8 v3
            STORE_N

            ; v3 = v1 * v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VMUL
            PUSH_CONST_3
            LOCAL_U8 v3
            STORE_N

            ; v3 = v1 / v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VDIV
            PUSH_CONST_3
            LOCAL_U8 v3
            STORE_N

            ; v3 = -v1
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            VNEG
            PUSH_CONST_3
            LOCAL_U8 v3
            STORE_N

            LEAVE 0, 0
        ");
    }

    [Fact]
    public void VectorCompoundAssignments()
    {
        CompileScript(
        scriptSource: @"
            VECTOR v1 = <<1.0, 2.0, 3.0>>, v2 = <<4.0, 5.0, 6.0>>
            v1 += v2
            v1 -= v2
            v1 *= v2
            v1 /= v2
        ",
        expectedAssembly: @"
        #define v1 2
        #define v2 5
            ENTER 0, 8
            ; v1 = <<1.0, 2.0, 3.0>>
            PUSH_CONST_F1
            PUSH_CONST_F2
            PUSH_CONST_F3
            PUSH_CONST_3
            LOCAL_U8 v1
            STORE_N

            ; v2 = <<4.0, 5.0, 6.0>>
            PUSH_CONST_F4
            PUSH_CONST_F5
            PUSH_CONST_F6
            PUSH_CONST_3
            LOCAL_U8 v2
            STORE_N

            ; v1 += v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VADD
            PUSH_CONST_3
            LOCAL_U8 v1
            STORE_N

            ; v1 -= v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VSUB
            PUSH_CONST_3
            LOCAL_U8 v1
            STORE_N

            ; v1 *= v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VMUL
            PUSH_CONST_3
            LOCAL_U8 v1
            STORE_N

            ; v1 /= v2
            PUSH_CONST_3
            LOCAL_U8 v1
            LOAD_N
            PUSH_CONST_3
            LOCAL_U8 v2
            LOAD_N
            VDIV
            PUSH_CONST_3
            LOCAL_U8 v1
            STORE_N

            LEAVE 0, 0
        ");
    }

    [Theory]
    [InlineData("==", "IEQ")]
    [InlineData("<>", "INE")]
    public void HandleTypesSupportEqualityOperators(string equalityOperator, string opcode)
    {
        CompileScript(
        scriptSource: $@"
            ENTITY_INDEX entity
            PED_INDEX ped
            VEHICLE_INDEX vehicle
            OBJECT_INDEX object

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
        expectedAssembly: $@"
            ENTER 0, 7

            ; b = entity op NULL
            LOCAL_U8_LOAD 2
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE 6

            ; b = entity op entity
            LOCAL_U8_LOAD 2
            LOCAL_U8_LOAD 2
            {opcode}
            LOCAL_U8_STORE 6

            ; b = ped op entity
            LOCAL_U8_LOAD 3
            LOCAL_U8_LOAD 2
            {opcode}
            LOCAL_U8_STORE 6

            ; b = ped op NULL
            LOCAL_U8_LOAD 3
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE 6

            ; b = ped op ped
            LOCAL_U8_LOAD 3
            LOCAL_U8_LOAD 3
            {opcode}
            LOCAL_U8_STORE 6

            ; b = vehicle op entity
            LOCAL_U8_LOAD 4
            LOCAL_U8_LOAD 2
            {opcode}
            LOCAL_U8_STORE 6

            ; b = vehicle op NULL
            LOCAL_U8_LOAD 4
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE 6

            ; b = vehicle op vehicle
            LOCAL_U8_LOAD 4
            LOCAL_U8_LOAD 4
            {opcode}
            LOCAL_U8_STORE 6

            ; b = object op entity
            LOCAL_U8_LOAD 5
            LOCAL_U8_LOAD 2
            {opcode}
            LOCAL_U8_STORE 6

            ; b = object op NULL
            LOCAL_U8_LOAD 5
            PUSH_CONST_0
            {opcode}
            LOCAL_U8_STORE 6

            ; b = object op object
            LOCAL_U8_LOAD 5
            LOCAL_U8_LOAD 5
            {opcode}
            LOCAL_U8_STORE 6

            LEAVE 0, 0
        ");
    }
}
