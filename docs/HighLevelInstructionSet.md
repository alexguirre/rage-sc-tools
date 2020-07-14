# High Level Instruction Set

The following instructions do not correspond to a specific opcode, instead they combine multiple regular instructions to provide an simpler assembly language.

| Mnemonic     | Operands                              | Stack<br>`[before] → [after]` | Description                                                                                                                              |
|--------------|---------------------------------------|:-----------------------------:|------------------------------------------------------------------------------------------------------------------------------------------|
| PUSH_CONST   | *n1*/*f1*/*str1* ... *nN*/*fN*/*strN* | `→ n1/f1/str1 ... nN/fN/strN` | Push multiple values to the top of the stack. Equivalent to multiple `PUSH_CONST_*` instructions, or `PUSH_STRING` in the case of `strN` |
| CALL_NATIVE  | *nativeName*                          |                               | Calls the specified native command. Equivalent to `$NATIVE` directive and a `NATIVE` instruction                                         |
| STATIC       | *staticName*                          |                               | Equivalent to a `STATIC_U8` or `STATIC_U16` instruction.                                                                                 |
| STATIC_LOAD  | *staticName*                          |                               | Equivalent to a `STATIC_U8_LOAD` or `STATIC_U16_LOAD` instruction.                                                                       |
| STATIC_STORE | *staticName*                          |                               | Equivalent to a `STATIC_U8_STORE` or `STATIC_U16_STORE` instruction.                                                                     |
| LOCAL        | *localName*                           |                               | Equivalent to a `LOCAL_U8` or `LOCAL_U16` instruction.                                                                                   |
| LOCAL_LOAD   | *localName*                           |                               | Equivalent to a `LOCAL_U8_LOAD` or `LOCAL_U16_LOAD` instruction.                                                                         |
| LOCAL_STORE  | *localName*                           |                               | Equivalent to a `LOCAL_U8_STORE` or `LOCAL_U16_STORE` instruction.                                                                       |
