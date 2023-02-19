namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using ScTools.ScriptLang;
    //using ScTools.ScriptLang.Semantics;

    using Xunit;

    public class TypeCheckerTests
    {
        [Fact]
        public void TestAssignIntToTextLabel()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl = 1234
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignStringToTextLabel()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl = 'hello'
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignTextLabelToTextLabelOfGreaterSize()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_15 tl1 = 'hello'
                    TEXT_LABEL_31 tl2 = tl1
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignTextLabelToTextLabelOfSmallerSize()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl1 = 'hello'
                    TEXT_LABEL_15 tl2 = tl1
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignTextLabelToTextLabelOfSameSize()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_15 tl1 = 'hello'
                    TEXT_LABEL_15 tl2 = tl1
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestCannotPassIntToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEST(1234)
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotPassStringToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEST('hello')
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotPassTextLabelOfDifferentSizeToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_15 tl
                    TEST(tl)
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestPassTextLabelOfSameSizeToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl
                    TEST(tl)
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestCannotAssignEnumToInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    INT n = MY_ENUM_A
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A
                ENDENUM
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotAssignIntToEnum()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    MY_ENUM n = 0
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A
                ENDENUM
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotCompareEnumAndInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    BOOL b = MY_ENUM_A >= 0
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A
                ENDENUM
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotOperateEnumAndInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    INT n = MY_ENUM_A + 1
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A
                ENDENUM
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestEnumToInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    INT n = ENUM_TO_INT(MY_ENUM_A)
                    BOOL b = ENUM_TO_INT(MY_ENUM_A) >= 0
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A
                ENDENUM
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestIntToEnum()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    MY_ENUM n = INT_TO_ENUM(MY_ENUM, 0)
                    BOOL b = n >= INT_TO_ENUM(MY_ENUM, 0)
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A
                ENDENUM
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestCannotSetArraySizeToEnumValue()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                ENDSCRIPT

                INT iNumbers[NUM_MY_ENUM]

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestArraySizeWithEnumToInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                ENDSCRIPT

                INT iNumbers[ENUM_TO_INT(NUM_MY_ENUM)]

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestArraySizeWithEnumToIntAndIntToEnum()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                ENDSCRIPT

                INT iNumbers[ENUM_TO_INT(INT_TO_ENUM(MY_ENUM, ENUM_TO_INT(NUM_MY_ENUM)))]

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestIntConstantWithEnumToInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM

                CONST_INT NUM_MY_ENUM_INT ENUM_TO_INT(NUM_MY_ENUM)
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestOperationsWithTypeNames()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    INT n = 1 + MY_STRUCT
                ENDSCRIPT

                STRUCT MY_STRUCT
                    FLOAT a
                ENDSTRUCT
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCountOfWithArray()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    INT array[10]
                    INT i = COUNT_OF(array)
                    TEST1(array)
                    TEST2(array)
                ENDSCRIPT

                PROC TEST1(INT array[])
                    INT i = COUNT_OF(array)
                ENDPROC
                PROC TEST2(INT array[10])
                    INT i = COUNT_OF(array)
                ENDPROC
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestCountOfWithEnumType()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    INT i = COUNT_OF(MY_ENUM)
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, MY_ENUM_C
                ENDENUM
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestCannotUseCountOfWithStructType()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    INT i = COUNT_OF(MY_STRUCT)
                ENDSCRIPT

                STRUCT MY_STRUCT
                    INT a, b, c
                ENDSTRUCT
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotAssignIntrinsicToFunctionPointer()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    MY_FUNC_T fp = F2I
                ENDSCRIPT

                PROTO FUNC INT MY_FUNC_T(FLOAT value)
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestSwitchCaseWithInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    SWITCH 5
                        CASE 1
                            BREAK
                        CASE 2
                            BREAK
                        CASE 3
                            BREAK
                        DEFAULT
                            BREAK
                    ENDSWITCH
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestSwitchCaseWithIntAndHashes()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    SWITCH 5
                        CASE 1
                            BREAK
                        CASE 2
                            BREAK
                        CASE HASH('hello')
                            BREAK
                        CASE HASH('world')
                            BREAK
                        DEFAULT
                            BREAK
                    ENDSWITCH
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestSwitchCaseWithEnum()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    SWITCH MY_ENUM_B
                        CASE MY_ENUM_A
                            BREAK
                        CASE MY_ENUM_B
                            BREAK
                        CASE INT_TO_ENUM(MY_ENUM, ENUM_TO_INT(MY_ENUM_B) + 1)
                            BREAK
                        CASE INT_TO_ENUM(MY_ENUM, -1)
                            BREAK
                        DEFAULT
                            BREAK
                    ENDSWITCH
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B
                ENDENUM
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestSwitchCaseCannotMixEnumWithInt()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    SWITCH 5
                        CASE 1
                            BREAK
                        CASE 2
                            BREAK
                        CASE MY_ENUM_A
                            BREAK
                    ENDSWITCH
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B
                ENDENUM
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestSwitchCaseCannotMixIntWithEnum()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    SWITCH MY_ENUM_A
                        CASE MY_ENUM_A
                            BREAK
                        CASE MY_ENUM_B
                            BREAK
                        CASE 1
                            BREAK
                    ENDSWITCH
                ENDSCRIPT

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B
                ENDENUM
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        private static DiagnosticsReport TypeCheck(string source)
        {
            throw new System.NotImplementedException();
            using var sourceReader = new StringReader(source);
            var d = new DiagnosticsReport();
            //var p = new Parser(sourceReader, "test.sc");
            //p.Parse(d);

            //var ast = new ScTools.ScriptLang.Ast.Program(SourceRange.Unknown);
            //var globalSymbols = GlobalSymbolTableBuilder.Build(ast, d);
            //IdentificationVisitor.Visit(ast, d, globalSymbols, NativeDB.Empty);
            //TypeChecker.Check(ast, d, globalSymbols);

            return d;
        }
    }
}
