using System;
using Irony.Interpreter.Ast;
using Irony.Parsing;
using SolScript.Parser.Terminals;

// ReSharper disable InconsistentNaming

namespace SolScript.Parser {
    [Language("SolScript", "0.1", "A truly stellar programming language.")]
    public class SolScriptGrammar : Grammar {
        public SolScriptGrammar() :
            base(true) {
            #region Terminals

            // =======================================
            // === IDENTIFIERS
            IdentifierTerminal _identifier = new IdentifierTerminal("_identifier");
            StringLiteral _string = CreateSolString("_string");
            NumberLiteral _number = CreateSolNumber("_number");
            SolScriptLongStringTerminal _long_string = new SolScriptLongStringTerminal("_long_string");
            SolScriptCommentTerminal Comment = new SolScriptCommentTerminal("Comment");
            NonGrammarTerminals.Add(Comment);
            // =======================================
            // === MEMBER SELECT OPERATORS
            // Single dots are used to implicitly define a string-index operation. They always have the highest precedence.
            KeyTerm DOT = Operator(".");
            DOT.Precedence = 5000;
            DOT.EditorInfo = new TokenEditorInfo(TokenType.Operator, TokenColor.Text, TokenTriggers.MemberSelect);
            // Colons are used to reference types. 
            // todo: should this even be an operator?
            KeyTerm COLON = Operator(":");
            COLON.EditorInfo = new TokenEditorInfo(TokenType.Operator, TokenColor.Text, TokenTriggers.MemberSelect);
            // =======================================
            // === MATHEMATICAL OPERATORS
            KeyTerm EQ = Operator("=");
            KeyTerm MINUS = Operator("-");
            KeyTerm UMINUS = Operator("-");
            KeyTerm PLUS = Operator("+");
            KeyTerm UPLUS = Operator("+");
            KeyTerm CONCAT = Operator("..");
            KeyTerm GETN = Operator("#");
            KeyTerm FDIV = Operator("/");
            KeyTerm IDIV = Operator("//");
            KeyTerm MOD = Operator("%");
            KeyTerm MULT = Operator("*");
            KeyTerm EXP = Operator("^");
            KeyTerm NIL_COAL = Operator("??");
            KeyTerm UPLUSPLUS = Operator("++");
            KeyTerm UMINUSMINUS = Operator("--");
            KeyTerm CMP_EQ = Operator("==");
            KeyTerm CMP_NEQ = Operator("!=");
            KeyTerm CMP_ST = Operator("<");
            KeyTerm CMP_ST_EQ = Operator("<=");
            KeyTerm CMP_GT = Operator(">");
            KeyTerm CMP_GT_EQ = Operator(">=");
            // =======================================
            // === STRUCTURAL OPERATORS
            KeyTerm IF = Keyword("if");
            KeyTerm THEN = Keyword("then");
            KeyTerm ELSEIF = Keyword("elseif");
            KeyTerm ELSE = Keyword("else");
            KeyTerm FOR = Keyword("for");
            KeyTerm IN = Keyword("in");
            KeyTerm FUNCTION = Keyword("function");
            KeyTerm RETURN = Keyword("return");
            KeyTerm BREAK = Keyword("break");
            KeyTerm CONTINUE = Keyword("continue");
            KeyTerm WHILE = Keyword("while");
            KeyTerm NOT = Keyword("not");
            KeyTerm AND = Keyword("and");
            KeyTerm OR = Keyword("or");
            KeyTerm LOCAL = Keyword("local");
            KeyTerm VAR = Keyword("var");
            KeyTerm INTERNAL = Keyword("internal");
            KeyTerm EXTENDS = Keyword("extends");
            KeyTerm DO = Keyword("do");
            KeyTerm END = Keyword("end");
            KeyTerm ELLIPSIS = Keyword("...");
            KeyTerm ANNOTATION = Keyword("annotation");
            KeyTerm SINGLETON = Keyword("singleton");
            KeyTerm ABSTRACT = Keyword("abstract");
            KeyTerm SEALED = Keyword("sealed");
            // =======================================
            // === KEYWORDS
            KeyTerm NIL = Keyword("nil");
            KeyTerm FALSE = Keyword("false");
            KeyTerm TRUE = Keyword("true");

            #endregion

            #region NonTerminals
            
            // =======================================
            // === MISC
            NonTerminal ChunkEnd = new NonTerminal("ChunkEnd");
            NonTerminal ClassDefinition = new NonTerminal("ClassDefinition");
            NonTerminal ClassDefinition_BodyMember_trans = new NonTerminal("ClassDefinition_BodyMember_trans");
            NonTerminal FunctionWithAccess = new NonTerminal("FunctionWithAccess");
            NonTerminal FieldWithAccess = new NonTerminal("FieldWithAccess");
            NonTerminal ClassDefinition_BodyMemberList = new NonTerminal("ClassDefinition_BodyMemberList");
            NonTerminal ClassDefinition_Extends_opt = new NonTerminal("ClassDefinition_Extends_opt");
            NonTerminal ClassDefinition_Body = new NonTerminal("ClassDefinition_Body");
            // Parameters
            NonTerminal ParameterList = new NonTerminal("ParameterList");
            NonTerminal ExplicitParameterList = new NonTerminal("ExplicitParameterList");
            NonTerminal Parameter = new NonTerminal("Parameter");
            NonTerminal Arguments_trans = new NonTerminal("$Arguments_trans");
            NonTerminal AppendOptionalArg_opt_trans = new NonTerminal("$AppendOptionalArg_opt_trans");
            // Types
            NonTerminal TypeInfo = new NonTerminal("TypeInfo");
            NonTerminal TypeInfo_opt = new NonTerminal("TypeInfo_opt");
            NonTerminal TypeInfo_Nullable = new NonTerminal("TypeInfo_Nullable");
            NonTerminal TypeInfo_Nullable_opt = new NonTerminal("TypeInfo_Nullable_opt");
            NonTerminal TypeRef = new NonTerminal("TypeRef");
            NonTerminal TypeRef_opt = new NonTerminal("TypeRef_opt");

            // =======================================
            // === STATEMENTS
            NonTerminal Statement = new NonTerminal("Statement");
            NonTerminal StatementList = new NonTerminal("StatementList");
            NonTerminal LastStatement = new NonTerminal("LastStatement");
            NonTerminal Statement_CallFunction = new NonTerminal("Statement_CallFunction");
            // Functions
            NonTerminal Statement_DeclareFunc = new NonTerminal("Statement_DeclareFunc");
            NonTerminal Statement_DeclareFunc_Local = new NonTerminal("Statement_DeclareFunc_Local");
            NonTerminal Statement_DeclareFunc_Global = new NonTerminal("Statement_DeclareFunc_Global");
            // Variables / Objects
            NonTerminal Statement_AssignVar = new NonTerminal("Statement_AssignVar");
            NonTerminal Statement_DeclareVar = new NonTerminal("Statement_DeclareVar");
            NonTerminal Statement_New = new NonTerminal("Statement_New");
            // Conditions / Iterations / Loops
            NonTerminal Statement_Conditional = new NonTerminal("Statement_Conditional");
            NonTerminal Statement_Conditional_ElseIf = new NonTerminal("Statement_Conditional_ElseIf");
            NonTerminal Statement_Conditional_ElseIfList = new NonTerminal("Statement_Conditional_ElseIfList*");
            NonTerminal Statement_Conditional_Else_opt = new NonTerminal("Statement_Conditional_Else_opt");
            NonTerminal Statement_For = new NonTerminal("Statement_For");
            NonTerminal Statement_Iterate = new NonTerminal("Statement_Iterate");
            NonTerminal Statement_While = new NonTerminal("Statement_While");
            NonTerminal Statement_Do = new NonTerminal("Statement_Do");

            // =======================================
            // === EXPRESSIONS
            NonTerminal Expression = new NonTerminal("Expression");
            NonTerminal Expression_Binary = new NonTerminal("Expression_Binary");
            NonTerminal Expression_Binary_Operand_trans = new NonTerminal("$BinaryExpression_Operand_trans");
            NonTerminal Expression_Unary = new NonTerminal("Expression_Unary");
            NonTerminal Expression_Unary_Operand_trans = new NonTerminal("$Expression_Unary_Operand_trans");
            NonTerminal Expression_GetVariable = new NonTerminal("Expression_GetVariable");
            NonTerminal Expression_Bool = new NonTerminal("Expression_Bool");
            NonTerminal Expression_Nil = new NonTerminal("Expression_Nil");
            NonTerminal Expression_Parenthetical = new NonTerminal("Expression_Parenthetical");
            NonTerminal FunctionParameters = new NonTerminal("FunctionParameters");
            NonTerminal Chunk = new NonTerminal("Chunk");
            NonTerminal Function_Name = new NonTerminal("Function_Name");
            NonTerminal VariableList = new NonTerminal("VariableList");
            NonTerminal NameList = new NonTerminal("name list");
            NonTerminal ExpressionList = new NonTerminal("ExpressionList");
            NonTerminal Expression_CreateFunc = new NonTerminal("Expression_CreateFunc");
            NonTerminal FunctionBody = new NonTerminal("FunctionBody");
            NonTerminal Expression_TableConstructor = new NonTerminal("Expression_TableConstructor");
            NonTerminal Field = new NonTerminal("Field");
            NonTerminal FieldList = new NonTerminal("FieldList");
            NonTerminal FieldSeparator = new NonTerminal("FieldSeparator");
            NonTerminal Expression_Statement = new NonTerminal("Expression_Statement");
            NonTerminal NamedVariable = new NonTerminal("NamedVariable");
            NonTerminal IndexedVariable = new NonTerminal("IndexedVariable");
            NonTerminal Variable = new NonTerminal("Variable");
            NonTerminal Assignment_opt = new NonTerminal("Assignment_opt");
            NonTerminal IdentifierPlusList = new NonTerminal("IdentifierPlusList");
            // Misc
            NonTerminal ClassModifier_opt = new NonTerminal("ClassModifier_opt");
            NonTerminal AccessModifier_opt = new NonTerminal("AccessModifier_opt");
            NonTerminal Annotation = new NonTerminal("Annotation");
            NonTerminal Annotation_opt = new NonTerminal("Annotation_opt");
            NonTerminal AnnotationList = new NonTerminal("AnnotationList");
            NonTerminal RootElement_trans = new NonTerminal("RootElement_trans");
            Root = new NonTerminal("ROOT");

            #endregion
            // todo: tertiary expression for: (a ? b : c)
            #region Grammar Rules
            RootElement_trans.Rule = FieldWithAccess
                | FunctionWithAccess
                | ClassDefinition
                ;
            Root.Rule = MakeStarRule(Root, RootElement_trans)
                ;
            AccessModifier_opt.Rule =
                Empty
                | INTERNAL
                | LOCAL
                ;
            ClassModifier_opt.Rule = 
                Empty 
                | ANNOTATION
                | SINGLETON
                | ABSTRACT
                | SEALED
                ;
            Expression_Bool.Rule = 
                TRUE 
                | FALSE
                ;
            Expression_Nil.Rule = 
                NIL
                ;
            IdentifierPlusList.Rule =
                MakePlusRule(IdentifierPlusList, ToTerm(","), _identifier)
                ;
            ClassDefinition.Rule =
                AnnotationList + ClassModifier_opt + ToTerm("class") + _identifier + ClassDefinition_Extends_opt + ClassDefinition_Body
                ;
            ClassDefinition_Extends_opt.Rule =
                EXTENDS + _identifier
                | Empty
                ;
            ClassDefinition_Body.Rule =
                ClassDefinition_BodyMemberList + END
                ;
            ClassDefinition_BodyMemberList.Rule =
                MakeStarRule(ClassDefinition_BodyMemberList, ClassDefinition_BodyMember_trans)
                ;
            ClassDefinition_BodyMember_trans.Rule =
                FieldWithAccess
                | FunctionWithAccess
                ;
            FieldWithAccess.Rule =
                AnnotationList + AccessModifier_opt + _identifier + TypeRef_opt + Assignment_opt
                ;
            Assignment_opt.Rule =
                EQ + Expression
                | Empty
                ;
            FunctionWithAccess.Rule =
                AnnotationList + AccessModifier_opt + FUNCTION + _identifier + FunctionParameters + TypeRef_opt + FunctionBody
                ;
            AnnotationList.Rule =
                MakeStarRule(AnnotationList, Annotation)
                ;
            Annotation.Rule =
                ToTerm("@") + _identifier + Arguments_trans
                | ToTerm("@") + _identifier
                ;
            Annotation_opt.Rule =
                Empty
                | Annotation
                ;
            StatementList.Rule =
                MakeStarRule(StatementList, Statement)
                ;
            ChunkEnd.Rule =
                Empty
                | LastStatement
                ;
            Chunk.Rule =
                StatementList + ChunkEnd
                ;
            Statement_DeclareVar.Rule =
                VAR + _identifier + TypeRef_opt + EQ + Expression
                | VAR + _identifier + TypeRef_opt
                ;
            Statement_AssignVar.Rule =
                Variable + EQ + Expression
                ;
            Statement_DeclareFunc.Rule =
                Statement_DeclareFunc_Local
                | Statement_DeclareFunc_Global;
            Statement_DeclareFunc_Global.Rule =
                FUNCTION + Variable + FunctionParameters + TypeRef_opt + FunctionBody
                ;
            Statement_DeclareFunc_Local.Rule =
                LOCAL + FUNCTION + Variable + FunctionParameters + TypeRef_opt + FunctionBody
                ;
            Expression_CreateFunc.Rule =
                FUNCTION + FunctionParameters+ TypeRef_opt  + FunctionBody
                ;
            FunctionParameters.Rule =
                "(" + ParameterList + ")" | "(" + ")"
                ;
            FunctionBody.Rule =
                Chunk + END
                ;
            Statement_New.Rule =
                ToTerm("new") + _identifier + Arguments_trans
                ;
            Statement_Conditional_Else_opt.Rule =
                Empty
                | ELSE + Chunk
                ;
            Statement_Conditional_ElseIf.Rule =
                ELSEIF + Expression + THEN + Chunk
                ;
            Statement_Conditional_ElseIfList.Rule =
                MakeStarRule(Statement_Conditional_ElseIfList, null, Statement_Conditional_ElseIf)
                ;
            Statement_Conditional.Rule =
                IF + Expression + THEN + Chunk + Statement_Conditional_ElseIfList + Statement_Conditional_Else_opt + END
                ;
            Statement_For.Rule =
                FOR + Statement + ToTerm(",") + Expression + ToTerm(",") + Statement + DO + Chunk + END
                ;
            Statement_Iterate.Rule =
                FOR + _identifier + IN + Expression + DO + Chunk + END
                ;
            Statement_While.Rule =
                WHILE + Expression + DO + Chunk + END
                ;
            Statement.Rule =
                Statement_AssignVar
                | Statement_DeclareVar
                | Statement_CallFunction
                | Statement_Conditional
                | Statement_For
                | Statement_Iterate
                | Statement_While
                | Statement_Do
                | Statement_DeclareFunc
                | Statement_New
                ;
            LastStatement.Rule =
                RETURN + Expression
                | RETURN 
                | BREAK
                | CONTINUE;
            Function_Name.Rule =
                MakePlusRule(Function_Name, DOT, _identifier)
                ;
            VariableList.Rule =
                MakePlusRule(VariableList, ToTerm(","), Expression_GetVariable)
                ;
            NameList.Rule =
                MakePlusRule(NameList, ToTerm(","), _identifier)
                ;
            ExpressionList.Rule =
                MakeStarRule(ExpressionList, ToTerm(","), Expression)
                ;
            Expression_Statement.Rule =
                Statement_CallFunction
                | Statement_Conditional
                | Statement_New
                | Statement_AssignVar
                ;
            Expression.Rule =
                 _number
                | Expression_Bool
                | Expression_Nil
                | _string
                | _long_string
                | Expression_Unary
                | Expression_Binary
                | ELLIPSIS
                | Expression_GetVariable
                | Expression_CreateFunc
                | Expression_Statement
                | Expression_Parenthetical
                | Expression_TableConstructor
                ;
            Expression_GetVariable.Rule =
                Variable
                ;
            Variable.Rule =
                NamedVariable
                | IndexedVariable
                ;
            NamedVariable.Rule =
                _identifier
                ;
            IndexedVariable.Rule =
                Expression + "[" + Expression + "]"
                | Expression + DOT + _identifier
                ;
            Expression_Parenthetical.Rule =
                "(" + Expression + ")"
                ;
            Statement_Do.Rule =
                DO + Chunk + END
                ;
            Statement_CallFunction.Rule =
                Expression + Arguments_trans
                | Expression + "()"
                ;
            Arguments_trans.Rule =
                "(" + ExpressionList + ")"
                ;
            TypeInfo.Rule =
                _identifier + TypeInfo_Nullable_opt
                ;
            TypeInfo_opt.Rule =
                Empty
                | TypeInfo
                ;
            TypeInfo_Nullable.Rule =
                ToTerm("?")
                | ToTerm("!")
                ;
            TypeInfo_Nullable_opt.Rule =
                Empty
                | TypeInfo_Nullable
                ;
            TypeRef.Rule =
                COLON + TypeInfo
                ;
            TypeRef_opt.Rule =
                Empty
                | TypeRef
                ;
            ExplicitParameterList.Rule =
                MakePlusRule(ExplicitParameterList, ToTerm(","), Parameter)
                ;
            Parameter.Rule =
                _identifier + TypeRef_opt
                ;
            AppendOptionalArg_opt_trans.Rule =
                Empty
                | ToTerm(",") + ELLIPSIS
                ;
            ParameterList.Rule =
                ExplicitParameterList + AppendOptionalArg_opt_trans
                | ELLIPSIS
                ;
            Expression_TableConstructor.Rule =
                "{" + FieldList + "}"
                ;
            FieldList.Rule =
                MakeStarRule(FieldList, FieldSeparator, Field)
                ;
            Field.Rule =
                "[" + Expression + "]" + "=" + Expression
                | _identifier + "=" + Expression
                | Expression
                ;
            FieldSeparator.Rule =
                ToTerm(",")
                | ";"
                ;
            Expression_Binary.Rule =
                Expression + Expression_Binary_Operand_trans + Expression
                | ToTerm("(") + Expression + Expression_Binary_Operand_trans + Expression + ToTerm(")")
                ;
            Expression_Unary.Rule =
                Expression_Unary_Operand_trans + Expression
                ;
            Expression_Binary_Operand_trans.Rule =
                PLUS
                | MINUS
                | MULT
                | FDIV
                | IDIV
                | EXP
                | MOD
                | CONCAT
                | CMP_EQ
                | CMP_NEQ
                | CMP_GT
                | CMP_GT_EQ
                | CMP_ST
                | CMP_ST_EQ
                | AND
                | OR
                | NIL_COAL
                ;
            Expression_Unary_Operand_trans.Rule =
                UPLUSPLUS 
                | UMINUSMINUS 
                | UPLUS
                | UMINUS 
                | NOT 
                | "!" 
                | GETN
                ;
            MarkTransient(
                ClassDefinition_BodyMember_trans,
                RootElement_trans,
                Expression_Binary_Operand_trans,
                Expression_Unary_Operand_trans,
                AppendOptionalArg_opt_trans,
                Arguments_trans);

            #endregion

            #region Braces, Keywords and Symbols

            RegisterBracePair("(", ")");
            RegisterBracePair("{", "}");
            RegisterBracePair("[", "]");

            MarkPunctuation(COLON, DOT, EQ, IF, THEN, ELSE, ELSEIF);
            MarkPunctuation(",", ";");
            MarkPunctuation("(", ")");
            MarkPunctuation("{", "}");
            MarkPunctuation("[", "]");

            RegisterOperators(1, Associativity.Left, OR);
            RegisterOperators(2, Associativity.Left, AND);
            RegisterOperators(3, Associativity.Left, CMP_GT, CMP_GT_EQ, CMP_ST, CMP_ST_EQ, CMP_EQ, CMP_NEQ);
            RegisterOperators(4, Associativity.Left, CONCAT);
            RegisterOperators(5, Associativity.Left, NIL_COAL);
            RegisterOperators(6, Associativity.Left, MINUS, PLUS);
            RegisterOperators(7, Associativity.Left, MULT, FDIV, MOD);
            RegisterOperators(8, Associativity.Left, NOT, UMINUS, UPLUS);
            RegisterOperators(9, Associativity.Right, UPLUSPLUS, UMINUSMINUS, EXP, GETN);
            RegisterOperators(9, Associativity.Left, UPLUSPLUS, UMINUSMINUS);
            RegisterOperators(10, Associativity.Left, DOT);

            #endregion

            LanguageFlags = LanguageFlags.SupportsCommandLine | LanguageFlags.TailRecursive;
        }
        
