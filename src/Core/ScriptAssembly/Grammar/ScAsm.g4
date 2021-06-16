grammar ScAsm;

program
    : (line EOL)* EOF
    ;

line
    : label? (directive | instruction)?
    ;

label
    : identifier ':'
    ;

instruction
    : opcode operandList?
    ;

operandList
    : operand (',' operand)*
    ;

operand
    : identifier                                    #identifierOperand
    | integer                                       #integerOperand
    | float                                         #floatOperand
    | value=operand ':' jumpTo=operand              #switchCaseOperand
    ;

directive
    : D_GLOBAL                                      #globalSegmentDirective
    | D_STATIC                                      #staticSegmentDirective
    | D_ARG                                         #argSegmentDirective
    | D_STRING                                      #stringSegmentDirective
    | D_CODE                                        #codeSegmentDirective
    | D_INCLUDE                                     #includeSegmentDirective
    | D_SCRIPT_NAME identifier                      #scriptNameDirective
    | D_SCRIPT_HASH integer                         #scriptHashDirective
    | D_GLOBAL_BLOCK integer                        #globalBlockDirective
    | D_CONST identifier (integer | float)          #constDirective
    | D_INT directiveOperandList                    #intDirective
    | D_FLOAT directiveOperandList                  #floatDirective
    | D_STR string                                  #strDirective
    | D_NATIVE integer                              #nativeDirective
    ;

directiveOperandList
    : directiveOperand (',' directiveOperand)*
    ;

directiveOperand
    : identifier                                                    #identifierDirectiveOperand
    | integer                                                       #integerDirectiveOperand
    | float                                                         #floatDirectiveOperand
    | (identifier | integer) K_DUP '(' directiveOperandList ')'     #dupDirectiveOperand
    ;

opcode
    : IDENTIFIER
    | K_DUP
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

// directives names
D_SCRIPT_NAME : '.' S C R I P T '_' N A M E;
D_SCRIPT_HASH : '.' S C R I P T '_' H A S H;
D_GLOBAL_BLOCK : '.' G L O B A L '_' B L O C K;
D_CONST : '.' C O N S T;
D_GLOBAL : '.' G L O B A L;
D_STATIC : '.' S T A T I C;
D_ARG : '.' A R G;
D_STRING : '.' S T R I N G;
D_CODE : '.' C O D E;
D_INCLUDE : '.' I N C L U D E;
D_INT : '.' I N T;
D_FLOAT : '.' F L O A T;
D_STR : '.' S T R;
D_NATIVE : '.' N A T I V E;

K_DUP : D U P;

IDENTIFIER
    :   [a-zA-Z_] [a-zA-Z_0-9]*
    ;

DECIMAL_INTEGER
    :   [-+]? DIGIT+
    ;

HEX_INTEGER
    :   '0x' HEX_DIGIT+
    ;

FLOAT
    : DECIMAL_INTEGER '.' DIGIT* FLOAT_EXPONENT?
    | '.' DIGIT+ FLOAT_EXPONENT?
    | DECIMAL_INTEGER FLOAT_EXPONENT
    ;

fragment FLOAT_EXPONENT
    : [eE] DECIMAL_INTEGER
    ;

STRING
    : UNCLOSED_STRING '"'
    | UNCLOSED_STRING_SQ '\''
    ;

UNCLOSED_STRING
    : '"' (~["\\\r\n] | '\\' (. | EOF))*
    ;

UNCLOSED_STRING_SQ
    : '\'' (~['\\\r\n] | '\\' (. | EOF))*
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
