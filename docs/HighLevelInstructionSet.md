# High Level Instruction Set

The following instructions do not correspond to a specific opcode, instead they combine multiple regular instructions to provide an simpler assembly language.

| Mnemonic    | Operands                              | Stack<br>`[before] → [after]` | Description                                                                                                                                                |
|-------------|---------------------------------------|:-----------------------------:|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| PUSH_STRING | *str1*                                |           `→ str1`            | Push the pointer to the string `str1` to the top of the stack. Equivalent to a `$STRING` directive, and a pair of `PUSH_CONST_*` and `STRING` instructions |
| PUSH        | *n1*/*f1*/*str1* ... *nN*/*fN*/*strN* | `→ n1/f1/str1 ... nN/fN/strN` | Push multiple values to the top of the stack. Equivalent to multiple `PUSH_CONST_*` instructions, or `PUSH_STRING` in the case of `strN`                   |
| CALL_NATIVE | *nativeName*                          |                               | Calls the specified native command. Equivalent to `$NATIVE` directive and a `NATIVE` instruction                                                           |
