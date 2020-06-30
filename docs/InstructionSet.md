# Instruction Set

| Mnemonic                 | Opcode | Operands       |    Stack<br>`[before] → [after]`     | Description                                                                                             |
|--------------------------|:------:|----------------|:------------------------------------:|---------------------------------------------------------------------------------------------------------|
| NOP                      |   00   |                |                 `→`                  | No operation                                                                                            |
| IADD                     |   01   |                |             `n1 n2 → n3`             | Add `n1` and `n2`                                                                                       |
| ISUB                     |   02   |                |             `n1 n2 → n3`             | Subtract `n2` from `n1`                                                                                 |
| IMUL                     |   03   |                |             `n1 n2 → n3`             | Multiply `n1` and `n2`                                                                                  |
| IDIV                     |   04   |                |             `n1 n2 → n3`             | Divide `n1` by `n2`                                                                                     |
| IMOD                     |   05   |                |             `n1 n2 → n3`             | Divide `n1` by `n2` and push the remainder to the top of the stack                                      |
| INOT                     |   06   |                |              `n1 → n2`               | Logical negation of `n1`: if `n1 == 0`, `1` is pushed to the top of the stack; otherwise, `0` is pushed |
| INEG                     |   07   |                |              `n1 → n2`               | Negate `n1`                                                                                             |
| IEQ                      |   08   |                |            `n1 n2 → flag`            | Is `n1` equal to `n2`?                                                                                  |
| INE                      |   09   |                |            `n1 n2 → flag`            | Is `n1` not equal to `n2`?                                                                              |
| IGT                      |   0A   |                |            `n1 n2 → flag`            | Is `n1` greater than `n2`?                                                                              |
| IGE                      |   0B   |                |            `n1 n2 → flag`            | Is `n1` greater than or equal to `n2`?                                                                  |
| ILT                      |   0C   |                |            `n1 n2 → flag`            | Is `n1` less than `n2`?                                                                                 |
| ILE                      |   0D   |                |            `n1 n2 → flag`            | Is `n1` less than or equal to `n2`?                                                                     |
| FADD                     |   0E   |                |             `f1 f2 → f3`             | Add `f1` and `f2`                                                                                       |
| FSUB                     |   0F   |                |             `f1 f2 → f3`             | Subtract `f2` from `f1`                                                                                 |
| FMUL                     |   10   |                |             `f1 f2 → f3`             | Multiply `f1` and `f2`                                                                                  |
| FDIV                     |   11   |                |             `f1 f2 → f3`             | Divide `f1` by `f2`                                                                                     |
| FMOD                     |   12   |                |             `f1 f2 → f3`             | Divide `f1` by `f2` and push the remainder to the top of the stack                                      |
| FNEG                     |   13   |                |              `f1 → f2`               | Negate `f1`                                                                                             |
| FEQ                      |   14   |                |            `f1 f2 → flag`            | Is `f1` equal to `f2`?                                                                                  |
| FNE                      |   15   |                |            `f1 f2 → flag`            | Is `f1` not equal to `f2`?                                                                              |
| FGT                      |   16   |                |            `f1 f2 → flag`            | Is `f1` greater than `f2`?                                                                              |
| FGE                      |   17   |                |            `f1 f2 → flag`            | Is `f1` greater than or equal to `f2`?                                                                  |
| FLT                      |   18   |                |            `f1 f2 → flag`            | Is `f1` less than `f2`?                                                                                 |
| FLE                      |   19   |                |            `f1 f2 → flag`            | Is `f1` less than or equal to `f2`?                                                                     |
| VADD                     |   1A   |                | `{x1 y1 z1} {x2 y2 z2} → {x3 y3 z3}` | Add vectors `{x1 y1 z1}` and `{x2 y2 z2}`                                                               |
| VSUB                     |   1B   |                | `{x1 y1 z1} {x2 y2 z2} → {x3 y3 z3}` | Subtract vector `{x2 y2 z2}` from `{x1 y1 z1}`                                                          |
| VMUL                     |   1C   |                | `{x1 y1 z1} {x2 y2 z2} → {x3 y3 z3}` | Multiply vectors `{x1 y1 z1}` and `{x2 y2 z2}`, component-wise                                          |
| VDIV                     |   1D   |                | `{x1 y1 z1} {x2 y2 z2} → {x3 y3 z3}` | Divide vector `{x1 y1 z1}` by `{x2 y2 z2}`, component-wise                                              |
| VNEG                     |   1E   |                |      `{x1 y1 z1} → {x2 y2 z2}`       | Negate each component of the vector `{x1 y1 z1}`                                                        |
| IAND                     |   1F   |                |             `n1 n2 → n3`             | Bitwise AND on `n1` and `n2`                                                                            |
| IOR                      |   20   |                |             `n1 n2 → n3`             | Bitwise OR on `n1` and `n2`                                                                             |
| IXOR                     |   21   |                |             `n1 n2 → n3`             | Bitwise XOR on `n1` and `n2`                                                                            |
| I2F                      |   22   |                |              `n1 → f1`               | Converts a 32-bit signed integer to a floating-point number                                             |
| F2I                      |   23   |                |              `n1 → f1`               | Converts a floating-point number to 32-bit signed integer                                               |
| F2V                      |   24   |                |          `f1 → {f1 f1 f1}`           | Converts a floating-point number to a vector, by duplicating it twice                                   |
| PUSH_CONST_U8            |   25   | *n1*           |                `→ n1`                | Push a 8-bit unsigned integer to the top of the stack                                                   |
| PUSH_CONST_U8_U8         |   26   | *n1* *n2*      |              `→ n1 n2`               | Push two 8-bit unsigned integers to the top of the stack                                                |
| PUSH_CONST_U8_U8_U8      |   27   | *n1* *n2* *n3* |             `→ n1 n2 n3`             | Push three 8-bit unsigned integers to the top of the stack                                              |
| PUSH_CONST_U32           |   28   | *n1*           |                `→ n1`                | Push a 32-bit unsigned integer to the top of the stack                                                  |
| PUSH_CONST_F             |   29   | *f1*           |                `→ f1`                | Push a floating-point number to the top of the stack                                                    |
| DUP                      |   2A   |                |             `n1 → n1 n1`             | Duplicate the value on the top of the stack                                                             |
| DROP                     |   2B   |                |                `n1 →`                | Remove the top value from the stack                                                                     |
| NATIVE                   |   2C   |                |                                      |                                                                                                         |
| ENTER                    |   2D   |                |                                      |                                                                                                         |
| LEAVE                    |   2E   |                |                                      |                                                                                                         |
| LOAD                     |   2F   |                |                                      |                                                                                                         |
| STORE                    |   30   |                |                                      |                                                                                                         |
| STORE_REV                |   31   |                |                                      |                                                                                                         |
| LOAD_N                   |   32   |                |                                      |                                                                                                         |
| STORE_N                  |   33   |                |                                      |                                                                                                         |
| ARRAY_U8                 |   34   |                |                                      |                                                                                                         |
| ARRAY_U8_LOAD            |   35   |                |                                      |                                                                                                         |
| ARRAY_U8_STORE           |   36   |                |                                      |                                                                                                         |
| LOCAL_U8                 |   37   |                |                                      |                                                                                                         |
| LOCAL_U8_LOAD            |   38   |                |                                      |                                                                                                         |
| LOCAL_U8_STORE           |   39   |                |                                      |                                                                                                         |
| STATIC_U8                |   3A   |                |                                      |                                                                                                         |
| STATIC_U8_LOAD           |   3B   |                |                                      |                                                                                                         |
| STATIC_U8_STORE          |   3C   |                |                                      |                                                                                                         |
| IADD_U8                  |   3D   | *n1*           |              `n2 → n3`               | Add `n1` (8-bit unsigned integer) and `n2`                                                              |
| IMUL_U8                  |   3E   | *n1*           |              `n2 → n3`               | Multiply `n1` (8-bit unsigned integer) and `n2`                                                         |
| IOFFSET                  |   3F   |                |                                      |                                                                                                         |
| IOFFSET_U8               |   40   |                |                                      |                                                                                                         |
| IOFFSET_U8_LOAD          |   41   |                |                                      |                                                                                                         |
| IOFFSET_U8_STORE         |   42   |                |                                      |                                                                                                         |
| PUSH_CONST_S16           |   43   | *n1*           |                `→ n1`                | Push a 16-bit signed integer to the top of the stack                                                    |
| IADD_S16                 |   44   | *n1*           |              `n2 → n3`               | Add `n1` (16-bit signed integer) and `n2`                                                               |
| IMUL_S16                 |   45   | *n1*           |              `n2 → n3`               | Multiply `n1` (16-bit signed integer) and `n2`                                                          |
| IOFFSET_S16              |   46   |                |                                      |                                                                                                         |
| IOFFSET_S16_LOAD         |   47   |                |                                      |                                                                                                         |
| IOFFSET_S16_STORE        |   48   |                |                                      |                                                                                                         |
| ARRAY_U16                |   49   |                |                                      |                                                                                                         |
| ARRAY_U16_LOAD           |   4A   |                |                                      |                                                                                                         |
| ARRAY_U16_STORE          |   4B   |                |                                      |                                                                                                         |
| LOCAL_U16                |   4C   |                |                                      |                                                                                                         |
| LOCAL_U16_LOAD           |   4D   |                |                                      |                                                                                                         |
| LOCAL_U16_STORE          |   4E   |                |                                      |                                                                                                         |
| STATIC_U16               |   4F   |                |                                      |                                                                                                         |
| STATIC_U16_LOAD          |   50   |                |                                      |                                                                                                         |
| STATIC_U16_STORE         |   51   |                |                                      |                                                                                                         |
| GLOBAL_U16               |   52   |                |                                      |                                                                                                         |
| GLOBAL_U16_LOAD          |   53   |                |                                      |                                                                                                         |
| GLOBAL_U16_STORE         |   54   |                |                                      |                                                                                                         |
| J                        |   55   |                |                                      |                                                                                                         |
| JZ                       |   56   |                |                                      |                                                                                                         |
| IEQ_JZ                   |   57   |                |                                      |                                                                                                         |
| INE_JZ                   |   58   |                |                                      |                                                                                                         |
| IGT_JZ                   |   59   |                |                                      |                                                                                                         |
| IGE_JZ                   |   5A   |                |                                      |                                                                                                         |
| ILT_JZ                   |   5B   |                |                                      |                                                                                                         |
| ILE_JZ                   |   5C   |                |                                      |                                                                                                         |
| CALL                     |   5D   |                |                                      |                                                                                                         |
| GLOBAL_U24               |   5E   |                |                                      |                                                                                                         |
| GLOBAL_U24_LOAD          |   5F   |                |                                      |                                                                                                         |
| GLOBAL_U24_STORE         |   60   |                |                                      |                                                                                                         |
| PUSH_CONST_U24           |   61   | *n1*           |                `→ n1`                | Push a 24-bit unsigned integer to the top of the stack                                                  |
| SWITCH                   |   62   |                |                                      |                                                                                                         |
| STRING                   |   63   |                |             `n1 → str1`              | Push the pointer to the string at offset `n1` to the top of stack                                       |
| STRINGHASH               |   64   |                |             `str1 → n1`              | Calculate the Jenkins one-at-a-time hash of the string at the top of the stack                          |
| TEXT_LABEL_ASSIGN_STRING |   65   |                |                                      |                                                                                                         |
| TEXT_LABEL_ASSIGN_INT    |   66   |                |                                      |                                                                                                         |
| TEXT_LABEL_APPEND_STRING |   67   |                |                                      |                                                                                                         |
| TEXT_LABEL_APPEND_INT    |   68   |                |                                      |                                                                                                         |
| TEXT_LABEL_COPY          |   69   |                |                                      |                                                                                                         |
| CATCH                    |   6A   |                |                                      |                                                                                                         |
| THROW                    |   6B   |                |                                      |                                                                                                         |
| CALLINDIRECT             |   6C   |                |                                      |                                                                                                         |
| PUSH_CONST_M1            |   6D   |                |                `→ -1`                | Push `-1` to the top of the stack                                                                       |
| PUSH_CONST_0             |   6E   |                |                `→ 0`                 | Push `0` to the top of the stack                                                                        |
| PUSH_CONST_1             |   6F   |                |                `→ 1`                 | Push `1` to the top of the stack                                                                        |
| PUSH_CONST_2             |   70   |                |                `→ 2`                 | Push `2` to the top of the stack                                                                        |
| PUSH_CONST_3             |   71   |                |                `→ 3`                 | Push `3` to the top of the stack                                                                        |
| PUSH_CONST_4             |   72   |                |                `→ 4`                 | Push `4` to the top of the stack                                                                        |
| PUSH_CONST_5             |   73   |                |                `→ 5`                 | Push `5` to the top of the stack                                                                        |
| PUSH_CONST_6             |   74   |                |                `→ 6`                 | Push `6` to the top of the stack                                                                        |
| PUSH_CONST_7             |   75   |                |                `→ 7`                 | Push `7` to the top of the stack                                                                        |
| PUSH_CONST_FM1           |   76   |                |               `→ -1.0`               | Push `-1.0` to the top of the stack                                                                     |
| PUSH_CONST_F0            |   77   |                |               `→ 0.0`                | Push `0.0` to the top of the stack                                                                      |
| PUSH_CONST_F1            |   78   |                |               `→ 1.0`                | Push `1.0` to the top of the stack                                                                      |
| PUSH_CONST_F2            |   79   |                |               `→ 2.0`                | Push `2.0` to the top of the stack                                                                      |
| PUSH_CONST_F3            |   7A   |                |               `→ 3.0`                | Push `3.0` to the top of the stack                                                                      |
| PUSH_CONST_F4            |   7B   |                |               `→ 4.0`                | Push `4.0` to the top of the stack                                                                      |
| PUSH_CONST_F5            |   7C   |                |               `→ 5.0`                | Push `5.0` to the top of the stack                                                                      |
| PUSH_CONST_F6            |   7D   |                |               `→ 6.0`                | Push `6.0` to the top of the stack                                                                      |
| PUSH_CONST_F7            |   7E   |                |               `→ 7.0`                | Push `7.0` to the top of the stack                                                                      |
