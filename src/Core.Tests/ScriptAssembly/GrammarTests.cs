﻿namespace ScTools.Tests.ScriptAssembly
{
    using Xunit;

    public class GrammarTests
    {
        [Fact]
        public void TestEverything()
        {
            var sc = Util.Assemble(@"
            .script_name my_script
            .script_hash 0x1234ABCD

            .const MY_DEFAULT_VALUE 4.0

            .global 1   ; segment for global variables, the integer is the block ID
gInt:       .int 10
gVector:    .float 1.0, 2.0, 3.0


            .static     ; segment for static variables
myInt:      .int 5
myInt2:     .int 0xF

myIntArray: .int 5, 1, 2, 3, 4, 5

myFloatArray:
            .const MY_FLOAT_ARRAY_SIZE 10       ; .const directive is compile-time only
            .int MY_FLOAT_ARRAY_SIZE
            .float MY_FLOAT_ARRAY_SIZE dup (1)

myVector:   .float 1.0, 2.0, 3.0

myOtherVector:
            .float 3 dup (MY_DEFAULT_VALUE)

emptySpace: .int 32 dup (0)


            .arg        ; segment for script args
myArg1:     .int 1


            .string     ; segment for strings
str1:       .str 'Hello, World'
str2:       .str 'Another string'


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

            STATIC_U8_LOAD myArg    ; access args
            DROP

            GLOBAL_U24_LOAD gInt    ; access globals through labels
            IADD_U8 1
            GLOBAL_U24_STORE gInt

            GLOBAL_U24_LOAD 262144  ; access globals through their offsets
            IADD_U8 1
            GLOBAL_U24_STORE 262144

            PUSH_CONST_U8 str1
            STRING
            PUSH_CONST_U8 str2
            STRING
            DROP
            DROP

            PUSH_CONST_U24 getMyFloat
            STATIC_U8_STORE getMyFloatRef

            PUSH_CONST_S16 1000
            NATIVE 1 0 WAIT

            LEAVE 0, 0


            .static     ; continue the static segment
myFloat:    .float 5.0
getMyFloatRef:
            .int 0

            .code       ; continue the code segment
getMyFloat: ENTER 0, 2
            STATIC_U8_LOAD myFloat
            LEAVE 0, 1

            .include    ; segment to define used natives
WAIT:                   .native 0x4EDE34FBADD967A6
CREATE_PED:             .native 0xD49F9B0955C367DE
_0x9614299DCB53E54B:    .native 0x9614299DCB53E54B
            ");

            Assert.Equal("my_script", sc.Name);
            Assert.Equal(0x1234ABCDu, sc.Hash);

            // globals
            Assert.Equal(4u, sc.GlobalsLength);
            Assert.Equal(1u, sc.GlobalsBlock);
            Assert.Equal(10, sc.GlobalsPages[0][0].AsInt32);
            Assert.Equal(1.0f, sc.GlobalsPages[0][1].AsFloat);
            Assert.Equal(2.0f, sc.GlobalsPages[0][2].AsFloat);
            Assert.Equal(3.0f, sc.GlobalsPages[0][3].AsFloat);

            // statics
            Assert.Equal(60u, sc.StaticsCount); // includes args
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
            for (int i = 0; i < 32; i++)
            {
                Assert.Equal(0, sc.Statics[25 + i].AsInt32); // emptySpace
            }
            Assert.Equal(5.0f, sc.Statics[57].AsFloat); // myFloat
            Assert.Equal(0, sc.Statics[58].AsInt32); // getMyFloatRef
            Assert.Equal(1, sc.Statics[59].AsInt32); // myArg1

            // strings
            Assert.Equal(12u + 1 + 14 + 1, sc.StringsLength);
            Assert.Equal("Hello, World", sc.String(0));
            Assert.Equal("Another String", sc.String(13));

            // natives
            Assert.Equal(3u, sc.NativesCount);
            Assert.Equal(0x4EDE34FBADD967A6u, sc.NativeHash(0));
            Assert.Equal(0xD49F9B0955C367DEu, sc.NativeHash(1));
            Assert.Equal(0x9614299DCB53E54Bu, sc.NativeHash(2));
        }
    }
}
