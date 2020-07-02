grammar ScAsm;

script
    : (line EOL)*
    ;

line
    : (instruction | directive | labelDecl)? comment?
    ;

instruction
    : identifier operandList
    ;

directive
    : '$' identifier operandList
    ;

operandList
    : operand*
    ;

operand
    : integer
    | float
    | string
    | globalLabel
    | localLabel
    ;

labelDecl
    : (globalLabel | localLabel) ':'
    ;

globalLabel
    : identifier
    ;
    
localLabel
    : '.' identifier
    ;

identifier
    : IDENTIFIER
    ;

integer
    : DECIMAL_INTEGER
    | HEX_INTEGER
    ;

float
    : FLOAT
    ;

string
    : STRING
    ;

comment
    : COMMENT
    ;

IDENTIFIER
    :   [a-zA-Z_] [a-zA-Z_0-9]*
    ;

FLOAT
    : DECIMAL_INTEGER '.' DECIMAL_INTEGER
    ;

DECIMAL_INTEGER
    :   [-+]? DIGIT+
    ;

HEX_INTEGER
    :   '0x' HEX_DIGIT+
    ;

STRING
    : UNCLOSED_STRING '"'
    ;

UNCLOSED_STRING
    : '"' (~["\\\r\n] | '\\' (. | EOF))*
    ;

COMMENT
    : ';' ~ [\r\n]* -> skip
    ;

EOL
    : [\r\n]+
    ;

WS
    : [ \t] -> skip
    ;

fragment DIGIT : [0-9];
fragment HEX_DIGIT : [0-9a-fA-F];

fragment A : [aA];
fragment B : [bB];
fragment C : [cC];
fragment D : [dD];
fragment E : [eE];
fragment F : [fF];
fragment G : [gG];
fragment H : [hH];
fragment I : [iI];
fragment J : [jJ];
fragment K : [kK];
fragment L : [lL];
fragment M : [mM];
fragment N : [nN];
fragment O : [oO];
fragment P : [pP];
fragment Q : [qQ];
fragment R : [rR];
fragment S : [sS];
fragment T : [tT];
fragment U : [uU];
fragment V : [vV];
fragment W : [wW];
fragment X : [xX];
fragment Y : [yY];
fragment Z : [zZ];