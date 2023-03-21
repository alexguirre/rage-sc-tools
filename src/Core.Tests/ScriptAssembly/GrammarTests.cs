namespace ScTools.Tests.ScriptAssembly
{
    using ScTools.GameFiles;

    using Xunit;

    public class GrammarTests
    {
        [Fact]
        public void TestEverything()
        {
            using var asm = Util.Assemble(@"
            .script_name 'my_script'
            .globals_signature 0x1234ABCD

            .global_block 1
            .global     ; segment for global variables, the integer is the block ID
gInt:       .int 10
gVector:    .float 1.0, 2.0, 3.0


            .static     ; segment for static variables
myInt:      .int 5
myInt2:     .int 0xF

myIntArray: .int 5, 1, 2, 3, 4, 5

myFloatArray:
            ; MY_FLOAT_ARRAY_SIZE = 10
            .int 10
            .float 10 dup (1)

myVector:   .float 1.0, 2.0, 3.0

myOtherVector:
            .float 3 dup (4.0)

randomData: .int 2 dup (22, 7 dup (33))
emptySpace: .int 16 dup (0)


            .arg        ; segment for script args
myArg1:     .int 1


            .string     ; segment for strings
str1:       .str 'Hello, World'
str2:       .str 'Another string'
str3:       .str 'Multi\nLine'


            .code       ; segment for code
main:       ENTER 0, 2   ; argsSize localsSize (localsSize = actualLocalsSize + argsSize + 2)
            J label2
            LEAVE 0, 0   ; argsSize returnSize

func1:      ENTER 0, 2
label1:
label2:
func1_label3:
            STATIC_U8_LOAD myInt    ; access statics through labels
            STATIC_U8_LOAD myInt2
            IADD
            STATIC_U8_STORE myInt

            STATIC_U8_LOAD 0        ; access statics through their offsets
            STATIC_U8_LOAD 1
            IADD
            STATIC_U8_STORE 0

            STATIC_U8_LOAD myArg1    ; access args
            DROP

            PUSH_CONST_U24 1234
            DROP

            GLOBAL_U24_LOAD gInt    ; access globals through labels
            IADD_U8 1
            GLOBAL_U24_STORE gInt

            GLOBAL_U24_LOAD gVector
            DROP

            GLOBAL_U24_LOAD 262144  ; access globals through their offsets
            IADD_U8 1
            GLOBAL_U24_STORE 262144

            PUSH_CONST_U8 str1
            STRING
            PUSH_CONST_U8 str2
            STRING
            DUP
            DROP
            DROP
            DROP

            PUSH_CONST_U24 getMyFloat
            STATIC_U8_STORE getMyFloatRef

            PUSH_CONST_S16 1000
            NATIVE 1, 0, WAIT

            ; MY_TEST_CONST = 16
            PUSH_CONST_U8 16
            PUSH_CONST_U8_U8 16, 16
            PUSH_CONST_U8_U8_U8 16, 16, 16
            DROP
            DROP
            DROP
            DROP
            DROP
            DROP

            PUSH_CONST_U8 myInt2    ; labels as operands (replaced with their absolute offset)
            DROP

            PUSH_CONST_U8 10.5      ; warning: float number truncated
            DROP

            PUSH_CONST_U8 10        ; integer
            DROP

            PUSH_CONST_U8 256       ; warning: possible loss of data
            DROP

            ; MY_TEST_FLOAT = 12.34
            PUSH_CONST_F 12.34
            PUSH_CONST_F 8
            PUSH_CONST_F 8.9
            DROP
            DROP
            DROP

            CALL getMyFloat
            DROP

            PUSH_CONST_S16 1000
            PUSH_CONST_S16 -1000
            PUSH_CONST_S16 60000    ; warning: possible loss of data
            PUSH_CONST_S16 -60000   ; warning: possible loss of data
            DROP
            DROP
            DROP
            DROP

            PUSH_CONST_U8 1
            SWITCH 1:case1, 2:case2, 3:case3, 4:0
caseDefault:
            PUSH_CONST_U8 0
            J switchEnd
case1:
            PUSH_CONST_U8 1
            J switchEnd
case2:
            PUSH_CONST_U8 2
            J switchEnd
case3:
            PUSH_CONST_U8 3
            J switchEnd
switchEnd:
            DROP

            LEAVE 0, 0


            .static     ; continue the static segment
myFloat:    .float 5.0
getMyFloatRef:
            .int 0

            .code       ; continue the code segment
getMyFloat: ENTER 0, 2
            STATIC_U8_LOAD myFloat
            LEAVE 0, 1

            .static
myInt64:    .int64 0xAABBCCDD11223344

            .include    ; segment to define used natives
WAIT:                   .native 0x4EDE34FBADD967A6
CREATE_PED:             .native 0xD49F9B0955C367DE
_0x9614299DCB53E54B:    .native 0x9614299DCB53E54B
            ");

            Assert.False(asm.Diagnostics.HasErrors);

            //// globals
            //Assert.Equal((1 << 18) | 0, asm.Labels["gInt"].Offset);
            //Assert.Equal((1 << 18) | 1, asm.Labels["gVector"].Offset);
            //// statics
            //Assert.Equal(0, asm.Labels["myInt"].Offset);
            //Assert.Equal(1, asm.Labels["myInt2"].Offset);
            //Assert.Equal(2, asm.Labels["myIntArray"].Offset);
            //Assert.Equal(8, asm.Labels["myFloatArray"].Offset);
            //Assert.Equal(19, asm.Labels["myVector"].Offset);
            //Assert.Equal(22, asm.Labels["myOtherVector"].Offset);
            //Assert.Equal(25, asm.Labels["randomData"].Offset);
            //Assert.Equal(41, asm.Labels["emptySpace"].Offset);
            //Assert.Equal(57, asm.Labels["myFloat"].Offset);
            //Assert.Equal(58, asm.Labels["getMyFloatRef"].Offset);
            //Assert.Equal(59, asm.Labels["myInt64"].Offset);
            //// args
            //Assert.Equal(60, asm.Labels["myArg1"].Offset);
            //// strings
            //Assert.Equal(0, asm.Labels["str1"].Offset);
            //Assert.Equal(13, asm.Labels["str2"].Offset);
            //Assert.Equal(28, asm.Labels["str3"].Offset);
            //// code
            //Assert.Equal(0, asm.Labels["main"].Offset);
            //Assert.Equal(11, asm.Labels["func1"].Offset);
            //Assert.Equal(16, asm.Labels["label1"].Offset);
            //Assert.Equal(16, asm.Labels["label2"].Offset);
            //Assert.Equal(16, asm.Labels["func1_label3"].Offset);
            //// natives
            //Assert.Equal(0, asm.Labels["WAIT"].Offset);
            //Assert.Equal(1, asm.Labels["CREATE_PED"].Offset);
            //Assert.Equal(2, asm.Labels["_0x9614299DCB53E54B"].Offset);


            var sc = asm.OutputScript;
            Assert.Equal("my_script", sc.Name);
            Assert.Equal(0x1234ABCDu, sc.GlobalsSignature);

            // globals
            Assert.Equal(4u, sc.GlobalsLength);
            Assert.Equal(1u, sc.GlobalsBlock);
            Assert.Equal(10, sc.GlobalsPages[0][0].AsInt32);
            Assert.Equal(1.0f, sc.GlobalsPages[0][1].AsFloat);
            Assert.Equal(2.0f, sc.GlobalsPages[0][2].AsFloat);
            Assert.Equal(3.0f, sc.GlobalsPages[0][3].AsFloat);

            // statics
            Assert.Equal(61u, sc.StaticsCount); // includes args
            Assert.Equal(1u, sc.ArgsCount);
            Assert.Equal(5, sc.Statics[0].AsInt32); // myInt
            Assert.Equal(0xF, sc.Statics[1].AsInt32); // myInt2
            Assert.Equal(5, sc.Statics[2].AsInt32); // myIntArray.length
            Assert.Equal(1, sc.Statics[3].AsInt32); // myIntArray[0]
            Assert.Equal(2, sc.Statics[4].AsInt32); // myIntArray[1]
            Assert.Equal(3, sc.Statics[5].AsInt32); // myIntArray[2]
            Assert.Equal(4, sc.Statics[6].AsInt32); // myIntArray[3]
            Assert.Equal(5, sc.Statics[7].AsInt32); // myIntArray[4]
            Assert.Equal(10, sc.Statics[8].AsInt32); // myFloatArray.length
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(1.0f, sc.Statics[9 + i].AsFloat); // myFloatArray[i]
            }
            Assert.Equal(1.0f, sc.Statics[19].AsFloat); // myVector.x
            Assert.Equal(2.0f, sc.Statics[20].AsFloat); // myVector.y
            Assert.Equal(3.0f, sc.Statics[21].AsFloat); // myVector.z
            Assert.Equal(4.0f, sc.Statics[22].AsFloat); // myOtherVector.x
            Assert.Equal(4.0f, sc.Statics[23].AsFloat); // myOtherVector.y
            Assert.Equal(4.0f, sc.Statics[24].AsFloat); // myOtherVector.z
            for (int i = 0; i < 2; i++)
            {
                Assert.Equal(22, sc.Statics[25 + i * 8 + 0].AsInt32); // randomData
                Assert.Equal(33, sc.Statics[25 + i * 8 + 1].AsInt32);
                Assert.Equal(33, sc.Statics[25 + i * 8 + 2].AsInt32);
                Assert.Equal(33, sc.Statics[25 + i * 8 + 3].AsInt32);
                Assert.Equal(33, sc.Statics[25 + i * 8 + 4].AsInt32);
                Assert.Equal(33, sc.Statics[25 + i * 8 + 5].AsInt32);
                Assert.Equal(33, sc.Statics[25 + i * 8 + 6].AsInt32);
                Assert.Equal(33, sc.Statics[25 + i * 8 + 7].AsInt32);
            }
            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(0, sc.Statics[41 + i].AsInt32); // emptySpace
            }
            Assert.Equal(5.0f, sc.Statics[57].AsFloat); // myFloat
            Assert.Equal(0, sc.Statics[58].AsInt32); // getMyFloatRef
            Assert.Equal(0xAABBCCDD11223344, sc.Statics[59].AsUInt64); // myInt64
            Assert.Equal(1, sc.Statics[60].AsInt32); // myArg1

            // strings
            Assert.Equal(12u + 1 + 14 + 1 + 10 + 1, sc.StringsLength);
            Assert.Equal("Hello, World", sc.String(0));
            Assert.Equal("Another string", sc.String(13));
            Assert.Equal("Multi\nLine", sc.String(28));

            // natives
            Assert.Equal(3u, sc.NativesCount);
            Assert.Equal(0x4EDE34FBADD967A6u, sc.NativeHash(0));
            Assert.Equal(0xD49F9B0955C367DEu, sc.NativeHash(1));
            Assert.Equal(0x9614299DCB53E54Bu, sc.NativeHash(2));

            var s = sc.DumpToString();
            ;
        }
    }
}
