grammar ScAsm;

program
    : (line EOL)*
    ;

line
    : label? (segmentDirective | directive | instruction)?
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
    | (integer | identifier) ':' labelId=identifier #switchCaseOperand
    ;

directive
    : D_SCRIPT_NAME identifier                      #scriptNameDirective
    | D_SCRIPT_HASH integer                         #scriptHashDirective
    | D_GLOBAL_BLOCK integer                        #globalBlockDirective
    | D_CONST identifier (integer|float)            #constDirective
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
    | (identifier | integer) K_TIMES '(' directiveOperandList ')'   #dupDirectiveOperand // 'times' instead of 'dup' due to ambiguitiy with opcode DUP
    ;

segmentDirective
    : D_GLOBAL              #globalSegmentDirective
    | D_STATIC              #staticSegmentDirective
    | D_ARG                 #argSegmentDirective
    | D_STRING              #stringSegmentDirective
    | D_CODE                #codeSegmentDirective
    | D_INCLUDE             #includeSegmentDirective
    ;

opcode
    : OPCODE
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
    | DECIMAL_INTEGER
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

K_TIMES : T I M E S;

OPCODE
    : N O P
    | I A D D
    | I S U B
    | I M U L
    | I D I V
    | I M O D
    | I N O T
    | I N E G
    | I E Q
    | I N E
    | I G T
    | I G E
    | I L T
    | I L E
    | F A D D
    | F S U B
    | F M U L
    | F D I V
    | F M O D
    | F N E G
    | F E Q
    | F N E
    | F G T
    | F G E
    | F L T
    | F L E
    | V A D D
    | V S U B
    | V M U L
    | V D I V
    | V N E G
    | I A N D
    | I O R
    | I X O R
    | I '2' F
    | F '2' I
    | F '2' V
    | P U S H '_' C O N S T '_' U '8'
    | P U S H '_' C O N S T '_' U '8' '_' U '8'
    | P U S H '_' C O N S T '_' U '8' '_' U '8' '_' U '8'
    | P U S H '_' C O N S T '_' U '3' '2'
    | P U S H '_' C O N S T '_' F
    | D U P
    | D R O P
    | N A T I V E
    | E N T E R
    | L E A V E
    | L O A D
    | S T O R E
    | S T O R E '_' R E V
    | L O A D '_' N
    | S T O R E '_' N
    | A R R A Y '_' U '8'
    | A R R A Y '_' U '8' '_' L O A D
    | A R R A Y '_' U '8' '_' S T O R E
    | L O C A L '_' U '8'
    | L O C A L '_' U '8' '_' L O A D
    | L O C A L '_' U '8' '_' S T O R E
    | S T A T I C '_' U '8'
    | S T A T I C '_' U '8' '_' L O A D
    | S T A T I C '_' U '8' '_' S T O R E
    | I A D D '_' U '8'
    | I M U L '_' U '8'
    | I O F F S E T
    | I O F F S E T '_' U '8'
    | I O F F S E T '_' U '8' '_' L O A D
    | I O F F S E T '_' U '8' '_' S T O R E
    | P U S H '_' C O N S T '_' S '1' '6'
    | I A D D '_' S '1' '6'
    | I M U L '_' S '1' '6'
    | I O F F S E T '_' S '1' '6'
    | I O F F S E T '_' S '1' '6' '_' L O A D
    | I O F F S E T '_' S '1' '6' '_' S T O R E
    | A R R A Y '_' U '1' '6'
    | A R R A Y '_' U '1' '6' '_' L O A D
    | A R R A Y '_' U '1' '6' '_' S T O R E
    | L O C A L '_' U '1' '6'
    | L O C A L '_' U '1' '6' '_' L O A D
    | L O C A L '_' U '1' '6' '_' S T O R E
    | S T A T I C '_' U '1' '6'
    | S T A T I C '_' U '1' '6' '_' L O A D
    | S T A T I C '_' U '1' '6' '_' S T O R E
    | G L O B A L '_' U '1' '6'
    | G L O B A L '_' U '1' '6' '_' L O A D
    | G L O B A L '_' U '1' '6' '_' S T O R E
    | J
    | J Z
    | I E Q '_' J Z
    | I N E '_' J Z
    | I G T '_' J Z
    | I G E '_' J Z
    | I L T '_' J Z
    | I L E '_' J Z
    | C A L L
    | G L O B A L '_' U '2' '4'
    | G L O B A L '_' U '2' '4' '_' L O A D
    | G L O B A L '_' U '2' '4' '_' S T O R E
    | P U S H '_' C O N S T '_' U '2' '4'
    | S W I T C H
    | S T R I N G
    | S T R I N G H A S H
    | T E X T '_' L A B E L '_' A S S I G N '_' S T R I N G
    | T E X T '_' L A B E L '_' A S S I G N '_' I N T
    | T E X T '_' L A B E L '_' A P P E N D '_' S T R I N G
    | T E X T '_' L A B E L '_' A P P E N D '_' I N T
    | T E X T '_' L A B E L '_' C O P Y
    | C A T C H
    | T H R O W
    | C A L L I N D I R E C T
    | P U S H '_' C O N S T '_' M '1'
    | P U S H '_' C O N S T '_' '0'
    | P U S H '_' C O N S T '_' '1'
    | P U S H '_' C O N S T '_' '2'
    | P U S H '_' C O N S T '_' '3'
    | P U S H '_' C O N S T '_' '4'
    | P U S H '_' C O N S T '_' '5'
    | P U S H '_' C O N S T '_' '6'
    | P U S H '_' C O N S T '_' '7'
    | P U S H '_' C O N S T '_' F M '1'
    | P U S H '_' C O N S T '_' F '0'
    | P U S H '_' C O N S T '_' F '1'
    | P U S H '_' C O N S T '_' F '2'
    | P U S H '_' C O N S T '_' F '3'
    | P U S H '_' C O N S T '_' F '4'
    | P U S H '_' C O N S T '_' F '5'
    | P U S H '_' C O N S T '_' F '6'
    | P U S H '_' C O N S T '_' F '7'
    ;

IDENTIFIER
    :   [a-zA-Z_] [a-zA-Z_0-9]*
    ;

FLOAT
    : DECIMAL_INTEGER '.' DIGIT+
    ;

DECIMAL_INTEGER
    :   [-+]? DIGIT+
    ;

HEX_INTEGER
    :   '0x' HEX_DIGIT+
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
