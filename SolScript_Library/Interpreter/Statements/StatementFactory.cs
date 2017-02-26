using System;
using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    public class StatementFactory
    {
        public StatementFactory(SolAssembly assembly)
        {
            Assembly = assembly;
        }

        public readonly SolAssembly Assembly;

        private readonly Stack<MetaItem> m_MetaStack = new Stack<MetaItem>();
        public string ActiveFile { get; set; }

        /// <exception cref="SolInterpreterException">An error occured.</exception>
        public void InterpretTree(ParseTree tree, SolGlobalsBuilder globals, out IReadOnlyCollection<SolClassBuilder> classes)
        {
            ActiveFile = tree.FileName;
            var classesList = new List<SolClassBuilder>();
            classes = classesList;
            foreach (ParseTreeNode rootedNode in tree.Root.ChildNodes) {
                switch (rootedNode.Term.Name) {
                    case "FunctionWithAccess": {
                        SolFunctionBuilder functionBuilder = GetFunctionWithAccess(rootedNode,
                            delegate(SolFunctionBuilder builder, SolChunk chunk, SolType returnType, SolParameter[] parameters, bool allowOptParameters) {
                                builder.Chunk(chunk).ScriptReturns(returnType).SetParameters(parameters).OptionalParameters(allowOptParameters);
                            });
                        globals.AddFunction(functionBuilder);
                        break;
                    }
                    case "FieldWithAccess": {
                        globals.AddField(GetFieldWithAccess(rootedNode));
                        break;
                    }
                    case "ClassDefinition": {
                        classesList.Add(GetClassDefinition(rootedNode));
                        break;
                    }
                    default: {
                        throw new SolInterpreterException(new SolSourceLocation(ActiveFile, rootedNode.Span.Location), $"Not supported root node id \"{rootedNode.Term.Name}\".");
                    }
                }
            }
        }

        public SolParameter[] GetParameters(ParseTreeNode node, out bool allowOptional)
        {
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

        public SolClassBuilder GetClassDefinition(ParseTreeNode node)
        {
            if (node.Term.Name != "ClassDefinition") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid class node id \"" + node.Term.Name + "\".");
            }
            // ===================================================================
            // ClassDefinition
            //  - AnnotationList
            //  - ClassModifier_opt
            //  - "class"
            //  - _identifier
            //  - ClassDefinition_Extends_opt
            //  - ClassDefinition_Body
            SolTypeMode typeMode;
            ParseTreeNode typeModeNode = node.ChildNodes[1];
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

            string className = node.ChildNodes[3].Token.Text;
            SolClassBuilder classBuilder = new SolClassBuilder(className, typeMode).AtLocation(new SolSourceLocation(ActiveFile, node.Span.Location));
            m_MetaStack.Push(new MetaItem {ActiveClass = classBuilder});
            // ===================================================================
            // == Annotations
            // AnnotationList
            //   - Empty | Annotation+
            // Annotation
            //   - "@"
            //   - _identifier
            //   - Arguments_trans
            ParseTreeNode annotationsListNode = node.ChildNodes[0];
            InsertAnnotations(annotationsListNode, classBuilder);
            // Push class builder to meta, so that instance functions can determine that they are instance functions.
            // ===================================================================
            // ClassDefinition_Mixins_opt
            //  - Empty
            // ClassDefinition_Mixins_opt
            //  - "extends"
            //  - _identifier
            ParseTreeNode mixinsNode = node.ChildNodes[4];
            if (mixinsNode.ChildNodes.Count != 0) {
                classBuilder.Extends(mixinsNode.ChildNodes[1].Token.Text);
            }
            // ===================================================================
            ParseTreeNode bodyMemberListNode = node.ChildNodes[5].ChildNodes[0];
            // ClassDefinition_Body
            //  - ClassDefinition_BodyMemberList
            //     - FieldWithAccess | FunctionWithAccess
            //  - "end"
            foreach (ParseTreeNode classMemberNode in bodyMemberListNode.ChildNodes) {
                switch (classMemberNode.Term.Name) {
                    // ===================================================================
                    case "FieldWithAccess": {
                        classBuilder.AddField(GetFieldWithAccess(classMemberNode));
                        break;
                    }
                    // ===================================================================
                    case "FunctionWithAccess": {
                        SolFunctionBuilder functionBuilder = GetFunctionWithAccess(classMemberNode,
                            delegate(SolFunctionBuilder builder, SolChunk chunk, SolType returnType, SolParameter[] parameters, bool allowOptParameters) {
                                builder.Chunk(chunk).SetParameters(parameters).OptionalParameters(allowOptParameters);
                                if (builder.Name != "__new") {
                                    builder.ScriptReturns(returnType);
                                } else {
                                    if (!returnType.CanBeNil || returnType.Type != "any" && returnType.Type != "nil") {
                                        throw new SolInterpreterException(builder.Location,
                                            "The constructor on class \"" + builder.Name + "\" specified \"" + returnType + "\" as return type. A constrcutor function must not specify a return type.");
                                    }
                                    builder.ScriptReturns(new SolType(SolNil.TYPE, true));
                                }
                            });
                        classBuilder.AddFunction(functionBuilder);
                        break;
                    }
                    // ===================================================================
                    default: {
                        throw new SolInterpreterException(new SolSourceLocation(ActiveFile, classMemberNode.Span.Location), $"Invalid class member node id \"{classMemberNode.Term.Name}\".");
                    }
                }
            }
            // ===================================================================
            m_MetaStack.Pop();
            return classBuilder;
        }

        /// <summary>
        ///     Creates a function with access modifiers.
        /// </summary>
        /// <param name="node">The node to generate the function from.</param>
        /// <param name="generator">
        ///     The generator delegate.
        ///     <list type="table">
        ///         <item>
        ///             <term>
        ///                 1. Parameter (<see cref="SolFunctionBuilder" />)
        ///             </term>
        ///             <description>
        ///                 This is the raw function builder object. The function name and the access modifiers have already been
        ///                 set.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 2. Parameter (<see cref="SolSourceLocation" />)
        ///             </term>
        ///             <description>
        ///                 The position in the source the function is located at.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 3. Parameter (<see cref="SolChunk" />)
        ///             </term>
        ///             <description>
        ///                 The chunk containing the function code.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 4. Parameter (<see cref="SolType" />)
        ///             </term>
        ///             <description>
        ///                 The return type of the function.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 5. Parameter (<see cref="SolParameter" />[])
        ///             </term>
        ///             <description>
        ///                 All defined parameters of the function.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 6. Parameter (<see cref="bool" />)
        ///             </term>
        ///             <description>
        ///                 Will this function accept optional parameters?
        ///             </description>
        ///         </item>
        ///     </list>
        /// </param>
        /// <returns>The created function builder.</returns>
        /// <remarks></remarks>
        public SolFunctionBuilder GetFunctionWithAccess(ParseTreeNode node, Action<SolFunctionBuilder, SolChunk, SolType, SolParameter[], bool> generator)
        {
            // todo: annotations in statement factory on functions with access (the name sucks btw)
            // 0 -> AnnotationList
            // 1 -> AccessModifier_opt
            // 2 -> "function"
            // 3 -> _identifier
            // 4 -> FunctionParameters
            // 5 -> TypeRef_opt
            // 6 -> FunctionBody
            ParseTreeNode annotationsListNode = node.ChildNodes[0];
            SolAccessModifier accessModifier = GetAccessModifier(node.ChildNodes[1]);
            ParseTreeNode funcNameNode = node.ChildNodes[3];
            ParseTreeNode parametersNode = node.ChildNodes[4];
            ParseTreeNode typeNode = node.ChildNodes[5];
            ParseTreeNode bodyNode = node.ChildNodes[6];

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
                ParseTreeNode explicitNode = parametersListNode.ChildNodes.FindChildByName("ExplicitParameterList");
                allowOptional = optionalNode != null;
                parameters = explicitNode == null ? new SolParameter[0] : GetExplicitParameters(explicitNode);
            }
            SolFunctionBuilder functionBuilder = new SolFunctionBuilder(funcName).SetAccessModifier(accessModifier);
            InsertAnnotations(annotationsListNode, functionBuilder);
            functionBuilder.AtLocation(new SolSourceLocation(ActiveFile, node.Span.Location));
            generator(functionBuilder, chunk, funcType, parameters, allowOptional);
            return functionBuilder;
        }

        private void InsertAnnotations(ParseTreeNode node, IAnnotateableBuilder builder)
        {
            if (node.Term.Name != "AnnotationList") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid annotation list: " + node.Term.Name);
            }
            foreach (ParseTreeNode annotationNode in node.ChildNodes) {
                string name = annotationNode.ChildNodes[1].Token.Text;
                SolExpression[] expressions = annotationNode.ChildNodes.Count == 3 ? GetExpressions(annotationNode.ChildNodes[2]) : Array.Empty<SolExpression>();
                builder.AddAnnotation(new SolAnnotationData(new SolSourceLocation(ActiveFile, node.Span.Location), name, expressions));
            }
        }

        public SolFieldBuilder GetFieldWithAccess(ParseTreeNode node)
        {
            // AnnotationList 
            // AccessModifier_opt
            // _identifier 
            // TypeRef_opt 
            // Assignment_opt;
            ParseTreeNode annotationsListNode = node.ChildNodes[0];
            SolAccessModifier accessModifier = GetAccessModifier(node.ChildNodes[1]);
            ParseTreeNode identifierNode = node.ChildNodes[2];
            string fieldName = identifierNode.Token.Text;
            SolType fieldType = GetTypeRef(node.ChildNodes[3]);
            ParseTreeNode assignmentOpt = node.ChildNodes[4];
            SolFieldBuilder fieldBuilder =
                new SolFieldBuilder(fieldName).FieldType(fieldType).SetAccessModifier(accessModifier).AtLocation(new SolSourceLocation(ActiveFile, identifierNode.Span.Location));
            InsertAnnotations(annotationsListNode, fieldBuilder);
            fieldBuilder.MakeScriptField(assignmentOpt.ChildNodes.Count != 0
                ? GetExpression(assignmentOpt.ChildNodes[0])
                : new Expression_Nil(Assembly, new SolSourceLocation(ActiveFile, assignmentOpt.Span.Location)));
            return fieldBuilder;
        }

        private SolAccessModifier GetAccessModifier(ParseTreeNode node)
        {
            if (node.Term.Name == "AccessModifier_opt") {
                if (node.ChildNodes.Count != 1) {
                    return SolAccessModifier.None;
                }
                node = node.ChildNodes[0];
            }
            switch (node.Token.Text) {
                case "local": {
                    return SolAccessModifier.Local;
                }
                case "internal": {
                    return SolAccessModifier.Internal;
                }
                default: {
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid access modifier node name \"" + node.Token.Text + "\".");
                }
            }
        }

        public SolChunk GetChunk(ParseTreeNode node)
        {
            if (node.Term.Name != "Chunk") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid chunk node id \"" + node.Term.Name + "\".");
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
                        SolExpression returnExpression = lastStatement.ChildNodes.Count > 1
                            ? GetExpression(lastStatement.ChildNodes[1])
                            : new Expression_Nil(Assembly, new SolSourceLocation(ActiveFile, lastStatement.Span.Location));
                        returnValue = new Expression_Return(Assembly, new SolSourceLocation(ActiveFile, lastStatement.Span.Location), returnExpression);
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
                        throw new SolInterpreterException(new SolSourceLocation(ActiveFile, lastStatement.ChildNodes[0].Span.Location),
                            "Invalid chunk last statement node id \"" + lastStatement.ChildNodes[0].Term.Name + "\".");
                    }
                }
            }
            return new SolChunk(Assembly, returnValue, GetStatements(node.ChildNodes[0]));
        }

        public SolExpression[] GetExpressions(ParseTreeNode node)
        {
            ParseTreeNodeList childNodes = node.ChildNodes;
            var array = new SolExpression[childNodes.Count];
            for (int i = 0; i < array.Length; i++) {
                array[i] = GetExpression(childNodes[i]);
            }
            return array;
        }

        public SolExpression BinaryExpression(ParseTreeNode node)
        {
            if (node.Term.Name != "Expression_Binary") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid binary expression root node id \"" + node.Term.Name + "\".");
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
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, opNode.Span.Location), "Invalid binary operation node id \"" + opNode.Term.Name + "\".");
                }
            }
            SolExpression left = GetExpression(leftNode);
            SolExpression right = GetExpression(rightNode);
            return new Expression_Binary(Assembly, new SolSourceLocation(ActiveFile, node.Span.Location), operation, left, right);
        }

        public SolExpression GetExpression(ParseTreeNode node)
        {
#if DEBUG
            if (node.Term.Name != "Expression") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid expression root node id \"" + node.Term.Name + "\".");
            }
