grammar ScLang;

script
    : (topLevelStatement? EOL)*
    ;

topLevelStatement
    : K_SCRIPT_NAME identifier  #scriptNameStatement
    | procedure                 #procedureStatement
    | struct                    #structStatement
    | variableDeclaration       #staticFieldStatement
    ;

statement
    : variableDeclaration               #variableDeclarationStatement
    | expression '=' expression         #assignmentStatement // TODO: more assignment operators (+=, -=, *=, /=, ...)
    
    | K_IF expression EOL
      statementBlock
      K_ENDIF                           #ifStatement
    
    | K_WHILE expression EOL
      statementBlock
      K_ENDWHILE                        #whileStatement
    
    | procedureCall                     #callStatement
    ;

statementBlock
    : (statement? EOL)*
    ;

expression
    : K_NOT expression                                                      #notExpression
    | expression ('+' | '-' | '*' | '/' | '%' | '|' | '&' | '^') expression #binaryExpression
    | '<<' expression (',' expression)* '>>'                                #aggregateExpression
    | identifier                                                            #identifierExpression
    | expression '.' identifier                                             #memberAccessExpression
    | expression arrayIndexer                                               #arrayAccessExpression
    | procedureCall                                                         #callExpression
    | (numeric | string | bool)                                             #literalExpression
    ;

procedureCall
    : identifier '(' (expression (',' expression)*)? ')'
    ;

variableDeclaration
    : variable ('=' expression)?
    ;

variable
    : type identifier arrayIndexer?
    ;

procedure
    : K_PROC identifier '(' ')' EOL
      statementBlock
      K_ENDPROC
    ;

struct
    : K_STRUCT identifier EOL
      (variableDeclaration? EOL)*
      K_ENDSTRUCT
    ;

arrayIndexer
    : '[' expression ']'
    ;

type
    : identifier
    ;

identifier
    : IDENTIFIER
    ;

numeric
    : integer
    | float
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

bool
    : K_TRUE
    | K_FALSE
    ; 

// keywords
K_PROC : P R O C;
K_ENDPROC : E N D P R O C;
K_TRUE : T R U E;
K_FALSE : F A L S E;
K_NOT : N O T;
K_IF : I F;
K_ENDIF : E N D I F;
K_WHILE : W H I L E;
K_ENDWHILE : E N D W H I L E;
K_STRUCT : S T R U C T;
K_ENDSTRUCT : E N D S T R U C T;
K_SCRIPT_NAME : S C R I P T '_' N A M E;

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
    ;

UNCLOSED_STRING
    : '"' (~["\\\r\n] | '\\' (. | EOF))*
    ;

COMMENT
    : '//' ~ [\r\n]* -> skip
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