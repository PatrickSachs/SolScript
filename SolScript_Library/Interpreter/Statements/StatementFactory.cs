using System;
using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Statements {
    public class StatementFactory {
        public readonly SolAssembly Assembly;
        public StatementFactory(SolAssembly assembly) {
            Assembly = assembly;
        }

        public SolClassBuilder[] GetClassDefinitions(ParseTreeNode node) {
            if (node.Term.Name != "ClassDefinitionList") {
                throw SolScriptInterpreterException.Raw(null, node.Span.Location + " : Invalid class list node type " +
                                                              node.Term.Name + "!");
            }
            var array = new SolClassBuilder[node.ChildNodes.Count];
            for (int i = 0; i < array.Length; i++) {
                array[i] = GetClassDefinition(node.ChildNodes[i]);
            }
            return array;
        }

        /*public ClassDef.AnnotationDef GetAnnotation(ParseTreeNode node) {
            string name = node.ChildNodes[1].Token.Text;
            var arguments = node.ChildNodes.Count == 3
                ? GetExpressions(node.ChildNodes[2])
                : new SolExpression[0];
            return new ClassDef.AnnotationDef(name, arguments);
        }

        public ClassDef.AnnotationDef[] GetAnnotations(ParseTreeNode node) {
            var annotations = new ClassDef.AnnotationDef[node.ChildNodes.Count];
            for (int i = 0; i < node.ChildNodes.Count; i++) {
                annotations[i] = GetAnnotation(node.ChildNodes[i]);
            }
            return annotations;
        }*/

        public SolParameter[] GetParameters(ParseTreeNode node, out bool allowOptional) {
            ParseTreeNode parametersListNode = node.ChildNodes[0];
            if (parametersListNode.ChildNodes.Count == 0) {
                allowOptional = false;
                return new SolParameter[0];
            }
            // Tree for args:
            // - ParameterList
            //  - ExplicitParameterList
            //   - Parameter
            //   - Parameter
            //  ...
            ParseTreeNode optionalNode = parametersListNode.ChildNodes.FindChildByName("...");
            ParseTreeNode explicitNode =
                parametersListNode.ChildNodes.FindChildByName("ExplicitParameterList");
            allowOptional = optionalNode != null;
            return explicitNode == null
                ? new SolParameter[0]
                : GetExplicitParameters(explicitNode);
        }

        public SolClassBuilder GetClassDefinition(ParseTreeNode node) {
            if (node.Term.Name != "ClassDefinition") {
                throw SolScriptInterpreterException.Raw(null, node.Span.Location + " : Invalid class node type " +
                                                              node.Term.Name + "!");
            }
            // ===================================================================
            // ClassDefinition
            //  - AnnotationList
            //  - "class"
            //  - ClassModifier_opt
            //  - _identifier
            //  - ClassDefinition_Extends_opt
            //  - ClassDefinition_Body
            SolTypeMode typeMode;
            ParseTreeNode typeModeNode = node.ChildNodes[2];
            if (typeModeNode.ChildNodes.Count == 0) {
                typeMode = SolTypeMode.Default;
            } else {
                switch (typeModeNode.ChildNodes[0].Term.Name) {
                    case "abstract": {
                        typeMode = SolTypeMode.Abstract;
                        break;
                    }
                    case "sealed": {
                        typeMode = SolTypeMode.Sealed;
                        break;
                    }
                    case "annotation": {
                        typeMode = SolTypeMode.Annotation;
                        break;
                    }
                    case "singleton": {
                        typeMode = SolTypeMode.Singleton;
                        break;
                    }
                    default: {
                        throw new ArgumentOutOfRangeException(nameof(typeModeNode));
                    }
                }
            }

            ParseTreeNode classNameNode = node.ChildNodes[3];
            string className = classNameNode.Token.Text;
            // ===================================================================
            // ClassDefinition_Mixins_opt
            //  - Empty
            // ClassDefinition_Mixins_opt
            //  - "extends"
            //  - _identifier
            ParseTreeNode mixinsNode = node.ChildNodes[4];
            // ===================================================================
            // == Annotations
            // AnnotationList
            //   - Empty | Annotation+
            // Annotation
            //   - "@"
            //   - _identifier
            //   - Arguments_trans
            // todo: annotations
            SolClassBuilder classBuilder = new SolClassBuilder(className, typeMode);
            if (mixinsNode.ChildNodes.Count != 0) {
                classBuilder.Extends(mixinsNode.ChildNodes[1].Token.Text);
            }
            // ===================================================================
            ParseTreeNode bodyMemberListNode = node.ChildNodes[5].ChildNodes[0];
            // ClassDefinition_Body
            //  - ClassDefinition_BodyMemberList
            //     - ClassDefinition_BodyMember*
            //  - "end"
            foreach (ParseTreeNode rawBodyMemberNode in bodyMemberListNode.ChildNodes) {
                // ClassDefinition_BodyMember
                //  - AnnotationList
                //  - ClassDefinition_BodyMember_Variable | ClassDefinition_BodyMember_Function
                // todo: annotations
                //var memberAnnotations = GetAnnotations(rawBodyMemberNode.ChildNodes[0]);
                ParseTreeNode childNode = rawBodyMemberNode.ChildNodes[1];
                switch (childNode.Term.Name) {
                    // ===================================================================
                    case "ClassDefinition_BodyMember_Variable": {
                        // ClassDefinition_BodyMember_Variable
                        //  - ClassDefinition_BodyMember_Variable_Global | ClassDefinition_BodyMember_Variable_Local
                        ParseTreeNode underlying = childNode.ChildNodes[0];
                        ParseTreeNode assignmentOpt;
                        string fieldName;
                        SolType fieldType;
                        bool fieldLocal;
                        switch (underlying.Term.Name) {
                            case "ClassDefinition_BodyMember_Variable_Local": {
                                // ClassDefinition_BodyMember_Variable_Local
                                //  - "local"
                                //  - _identifier
                                //  - TypeRef_opt
                                //  - Assignment_opt
                                //     - Expression
                                fieldName = underlying.ChildNodes[1].Token.Text;
                                fieldType = GetTypeRef(underlying.ChildNodes[2]);
                                assignmentOpt = underlying.ChildNodes[3];
                                fieldLocal = true;
                                break;
                            }
                            case "ClassDefinition_BodyMember_Variable_Global": {
                                // ClassDefinition_BodyMember_Variable_Global
                                //  - _identifier
                                //  - TypeRef_opt
                                //  - Assignment_opt
                                //     - Expression
                                fieldName = underlying.ChildNodes[0].Token.Text;
                                fieldType = GetTypeRef(underlying.ChildNodes[1]);
                                assignmentOpt = underlying.ChildNodes[2];
                                fieldLocal = false;
                                break;
                            }
                            default: {
                                throw SolScriptInterpreterException.Raw(null, underlying.Span.Location +
                                                                              " : Invalid member variable node type " +
                                                                              underlying.Term.Name + "!");
                            }
                        }
                            // todo: modifier support by language!
                            // todo: annotations
                            SolFieldBuilder fieldBuilder = new SolFieldBuilder(fieldName, fieldType).SetLocal(fieldLocal);
                        if (assignmentOpt.ChildNodes.Count != 0) {
                            fieldBuilder.MakeScriptField(GetExpression(assignmentOpt.ChildNodes[0]));
                        }
                        classBuilder.AddField(fieldBuilder);
                        break;
                    }
                    // ===================================================================
                    // todo: properly nest this
                    case "ClassDefinition_BodyMember_Function": {
                        ParseTreeNode underlying = childNode.ChildNodes[0];
                        // identifies the name/table location of the function
                        ParseTreeNode funcNameNode;
                        ParseTreeNode parametersNode;
                        ParseTreeNode typeNode;
                        ParseTreeNode bodyNode;
                        bool local;
                        switch (underlying.Term.Name) {
                            case "ClassDefinition_BodyMember_Function_Global": {
                                // global declaration
                                // 0 -> "function"
                                // 1 -> _identifier
                                // 2 -> FunctionParameters
                                // 3 -> TypeRef_opt
                                // 4 -> FunctionBody
                                funcNameNode = underlying.ChildNodes[1];
                                parametersNode = underlying.ChildNodes[2];
                                typeNode = underlying.ChildNodes[3];
                                bodyNode = underlying.ChildNodes[4];
                                local = false;
                                break;
                            }
                            case "ClassDefinition_BodyMember_Function_Local": {
                                // local declaration
                                // 0 -> "local"
                                // 1 -> "function"
                                // 2 -> _identifier
                                // 3 -> FunctionParameters
                                // 4 -> TypeRef_opt
                                // 5 -> FunctionBody
                                funcNameNode = underlying.ChildNodes[2];
                                parametersNode = underlying.ChildNodes[3];
                                typeNode = underlying.ChildNodes[4];
                                bodyNode = underlying.ChildNodes[5];
                                local = true;
                                break;
                            }
                            default: {
                                throw new NotSupportedException(node.Span.Location + " : Function declaration type " +
                                                                node.Term.Name +
                                                                " is not supported. Make sure to update your SolScript interpreter version.");
                            }
                        }
                        string funcName = funcNameNode.Token.Text;
                        SolType funcType = GetTypeRef(typeNode);
                        ParseTreeNode parametersListNode = parametersNode.ChildNodes[0];
                        bool allowOptional;
                        SolParameter[] parameters;
                        SolChunk chunk = GetChunk(bodyNode.ChildNodes[0]);
                        if (parametersListNode.ChildNodes.Count == 0) {
                            // This cannot be parsed in one step.
                            // Tree for no args:
                            // - "()"
                            parameters = new SolParameter[0];
                            allowOptional = false;
                        } else {
                            // Tree for args:
                            // - ParameterList
                            //  - ExplicitParameterList
                            //   - Parameter
                            //   - Parameter
                            //  ...
                            ParseTreeNode optionalNode = parametersListNode.ChildNodes.FindChildByName("...");
                            ParseTreeNode explicitNode =
                                parametersListNode.ChildNodes.FindChildByName("ExplicitParameterList");
                            allowOptional = optionalNode != null;
                            parameters = explicitNode == null
                                ? new SolParameter[0]
                                : GetExplicitParameters(explicitNode);
                        }
                        // todo: modifier in language
                        // todo: annotations
                        SolFunctionBuilder functionBuilder = new SolFunctionBuilder(funcName).SetLocal(local)
                                .MakeScriptFunction(new SolScriptClassFunction(Assembly, node.Span.Location, classBuilder.Name, chunk, funcType, allowOptional, parameters));
                        classBuilder.AddFunction(functionBuilder);
                        break;
                    }
                    // ===================================================================
                    default: {
                        throw SolScriptInterpreterException.Raw(null, childNode.Span.Location +
                                                                      " : Invalid class member node type " +
                                                                      childNode.Term.Name + "!");
                    }
                }
            }
            // ===================================================================
            return classBuilder;
        }

        public SolChunk GetChunk(ParseTreeNode node) {
            if (node.Term.Name != "Chunk") {
                throw SolScriptInterpreterException.Raw(null,
                    node.Span.Location + " : Invalid chunk " + node.Term.Name +
                    ". Only Chunks can be Chunks.");
            }
            ParseTreeNode chunkEndNode = node.ChildNodes[1];
            TerminatingSolExpression returnValue;
            if (chunkEndNode.ChildNodes.Count == 0) {
                // Passing null is required as the ReturnValue variable will 
                // determine if the parent chunk should be terminated as well.
                returnValue = null;
            } else {
                // ChunkEnd
                //   LastStatement
                //     "return" Expression / "continue" / "break"
                ParseTreeNode lastStatement = chunkEndNode.ChildNodes[0];
                switch (lastStatement.ChildNodes[0].Term.Name) {
                    case "return": {
                        returnValue = new Expression_Return(Assembly, lastStatement.Span.Location, GetExpression(lastStatement.ChildNodes[1]));
                        break;
                    }
                    case "break": {
                        returnValue = Expression_Break.InstanceOf(Assembly);
                        break;
                    }
                    case "continue": {
                        returnValue = Expression_Continue.InstanceOf(Assembly);
                        break;
                    }
                    default: {
                        throw SolScriptInterpreterException.Raw(null, "Chunk last statement cannot be " + lastStatement.ChildNodes[0].Term.Name + "!");
                    }
                }
            }
            return new SolChunk(Assembly) {
                Statements = GetStatements(node.ChildNodes[0]),
                ReturnExpression = returnValue
            };
        }

        public SolExpression[] GetExpressions(ParseTreeNode node) {
            ParseTreeNodeList childNodes = node.ChildNodes;
            var array = new SolExpression[childNodes.Count];
            for (int i = 0; i < array.Length; i++) {
                array[i] = GetExpression(childNodes[i]);
            }
            return array;
        }

        public SolExpression BinaryExpression(ParseTreeNode node) {
            if (node.Term.Name != "Expression_Binary") {
                throw SolScriptInterpreterException.Raw(null,
                    node.Span.Location + " : Invalid binary expression node type " +
                    node.Term.Name + ". Only Expression_Binary is allowed!");
            }

            ParseTreeNode leftNode = node.ChildNodes[0];
            ParseTreeNode opNode = node.ChildNodes[1];
            ParseTreeNode rightNode = node.ChildNodes[2];
            Expression_Binary.OperationRef operation;
            switch (opNode.Token.Text) {
                case "&&":
                case "and": {
                    operation = Expression_Binary.And.Instance;
                    break;
                }
                case "||":
                case "or": {
                    operation = Expression_Binary.Or.Instance;
                    break;
                }
                case "+": {
                    operation = Expression_Binary.Addition.Instance;
                    break;
                }
                case "-": {
                    operation = Expression_Binary.Substraction.Instance;
                    break;
                }
                case "*": {
                    operation = Expression_Binary.Multiplication.Instance;
                    break;
                }
                case "/": {
                    operation = Expression_Binary.Division.Instance;
                    break;
                }
                case "^": {
                    operation = Expression_Binary.Exponentiation.Instance;
                    break;
                }
                case "%": {
                    operation = Expression_Binary.Modulus.Instance;
                    break;
                }
                case "..": {
                    operation = Expression_Binary.Concatenation.Instance;
                    break;
                }
                case "==": {
                    operation = Expression_Binary.CompareEqual.Instance;
                    break;
                }
                case "!=":
                case "~=": {
                    operation = Expression_Binary.CompareNotEqual.Instance;
                    break;
                }
                case ">": {
                    operation = Expression_Binary.CompareGreater.Instance;
                    break;
                }
                case ">=":
                case "=>": {
                    operation = Expression_Binary.CompareGreaterOrEqual.Instance;
                    break;
                }
                case "<": {
                    operation = Expression_Binary.CompareSmaller.Instance;
                    break;
                }
                case "<=":
                case "=<": {
                    operation = Expression_Binary.CompareSmallerOrEqual.Instance;
                    break;
                }
                case "??": {
                    operation = Expression_Binary.NilCoalescing.Instance;
                    break;
                }
                default: {
                    throw new NotSupportedException(opNode.Span.Location + " : Binary operation type '" +
                                                    opNode.Token.Text +
                                                    "' is not supported. Make sure to update your SolScript interpreter version.");
                }
            }
            SolExpression left = GetExpression(leftNode);
            SolExpression right = GetExpression(rightNode);
            return new Expression_Binary(Assembly, node.Span.Location) {
                Left = left,
                Operation = operation,
                Right = right
            };
        }

        public SolExpression GetExpression(ParseTreeNode node) {
#if DEBUG
            if (node.Term.Name != "Expression") {
                throw new Exception(node.Span.Location + " : Debug: GetExpression() only supports Expression! got " +
                                    node.Term.Name);
            }
#endif
            ParseTreeNode expressionNode = node.ChildNodes[0];
            switch (expressionNode.Term.Name) {
                case "_string": {
                    string text = expressionNode.Token.Text;
                    return new Expression_String(Assembly, expressionNode.Span.Location,
                        text.Substring(1, text.Length - 2).UnEscape());
                } // _string
                case "_number": {
                    return new Expression_Number(Assembly, expressionNode.Span.Location, double.Parse(expressionNode.Token.Text));
                } // _number
                case "Expression_Parenthetical": {
                    // This node is not transient since that would enable Expression->Expression 
                    // trees which would require an additional while loop in the expression factory.
                    // todo: recursion inside paren-exp might be problematic.
                    return GetExpression(expressionNode.ChildNodes[0]);
                }
                case "Expression_GetVariable": {
                    // Expression_GetVariable->Variable->(Named/IndexedVariable)
                    ParseTreeNode underlying = expressionNode.ChildNodes[0].ChildNodes[0];
                    Expression_GetVariable.SourceRef source;
                    switch (underlying.Term.Name) {
                        case "IndexedVariable": {
                            // IndexedVariable->(Expression, Expression/_identifier)
                            ParseTreeNode tableGetter = underlying.ChildNodes[0];
                            ParseTreeNode keyGetter = underlying.ChildNodes[1];
                            SolExpression table = GetExpression(tableGetter);
                            SolExpression key;
                            switch (keyGetter.Term.Name) {
                                // todo: duplicate code with GetVariableTarget() - solve.
                                case "_identifier": {
                                    // We are using identifiers instead of names variables since named variables retrieve their 
                                    // value from the context directly. However we want to retrieve a value from the table using 
                                    // the given key.
                                    key = new Expression_String(Assembly, expressionNode.Span.Location, keyGetter.Token.Text);
                                    break;
                                }
                                // todo: recursion inside binary-exp might be problematic.
                                case "Expression": {
                                    key = GetExpression(keyGetter);
                                    break;
                                }
                                default: {
                                    throw new NotSupportedException(keyGetter.Span.Location +
                                                                    " : Table key getter type " + keyGetter.Term.Name +
                                                                    " is not supported. Make sure to update your SolScript interpreter version.");
                                }
                            }
                            source = new Expression_GetVariable.IndexedVariable(tableGetter.Span.Location) {
                                TableGetter = table,
                                KeyGetter = key
                            };
                            break;
                        }
                        case "NamedVariable": {
                            ParseTreeNode identifier = underlying.ChildNodes[0];
                            source = new Expression_GetVariable.NamedVariable(identifier.Span.Location) {
                                Name = identifier.Token.Text
                            };
                            break;
                        }
                        default: {
                            throw new NotSupportedException(underlying.Span.Location + " : Variable getter type " +
                                                            underlying.Term.Name +
                                                            " is not supported. Make sure to update your SolScript interpreter version.");
                        }
                    }
                    return new Expression_GetVariable(Assembly, expressionNode.Span.Location) {Source = source};
                }
                case "Expression_Unary": {
                    string op = expressionNode.ChildNodes[0].Token.Text;
                    Expression_Unary.OperationRef operationRef;
                    switch (op) {
                        case "!":
                        case "not": {
                            operationRef = Expression_Unary.NotOperation.Instance;
                            break;
                        }
                        case "#": {
                            operationRef = Expression_Unary.GetNOperation.Instance;
                            break;
                        }
                        case "-": {
                            operationRef = Expression_Unary.MinusOperation.Instance;
                            break;
                        }
                        case "+": {
                            operationRef = Expression_Unary.PlusOperation.Instance;
                            break;
                        }
                        case "++": {
                            operationRef = Expression_Unary.PlusPlusOperation.Instance;
                            break;
                        }
                        case "--": {
                            operationRef = Expression_Unary.MinusMinusOperation.Instance;
                            break;
                        }
                        default: {
                            throw new NotSupportedException(expressionNode.ChildNodes[0].Span.Location +
                                                            " : Unary expression " + op + " is not supported!");
                        }
                    }
                    // todo: recursion inside unary-exp might be problematic.
                    SolExpression expression = GetExpression(expressionNode.ChildNodes[1]);
                    return new Expression_Unary(Assembly, expressionNode.Span.Location) {
                        Operation = operationRef,
                        ValueGetter = expression
                    };
                }
                case "Expression_Binary": {
                    return BinaryExpression(expressionNode);
                }
                case "Expression_Statement": {
                    SolStatement statement = GetStatement(expressionNode);
                    return new Expression_Statement(Assembly, expressionNode.Span.Location) {
                        Statement = statement
                    };
                }
                case "Expression_CreateFunc": {
                    bool allowOptional;
                    var parameters = GetParameters(expressionNode.ChildNodes[1], out allowOptional);
                    SolType type = GetTypeRef(expressionNode.ChildNodes[2]);
                    SolChunk chunk = GetChunk(expressionNode.ChildNodes[3].ChildNodes[0]);
                    return new Expression_CreateFunc(Assembly, expressionNode.Span.Location) {
                        Parameters = parameters,
                        ParameterAllowOptional = allowOptional,
                        Chunk = chunk,
                        Type = type
                    };
                }
                case "Expression_TableConstructor": {
                    ParseTreeNode fieldListNode = expressionNode.ChildNodes[0];
                    int fieldCount = fieldListNode.ChildNodes.Count;
                    Expression_TableConstructor tableCtor = new Expression_TableConstructor(Assembly, expressionNode.Span.Location) {
                        Keys = new SolExpression[fieldCount],
                        Values = new SolExpression[fieldCount]
                    };
                    int nextN = 0;
                    for (int i = 0; i < fieldCount; i++) {
                        // TableField
                        //   - _identifier/Expression
                        //   - =
                        //   - Expression
                        // TableField
                        //   - Expression
                        ParseTreeNode fieldNode = fieldListNode.ChildNodes[i];
                        SolExpression key;
                        SolExpression value;
                        // todo: Recursion inside table c-tors might be problematic.
                        switch (fieldNode.ChildNodes.Count) {
                            case 1: {
                                ParseTreeNode valueNode = fieldNode.ChildNodes[0];
                                key = new Expression_Number(Assembly, fieldNode.Span.Location, nextN++);
                                value = GetExpression(valueNode);
                                break;
                            }
                            case 3: {
                                ParseTreeNode keyNode = fieldNode.ChildNodes[0];
                                ParseTreeNode valueNode = fieldNode.ChildNodes[2];
                                switch (keyNode.Term.Name) {
                                    case "_identifier": {
                                        key = new Expression_String(Assembly, keyNode.Span.Location,
                                            keyNode.Token.Text);
                                        break;
                                    }
                                    case "Expression": {
                                        key = GetExpression(keyNode);
                                        break;
                                    }
                                    default: {
                                        throw new NotSupportedException(keyNode.Span.Location + " : " +
                                                                        keyNode.Term.Name +
                                                                        " nodes are not supported as table keys!");
                                    }
                                }
                                value = GetExpression(valueNode);
                                break;
                            }
                            default: {
                                throw new NotSupportedException(fieldNode.Span.Location +
                                                                " : A table field must either be in the form of 'X = Y' or 'X'. Found " +
                                                                fieldNode.ChildNodes.Count + " child nodes.");
                            }
                        }
                        tableCtor.Keys[i] = key;
                        tableCtor.Values[i] = value;
                    }
                    return tableCtor;
                }
                default: {
                    throw new NotSupportedException(node.Span.Location + " : Expression " + expressionNode.Term.Name +
                                                    " is not supported. Make sure to update your SolScript interpreter version.");
                }
            }
        }

        public SolStatement[] GetStatements(ParseTreeNode node) {
            if (node.Term.Name != "StatementList") {
                throw SolScriptInterpreterException.Raw(null, node.Span.Location + " : Invalid StatementList " +
                                                              node.Term.Name +
                                                              ". Only StatementLists can be StatementLists.");
            }
            var array = new SolStatement[node.ChildNodes.Count];
            for (int i = 0; i < array.Length; i++) {
                array[i] = GetStatement(node.ChildNodes[i]);
            }
            return array;
        }

        public SolStatement GetStatement(ParseTreeNode node) {
#if DEBUG
            if (node.Term.Name != "Statement" && node.Term.Name != "Expression_Statement") {
                throw new Exception(node.Span.Location +
                                    " : Debug: GetStatement() only supports Statement or Expression_Statement! got " +
                                    node.Term.Name);
            }
#endif
            // Statement->(All different statement types)
            ParseTreeNode statementNode = node.ChildNodes[0];
            //SolDebug.WriteLine("Current: " + statementNode.Term.Name);
            switch (statementNode.Term.Name) {
                case "Statement_DeclareVar": {
                    ParseTreeNode underlying = statementNode.ChildNodes[0];
                    ParseTreeNode variableNode;
                    ParseTreeNode expressionNode;
                    ParseTreeNode typeRefNode;
                    bool local;
                    switch (underlying.Term.Name) {
                        // todo: declaration without assignment
                        case "Statement_DeclareVar_Local":
                            // local assignment
                            // 0 -> "local"
                            // 1 -> _identifier
                            // 2 -> TypeRef
                            // 3 -> Expression
                            variableNode = underlying.ChildNodes[1];
                            typeRefNode = underlying.ChildNodes[2];
                            expressionNode = underlying.ChildNodes[3];
                            local = true;
                            break;
                        case "Statement_DeclareVar_Global":
                            // global assignment
                            // 0 -> _identifier
                            // 1 -> TypeRef
                            // 2 -> Expression
                            variableNode = underlying.ChildNodes[0];
                            typeRefNode = underlying.ChildNodes[1];
                            expressionNode = underlying.ChildNodes[2];
                            local = false;
                            break;
                        default:
                            throw new NotSupportedException(underlying.Span.Location + " : Variable Assignment " +
                                                            underlying.Term.Name +
                                                            " is not supported. Make sure to update your SolScript interpreter version.");
                    }
                    SolExpression expression = GetExpression(expressionNode);
                    string varName = variableNode.Token.Text;
                    SolType type = GetTypeRef(typeRefNode);
                    return new Statement_DeclareVar(Assembly, underlying.Span.Location, varName, type, expression);
                } // Statement_DeclareVar
                case "Statement_AssignVar": {
                    // Statement_AssignVar
                    //  - Variable
                    //  - Expression
                    ParseTreeNode variableNode = statementNode.ChildNodes[0];
                    ParseTreeNode expressionNode = statementNode.ChildNodes[1];
                    Statement_AssignVar.TargetRef variable = GetVariableTarget(variableNode);
                    SolExpression expression = GetExpression(expressionNode);
                    return new Statement_AssignVar(Assembly, statementNode.Span.Location) {
                        Target = variable,
                        ValueGetter = expression
                    };
                } // Statement_DeclareVar
                case "Statement_CallFunction": {
                    /* Statement_CallFunction.Rule = Expression + "." + _identifier + Arguments_trans; */
                    ParseTreeNode classGetterNode = statementNode.ChildNodes[0];
                    ParseTreeNode functionNameNode = statementNode.ChildNodes[2];
                    ParseTreeNode argumentsNode = statementNode.ChildNodes[3];
                    SolExpression classGetter = GetExpression(classGetterNode);
                    var arguments = GetExpressions(argumentsNode);
                    return new Statement_CallFunction(Assembly, statementNode.Span.Location, classGetter, 
                        new SolString(functionNameNode.Token.Text), arguments);
                } // Statement_CallFunction
                /*case SolScriptGrammar.STATEMENT_DECLARE_FUNC: {
                    ParseTreeNode underlying = statementNode.ChildNodes[0];
                    // identifies the name/table location of the function
                    ParseTreeNode variableNode;
                    ParseTreeNode parametersNode;
                    ParseTreeNode typeNode;
                    ParseTreeNode bodyNode;
                    bool local;
                    switch (underlying.Term.Name) {
                        case "Statement_DeclareFunc_Global": {
                            // global declaration
                            // 0 -> "function"
                            // 1 -> Variable
                            // 2 -> FunctionParameters
                            // 3 -> TypeRef_opt
                            // 4 -> FunctionBody
                            variableNode = underlying.ChildNodes[1];
                            parametersNode = underlying.ChildNodes[2];
                            typeNode = underlying.ChildNodes[3];
                            bodyNode = underlying.ChildNodes[4];
                            local = false;
                            break;
                        }
                        case "Statement_DeclareFunc_Local": {
                            // local declaration
                            // 0 -> "local"
                            // 1 -> "function"
                            // 2 -> Variable
                            // 3 -> FunctionParameters
                            // 4 -> TypeRef_opt
                            // 5 -> FunctionBody
                            variableNode = underlying.ChildNodes[2];
                            parametersNode = underlying.ChildNodes[3];
                            typeNode = underlying.ChildNodes[4];
                            bodyNode = underlying.ChildNodes[5];
                            local = true;
                            break;
                        }
                        default: {
                            throw new NotSupportedException(node.Span.Location + " : Function declaration type " +
                                                            node.Term.Name +
                                                            " is not supported. Make sure to update your SolScript interpreter version.");
                        }
                    }
                    Statement_AssignVar.TargetRef variable = GetVariableTarget(variableNode,
                        new SolType("function", false), local);
                    SolType type = GetTypeRef(typeNode);
                    ParseTreeNode parametersListNode = parametersNode.ChildNodes[0];
                    bool allowOptional;
                    SolParameter[] parameters;
                    SolChunk chunk = GetChunk(bodyNode.ChildNodes[0]);
                    if (parametersListNode.ChildNodes.Count == 0) {
                        // This cannot be parsed in one step.
                        // Tree for no args:
                        // - "()"
                        SolDebug.WriteLine("no func args, provided as ()");
                        parameters = new SolParameter[0];
                        allowOptional = false;
                    } else {
                        // Tree for args:
                        // - ParameterList
                        //  - ExplicitParameterList
                        //   - Parameter
                        //   - Parameter
                        //  ...
                        ParseTreeNode optionalNode = parametersListNode.ChildNodes.FindChildByName("...");
                        ParseTreeNode explicitNode =
                            parametersListNode.ChildNodes.FindChildByName("ExplicitParameterList");
                        allowOptional = optionalNode != null;
                        parameters = explicitNode == null ? new SolParameter[0] : GetExplicitParameters(explicitNode);
                    }
                    return new Statement_AssignVar(underlying.Span.Location) {
                        Target = variable,
                        ValueGetter = new Expression_CreateFunc(underlying.Span.Location) {
                            Parameters = parameters,
                            ParameterAllowOptional = allowOptional,
                            Type = type,
                            Chunk = chunk
                        }
                    };
                } // Statement_DeclareFunc*/
                case "Statement_For": {
                    // Statement_For
                    //  - "for"
                    //  - Statement
                    //  - Expression
                    //  - Statement
                    //  - "do"
                    //  - Chunk
                    //  - "end"
                    ParseTreeNode initNode = statementNode.ChildNodes[1];
                    ParseTreeNode conditionNode = statementNode.ChildNodes[2];
                    ParseTreeNode afterNode = statementNode.ChildNodes[3];
                    ParseTreeNode chunkNode = statementNode.ChildNodes[5];
                    // Recursion is fine inside the loop conditions.
                    SolStatement init = GetStatement(initNode);
                    SolExpression condition = GetExpression(conditionNode);
                    SolStatement after = GetStatement(afterNode);
                    SolChunk chunk = GetChunk(chunkNode);
                    return new Statement_For(Assembly, statementNode.Span.Location, init, condition, after, chunk);
                } // Statement_For
                case "Statement_Iterate": {
                    // Statement_Iterate
                    //  - "for"
                    //  - _identifier
                    //  - "in"
                    //  - Expression
                    //  - "do"
                    //  - Chunk
                    //  - "end"
                    // Note: No type declaration here since iterators will always return a 
                    // fixed type (or possibly varying types if the implementation chooses
                    // to do so). Maybe in the future if this proves to too inconsistent.
                    ParseTreeNode identifierNode = statementNode.ChildNodes[1];
                    ParseTreeNode expressionNode = statementNode.ChildNodes[3];
                    ParseTreeNode chunkNode = statementNode.ChildNodes[5];
                    string iterName = identifierNode.Token.Text;
                    SolExpression iterExp = GetExpression(expressionNode);
                    SolChunk chunk = GetChunk(chunkNode);
                    return new Statement_Iterate(Assembly, statementNode.Span.Location, iterExp, iterName, chunk);
                } // Statement_Iterate
                case "Statement_Do": {
                    // Statement_Do
                    //  - "do"
                    //  - Chunk
                    //  - "end"
                    ParseTreeNode chunkNode = statementNode.ChildNodes[1];
                    SolChunk chunk = GetChunk(chunkNode);
                    return new Statement_Do(Assembly, statementNode.Span.Location, chunk);
                } // Statement_Do
                case "Statement_While": {
                    // Statement_While
                    //  - "while"
                    //  - Expression
                    //  - "do"
                    //  - Chunk
                    //  - "end"
                    ParseTreeNode conditionNode = statementNode.ChildNodes[1];
                    ParseTreeNode chunkNode = statementNode.ChildNodes[3];
                    SolDebug.WriteLine(conditionNode.ToString());
                    SolDebug.WriteLine(chunkNode.ToString());
                    SolExpression conditionExp = GetExpression(conditionNode);
                    SolChunk chunk = GetChunk(chunkNode);
                    return new Statement_While(Assembly, statementNode.Span.Location, conditionExp, chunk);
                } // Statement_Iterate
                case "Statement_Conditional": {
                    ParseTreeNode elseIfListNode = statementNode.ChildNodes[2];
                    ParseTreeNode elseNode = statementNode.ChildNodes[3];
                    var branches = new Statement_Conditional.IfBranch[elseIfListNode.ChildNodes.Count + 1];
                    branches[0] = GetIfOrElseIfBranch(statementNode);
                    for (int i = 0; i < elseIfListNode.ChildNodes.Count; i++) {
                        ParseTreeNode elseIfNode = elseIfListNode.ChildNodes[i];
                        branches[i + 1] = GetIfOrElseIfBranch(elseIfNode);
                    }
                    SolChunk elseChunk = elseNode.ChildNodes.Count != 0 ? GetChunk(elseNode.ChildNodes[0]) : null;
                    return new Statement_Conditional(Assembly, statementNode.Span.Location) {If = branches, Else = elseChunk};
                } // Statement_Conditional
                case "Statement_New": {
                    // 0: "new"
                    // 1: _identifier
                    // 2: ExpressionList
                    string typeName = statementNode.ChildNodes[1].Token.Text;
                    var expressions = GetExpressions(statementNode.ChildNodes[2]);
                    return new Statement_New(Assembly, statementNode.Span.Location, typeName, expressions);
                } // Statement_New
                /*case "Statement_Continue": {
                    return Statement_Continue.Instance;
                }
                case "Statement_Break": {
                    return StB
                }*/
                default: {
                    throw new NotSupportedException("Statement " + statementNode.Term.Name +
                                                    " is not supported. Make sure to update your SolScript interpreter version.");
                } // default
            }
        }

        private Statement_Conditional.IfBranch GetIfOrElseIfBranch(ParseTreeNode node) {
            ParseTreeNode conditionNode = node.ChildNodes[0];
            ParseTreeNode chunkNode = node.ChildNodes[1];
            return new Statement_Conditional.IfBranch {
                Condition = GetExpression(conditionNode),
                Chunk = GetChunk(chunkNode)
            };
        }

        public Statement_AssignVar.TargetRef GetVariableTarget(ParseTreeNode node /*, bool local*/) {
            switch (node.Term.Name) {
                case "Variable": {
                    // Recursion is fine here, as it will only call it once.
                    return GetVariableTarget(node.ChildNodes[0]);
                }
                case "NamedVariable": {
                    return new Statement_AssignVar.NamedVariable {
                        Name = node.ChildNodes[0].Token.Text
                        //Local = local
                    };
                }
                case "IndexedVariable": {
                    ParseTreeNode tableNode = node.ChildNodes[0];
                    ParseTreeNode keyNode = node.ChildNodes[1];
                    SolExpression table = GetExpression(tableNode);
                    SolExpression key;
                    switch (keyNode.Term.Name) {
                        case "_identifier": {
                            // We are using identifiers instead of names variables since named variables retrieve their 
                            // value from the context directly. However we want to retrieve a value from the table using 
                            // the given key.
                            key = new Expression_String(Assembly, keyNode.Span.Location, keyNode.Token.Text);
                            break;
                        }
                        case "Expression": {
                            key = GetExpression(keyNode);
                            break;
                        }
                        default: {
                            throw new NotSupportedException(keyNode.Span.Location +
                                                            " : Table key getter type " + keyNode.Term.Name +
                                                            " is not supported. Make sure to update your SolScript interpreter version.");
                        }
                    }
                    return new Statement_AssignVar.IndexedVariable {
                        TableGetter = table,
                        KeyGetter = key
                    };
                }
                default: {
                    throw new NotSupportedException(node.Span.Location + " : Variable Type " + node.Term.Name +
                                                    " is not supported. Make sure to update your SolScript interpreter version.");
                }
            }
        }

        public SolType GetTypeRef(ParseTreeNode node) {
            switch (node.Term.Name) {
                case "TypeRef_opt":
                    if (node.ChildNodes.Count == 0) {
                        return new SolType("any", true);
                    }
                    node = node.ChildNodes[0];
                    break;
                case "TypeRef":
                    break;
                default:
                    throw new NotSupportedException(node.Span.Location + " : TypeRef " + node.Term.Name +
                                                    " is not supported. Make sure to update your SolScript interpreter version.");
            }
            // TypeRef->(":", TypeInfo)
            ParseTreeNode typeInfo = node.ChildNodes[0];
            // TypeInfo->(<type>, TypeInfo_Nullable_opt)
            string type = typeInfo.ChildNodes[0].Token.Text;
            // TODO: The Index of nullable is 1 due to the literal ":" 
            // which simply does not disappear despite being marked as 
            // Punctuation.
            ParseTreeNode nullableOpt = typeInfo.ChildNodes[1];
            if (nullableOpt.ChildNodes.Count == 0) {
                return new SolType(type);
            }
            // TypeInfo_Nullable_opt->TypeInfo_Nullable_opt-><symbol>
            ParseTreeNode nullableSymbol = nullableOpt.ChildNodes[0].ChildNodes[0];
            string nullableStr = nullableSymbol.Token.Text;
            return new SolType(type, nullableStr[0] == '?');
        }

        public SolParameter[] GetExplicitParameters(ParseTreeNode node) {
#if DEBUG
            if (node.Term.Name != "ExplicitParameterList") {
                throw new Exception("Debug: GetExplicitParameters() only supports ExplicitParameterList! got " +
                                    node.Term.Name);
            }
#endif
            ParseTreeNodeList childNodes = node.ChildNodes;
            var parameterArray = new SolParameter[childNodes.Count];
            for (int i = 0; i < childNodes.Count; i++) {
                parameterArray[i] = GetParameter(childNodes[i]);
            }
            return parameterArray;
        }

        public SolParameter GetParameter(ParseTreeNode node) {
#if DEBUG
            if (node.Term.Name != "Parameter") {
                throw new Exception("Debug: GetParameter() only supports Parameter! got " + node.Term.Name);
            }
#endif
            string name = node.ChildNodes[0].Token.Text;
            // Parameter->(<name>, TypeRef_opt)
            ParseTreeNode typeRefOpt = node.ChildNodes[1];
            return new SolParameter(name, GetTypeRef(typeRefOpt));
        }
    }
}