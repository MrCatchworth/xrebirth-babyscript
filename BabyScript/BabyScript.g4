grammar BabyScript;
import BabyScriptLexing;

options
{
    language=CSharp;
}

@header
{
    using System.Linq;
    using System.Text.RegularExpressions;
}

@parser::members
{
    private static Regex ElementNameRegex = new Regex("[A-Za-z][a-zA-Z0-9_]*");
    
    private bool NextTokenValidId()
    {
        IToken nextToken = CurrentToken;
        return nextToken != null && ElementNameRegex.Match(nextToken.Text).Success;
    }
}

document returns [BabyElement root]
    : nodeList+=node+ EOF { $nodeList.Count(c => c.treeNode is BabyElement) == 1 }?
        {
            $root = (BabyElement)($nodeList.First(c => c.treeNode is BabyElement).treeNode);
        }
    ;

expr returns [string fullText]
    @after
    {
        //Console.Error.WriteLine("Hello from line {0}", $start.Line);
        ICharStream stream = $start.InputStream;
        $fullText = stream.GetText(new Interval($start.StartIndex, $stop.StopIndex));
    }
    : IF expr THEN expr (ELSE expr)?
	| PLUS expr
    | MINUS expr
    | TABLE '[' tableDef? ']' //table initialiser
    | (NOT|TYPEOF) expr //not, typeof
    | (SIN|COS|SQRT) OPPAREN expr CLPAREN //sin, cos, sqrt
    | expr TIMES expr
    | expr DIVIDE expr
    | expr MODULO expr
    | expr PLUS expr
    | expr MINUS expr
    | expr (LT|LT2) expr
    | expr (LE|LE2) expr
    | expr (GT|GT2) expr
    | expr (GE|GE2) expr
    | expr EQUAL expr
    | expr NEQUAL expr
    | expr (AND|AND2) expr
    | expr (OR|OR2) expr
    | '@' expr
    | expr '?'
    | atom ('.' atom)* //atom and optional lookups
    ;
    
exprEof
    : expr EOF
    ;
    
tableDef
    : tableRowDef (',' tableRowDef)*
    ;
    
tableRowDef
    : ( {CurrentToken.Text[0] == '$'}? ID | '{' expr '}' ) '=' expr
    ;

squareList
    : '[' (expr (',' expr)*)? ']'
    ;

braceList
    : '{' (expr (',' expr)*)? '}'
    ;
    
atom
    : NUMBER
    | SINGLE_QUOTE_STRING
    | squareList
    | braceList
    | OPPAREN expr CLPAREN UNITCAST? //subexpression with optional type cast
    // | ID
	| {NextTokenValidId();} . //this is so messy
    ;

node returns [BabyNode treeNode]
    : {NextTokenValidId()}? eleName=. elementAttributes nodeChildren
        {
            $treeNode = new BabyElement($eleName.text, $elementAttributes.attrs, $nodeChildren.nodes);
        } # Element
    | leftHand=expr '=' rightHand=expr ';'
        {
            $treeNode = BabyElement.CreateAssignment($leftHand.fullText, $rightHand.fullText);
        } # Element
	| BLOCK_COMMENT
		{
			$treeNode = new BabyComment($BLOCK_COMMENT.text.Substring(2, $BLOCK_COMMENT.text.Length-4));
		} # Comment
    ;

elementAttributes returns [BabyAttribute[] attrs]
    : OPPAREN (rawList+=attribute (',' rawList+=attribute)*)? CLPAREN
        {
            $attrs = $rawList.Select(a => a.attr).ToArray();
        }
    |
        {
            $attrs = new BabyAttribute[0];
        }
    ;

attribute returns [BabyAttribute attr]
    : {NextTokenValidId()}? attrName=. ':' DOUBLE_QUOTE_STRING
        {
            $attr = new BabyAttribute($attrName.text, $DOUBLE_QUOTE_STRING.text);
        }
    | {NextTokenValidId()}? attrName=. ':' expr
        {
            $attr = new BabyAttribute($attrName.text, $expr.fullText);
        }
    | expr
        {
            //Console.WriteLine("Found an anonymous attribute: {0}", $expr.fullText);
            $attr = new BabyAttribute(null, $expr.fullText);
        }
	| DOUBLE_QUOTE_STRING
		{
			$attr = new BabyAttribute(null, $DOUBLE_QUOTE_STRING.text);
		}
    ;

nodeChildren returns [BabyNode[] nodes]
    : ';'
        {
            $nodes = new BabyNode[0];
        }
    | '{' rawList+=node* '}'
        {
            $nodes = $rawList.Select(n => n.treeNode).ToArray();
        }
    ;