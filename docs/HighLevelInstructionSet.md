# High Level Instruction Set

The following instructions do not correspond to a specific opcode, instead they combine multiple regular instructions to provide an simpler assembly language.

| Mnemonic    | Operands         | Stack<br>`[before] → [after]` | Description                                                                                                                                                |
|-------------|------------------|:-----------------------------:|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| PUSH_STRING | *str1*           |           `→ str1`            | Push the pointer to the string `str1` to the top of the stack. Equivalent to a `$STRING` directive, and a pair of `PUSH_CONST_*` and `STRING` instructions |
| PUSH        | *n1*/*f1*/*str1* |        `→ n1/f1/str1`         | Push a value to the top of the stack. Equivalent to a `PUSH_CONST_*` instruction, or `PUSH_STRING` in the case of `str1`                                   |
| CALL_NATIVE | *nativeName*     |                               | Calls the specified native command. Equivalent to `$NATIVE` directive and a `NATIVE` instruction                                                           |