#endif
            ParseTreeNode expressionNode = node.ChildNodes[0];
            switch (expressionNode.Term.Name) {
                case "_string": {
                    string text;
                    try {
                        text = expressionNode.Token.ValueString.UnEscape();
                    } catch (ArgumentException ex) {
                        throw new SolInterpreterException(new SolSourceLocation(ActiveFile, expressionNode.Span.Location), "Failed to parse string: " + ex.Message, ex);
                    }
                    return new Expression_String(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), text);
                } // _string
                case "_number": {
                    return new Expression_Number(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), new SolNumber(double.Parse(expressionNode.Token.ValueString)));
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
                            ParseTreeNode indexableGetter = underlying.ChildNodes[0];
                            ParseTreeNode keyGetter = underlying.ChildNodes[1];
                            SolExpression indexable = GetExpression(indexableGetter);
                            SolExpression key;
                            switch (keyGetter.Term.Name) {
                                // todo: duplicate code with GetVariableAssignmentTarget() - solve.
                                case "_identifier": {
                                    // We are using identifiers instead of names variables since named variables retrieve their 
                                    // value from the context directly. However we want to retrieve a value from the table using 
                                    // the given key.
                                    key = new Expression_String(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), keyGetter.Token.Text);
                                    break;
                                }
                                // todo: recursion inside binary-exp might be problematic.
                                case "Expression": {
                                    key = GetExpression(keyGetter);
                                    break;
                                }
                                default: {
                                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, keyGetter.Span.Location),
                                        "Invalid indexed variable getter node id \"" + keyGetter.Term.Name + "\".");
                                }
                            }
                            source = new Expression_GetVariable.IndexedVariable(indexable, key);
                            break;
                        }
                        case "NamedVariable": {
                            ParseTreeNode identifier = underlying.ChildNodes[0];
                            source = new Expression_GetVariable.NamedVariable(identifier.Token.Text);
                            break;
                        }
                        default: {
                            throw new SolInterpreterException(new SolSourceLocation(ActiveFile, underlying.Span.Location), "Invalid variable getter node id \"" + underlying.Term.Name + "\".");
                        }
                    }
                    MetaItem meta = m_MetaStack.Count != 0 ? m_MetaStack.Peek() : null;
                    return new Expression_GetVariable(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), source, meta?.ActiveClass.Name);
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
                            throw new SolInterpreterException(new SolSourceLocation(ActiveFile, expressionNode.ChildNodes[0].Span.Location),
                                "Invalid unary expression node id \"" + expressionNode.ChildNodes[0].Term.Name + "\".");
                        }
                    }
                    // todo: recursion inside unary-exp might be problematic.
                    SolExpression expression = GetExpression(expressionNode.ChildNodes[1]);
                    return new Expression_Unary(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location)) {
                        Operation = operationRef,
                        ValueGetter = expression
                    };
                }
                case "Expression_Binary": {
                    return BinaryExpression(expressionNode);
                }
                case "Expression_Tertiary": {
                    SolExpression condition = GetExpression(expressionNode.ChildNodes[0]);
                    SolExpression trueValue = GetExpression(expressionNode.ChildNodes[2]);
                    SolExpression falseValue = GetExpression(expressionNode.ChildNodes[4]);
                    return new Expression_Tertiary(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), Expression_Tertiary.Conditional.Instance, condition, trueValue, falseValue);
                }
                case "Expression_Statement": {
                    SolStatement statement = GetStatement(expressionNode);
                    return new Expression_Statement(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location)) {
                        Statement = statement
                    };
                }
                case "Expression_CreateFunc": {
                    bool allowOptional;
                    SolParameter[] parameters = GetParameters(expressionNode.ChildNodes[1], out allowOptional);
                    SolType type = GetTypeRef(expressionNode.ChildNodes[2]);
                    SolChunk chunk = GetChunk(expressionNode.ChildNodes[3].ChildNodes[0]);
                    return new Expression_CreateFunc(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), chunk, type, allowOptional, parameters);
                }
                case "Expression_TableConstructor": {
                    ParseTreeNode fieldListNode = expressionNode.ChildNodes[0];
                    int fieldCount = fieldListNode.ChildNodes.Count;
                    var keys = new SolExpression[fieldCount];
                    var values = new SolExpression[fieldCount];
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
                                key = new Expression_Number(Assembly, new SolSourceLocation(ActiveFile, fieldNode.Span.Location), new SolNumber(nextN++));
                                value = GetExpression(valueNode);
                                break;
                            }
                            case 3: {
                                ParseTreeNode keyNode = fieldNode.ChildNodes[0];
                                ParseTreeNode valueNode = fieldNode.ChildNodes[2];
                                switch (keyNode.Term.Name) {
                                    case "_identifier": {
                                        key = new Expression_String(Assembly, new SolSourceLocation(ActiveFile, keyNode.Span.Location),
                                            keyNode.Token.Text);
                                        break;
                                    }
                                    case "Expression": {
                                        key = GetExpression(keyNode);
                                        break;
                                    }
                                    default: {
                                        throw new SolInterpreterException(new SolSourceLocation(ActiveFile, keyNode.Span.Location), "Invalid table key node id \"" + keyNode.Term.Name + "\".");
                                    }
                                }
                                value = GetExpression(valueNode);
                                break;
                            }
                            default: {
                                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, fieldNode.Span.Location),
                                    "Invalid table initializer format. A table field must either be in the form of 'X = Y' or 'X'.");
                            }
                        }
                        keys[i] = key;
                        values[i] = value;
                    }
                    return new Expression_TableConstructor(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), keys, values);
                }
                case "Expression_Bool": {
                    switch (expressionNode.ChildNodes[0].Term.Name) {
                        case "true": {
                            return new Expression_Bool(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), SolBool.True);
                        }
                        case "false": {
                            return new Expression_Bool(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location), SolBool.False);
                        }
                        default: {
                            throw new SolInterpreterException(new SolSourceLocation(ActiveFile, expressionNode.Span.Location),
                                "Invalid bool expression \"" + expressionNode.ChildNodes[0].Term.Name + "\".");
                        }
                    }
                }
                case "Expression_Nil": {
                    return new Expression_Nil(Assembly, new SolSourceLocation(ActiveFile, expressionNode.Span.Location));
                }
                default: {
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, expressionNode.Span.Location), "Invalid expression node id \"" + expressionNode.Term.Name + "\".");
                }
            }
        }

        public SolStatement[] GetStatements(ParseTreeNode node)
        {
            if (node.Term.Name != "StatementList") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid statement list node id \"" + node.Term.Name + "\".");
            }
            var array = new SolStatement[node.ChildNodes.Count];
            for (int i = 0; i < array.Length; i++) {
                array[i] = GetStatement(node.ChildNodes[i]);
            }
            return array;
        }

        public SolStatement GetStatement(ParseTreeNode node)
        {
#if DEBUG
            if (node.Term.Name != "Statement" && node.Term.Name != "Expression_Statement") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid statement root node id \"" + node.Term.Name + "\".");
            }
