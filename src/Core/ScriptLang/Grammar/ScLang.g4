grammar ScLang;

program
    : ((directive | declaration)? EOL)* (directive | declaration)? // repeated so a new line is not required at the end of the file, TODO: is there a better way to do this?
    ;

directive
    : K_SCRIPT_NAME identifier                                          #scriptNameDirective
    | K_SCRIPT_HASH integer                                             #scriptHashDirective
    | K_USING string                                                    #usingDirective
    ;

declaration
    : K_PROC name=identifier parameterList EOL
      statementBlock
      K_ENDPROC                                                             #procedureDeclaration

    | K_FUNC returnType=identifier name=identifier parameterList EOL
      statementBlock
      K_ENDFUNC                                                             #functionDeclaration

    | K_PROTO K_PROC name=identifier parameterList                          #procedurePrototypeDeclaration
    | K_PROTO K_FUNC returnType=identifier name=identifier parameterList    #functionPrototypeDeclaration

    | K_NATIVE K_PROC name=identifier parameterList                         #procedureNativeDeclaration
    | K_NATIVE K_FUNC returnType=identifier name=identifier parameterList   #functionNativeDeclaration

    | K_STRUCT identifier EOL
      structFieldList
      K_ENDSTRUCT                                                       #structDeclaration

    | K_ENUM identifier EOL
      enumList
      K_ENDENUM                                                         #enumDeclaration

    | K_CONST varDeclaration                                            #constantVariableDeclaration
    | K_ARG varDeclaration                                              #argVariableDeclaration
    | varDeclaration                                                    #staticVariableDeclaration

    | K_GLOBAL block=integer owner=identifier EOL
      (varDeclaration? EOL)*
      K_ENDGLOBAL                                                       #globalBlockDeclaration
    ;

statement
    : varDeclaration                                                    #variableDeclarationStatement
    | left=expression op=('=' | '*=' | '/=' | '%=' | '+=' | '-=' | '&=' | '^=' | '|=') right=expression   #assignmentStatement

    | K_IF condition=expression EOL
      thenBlock=statementBlock
      elifBlock*
      elseBlock?
      K_ENDIF                                                   #ifStatement
    
    | K_WHILE condition=expression EOL
      statementBlock
      K_ENDWHILE                                                #whileStatement
    
    | K_REPEAT limit=expression counter=expression EOL
      statementBlock
      K_ENDREPEAT                                               #repeatStatement
    
    | K_SWITCH expression EOL
      switchCase*
      K_ENDSWITCH                                               #switchStatement

    | K_BREAK                                                   #breakStatement
    | K_RETURN expression?                                      #returnStatement
    | K_GOTO identifier                                         #gotoStatement
    | expression argumentList                                   #invocationStatement
    ;

elifBlock
    : K_ELIF condition=expression EOL
      statementBlock
    ;

elseBlock
    : K_ELSE EOL
      statementBlock
    ;

label
    : identifier ':'
    ;

labeledStatement
    : label? statement?
    ;

statementBlock
    : (labeledStatement EOL)*
    ;

expression
    : '(' expression ')'                                            #parenthesizedExpression
    | expression '.' identifier                                     #fieldAccessExpression
    | expression argumentList                                       #invocationExpression
    | expression arrayIndexer                                       #indexingExpression
    | op=(K_NOT | '-') expression                                   #unaryExpression
    | left=expression op=('*' | '/' | '%') right=expression         #binaryExpression
    | left=expression op=('+' | '-') right=expression               #binaryExpression
    | left=expression op='&' right=expression                       #binaryExpression
    | left=expression op='^' right=expression                       #binaryExpression
    | left=expression op='|' right=expression                       #binaryExpression
    | left=expression op=('<' | '>' | '<=' | '>=') right=expression #binaryExpression
    | left=expression op=('==' | '<>') right=expression             #binaryExpression
    | left=expression op=K_AND right=expression                     #binaryExpression
    | left=expression op=K_OR right=expression                      #binaryExpression
    | '<<' x=expression ',' y=expression ',' z=expression '>>'            #vectorExpression
    | identifier                                                    #identifierExpression
    | integer                                                       #intLiteralExpression
    | float                                                         #floatLiteralExpression
    | string                                                        #stringLiteralExpression
    | bool                                                          #boolLiteralExpression
    | K_SIZE_OF '(' expression ')'                                  #sizeOfExpression
    | K_NULL                                                        #nullExpression
    ;

switchCase
    : K_CASE value=expression EOL
      statementBlock                                                #valueSwitchCase
    | K_DEFAULT EOL
      statementBlock                                                #defaultSwitchCase
    ;

enumList
    : (enumMemberDeclarationList? EOL)*
    ;

