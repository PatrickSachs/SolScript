﻿using System;
using Irony.Interpreter.Ast;
using Irony.Parsing;
using SolScript.Parser.Terminals;

// ReSharper disable InconsistentNaming

namespace SolScript.Parser {
    [Language("SolScript", "0.1", "A truly stellar programming language.")]
    public class SolScriptGrammar : Grammar {
        public SolScriptGrammar() :
            base(false) {
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
            //KeyTerm BREAK = Keyword("break");
            KeyTerm WHILE = Keyword("while");
            KeyTerm NOT = Keyword("not");
            KeyTerm AND = Keyword("and");
            KeyTerm OR = Keyword("or");
            KeyTerm LOCAL = Keyword("local");
            KeyTerm DO = Keyword("do");
            KeyTerm END = Keyword("end");
            KeyTerm ELLIPSIS = Keyword("...");
            // =======================================
            // === KEYWORDS
            ConstantTerminal NIL = new ConstantTerminal("nil");
            ConstantTerminal FALSE = new ConstantTerminal("false");
            ConstantTerminal TRUE = new ConstantTerminal("true");

            #endregion

            #region NonTerminals
            
            // =======================================
            // === MISC
            NonTerminal ChunkEnd = new NonTerminal("ChunkEnd");
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
            NonTerminal StatementWithTerminator_opt = new NonTerminal("$StatementWithTerminator_opt",
                Statement | Statement + ";");
            NonTerminal LastStatementWithTerminator_opt_trans = new NonTerminal(
                "$LastStatementWithTerminator_opt_trans", LastStatement | LastStatement + ";");
            // Functions
            NonTerminal Statement_DeclareFunc = new NonTerminal("Statement_DeclareFunc");
            NonTerminal Statement_DeclareFunc_Local = new NonTerminal("Statement_DeclareFunc_Local");
            NonTerminal Statement_DeclareFunc_Global = new NonTerminal("Statement_DeclareFunc_Global");
            // Variables / Objects
            NonTerminal Statement_AssignVar = new NonTerminal("Statement_AssignVar");
            NonTerminal Statement_DeclareVar = new NonTerminal("Statement_DeclareVar");
            NonTerminal Statement_DeclareVar_Local = new NonTerminal("Statement_DeclareVar_Local");
            NonTerminal Statement_DeclareVar_Global = new NonTerminal("Statement_DeclareVar_Global");
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

            NonTerminal Expression_Parenthetical = new NonTerminal("Expression_Parenthetical");
            NonTerminal FunctionParameters = new NonTerminal("FunctionParameters");
            NonTerminal Chunk = new NonTerminal("Chunk");
            NonTerminal Function_Name = new NonTerminal("Function_Name");
            NonTerminal VariableList = new NonTerminal("VariableList");
            NonTerminal NameList = new NonTerminal("name list");
            NonTerminal ExpressionList = new NonTerminal("ExpressionList");
            NonTerminal Statement_CallFunction = new NonTerminal("Statement_CallFunction");
            NonTerminal Expression_DeclareFunc = new NonTerminal("Expression_CreateFunc");
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
            NonTerminal ClassDefinition = new NonTerminal("ClassDefinition");
            NonTerminal ClassDefinitionList = new NonTerminal("ClassDefinitionList");
            NonTerminal ClassDefinition_BodyMember = new NonTerminal("ClassDefinition_BodyMember");
            NonTerminal ClassDefinition_BodyMember_Function = new NonTerminal("ClassDefinition_BodyMember_Function");
            NonTerminal ClassDefinition_BodyMember_Function_Local =
                new NonTerminal("ClassDefinition_BodyMember_Function_Local");
            NonTerminal ClassDefinition_BodyMember_Function_Global =
                new NonTerminal("ClassDefinition_BodyMember_Function_Global");
            NonTerminal ClassDefinition_BodyMember_Variable = new NonTerminal("ClassDefinition_BodyMember_Variable");
            NonTerminal ClassDefinition_BodyMember_Variable_Local =
                new NonTerminal("ClassDefinition_BodyMember_Variable_Local");
            NonTerminal ClassDefinition_BodyMember_Variable_Global =
                new NonTerminal("ClassDefinition_BodyMember_Variable_Global");
            NonTerminal ClassDefinition_BodyMemberList = new NonTerminal("ClassDefinition_BodyMemberList");
            NonTerminal ClassDefinition_Mixins_opt = new NonTerminal("ClassDefinition_Mixins_opt");
            NonTerminal ClassDefinition_Body = new NonTerminal("ClassDefinition_Body");
            NonTerminal IdentifierPlusList = new NonTerminal("IdentifierPlusList");
            NonTerminal Annotation_opt = new NonTerminal("Annotation_opt");
            NonTerminal Annotation = new NonTerminal("Annotation");
            NonTerminal AnnotationList = new NonTerminal("AnnotationList");

            #endregion

            #region Grammar Rules

            Root =
                ClassDefinitionList;
            IdentifierPlusList.Rule =
                MakePlusRule(IdentifierPlusList, ToTerm(","), _identifier)
                ;
            ClassDefinitionList.Rule =
                MakeStarRule(ClassDefinitionList, ClassDefinition)
                ;
            ClassDefinition.Rule =
                AnnotationList + ToTerm("class") + _identifier + ClassDefinition_Mixins_opt + ClassDefinition_Body
                ;
            ClassDefinition_Mixins_opt.Rule =
                ToTerm("mixin") + IdentifierPlusList
                | Empty
                ;
            ClassDefinition_Body.Rule =
                ClassDefinition_BodyMemberList + END
                ;
            ClassDefinition_BodyMemberList.Rule =
                MakeStarRule(ClassDefinition_BodyMemberList, ClassDefinition_BodyMember)
                ;
            ClassDefinition_BodyMember.Rule =
                ClassDefinition_BodyMember_Variable
                | ClassDefinition_BodyMember_Function
                ;
            ClassDefinition_BodyMember_Variable.Rule =
                ClassDefinition_BodyMember_Variable_Global
                | ClassDefinition_BodyMember_Variable_Local
                ;
            ClassDefinition_BodyMember_Variable_Global.Rule =
                _identifier + TypeRef + Assignment_opt
                ;
            ClassDefinition_BodyMember_Variable_Local.Rule =
                LOCAL + _identifier + TypeRef + Assignment_opt
                ;
            Assignment_opt.Rule =
                EQ + Expression
                | Empty
                ;
            ClassDefinition_BodyMember_Function.Rule =
                ClassDefinition_BodyMember_Function_Global
                | ClassDefinition_BodyMember_Function_Local
                ;
            ClassDefinition_BodyMember_Function_Global.Rule =
                FUNCTION + _identifier + FunctionParameters + TypeRef_opt + FunctionBody
                ;
            ClassDefinition_BodyMember_Function_Local.Rule =
                LOCAL + FUNCTION + _identifier + FunctionParameters + TypeRef_opt + FunctionBody
                ;
            AnnotationList.Rule =
                MakeStarRule(AnnotationList, Annotation)
                ;
            Annotation.Rule =
                ToTerm("@") + _identifier + Arguments_trans;
            Annotation_opt.Rule =
                Empty
                | Annotation
                ;
            StatementList.Rule =
                MakeStarRule(StatementList, StatementWithTerminator_opt)
                ;
            ChunkEnd.Rule =
                Empty
                | LastStatementWithTerminator_opt_trans
                ;
            Chunk.Rule =
                StatementList + ChunkEnd
                ;
            Statement_DeclareVar.Rule =
                Statement_DeclareVar_Local
                | Statement_DeclareVar_Global
                ;
            Statement_DeclareVar_Local.Rule =
                LOCAL + _identifier + TypeRef_opt + EQ + Expression
                | LOCAL + _identifier + TypeRef_opt
                ;
            Statement_DeclareVar_Global.Rule =
                _identifier + TypeRef_opt + EQ + Expression
                | _identifier + TypeRef_opt
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
            Expression_DeclareFunc.Rule =
                FUNCTION + FunctionParameters + FunctionBody
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
                /*| BREAK*/;
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
                | Expression_Unary
                | Expression_Binary
                | _string
                | _long_string
                | ELLIPSIS
                | Expression_DeclareFunc
                | Expression_Statement
                | Expression_Parenthetical
                | Expression_TableConstructor
                | Expression_GetVariable
                | NIL
                | FALSE
                | TRUE
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
                ;
            Expression_Unary_Operand_trans.Rule =
                UPLUS
                | UMINUS 
                | NOT 
                | "!" 
                | GETN
                ;
            MarkTransient(
                StatementWithTerminator_opt,
                Expression_Binary_Operand_trans,
                Expression_Unary_Operand_trans,
                AppendOptionalArg_opt_trans,
                Arguments_trans,
                LastStatementWithTerminator_opt_trans);

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
            RegisterOperators(5, Associativity.Left, MINUS, PLUS);
            RegisterOperators(6, Associativity.Left, MULT, FDIV, MOD);
            RegisterOperators(7, Associativity.Left, NOT, UMINUS, UPLUS);
            RegisterOperators(8, Associativity.Right, EXP);
            RegisterOperators(9, Associativity.Left, DOT);

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
            strLit.AddStartEnd("'", "'", StringOptions.AllowsDoubledQuote);
            strLit.AddStartEnd("\"", "\"", StringOptions.None);
            return strLit;
        }
    }
}