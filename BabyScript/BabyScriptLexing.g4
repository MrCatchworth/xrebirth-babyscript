lexer grammar BabyScriptLexing;

@header
{
}

@lexer::members
{
    private int ParenDepth = 0;
}

CLPAREN : ')'
    {
        ParenDepth--;
    }
    ;
OPPAREN : '('
    {
        ParenDepth++;
    }
    ;

COMMENT : '//' ~('\r' | '\n')* -> skip ; //the newlines don't have to be part of the actual comment, those are skipped as whitespace anyway
WS : [ \t\r\n]+ -> skip ;

NUMBER : '-'? ('0'..'9')+ ('.' ('0' .. '9')*)? ('e' ('0' .. '9')+)? UNITCAST?;
DOUBLE_QUOTE_STRING : '"' (~('\\'|'"') | '\\'. )* '"' ;
SINGLE_QUOTE_STRING : '\'' (~('\\'|'\'') | '\\'. )* '\'' ;
PLUS : '+';
MINUS : '-';
TIMES : '*';
DIVIDE : '/';
MODULO : '%';
LT : '<';
LT2 : 'lt';
LE : '<=';
LE2 : 'le';
GT : '>';
GT2 : 'gt';
GE : '>=';
GE2 : 'ge';
EQUAL : '==';
NEQUAL : '!=';
AND : '&&';
AND2 : 'and';
OR : '||';
OR2 : 'or';

NOT :           'not';
TYPEOF :        'typeof';

SIN :           'sin';
COS :           'cos';
SQRT :          'sqrt';

IF :            'if';
THEN :          'then';
ELSE :          'else';

TABLE :         'table';

UNITCAST
    : 'i'
    | 'L'
    | 'f'
    | 'LF'
    | 'ct'
    | 'Cr'
    | 'm'
    | 'km'
    | 'deg'
    | 'rad'
    | 'hp'
    | 'ms'
    | 's'
    | 'min'
    | 'h'
    ;

ID : '$'?[A-Za-z] [A-Za-z0-9_]* ;