#endif
            // Statement->(All different statement types)
            ParseTreeNode statementNode = node.ChildNodes[0];
            //SolDebug.WriteLine("Current: " + statementNode.Term.Name);
            switch (statementNode.Term.Name) {
                case "Statement_DeclareVar": {
                    /*
                    Statement_DeclareVar.Rule =
                        VAR + _identifier + TypeRef_opt + EQ + Expression
                        | VAR + _identifier + TypeRef_opt
                        ;
                    */
                    ParseTreeNode variableNode = statementNode.ChildNodes[1];
                    ParseTreeNode typeRefNode = statementNode.ChildNodes[2];
                    SolExpression initialValue = null;
                    if (statementNode.ChildNodes.Count == 4) {
                        // If the initial value is null the variable will only be decalred and not assigned.
                        initialValue = GetExpression(statementNode.ChildNodes[3]);
                    }
                    SolType type = GetTypeRef(typeRefNode);
                    return new Statement_DeclareVar(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), variableNode.Token.Text, type, initialValue);
                } // Statement_DeclareVar
                case "Statement_AssignVar": {
                    // Statement_AssignVar
                    //  - Variable
                    //  - Expression
                    ParseTreeNode variableNode = statementNode.ChildNodes[0];
                    ParseTreeNode expressionNode = statementNode.ChildNodes[1];
                    Statement_AssignVar.TargetRef variable = GetVariableAssignmentTarget(variableNode);
                    SolExpression expression = GetExpression(expressionNode);
                    return new Statement_AssignVar(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), variable, expression);
                } // Statement_DeclareVar
                case "Statement_CallFunction": {
                    /* Statement_CallFunction = Expression + Arguments_trans; */
                    /*SolString functionName;
                SolExpression classGetter;
                ParseTreeNode indexVariableNode = statementNode.ChildNodes[0];
                {
                    ParseTreeNode classGetterNode = indexVariableNode.ChildNodes[0];
                    ParseTreeNode functionGetterNode = indexVariableNode.ChildNodes[1];
                    classGetter = GetExpression(classGetterNode);
                    switch (functionGetterNode.Term.Name) {
                        case "_identifier": {
                            functionName = new SolString(functionGetterNode.Token.Text);
                            break;
                        }
                        case "Expression": {
                            // once dynmic indexising is implemented, this will be duplictae code with GetVariableAssignmentTarget()
                            throw new NotImplementedException("As of now classes cannot be dynamically indexed. Sorry, maybe in the next version :) - This will be a meta call.");
                        }
                        default: {
                            throw new SolInterpreterException(new SolSourceLocation(ActiveFile, functionGetterNode.Span.Location),
                                "Invalid class instance function getter node id \"" + functionGetterNode.Term.Name + "\".");
                        }
                    }
                }*/
                    SolExpression expression = GetExpression(statementNode.ChildNodes[0]);
                    SolExpression[] arguments = statementNode.ChildNodes[1].ChildNodes.Count != 0 ? GetExpressions(statementNode.ChildNodes[1]) : Array.Empty<SolExpression>();
                    MetaItem meta = m_MetaStack.Count != 0 ? m_MetaStack.Peek() : null;
                    return new Statement_CallInstanceFunction(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), meta?.ActiveClass.Name, expression, arguments);
                } // Statement_CallInstanceFunction
                /*case "Statement_CallFunctionCompilerResolveRef": {
                    // Statement_CallFunctionCompilerResolveRef.Rule = _identifier + Arguments_trans; 
                    // Step 1: determine context
                    //  case 1: in class -> use class internals
                    //  case 2: not in class -> use assembly globals todo: proper "out of class" support for stuff
                    MetaItem meta = m_MetaStack.Count != 0 ? m_MetaStack.Peek() : null;
                    string functionName = statementNode.ChildNodes[0].Token.Text;
                    SolExpression[] arguments = GetExpressions(statementNode.ChildNodes[1]);
                    if (meta != null)
                        {
                            Expression_GetVariable.SourceRef source = new Expression_GetVariable.NamedVariable("self");
                        return new Statement_CallInstanceFunction(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), meta.ActiveClass.Name,
                            new Expression_GetVariable(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), source, meta.ActiveClass.Name),
                            new SolString(functionName), arguments);
                    }
                    return new Statement_CallGlobalFunctionFromGlobalContext(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), functionName, arguments);
                } // Statement_CallFunctionCompilerResolveRef*/
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
                    return new Statement_For(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), init, condition, after, chunk);
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
                    return new Statement_Iterate(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), iterExp, iterName, chunk);
                } // Statement_Iterate
                case "Statement_Do": {
                    // Statement_Do
                    //  - "do"
                    //  - Chunk
                    //  - "end"
                    ParseTreeNode chunkNode = statementNode.ChildNodes[1];
                    SolChunk chunk = GetChunk(chunkNode);
                    return new Statement_Do(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), chunk);
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
                    return new Statement_While(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), conditionExp, chunk);
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
                    return new Statement_Conditional(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location)) {If = branches, Else = elseChunk};
                } // Statement_Conditional
                case "Statement_New": {
                    // 0: "new"
                    // 1: _identifier
                    // 2: ExpressionList
                    string typeName = statementNode.ChildNodes[1].Token.Text;
                    SolExpression[] expressions = GetExpressions(statementNode.ChildNodes[2]);
                    return new Statement_New(Assembly, new SolSourceLocation(ActiveFile, statementNode.Span.Location), typeName, expressions);
                } // Statement_New
                /*case "Statement_Continue": {
                    return Statement_Continue.Instance;
                }
                case "Statement_Break": {
                    return StB
                }*/
                default: {
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, statementNode.Span.Location), "Invalid statement node id \"" + statementNode.Term.Name + "\".");
                } // default
            }
        }

        private Statement_Conditional.IfBranch GetIfOrElseIfBranch(ParseTreeNode node)
        {
            ParseTreeNode conditionNode = node.ChildNodes[0];
            ParseTreeNode chunkNode = node.ChildNodes[1];
            return new Statement_Conditional.IfBranch {
                Condition = GetExpression(conditionNode),
                Chunk = GetChunk(chunkNode)
            };
        }

        public Expression_GetVariable.SourceRef GetVariableSourceTarget(ParseTreeNode node /*, bool local*/)
        {
            switch (node.Term.Name) {
                case "Variable": {
                    // Recursion is fine here, as it will only call it once.
                    return GetVariableSourceTarget(node.ChildNodes[0]);
                }
                case "NamedVariable": {
                    return new Expression_GetVariable.NamedVariable(node.ChildNodes[0].Token.Text);
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
                            key = new Expression_String(Assembly, new SolSourceLocation(ActiveFile, keyNode.Span.Location), keyNode.Token.Text);
                            break;
                        }
                        case "Expression": {
                            key = GetExpression(keyNode);
                            break;
                        }
                        default: {
                            throw new SolInterpreterException(new SolSourceLocation(ActiveFile, keyNode.Span.Location), "Invalid variable source key getter node id \"" + keyNode.Term.Name + "\".");
                        }
                    }
                    return new Expression_GetVariable.IndexedVariable(table, key);
                }
                default: {
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid variable source target node id \"" + node.Term.Name + "\".");
                }
            }
        }

        private void GetIndexedVariableExpressions(ParseTreeNode node, out SolExpression indexable, out SolExpression key)
        {
            ParseTreeNode tableNode = node.ChildNodes[0];
            ParseTreeNode keyNode = node.ChildNodes[1];
            indexable = GetExpression(tableNode);
            switch (keyNode.Term.Name) {
                case "_identifier": {
                    // We are using identifiers instead of names variables since named variables retrieve their 
                    // value from the context directly. However we want to retrieve a value from the table using 
                    // the given key.
                    key = new Expression_String(Assembly, new SolSourceLocation(ActiveFile, keyNode.Span.Location), keyNode.Token.Text);
                    break;
                }
                case "Expression": {
                    key = GetExpression(keyNode);
                    break;
                }
                default: {
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, keyNode.Span.Location), "Invalid variable key getter node id \"" + keyNode.Term.Name + "\".");
                }
            }
        }

        public Statement_AssignVar.TargetRef GetVariableAssignmentTarget(ParseTreeNode node /*, bool local*/)
        {
            switch (node.Term.Name) {
                case "Variable": {
                    // Recursion is fine here, as it will only call it once.
                    return GetVariableAssignmentTarget(node.ChildNodes[0]);
                }
                case "NamedVariable": {
                    return new Statement_AssignVar.NamedVariable(node.ChildNodes[0].Token.Text);
                }
                case "IndexedVariable": {
                    SolExpression indexable;
                    SolExpression key;
                    GetIndexedVariableExpressions(node, out indexable, out key);
                    return new Statement_AssignVar.IndexedVariable(indexable, key);
                }
                default: {
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid variable assignment target node id \"" + node.Term.Name + "\".");
                }
            }
        }

        public SolType GetTypeRef(ParseTreeNode node)
        {
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
                    throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid type reference node id \"" + node.Term.Name + "\".");
            }
            // TypeRef->(":", TypeInfo)
            ParseTreeNode typeInfo = node.ChildNodes[0];
            // TypeInfo->(<type>, TypeInfo_Nullable_opt)
            string type = typeInfo.ChildNodes[0].Token.Text;
            // todo: The Index of nullable is 1 due to the literal ":" 
            // which simply does not disappear despite being marked as 
            // Punctuation.
            ParseTreeNode nullableOpt = typeInfo.ChildNodes[1];
            if (nullableOpt.ChildNodes.Count == 0) {
                return new SolType(type, false);
            }
            // TypeInfo_Nullable_opt->TypeInfo_Nullable_opt-><symbol>
            ParseTreeNode nullableSymbol = nullableOpt.ChildNodes[0].ChildNodes[0];
            string nullableStr = nullableSymbol.Token.Text;
            return new SolType(type, nullableStr[0] == '?');
        }

        public SolParameter[] GetExplicitParameters(ParseTreeNode node)
        {
#if DEBUG
            if (node.Term.Name != "ExplicitParameterList") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid explicit parameter list node id \"" + node.Term.Name + "\".");
            }
#endif
            ParseTreeNodeList childNodes = node.ChildNodes;
            var parameterArray = new SolParameter[childNodes.Count];
            for (int i = 0; i < childNodes.Count; i++) {
                parameterArray[i] = GetParameter(childNodes[i]);
            }
            return parameterArray;
        }

        public SolParameter GetParameter(ParseTreeNode node)
        {
#if DEBUG
            if (node.Term.Name != "Parameter") {
                throw new SolInterpreterException(new SolSourceLocation(ActiveFile, node.Span.Location), "Invalid parameter node id \"" + node.Term.Name + "\".");
            }
#endif
            string name = node.ChildNodes[0].Token.Text;
            // Parameter->(<name>, TypeRef_opt)
            ParseTreeNode typeRefOpt = node.ChildNodes[1];
            return new SolParameter(name, GetTypeRef(typeRefOpt));
        }

        #region Nested type: MetaItem

        private class MetaItem
        {
            public SolClassBuilder ActiveClass;
        }

        #endregion
    }
}