        public KeyTerm Keyword(string keyword) {
            KeyTerm term = ToTerm(keyword);
            term.SetFlag(TermFlags.IsKeyword, true);
            term.SetFlag(TermFlags.IsReservedWord, true);
            term.EditorInfo = new TokenEditorInfo(TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

            return term;
        }

        public KeyTerm Operator(string op) {
            string opCased = CaseSensitive ? op : op.ToLower();
            KeyTerm term = new KeyTerm(opCased, op);
            term.SetFlag(TermFlags.IsOperator, true);
            term.EditorInfo = new TokenEditorInfo(TokenType.Operator, TokenColor.Keyword, TokenTriggers.None);

            return term;
        }

        protected static NumberLiteral CreateSolNumber(string name) {
            NumberLiteral term = new NumberLiteral(name, NumberOptions.AllowStartEndDot);
            //default int types are Integer (32bit) -> LongInteger (BigInt); Try Int64 before BigInt: Better performance?
            term.DefaultIntTypes = new[] {TypeCode.Int32, TypeCode.Int64, NumberLiteral.TypeCodeBigInt};
            term.DefaultFloatType = TypeCode.Double; // it is default
            term.AddPrefix("0x", NumberOptions.Hex);

            return term;
        }

        protected static StringLiteral CreateSolString(string name) {
            //return new SolScriptStringLiteral(name);
            var strLit = new StringLiteral(name);
            strLit.AddStartEnd("'", "'", StringOptions.AllowsDoubledQuote | StringOptions.NoEscapes);
            strLit.AddStartEnd("\"", "\"", StringOptions.NoEscapes);
            return strLit;
        }
    }
}