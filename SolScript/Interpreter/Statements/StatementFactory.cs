using System;
using System.Collections.Generic;
using System.Linq;
using Irony.Parsing;
using PSUtility.Enumerables;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter.Statements
{
    internal class StatementFactory
    {
        public StatementFactory(SolAssembly assembly)
        {
            Assembly = assembly;
        }

        public readonly SolAssembly Assembly;

        private readonly Stack<MetaItem> m_MetaStack = new Stack<MetaItem>();
        public string ActiveFile { get; set; }

        internal class TreeData
        {
            public readonly IList<SolClassDefinition> Classes = new PSUtility.Enumerables.List<SolClassDefinition>();
            public readonly IList<SolFunctionDefinition> Functions = new PSUtility.Enumerables.List<SolFunctionDefinition>();
            public readonly IList<SolFieldDefinition> Fields = new PSUtility.Enumerables.List<SolFieldDefinition>();
        }

        /// <exception cref="SolInterpreterException">An error occured.</exception>
        public TreeData InterpretTree(ParseTree tree)
        {
            ActiveFile = tree.FileName;
            TreeData data = new TreeData();
            foreach (ParseTreeNode rootedNode in tree.Root.ChildNodes) {
                switch (rootedNode.Term.Name) {
                    case "FunctionWithAccess": {
                        data.Functions.Add(GetFunctionWithAccess(rootedNode));
                        break;
                    }
                    case "FieldWithAccess": {
                        data.Fields.Add(GetFieldWithAccess(rootedNode));
                        break;
                    }
                    case "ClassDefinition": {
                            data.Classes.Add(GetClassDefinition(rootedNode));
                        break;
                    }
                    default: {
                        throw new SolInterpreterException(rootedNode.Span.Location, $"Not supported root node id \"{rootedNode.Term.Name}\".");
                    }
                }
            }
            return data;
        }

        #region Parameters

        public SolParameterInfo GetParameters(ParseTreeNode node)
        {
            ParseTreeNode parametersListNode = node.ChildNodes[0];
            if (parametersListNode.ChildNodes.Count == 0)
            {
                return SolParameterInfo.None;
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
            if (explicitNode == null)
            {
                return optionalNode == null ? SolParameterInfo.None : SolParameterInfo.Any;
            }
            return new SolParameterInfo(GetExplicitParameters(explicitNode), optionalNode != null);
        }


        public SolParameter[] GetExplicitParameters(ParseTreeNode node)
        {
            if (node.Term.Name != "ExplicitParameterList")
            {
                throw new SolInterpreterException(node.Span.Location, "Invalid explicit parameter list node id \"" + node.Term.Name + "\".");
            }
            ParseTreeNodeList childNodes = node.ChildNodes;
            var parameterArray = new SolParameter[childNodes.Count];
            for (int i = 0; i < childNodes.Count; i++)
            {
                parameterArray[i] = GetParameter(childNodes[i]);
            }
            return parameterArray;
        }

        public SolParameter GetParameter(ParseTreeNode node)
        {
            if (node.Term.Name != "Parameter")
            {
                throw new SolInterpreterException(node.Span.Location, "Invalid parameter node id \"" + node.Term.Name + "\".");
            }
            string name = node.ChildNodes[0].Token.Text;
            // Parameter->(<name>, TypeRef_opt)
            ParseTreeNode typeRefOpt = node.ChildNodes[1];
            return new SolParameter(name, GetTypeRef(typeRefOpt));
        }


        #endregion

        public SolClassDefinition GetClassDefinition(ParseTreeNode node)
        {
            if (node.Term.Name != "ClassDefinition") {
                throw new SolInterpreterException(node.Span.Location, "Invalid class node id \"" + node.Term.Name + "\".");
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
            SolClassDefinition classDefinition = new SolClassDefinition();
            classDefinition.Type = className;
            classDefinition.TypeMode = typeMode;
            classDefinition.InjectSourceLocation(node.Span.Location);
            m_MetaStack.Push(new MetaItem {ActiveClass = classDefinition });
            // ===================================================================
            // == Annotations
            // AnnotationList
            //   - Empty | Annotation+
            // Annotation
            //   - "@"
            //   - _identifier
            //   - Arguments_trans
            ParseTreeNode annotationsListNode = node.ChildNodes[0];
            InsertAnnotations(annotationsListNode, classDefinition);
            // Push class builder to meta, so that instance functions can determine that they are instance functions.
            // ===================================================================
            // ClassDefinition_Mixins_opt
            //  - Empty
            // ClassDefinition_Mixins_opt
            //  - "extends"
            //  - _identifier
            ParseTreeNode mixinsNode = node.ChildNodes[4];
            if (mixinsNode.ChildNodes.Count != 0) {
                classDefinition.BaseClassReference = new SolClassDefinitionReference(Assembly, mixinsNode.ChildNodes[1].Token.Text);
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
                        var fieldDefinition = GetFieldWithAccess(classMemberNode);
                        if (classDefinition.FieldLookup.ContainsKey(fieldDefinition.Name)) {
                            throw new SolInterpreterException(classMemberNode.Span.Location, "The field \"" + fieldDefinition.Name + "\" exists multiple times within the class \"" + classDefinition.Type +"\".");
                        }
                            classDefinition.AssignFieldDirect(fieldDefinition);
                        break;
                    }
                    // ===================================================================
                    case "FunctionWithAccess": {
                        SolFunctionDefinition functionDefinition = GetFunctionWithAccess(classMemberNode);
                            if (classDefinition.FunctionLookup.ContainsKey(functionDefinition.Name))
                            {
                                throw new SolInterpreterException(classMemberNode.Span.Location, "The function \"" + functionDefinition.Name + "\" exists multiple times within the class \"" + classDefinition.Type + "\".");
                            }
                            classDefinition.AssignFunctionDirect(functionDefinition);
                        break;
                    }
                    // ===================================================================
                    default: {
                        throw new SolInterpreterException(classMemberNode.Span.Location, $"Invalid class member node id \"{classMemberNode.Term.Name}\".");
                    }
                }
            }
            // ===================================================================
            m_MetaStack.Pop();
            return classDefinition;
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
        public SolFunctionDefinition GetFunctionWithAccess(ParseTreeNode node)
        {
            // todo: annotations in statement factory on functions with access (the name sucks btw)
            // 0 -> AnnotationList
            // 1 -> AccessModifier_opt
            // 2 -> MemberModifier_opt
            // 3 -> "function"
            // 4 -> _identifier
            // 5 -> FunctionParameters
            // 6 -> TypeRef_opt
            // 7 -> FunctionBody
            ParseTreeNode annotationsListNode = node.ChildNodes[0];
            SolAccessModifier accessModifier = GetAccessModifier(node.ChildNodes[1]);
            SolMemberModifier memberModifier = GetMemberModifier(node.ChildNodes[2]);
            ParseTreeNode funcNameNode = node.ChildNodes[4];
            ParseTreeNode parametersNode = node.ChildNodes[5];
            ParseTreeNode typeNode = node.ChildNodes[6];
            ParseTreeNode bodyNode = node.ChildNodes[7];

            string funcName = funcNameNode.Token.Text;
            SolType funcType = GetTypeRef(typeNode);
            SolParameterInfo parameters = GetParameters(parametersNode);
            SolChunk chunk = GetChunk(bodyNode.ChildNodes[0]);
            SolFunctionDefinition functionDefinition = new SolFunctionDefinition {
                Name = funcName,
                AccessModifier = accessModifier,
                MemberModifier = memberModifier,
                ParameterInfo = parameters,
                Chunk = new SolChunkWrapper(chunk),
                ReturnType = funcType
            };
            functionDefinition.InjectSourceLocation(node.Span.Location);
            InsertAnnotations(annotationsListNode, functionDefinition);
            return functionDefinition;
        }

        private void InsertAnnotations(ParseTreeNode node, SolAnnotateableDefinitionBase builder)
        {
            if (node.Term.Name != "AnnotationList") {
                throw new SolInterpreterException(node.Span.Location, "Invalid annotation list: " + node.Term.Name);
            }
            foreach (ParseTreeNode annotationNode in node.ChildNodes) {
                string name = annotationNode.ChildNodes[1].Token.Text;
                SolExpression[] expressions = annotationNode.ChildNodes.Count == 3 ? GetExpressions(annotationNode.ChildNodes[2]) : EmptyArray<SolExpression>.Value;
                builder.AddAnnotation(new SolAnnotationDefinition(new SolClassDefinitionReference(Assembly, name), expressions));
            }
        }

        public SolFieldDefinition GetFieldWithAccess(ParseTreeNode node)
        {
            // 0 AnnotationList 
            // 1 AccessModifier_opt
            // 2 MemberModifier_opt
            // 3 _identifier 
            // 4 TypeRef_opt 
            // 5 Assignment_opt;
            ParseTreeNode annotationsListNode = node.ChildNodes[0];
            SolAccessModifier accessModifier = GetAccessModifier(node.ChildNodes[1]);
            SolMemberModifier memberModifier = GetMemberModifier(node.ChildNodes[2]);
            ParseTreeNode identifierNode = node.ChildNodes[3];
            string fieldName = identifierNode.Token.Text;
            SolType fieldType = GetTypeRef(node.ChildNodes[4]);
            ParseTreeNode assignmentOpt = node.ChildNodes[5];
            SolExpression init = assignmentOpt.ChildNodes.Count != 0
                ? GetExpression(assignmentOpt.ChildNodes[0])
                : null;
            SolFieldDefinition fieldDefinition = new SolFieldDefinition {
                Name = fieldName,
                Initializer = new SolFieldInitializerWrapper(init),
                AccessModifier = accessModifier,
                Type = fieldType
            };
            fieldDefinition.InjectSourceLocation(node.Span.Location);
            /*
            SolFieldBuilder fieldBuilder = SolFieldBuilder.NewScriptField(fieldName, init, init?.Location ?? new SolSourceLocation(ActiveFile, assignmentOpt.Span.Location))
                .SetFieldType(SolTypeBuilder.Fixed(fieldType))
                .SetAccessModifier(accessModifier)
                .SetMemberModifier(memberModifier);*/
            InsertAnnotations(annotationsListNode, fieldDefinition);
            return fieldDefinition;
        }

        private SolMemberModifier GetMemberModifier(ParseTreeNode node)
        {
            if (node.Term.Name == "MemberModifier_opt") {
                if (node.ChildNodes.Count != 1) {
                    return SolMemberModifier.Default;
                }
                node = node.ChildNodes[0];
            }
            switch (node.Token.Text)
            {
                case "default":
                    {
                        return SolMemberModifier.Default;
                    }
                case "override":
                    {
                        return SolMemberModifier.Override;
                    }
                case "abstract": {
                    return SolMemberModifier.Abstract;
                }
                default: {
                    throw new SolInterpreterException(node.Span.Location, "Invalid member modifier node name \"" + node.Token.Text + "\".");
                }
            }
        }

        private SolAccessModifier GetAccessModifier(ParseTreeNode node)
        {
            if (node.Term.Name == "AccessModifier_opt") {
                if (node.ChildNodes.Count != 1) {
                    return SolAccessModifier.Global;
                }
                node = node.ChildNodes[0];
            }
            switch (node.Token.Text)
            {
                case "global":
                    {
                        return SolAccessModifier.Global;
                    }
                case "local":
                    {
                        return SolAccessModifier.Local;
                    }
                case "internal": {
                    return SolAccessModifier.Internal;
                }
                default: {
                    throw new SolInterpreterException(node.Span.Location, "Invalid access modifier node name \"" + node.Token.Text + "\".");
                }
            }
        }

        public SolChunk GetChunk(ParseTreeNode node)
        {
            if (node.Term.Name != "Chunk") {
                throw new SolInterpreterException(node.Span.Location, "Invalid chunk node id \"" + node.Term.Name + "\".");
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
                            : Expression_Nil.InstanceOf(Assembly);
                        returnValue = new Expression_Return(returnExpression);
                            returnValue.InjectSourceLocation(lastStatement.Span.Location);
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
                        throw new SolInterpreterException(lastStatement.ChildNodes[0].Span.Location,
                            "Invalid chunk last statement node id \"" + lastStatement.ChildNodes[0].Term.Name + "\".");
                    }
                }
            }
            SolChunk chunk = new SolChunk(Assembly, node.Span.Location, returnValue, GetStatements(node.ChildNodes[0]));
            chunk.InjectSourceLocation(node.Span.Location);
            return chunk;
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
                throw new SolInterpreterException(node.Span.Location, "Invalid binary expression root node id \"" + node.Term.Name + "\".");
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
                    operation = Expression_Binary.Modulo.Instance;
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
                    throw new SolInterpreterException(opNode.Span.Location, "Invalid binary operation node id \"" + opNode.Term.Name + "\".");
                }
            }
            SolExpression left = GetExpression(leftNode);
            SolExpression right = GetExpression(rightNode);
            var bin = new Expression_Binary(operation, left, right);
            bin.InjectSourceLocation(node.Span.Location);
            return bin;
        }

        public SolExpression GetExpression(ParseTreeNode node)
        {
#if DEBUG
            if (node.Term.Name != "Expression") {
                throw new SolInterpreterException(node.Span.Location, "Invalid expression root node id \"" + node.Term.Name + "\".");
            }
#endif
            ParseTreeNode expressionNode = node.ChildNodes[0];
            switch (expressionNode.Term.Name) {
                case "_string": {
                    string text;
                    try {
                        text = expressionNode.Token.ValueString /*.UnEscape()*/;
                    } catch (ArgumentException ex) {
                        throw new SolInterpreterException(expressionNode.Span.Location, "Failed to parse string: " + ex.Message, ex);
                    }
                    return new Expression_Literal(SolString.ValueOf(text));
                } // _string
                case "_long_string": {
                    string text;
                    try {
                        text = expressionNode.Token.ValueString;
                    } catch (ArgumentException ex) {
                        throw new SolInterpreterException(expressionNode.Span.Location, "Failed to parse long string: " + ex.Message, ex);
                    }
                    return new Expression_Literal(SolString.ValueOf(text));
                } // _string
                case "_number": {
                    return new Expression_Literal(new SolNumber((double)expressionNode.Token.Value));
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
                    AVariable source;
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
                                    key = new Expression_Literal(SolString.ValueOf(keyGetter.Token.Text));
                                    break;
                                }
                                // todo: recursion inside binary-exp might be problematic.
                                case "Expression": {
                                    key = GetExpression(keyGetter);
                                    break;
                                }
                                default: {
                                    throw new SolInterpreterException(keyGetter.Span.Location,
                                        "Invalid indexed variable getter node id \"" + keyGetter.Term.Name + "\".");
                                }
                            }
                            source =  new AVariable.Indexed(indexable, key);
                            break;
                        }
                        case "NamedVariable": {
                            ParseTreeNode identifier = underlying.ChildNodes[0];
                            source = new AVariable.Named(identifier.Token.Text);
                            break;
                        }
                        default: {
                            throw new SolInterpreterException(underlying.Span.Location, "Invalid variable getter node id \"" + underlying.Term.Name + "\".");
                        }
                    }
                    //MetaItem meta = m_MetaStack.Count != 0 ? m_MetaStack.Peek() : null;
                    return new Expression_GetVariable(source);
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
                        /*case "++": {
                            operationRef = Expression_Unary.PlusPlusOperation.Instance;
                            break;
                        }
                        case "--": {
                            operationRef = Expression_Unary.MinusMinusOperation.Instance;
                            break;
                        }*/
                        default: {
                            throw new SolInterpreterException(expressionNode.ChildNodes[0].Span.Location,
                                "Invalid unary expression node id \"" + expressionNode.ChildNodes[0].Term.Name + "\".");
                        }
                    }
                    // todo: recursion inside unary-exp might be problematic.
                    SolExpression expression = GetExpression(expressionNode.ChildNodes[1]);
                    var unar = new Expression_Unary {
                        Operation = operationRef,
                        ValueGetter = expression
                    };
                        unar.InjectSourceLocation(expressionNode.Span.Location);
                    return unar;
                }
                case "Expression_Binary": {
                    return BinaryExpression(expressionNode);
                }
                case "Expression_Tertiary": {
                    SolExpression condition = GetExpression(expressionNode.ChildNodes[0]);
                    SolExpression trueValue = GetExpression(expressionNode.ChildNodes[2]);
                    SolExpression falseValue = GetExpression(expressionNode.ChildNodes[4]);
                    var ter= new Expression_Tertiary(Expression_Tertiary.Conditional.Instance, condition, trueValue, falseValue);
                        ter.InjectSourceLocation(expressionNode.Span.Location);
                    return ter;
                }
                case "Expression_Statement": {
                    SolStatement statement = GetStatement(expressionNode);
                    var expr = new Expression_Statement(statement);
                        expr.InjectSourceLocation(expressionNode.Span.Location);
                    return expr;
                }
                case "Expression_CreateFunc": {
                    SolParameterInfo parameters = GetParameters(expressionNode.ChildNodes[1]);
                    SolType type = GetTypeRef(expressionNode.ChildNodes[2]);
                    SolChunk chunk = GetChunk(expressionNode.ChildNodes[3].ChildNodes[0]);
                        var expr = new Expression_CreateFunc(chunk, type, parameters); ;
                        expr.InjectSourceLocation(expressionNode.Span.Location);
                        return expr;
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
                                key = new Expression_Literal(new SolNumber(nextN++));
                                value = GetExpression(valueNode);
                                break;
                            }
                            case 3: {
                                ParseTreeNode keyNode = fieldNode.ChildNodes[0];
                                ParseTreeNode valueNode = fieldNode.ChildNodes[2];
                                switch (keyNode.Term.Name) {
                                    case "_identifier": {
                                        key = new Expression_Literal(SolString.ValueOf(keyNode.Token.Text));
                                        break;
                                    }
                                    case "Expression": {
                                        key = GetExpression(keyNode);
                                        break;
                                    }
                                    default: {
                                        throw new SolInterpreterException(keyNode.Span.Location, "Invalid table key node id \"" + keyNode.Term.Name + "\".");
                                    }
                                }
                                value = GetExpression(valueNode);
                                break;
                            }
                            default: {
                                throw new SolInterpreterException(fieldNode.Span.Location,
                                    "Invalid table initializer format. A table field must either be in the form of 'X = Y' or 'X'.");
                            }
                        }
                        keys[i] = key;
                        values[i] = value;
                    }
                    return new Expression_TableConstructor(Assembly, expressionNode.Span.Location, keys, values);
                }
                case "Expression_Bool": {
                    switch (expressionNode.ChildNodes[0].Term.Name) {
                        case "true": {
                            var expr = new Expression_Literal(SolBool.True);
                                    expr.InjectSourceLocation(expressionNode.Span.Location);
                            return expr;
                        }
                        case "false": {
                                    var expr= new Expression_Literal(SolBool.False);
                                    expr.InjectSourceLocation(expressionNode.Span.Location);
                                    return expr;
                                }
                        default: {
                            throw new SolInterpreterException(expressionNode.Span.Location,
                                "Invalid bool expression \"" + expressionNode.ChildNodes[0].Term.Name + "\".");
                        }
                    }
                }
                case "Expression_Nil": {
                    return Expression_Nil.InstanceOf(Assembly);
                }
                default: {
                    throw new SolInterpreterException( expressionNode.Span.Location, "Invalid expression node id \"" + expressionNode.Term.Name + "\".");
                }
            }
        }

        public SolStatement[] GetStatements(ParseTreeNode node)
        {
            if (node.Term.Name != "StatementList") {
                throw new SolInterpreterException(node.Span.Location, "Invalid statement list node id \"" + node.Term.Name + "\".");
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
                throw new SolInterpreterException(node.Span.Location, "Invalid statement root node id \"" + node.Term.Name + "\".");
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
                    return new Statement_DeclareVar(Assembly, statementNode.Span.Location, variableNode.Token.Text, type, initialValue);
                } // Statement_DeclareVar
                case "Statement_AssignVar": {
                    // Statement_AssignVar
                    //  - Variable
                    //  - Expression
                    ParseTreeNode variableNode = statementNode.ChildNodes[0];
                    ParseTreeNode expressionNode = statementNode.ChildNodes[1];
                    AVariable variable = GetVariableAssignmentTarget(variableNode);
                    SolExpression expression = GetExpression(expressionNode);
                    MetaItem meta = m_MetaStack.Count != 0 ? m_MetaStack.Peek() : null;
                    return new Statement_AssignVar(Assembly, statementNode.Span.Location, variable, expression, meta?.ActiveClass.Type);
                } // Statement_DeclareVar
                case "Statement_CallFunction": {
                    SolExpression expression = GetExpression(statementNode.ChildNodes[0]);
                    SolExpression[] arguments = statementNode.ChildNodes[1].ChildNodes.Count != 0 ? GetExpressions(statementNode.ChildNodes[1]) : EmptyArray<SolExpression>.Value;
                    MetaItem meta = m_MetaStack.Count != 0 ? m_MetaStack.Peek() : null;
                    return new Statement_CallFunction(Assembly, statementNode.Span.Location, meta?.ActiveClass.Type, expression, new Array<SolExpression>(arguments));
                } // Statement_CallFunction
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
                    return new Statement_For(Assembly,statementNode.Span.Location, init, condition, after, chunk);
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
                    return new Statement_Iterate(Assembly,  statementNode.Span.Location, iterExp, iterName, chunk);
                } // Statement_Iterate
                case "Statement_Do": {
                    // Statement_Do
                    //  - "do"
                    //  - Chunk
                    //  - "end"
                    ParseTreeNode chunkNode = statementNode.ChildNodes[1];
                    SolChunk chunk = GetChunk(chunkNode);
                    return new Statement_Do(Assembly,  statementNode.Span.Location, chunk);
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
                    var st = new Statement_Conditional(new Array<Statement_Conditional.IfBranch>(branches), elseChunk);
                        st.InjectSourceLocation(statementNode.Span.Location);
                    return st;
                } // Statement_Conditional
                case "Statement_New": {
                    // 0: "new"
                    // 1: _identifier
                    // 2: ExpressionList
                    string typeName = statementNode.ChildNodes[1].Token.Text;
                    SolExpression[] expressions = GetExpressions(statementNode.ChildNodes[2]);
                    return new Statement_New(Assembly, statementNode.Span.Location, typeName, expressions);
                } // Statement_New
                case "Statement_Self": {
                    // 0: "self"
                    MetaItem meta;
                    try {
                        meta = m_MetaStack.Peek();
                    } catch (InvalidOperationException ex) {
                        throw new SolInterpreterException(statementNode.Span.Location, "Can only use self keywords inside classes.", ex);
                    }
                    return new Statement_Self(Assembly, statementNode.Span.Location, meta.ActiveClass.Type);
                } // Statement_Self
                case "Statement_Base": {
                    // 0: "base"
                    // 1: _identifier/Expression
                    MetaItem meta;
                    try {
                        meta = m_MetaStack.Peek();
                    } catch (InvalidOperationException ex) {
                        throw new SolInterpreterException(statementNode.Span.Location, "Can only use base keywords inside classes.", ex);
                    }
                    SolExpression expression;
                    switch (statementNode.ChildNodes[1].Term.Name) {
                        case "_identifier": {
                            expression = new Expression_Literal(SolString.ValueOf(statementNode.ChildNodes[1].Token.ValueString));
                            break;
                        }
                        case "Expression": {
                            expression = GetExpression(statementNode.ChildNodes[1]);
                            break;
                        }
                        default: {
                            throw new SolInterpreterException(statementNode.ChildNodes[1].Span.Location,
                                "Invalid base accessor: " + statementNode.ChildNodes[1].Term.Name);
                        }
                    }
                    return new Statement_Base(Assembly, statementNode.Span.Location, expression);
                } // Statement_Base
                default: {
                    throw new SolInterpreterException(statementNode.Span.Location, "Invalid statement node id \"" + statementNode.Term.Name + "\".");
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

        /*public Expression_GetVariable.SourceRef GetVariableSourceTarget(ParseTreeNode node)
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
        }*/

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
                    key = new Expression_Literal(SolString.ValueOf(keyNode.Token.Text));
                    break;
                }
                case "Expression": {
                    key = GetExpression(keyNode);
                    break;
                }
                default: {
                    throw new SolInterpreterException(keyNode.Span.Location, "Invalid variable key getter node id \"" + keyNode.Term.Name + "\".");
                }
            }
        }

        public AVariable GetVariableAssignmentTarget(ParseTreeNode node /*, bool local*/)
        {
            switch (node.Term.Name) {
                case "Variable": {
                    // Recursion is fine here, as it will only call it once.
                    return GetVariableAssignmentTarget(node.ChildNodes[0]);
                }
                case "NamedVariable": {
                    return new AVariable.Named(node.ChildNodes[0].Token.Text);
                }
                case "IndexedVariable": {
                    SolExpression indexable;
                    SolExpression key;
                    GetIndexedVariableExpressions(node, out indexable, out key);
                    return new AVariable.Indexed(indexable, key);
                }
                default: {
                    throw new SolInterpreterException(node.Span.Location, "Invalid variable assignment target node id \"" + node.Term.Name + "\".");
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
                    throw new SolInterpreterException(node.Span.Location, "Invalid type reference node id \"" + node.Term.Name + "\".");
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

        #region Nested type: MetaItem

        private class MetaItem
        {
            public SolClassDefinition ActiveClass;
        }

        #endregion
    }
}