enumMemberDeclarationList
    : enumMemberDeclaration (',' enumMemberDeclaration)*
    ;

enumMemberDeclaration
    : identifier ('=' initializer=expression)?
    ;

varDeclaration
    : type=identifier initDeclaratorList
    ;

varDeclarationNoInit
    : type=identifier declaratorList
    ;

singleVarDeclarationNoInit
    : type=identifier declarator
    ;
    
declaratorList
    : declarator (',' declarator)*
    ;

initDeclaratorList
    : initDeclarator (',' initDeclarator)*
    ;

initDeclarator
    : declarator ('=' initializer=expression)?
    ;

declarator
    : noRefDeclarator
    | refDeclarator
    ;

refDeclarator
    : '&' noRefDeclarator
    ;

noRefDeclarator
    : identifier                            #simpleDeclarator
    | noRefDeclarator '[' expression? ']'    #arrayDeclarator
    | '(' refDeclarator ')'                 #parenthesizedRefDeclarator
    ;

structFieldList
    : (varDeclarationNoInit? EOL)*
    ;

argumentList
    : '(' (expression (',' expression)*)? ')'
    ;

parameterList
    : '(' (singleVarDeclarationNoInit (',' singleVarDeclarationNoInit)*)? ')'
    ;

arrayIndexer
    : '[' expression ']'
    ;

identifier
    : IDENTIFIER
    ;

integer
    : DECIMAL_INTEGER
    | HEX_INTEGER
    | HASH_STRING
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
K_FUNC : F U N C;
K_ENDFUNC : E N D F U N C;
K_STRUCT : S T R U C T;
K_ENDSTRUCT : E N D S T R U C T;
K_ENUM : E N U M;
K_ENDENUM : E N D E N U M;
K_PROTO : P R O T O;
K_NATIVE : N A T I V E;
K_TRUE : T R U E;
K_FALSE : F A L S E;
K_NOT : N O T;
K_AND : A N D;
K_OR : O R;
K_IF : I F;
K_ELIF : E L I F;
K_ELSE : E L S E;
K_ENDIF : E N D I F;
K_WHILE : W H I L E;
K_ENDWHILE : E N D W H I L E;
K_REPEAT : R E P E A T;
K_ENDREPEAT : E N D R E P E A T;
K_SWITCH : S W I T C H;
K_ENDSWITCH : E N D S W I T C H;
K_CASE : C A S E;
K_DEFAULT : D E F A U L T;
K_BREAK : B R E A K;
K_RETURN : R E T U R N;
K_GOTO : G O T O;
K_SCRIPT_NAME : S C R I P T '_' N A M E;
K_SCRIPT_HASH : S C R I P T '_' H A S H;
K_USING : U S I N G;
K_CONST : C O N S T;
K_ARG : A R G;
K_GLOBAL : G L O B A L;
K_ENDGLOBAL : E N D G L O B A L;
K_SIZE_OF : S I Z E '_' O F;
K_NULL : N U L L;

OP_ADD: '+';
OP_SUBTRACT: '-';
OP_MULTIPLY: '*';
OP_DIVIDE: '/';
OP_MODULO: '%';
OP_OR: '|';
OP_AND: '&';
OP_XOR: '^';
OP_EQUAL: '==';
OP_NOT_EQUAL: '<>';
OP_GREATER: '>';
OP_GREATER_OR_EQUAL: '>=';
OP_LESS: '<';
OP_LESS_OR_EQUAL: '<=';
OP_ASSIGN: '=';
OP_ASSIGN_ADD: '+=';
OP_ASSIGN_SUBTRACT: '-=';
OP_ASSIGN_MULTIPLY: '*=';
OP_ASSIGN_DIVIDE: '/=';
OP_ASSIGN_MODULO: '%=';
OP_ASSIGN_OR: '|=';
OP_ASSIGN_AND: '&=';
OP_ASSIGN_XOR: '^=';

IDENTIFIER
    :   [a-zA-Z_] [a-zA-Z_0-9]*
    ;

FLOAT
    : DECIMAL_INTEGER '.' DIGIT* FLOAT_EXPONENT?
    | '.' DIGIT+ FLOAT_EXPONENT?
    | DECIMAL_INTEGER FLOAT_EXPONENT
    ;

fragment FLOAT_EXPONENT
    : [eE] DECIMAL_INTEGER
    ;

DECIMAL_INTEGER
    :   [-+]? DIGIT+
    ;

HEX_INTEGER
    :   '0x' HEX_DIGIT+
    ;

HASH_STRING
    : UNCLOSED_HASH_STRING '`'
    ;

UNCLOSED_HASH_STRING
    : '`' (~[`\\\r\n] | '\\' (. | EOF))*
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