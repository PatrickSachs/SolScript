using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using PSUtility.Reflection;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Properties;

namespace SolScript.Utility
{
    /// <summary>
    ///     This class contains several helper methods for SolScript. These methods are not directly related to SolScript(or
    ///     not meant to be used by users) and thus not not exposed in the public API.
    /// </summary>
    internal static class InternalHelper
    {
        /// <summary>
        /// Writes the source location to the binary writer.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="context">The compilation context.</param>
        /// <exception cref="IOException">An I/O error occurs. </exception>
        internal static void CompileTo(this SourceLocation location, BinaryWriter writer, SolCompliationContext context)
        {
            uint fileIndex = context.FileIndexOf(location.File);
            writer.Write(fileIndex);
            // Lines over 65k are not supported. This saves us a ton of bytes.
            writer.Write(location.Position);
            writer.Write((ushort)location.Line);
            writer.Write((ushort)location.Column);
        }

        internal class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
            private ReferenceEqualityComparer() { }
            /// <inheritdoc />
            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            /// <inheritdoc />
            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }

        internal const string O_PARSER_MSG = "Only used by the parser. Please use a different overload instead.";
        internal const bool O_PARSER_ERR = true;

        private static readonly ReadOnlyHashSet<Type> s_FuncGenericTypes = new PSHashSet<Type> {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>)
        }.AsReadOnly();

        private static readonly ReadOnlyHashSet<Type> s_ActionGenericTypes = new PSHashSet<Type> {
            typeof(Action),
            typeof(Action<>),
            typeof(Action<,>),
            typeof(Action<,,>),
            typeof(Action<,,,>)
        }.AsReadOnly();

        /*internal static SolSourceLocation Location(this ParseTreeNode @this, string file)
        {
            return @this.Span.Location.ToSol(file);
        }

        internal static SolSourceLocation ToSol(this SourceLocation @this, string fileName)
        {
            return new SolSourceLocation(fileName, @this);
        }*/

        public static string ToString(this string @this, params object[] args)
        {
            if (@this == null) {
                return @"null";
            }
            return string.Format(@this, args);
        }

        public static bool IsOverride(this MethodInfo method)
        {
            return !method.Equals(method.GetBaseDefinition());
        }

        /*/// <summary>
        ///     Converts annotation builders into annotation definitions. Validates the annotations type and type mode.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="data">The annotation data.</param>
        /// <exception cref="SolMarshallingException">An annotation class does not exist/is not an annotation.</exception>
        public static Array<SolAnnotationDefinition> AnnotationsFromData(SolAssembly assembly, IReadOnlyList<SolAnnotationBuilder> data)
        {
            var annotations = new Array<SolAnnotationDefinition>(data.Count);
            for (int i = 0; i < annotations.Length; i++) {
                SolClassDefinition annotationDefinition;
                if (!assembly.TryGetClass(data[i].Name, out annotationDefinition)) {
                    throw new SolMarshallingException(data[i].Name, "The class used as annotation \"" + data[i].Name + "\" does not exist.");
                }
                if (annotationDefinition.TypeMode != SolTypeMode.Annotation) {
                    throw new SolMarshallingException(data[i].Name, "The class \"" + annotationDefinition.Type + "\" used as annotation is not an annotation.");
                }
                annotations[i] = new SolAnnotationDefinition(data[i].Location, annotationDefinition, data[i].Arguments);
            }
            return annotations;
        }*/

        /// <summary>
        ///     Checks if a <see cref="SolValue" /> is <see cref="SolNil" /> or null.
        /// </summary>
        /// <param name="value">The <see cref="SolValue" /> to check.</param>
        /// <returns>true if the see <see cref="SolValue" /> is <see cref="SolNil" />, false if not.</returns>
        public static bool IsNil([CanBeNull] this SolValue value)
        {
            return value == null || value.Type == SolNil.TYPE;
        }

        /// <summary>
        ///     Checks if the given type is an <see cref="Action" />.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is an action, false if not.</returns>
        /// <exception cref="ArgumentException">The type is not an open geneic type.</exception>
        public static bool IsOpenGenericAction(Type type)
        {
            if (type.ContainsGenericParameters) {
                throw new ArgumentException("The type is not an open generic type.", nameof(type));
            }
            return s_ActionGenericTypes.Contains(type);
        }

        /// <summary>
        ///     Checks if <paramref name="mainArray" /> contains <paramref name="contentArray" /> starting at index
        ///     <paramref name="mainStartIndex" />.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="mainArray">The array to check for contents.</param>
        /// <param name="contentArray">The content array.</param>
        /// <param name="mainStartIndex">
        ///     The index in <paramref name="mainArray" /> <paramref name="contentArray" /> has to start
        ///     at.
        /// </param>
        /// <param name="referenceEquals">If this is true values will be compared by reference, if false by equality.</param>
        /// <returns>true if <paramref name="mainArray" /> contained <paramref name="contentArray" />, false if not.</returns>
        public static bool ArrayContainsAt<T>(T[] mainArray, T[] contentArray, int mainStartIndex = 0, bool referenceEquals = true)
        {
            if (mainArray.Length < contentArray.Length + mainStartIndex) {
                return false;
            }
            for (int i = 0; i < contentArray.Length; i++) {
                T main = mainArray[i + mainStartIndex];
                if (referenceEquals) {
                    if (!ReferenceEquals(main, contentArray[i])) {
                        return false;
                    }
                } else {
                    if (!main.Equals(contentArray[i])) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///     Checks if the given type is a <see cref="Func{TResult}" />.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is a func, false if not.</returns>
        /// <exception cref="ArgumentException">The type is not an open geneic type.</exception>
        public static bool IsOpenGenericFunc(Type type)
        {
            if (type.ContainsGenericParameters) {
                throw new ArgumentException("The type is not an open geneic type.", nameof(type));
            }
            return s_FuncGenericTypes.Contains(type);
        }

        /// <summary>
        ///     This method uniformly creates an exception for the result of a variable get exception. You may wish to check for
        ///     <see cref="VariableState.Success" /> beforehand.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="state">The state of the variable get operation.</param>
        /// <param name="exception">A wrapped exception.</param>
        /// <param name="location">The location in code.</param>
        /// <returns>The exception, ready to be thrown.</returns>
        /// <remarks>This method does NOT THROW the exception, only create the exception object.</remarks>
        internal static SolVariableException CreateVariableGetException(string name, VariableState state, Exception exception, SourceLocation location)
        {
            switch (state) {
                case VariableState.Success:
                    return new SolVariableException(location, $"Cannot get the value of variable \"{name}\" - The operation was not expected to succeed.", exception);
                case VariableState.FailedCouldNotResolveNativeReference:
                    return new SolVariableException(location, $"Cannot get the value of variable \"{name}\" - The underlying native object could not be resolved.", exception);
                case VariableState.FailedNotAssigned:
                    return new SolVariableException(location, $"Cannot get the value of variable \"{name}\" - The variable has not been assigned.", exception);
                case VariableState.FailedNotDeclared:
                    return new SolVariableException(location, $"Cannot get the value of variable \"{name}\" - The variable has not been declared.", exception);
                case VariableState.FailedTypeMismatch:
                    return new SolVariableException(location, $"Cannot get the value of variable \"{name}\" - A type mismatch occured.", exception);
                case VariableState.FailedNativeException:
                    return new SolVariableException(location, $"Cannot get the value of variable \"{name}\" - A native error occured.", exception);
                case VariableState.FailedRuntimeError:
                    return new SolVariableException(location, $"Cannot get the value of variable \"{name}\" - A runtime error occured.", exception);
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        /// <summary>
        ///     This method uniformly creates an exception for the result of a variable set exception. You may wish to check for
        ///     <see cref="VariableState.Success" /> beforehand.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="state">The state of the variable set operation.</param>
        /// <param name="exception">A wrapped exception.</param>
        /// <param name="location">The location in code.</param>
        /// <returns>The exception, ready to be thrown.</returns>
        /// <remarks>This method does NOT THROW the exception, only create the exception object.</remarks>
        internal static SolVariableException CreateVariableSetException(string name, VariableState state, Exception exception, SourceLocation location)
        {
            switch (state) {
                case VariableState.Success:
                    return new SolVariableException(location, $"Cannot set the value of variable \"{name}\" - The operation was not expected to succeed.", exception);
                case VariableState.FailedCouldNotResolveNativeReference:
                    return new SolVariableException(location, $"Cannot set the value of variable \"{name}\" - The underlying native object could not be resolved.", exception);
                case VariableState.FailedNotAssigned:
                    return new SolVariableException(location, $"Cannot set the value of variable \"{name}\" - The variable has not been assigned.", exception);
                case VariableState.FailedNotDeclared:
                    return new SolVariableException(location, $"Cannot set the value of variable \"{name}\" - The variable has not been declared.", exception);
                case VariableState.FailedTypeMismatch:
                    return new SolVariableException(location, $"Cannot set the value of variable \"{name}\" - A type mismatch occured.", exception);
                case VariableState.FailedNativeException:
                    return new SolVariableException(location, $"Cannot set the value of variable \"{name}\" - A native error occured.", exception);
                case VariableState.FailedRuntimeError:
                    return new SolVariableException(location, $"Cannot set the value of variable \"{name}\" - A runtime error occured.", exception);
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }


        internal static T[] ArrayFilledWith<T>(T value, int length)
        {
            var array = new T[length];
            for (int i = 0; i < length; i++) {
                array[i] = value;
            }
            return array;
        }

/*
        private class LayeredRecursionWorker
        {
            private readonly IEnumerable<ParseTreeNode> m_Nodes;
            private readonly bool m_Backwards;

            public LayeredRecursionWorker(IEnumerable<ParseTreeNode> nodes, bool backwards = false)
            {
                m_Nodes = nodes;
                m_Backwards = backwards;
            }

            public ParseTreeNode Query(Predicate<ParseTreeNode> predicate)
            {
                foreach (ParseTreeNode node in m_Nodes)
                {
                    if (predicate(node)) {
                        return node;
                    }
                }
                var worked = Work(m_Nodes);

            }
            
            private IEnumerable<ParseTreeNode> Work(IList<ParseTreeNode> list)
            {
                foreach (ParseTreeNode parseTreeNode in m_Backwards ? list.Reverse() : list) {
                    if (m_Backwards) {
                        var childrenArray = parseTreeNode.ChildNodes.ToArray();
                        for (int i = childrenArray.Length - 1; i >= 0; i--) {
                            yield return childrenArray[i];
                        }
                    } else {
                        foreach (ParseTreeNode childNode in parseTreeNode.ChildNodes) {
                            yield return childNode;
                        }
                    }
                }
            }
        }
        */

        [CanBeNull]
        internal static ParseTreeNode FindChildByName(this ParseTreeNodeList @this, string name, NodeRecursionMode recursive = NodeRecursionMode.Layer, int maxDepth = -1)
        {
            if (maxDepth == 0) {
                return null;
            }
            switch (recursive) {
                case NodeRecursionMode.None: {
                    return @this.Find(p => p.Term.Name == name);
                }
                case NodeRecursionMode.Layer: {
                    ParseTreeNode found;
                    if ((found = @this.Find(p => p.Term.Name == name)) != null) {
                        return found;
                    }
                    var worker = new PSList<ParseTreeNode>(@this);
                    foreach (ParseTreeNode child in @this) {
                        ParseTreeNodeList childList = child.ChildNodes;
                        if (childList == null || childList.Count == 0) {
                            continue;
                        }
                        found = FindChildByName(child.ChildNodes, name, NodeRecursionMode.Layer, maxDepth - 1);
                        if (found != null) {
                            return found;
                        }
                    }
                    return null;
                }
                case NodeRecursionMode.Direct: {
                    foreach (ParseTreeNode child in @this) {
                        ParseTreeNode found = FindChildByName(child, name, NodeRecursionMode.Direct, true, maxDepth - 1);
                        if (found != null) {
                            return found;
                        }
                    }
                    return null;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(recursive), recursive, null);
            }
        }

        internal static string Name(this ParseTreeNode @this)
        {
            return @this.Term.Name;
        }

        [CanBeNull]
        internal static ParseTreeNode FindChildByName(this ParseTreeNode @this, string name, NodeRecursionMode recursive = NodeRecursionMode.Layer, bool allowSelf = true, int maxDepth = -1)
        {
            if (maxDepth == 0) {
                return null;
            }
            if (allowSelf && @this.Term.Name == name) {
                return @this;
            }
            return FindChildByName(@this.ChildNodes, name, recursive, maxDepth);
        }

        [NotNull, DebuggerStepThrough]
        internal static T NotNull<T>([CanBeNull] this T @this)
        {
#if DEBUG
            if (@this == null) {
                throw new NullReferenceException("Unexpected null value!");
            }
#endif
            return @this;
        }

        [DebuggerStepThrough]
        internal static Terminators BuildTerminators(bool incReturn, bool incBreak, bool incContinue)
        {
            Terminators terminators = Terminators.None;
            if (incReturn) {
                terminators |= Terminators.Return;
            }
            if (incBreak) {
                terminators |= Terminators.Break;
            }
            if (incContinue) {
                terminators |= Terminators.Continue;
            }
            return terminators;
        }

        [DebuggerStepThrough]
        internal static bool DidReturn(Terminators terminators)
        {
            return (terminators & Terminators.Return) == Terminators.Return;
        }

        [DebuggerStepThrough]
        internal static bool DidBreak(Terminators terminators)
        {
            return (terminators & Terminators.Break) == Terminators.Break;
        }

        [DebuggerStepThrough]
        internal static bool DidContinue(Terminators terminators)
        {
            return (terminators & Terminators.Continue) == Terminators.Continue;
        }

        /// <summary>
        ///     Converts a number to an integer.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        /// <param name="integer">The resulting integer.</param>
        /// <returns>True if the integer is the exact same as the number, false if not.</returns>
        internal static bool NumberToInteger(SolNumber number, out int integer)
        {
            integer = (int) number.Value;
            if (integer == number.Value) {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Invokes the given <see cref="MethodBase" /> in a sandbox. This means that all exceptions will be catched and neatly
        ///     wrapped in SolScript compatible exceptions.
        /// </summary>
        /// <param name="context">The exceution context. Required to generate the stack trace.</param>
        /// <param name="method">The method to call.</param>
        /// <param name="target">The object to call the method on.</param>
        /// <param name="arguments">The method arguments.</param>
        /// <returns>The return value of the method call.</returns>
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured. Excecution may have to be halted.</exception>
        [CanBeNull]
        internal static object SandboxInvokeMethod(SolExecutionContext context, MethodBase method, [CanBeNull] object target, object[] arguments)
        {
            object nativeObject;
            try {
                ConstructorInfo ctorInfo = method as ConstructorInfo;
                if (ctorInfo != null) {
                    if (target != null) {
                        throw new InvalidOperationException("Invalid native object to call native method \"" + method.Name + "\" on: Constrcutors cannot be called on an object.");
                    }
                    nativeObject = ctorInfo.Invoke(arguments);
                } else {
                    nativeObject = method.Invoke(target, arguments);
                }
            } catch (Exception ex) {
                if (ex is TargetInvocationException) {
                    if (ex.InnerException is SolRuntimeException) {
                        // If the method threw a proper runtime exception we'll just let it bubble through.
                        throw (SolRuntimeException) ex.InnerException;
                    }
                    if (ex.InnerException is SolRuntimeNativeException) {
                        SolRuntimeNativeException native = (SolRuntimeNativeException) ex.InnerException;
                        // ReSharper disable once ThrowFromCatchWithNoInnerException
                        // Throwing with inner exception since we want to "convert" the NativeRuntime-
                        // Exception and swallow the InvocationException.
                        throw new SolRuntimeException(context, native.Message, native.InnerException);
                    }
                    // ReSharper disable once ThrowFromCatchWithNoInnerException
                    // Throwing with inner exception since we want to have the error message that created the 
                    // TargetInvocationException and not the target invocation exception itself.
                    throw new SolRuntimeException(context, "A native exception occured while calling this instance function.", ex.InnerException);
                }
                if (ex is TargetException) {
                    throw new InvalidOperationException("Invalid native object to call native method \"" + method.Name + "\" on: " + ex.Message, ex);
                }
                if (ex is TargetParameterCountException) {
                    throw new InvalidOperationException("Invalid native parameter count to call native method \"" + method.Name + "\" with: " + ex.Message, ex);
                }
                if (ex is MethodAccessException) {
                    throw new InvalidOperationException("Cannot access native method \"" + method.Name + "\": " + ex.Message, ex);
                }
                throw new InvalidOperationException("An unspecified internal error occured while calling native method \"" + method.Name + "\": " + ex.Message, ex);
            }
            return nativeObject;
        }

        /*/// <summary>
        ///     Creates the given annotations.
        /// </summary>
        /// <param name="context">The context to use for calling their constructors.</param>
        /// <param name="variables">The variables to evaluate their constructor expressions in.</param>
        /// <param name="definitions">The annotation definitions.</param>
        /// <param name="provider">
        ///     (Optional) If the annotations are on a native object pass it here. This allows us to use the type bound attribute instance instead of creating a new instance. 
        /// </param>
        /// <returns>The annotation instances.</returns>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        public static SolClass[] CreateAnnotations(SolExecutionContext context, IVariables variables, 
            IReadOnlyList<SolAnnotationDefinition> definitions, ICustomAttributeProvider provider)
        {
            var annotations = new SolClass[definitions.Count];
            for (int i = 0; i < annotations.Length; i++) {
                SolAnnotationDefinition annotation = definitions[i];
                var annotationArgs = new SolValue[annotation.Arguments.Count];
                for (int j = 0; j < annotationArgs.Length; j++) {
                    annotationArgs[j] = annotation.Arguments[j].Evaluate(context, variables);
                }
                SolClass annotationInstance;
                DynamicReference describedRef = null;
                DynamicReference descriptorRef = null;
                if (provider != null)
                {
                    // If we are subclassing attribute 
                    if (annotation.Definition.DescriptorType.IsSubclassOf(typeof(Attribute)))
                    {
                        descriptorRef = new StaticAttributeRef(provider, annotation.Definition.DescriptorType);
                    }
                    if (annotation.Definition.DescribedType == annotation.Definition.DescriptorType) {
                        describedRef = descriptorRef;
                    }
                    else if (annotation.Definition.DescribedType.IsSubclassOf(typeof(Attribute)))
                    {
                        descriptorRef = new StaticAttributeRef(provider, annotation.Definition.DescribedType);
                    }
                }
                if (provider != null && (annotation.Definition.DescriptorType?.IsSubclassOf(typeof(Attribute)) ?? false)) {
                    // We cannot create the instance of new native attributes(or should not). Thus we need some special handling for them.
                    // By default the static attribute is used. The instance is typically then overridden by annotation creation methods.
                    annotationInstance = annotation.Assembly.New(annotation.Definition, AnnotationClassCreationOptionsNoCtor, annotationArgs);
                    DynamicReference attributeReference = new StaticAttributeRef(provider, annotation.Definition.DescriptorType);
                    SolClass.Inheritance inheritance = annotationInstance.InheritanceChain;
                    while (inheritance != null) {
                        inheritance.NativeReference = attributeReference;
                        inheritance = inheritance.BaseInheritance;
                    }
                } else {
                    annotationInstance = annotation.Assembly.New(annotation.Definition, AnnotationClassCreationOptions, annotationArgs);
                }
                annotations[i] = annotationInstance;
            }
            return annotations;
        }*/


        /// <summary>
        ///     Creates an instance of the given object using <see cref="Activator.CreateInstance(Type, object[])" />.
        /// </summary>
        /// <typeparam name="T">The exception type to throw.</typeparam>
        /// <param name="type">The type to create.</param>
        /// <param name="arguments">The constructor arguments.</param>
        /// <param name="exceptionFactory">A delegate to create the exception instances(1: Exception message, 2: Inner Exception).</param>
        /// <returns>The object.</returns>
        /// <exception cref="Exception">An error occured while creating the object(Actual type: <typeparamref name="T" />).</exception>
        internal static object SandboxCreateObject<T>(Type type, object[] arguments, Func<string, Exception, T> exceptionFactory) where T : Exception
        {
            try {
                return Activator.CreateInstance(type, arguments);
            } catch (TargetInvocationException ex) {
                throw exceptionFactory($"An exception occured while invoking the constructor of type \"{type}\".", ex);
            } catch (MethodAccessException ex) {
                throw exceptionFactory($"No access to the constructor of type \"{type}\".", ex);
            } catch (MemberAccessException ex) {
                throw exceptionFactory($"Cannot instantiate abstract class \"{type}\".", ex);
            } catch (InvalidComObjectException ex) {
                throw exceptionFactory($"Invalid Com Object of type \"{type}\".", ex);
            } catch (COMException ex) {
                throw exceptionFactory($"\"{type}\" is a COM object but the class identifier used to obtain the type is invalid, or the identified class is not registered.", ex);
            } catch (TypeLoadException ex) {
                throw exceptionFactory($"\"{type}\" is not a valid type.", ex);
            } catch (ArgumentException ex) {
                throw exceptionFactory($"\"{type}\" is an open generic type. ", ex);
            }
        }

        /// <summary>
        ///     Quick helper to create a number object of the given type with the given value.
        /// </summary>
        /// <param name="numberType">The type of the number you wish to create.</param>
        /// <param name="value">The value of the number. May loose accuracy(e.g. if using an integer).</param>
        /// <param name="number">The number. Only valid if the method returned true.</param>
        /// <param name="nullable">Should nullable number types be checked aswell?</param>
        /// <param name="isNull">Only valid if <paramref name="nullable" /> is true - Should the value of nullable types be null?</param>
        /// <returns>true if the number object could be created, false if not(e.g. type is not a numeric type).</returns>
        /// <exception cref="SolMarshallingException">An exception occured while creating the number value.</exception>
        /// <remarks>Supported are: double, float, int, uint, long, ulong, short, ushort, byte, sbyte, char, decimal</remarks>
        [ContractAnnotation("number:null => false")]
        internal static bool TryNumberObject(Type numberType, double value, out object number, bool nullable = true, bool isNull = false)
        {
            try {
                if (numberType == typeof(int)) {
                    number = (int) value;
                    return true;
                }
                if (numberType == typeof(double)) {
                    number = value;
                    return true;
                }
                if (numberType == typeof(float)) {
                    number = (float) value;
                    return true;
                }
                if (numberType == typeof(long)) {
                    number = (long) value;
                    return true;
                }
                if (numberType == typeof(uint)) {
                    number = (uint) value;
                    return true;
                }
                if (numberType == typeof(ulong)) {
                    number = (ulong) value;
                    return true;
                }
                if (numberType == typeof(short)) {
                    number = (short) value;
                    return true;
                }
                if (numberType == typeof(ushort)) {
                    number = (ushort) value;
                    return true;
                }
                if (numberType == typeof(byte)) {
                    number = (byte) value;
                    return true;
                }
                if (numberType == typeof(sbyte)) {
                    number = (sbyte) value;
                    return true;
                }
                if (numberType == typeof(char)) {
                    number = (char) (short) value;
                    return true;
                }
                if (numberType == typeof(decimal)) {
                    number = new decimal(value);
                    return true;
                }
                if (nullable) {
                    if (numberType == typeof(int?)) {
                        number = isNull ? new int?() : (int?) value;
                        return true;
                    }
                    if (numberType == typeof(double?)) {
                        number = isNull ? new double?() : (double?) value;
                        return true;
                    }
                    if (numberType == typeof(float?)) {
                        number = isNull ? new float?() : (float?) value;
                        return true;
                    }
                    if (numberType == typeof(long?)) {
                        number = isNull ? new long?() : (long?) value;
                        return true;
                    }
                    if (numberType == typeof(uint?)) {
                        number = isNull ? new uint?() : (uint?) value;
                        return true;
                    }
                    if (numberType == typeof(ulong?)) {
                        number = isNull ? new ulong?() : (ulong?) value;
                        return true;
                    }
                    if (numberType == typeof(short?)) {
                        number = isNull ? new short?() : (short?) value;
                        return true;
                    }
                    if (numberType == typeof(ushort?)) {
                        number = isNull ? new ushort?() : (ushort?) value;
                        return true;
                    }
                    if (numberType == typeof(byte?)) {
                        number = isNull ? new byte?() : (byte?) value;
                        return true;
                    }
                    if (numberType == typeof(sbyte?)) {
                        number = isNull ? new sbyte?() : (sbyte?) value;
                        return true;
                    }
                    if (numberType == typeof(char?)) {
                        number = isNull ? new char?() : (char?) (short) value;
                        return true;
                    }
                    if (numberType == typeof(decimal?)) {
                        number = isNull ? new decimal?() : (decimal?) new decimal(value);
                        return true;
                    }
                }
            } catch
                (OverflowException ex) {
                throw new SolMarshallingException($"Overflow while creating a \"{numberType.Name}\" number object.", ex);
            }
            number = null;
            return false;
        }

        /// <inheritdoc cref="TryNumberObject" />
        /// <param name="numberType">The type of the number you wish to create.</param>
        /// <param name="value">The value of the number. May loose accuracy(e.g. if using an integer).</param>
        /// <returns>The number object.</returns>
        /// <param name="nullable">Should nullable number types be checked aswell?</param>
        /// <param name="isNull">Only valid if <paramref name="nullable" /> is true - Should the value of nullable types be null?</param>
        /// <returns>true if the number object could be created, false if not(e.g. type is not a numeric type).</returns>
        /// <exception cref="SolMarshallingException">
        ///     The type is not a numeric type/An exception occured while creating the number
        ///     value.
        /// </exception>
        internal static object NumberObject(Type numberType, double value, bool nullable = true, bool isNull = false)
        {
            object number;
            if (!TryNumberObject(numberType, value, out number, nullable, isNull)) {
                throw new SolMarshallingException(numberType, "The type is not a numeric type.");
            }
            return number;
        }

        /// <summary>
        ///     Gets the type of a member info. This method respects the the contracts of said member info.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="member">The name to exctract the info out of.</param>
        /// <returns>The type of this member info. This takes the <see cref="SolContractAttribute" /> into consideration.</returns>
        /// <exception cref="SolMarshallingException">No matching SolType for the return type.</exception>
        internal static SolType GetMemberReturnType(SolAssembly assembly, MethodInfo member)
        {
            SolContractAttribute contract = member.GetCustomAttribute<SolContractAttribute>();
            if (contract != null) {
                return contract.GetSolType();
            }
            return SolMarshal.GetSolType(assembly, member.ReturnType);
        }

        /// <inheritdoc
        ///     cref="GetMemberReturnType(SolScript.Interpreter.SolAssembly,System.Reflection.MethodInfo)" />
        /// <exception cref="SolMarshallingException">No matching SolType for the return type.</exception>
        internal static SolType GetMemberReturnType(SolAssembly assembly, ConstructorInfo member)
        {
            SolContractAttribute contract = member.GetCustomAttribute<SolContractAttribute>();
            if (contract != null) {
                return contract.GetSolType();
            }
            return SolMarshal.GetSolType(assembly, member.DeclaringType);
        }

        /// <inheritdoc
        ///     cref="GetMemberReturnType(SolScript.Interpreter.SolAssembly,System.Reflection.MethodInfo)" />
        /// <exception cref="SolMarshallingException">No matching SolType for the return type.</exception>
        internal static SolType GetMemberReturnType(SolAssembly assembly, FieldOrPropertyInfo member)
        {
            SolContractAttribute contract = member.GetCustomAttribute<SolContractAttribute>();
            if (contract != null) {
                return contract.GetSolType();
            }
            return SolMarshal.GetSolType(assembly, member.DataType);
        }


        internal static string FullName(this MemberInfo @this)
        {
            Type decl = @this.DeclaringType;
            if (decl == null) {
                return @this.Name;
            }
            return decl.FullName + "." + @this.Name;
        }

        internal static string FullName(this ParameterInfo @this)
        {
            MemberInfo decl = @this.Member;
            return decl.FullName() + "." + @this.Name;
        }

        /// <summary>
        ///     Builds a parameterinfo object from the given parameter info array.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="parameterInfo">The parameters.</param>
        /// <returns>The parameter info.</returns>
        /// <exception cref="SolMarshallingException">Failed to marshal a parameter type.</exception>
        internal static SolParameterInfo.Native GetParameterInfo(SolAssembly assembly, ParameterInfo[] parameterInfo)
        {
            if (parameterInfo.Length == 0) {
                return SolParameterInfo.Native.None;
            }
            // If null     -> false 
            // if not null -> true (+ value = optional array element type)
            Type allowOptionalType = null;
            int offsetStart = 0;
            int offsetEnd = 0;
            bool sendContext = false;
            if (parameterInfo[0].ParameterType == typeof(SolExecutionContext)) {
                sendContext = true;
                offsetStart++;
            }
            if (parameterInfo[parameterInfo.Length - 1].GetCustomAttribute<ParamArrayAttribute>() != null) {
                ParameterInfo paramsParameter = parameterInfo[parameterInfo.Length - 1];
                allowOptionalType = paramsParameter.ParameterType.GetElementType();
                offsetEnd++;
            }
            var parameters = new SolParameter[parameterInfo.Length - offsetStart - offsetEnd];
            var marshalTypes = new Type[parameters.Length + (allowOptionalType != null ? 1 : 0)];
            for (int i = offsetStart; i < parameterInfo.Length - offsetEnd; i++) {
                // i is the index in the parameter info array.
                ParameterInfo activeParameter = parameterInfo[i];
                SolContractAttribute customContract = activeParameter.GetCustomAttribute<SolContractAttribute>();
                SolLibraryNameAttribute customName = activeParameter.GetCustomAttribute<SolLibraryNameAttribute>();
                SolType type;
                try {
                    type = customContract?.GetSolType() ?? SolMarshal.GetSolType(assembly, activeParameter.ParameterType);
                } catch (SolMarshallingException ex) {
                    throw new SolMarshallingException(Resources.Err_FailedToBuildNativeParameter.ToString(activeParameter.FullName()), ex);
                }
                parameters[i - offsetStart] = new SolParameter(customName?.Name ?? activeParameter.Name, type);
                marshalTypes[i - offsetStart] = activeParameter.ParameterType;
            }
            if (allowOptionalType != null) {
                marshalTypes[marshalTypes.Length - 1] = allowOptionalType;
            }
            bool allowOptional = allowOptionalType != null;
            return new SolParameterInfo.Native(parameters, marshalTypes, allowOptional, sendContext);
        }

        public static SolClass[] CreateAnnotations(this SolAnnotationDefinition[] definitions, SolExecutionContext context, IVariables variables)
        {
            if (definitions.Length == 0)
            {
                return ArrayUtility.Empty<SolClass>();
            }
            SolClass[] classes = new SolClass[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                var def = definitions[i];
                classes[i] = def.Definition.Assembly.New(def.Definition, ClassCreationOptions.Enforce(), def.Arguments.Evaluate(context, variables));
            }
            return classes;
        }

        public static SolClass[] CreateAnnotations(this IList<SolAnnotationDefinition> definitions, SolExecutionContext context, IVariables variables)
        {
            if (definitions.Count == 0)
            {
                return ArrayUtility.Empty<SolClass>();
            }
            SolClass[] classes = new SolClass[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                classes[i] = def.Definition.Assembly.New(def.Definition, ClassCreationOptions.Enforce(), def.Arguments.Evaluate(context, variables));
            }
            return classes;
        }

        public static SolClass[] CreateAnnotations(this ReadOnlyList<SolAnnotationDefinition> definitions, SolExecutionContext context, IVariables variables)
        {
            if (definitions.Count == 0)
            {
                return ArrayUtility.Empty<SolClass>();
            }
            SolClass[] classes = new SolClass[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                classes[i] = def.Definition.Assembly.New(def.Definition, ClassCreationOptions.Enforce(), def.Arguments.Evaluate(context, variables));
            }
            return classes;
        }

        /// <summary>
        ///     Evaluates an array of expressions.
        /// </summary>
        /// <param name="expressions">The expression array.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="parentVariables">The parent variables.</param>
        /// <returns>The value array.</returns>
        public static SolValue[] Evaluate(this SolExpression[] expressions, SolExecutionContext context, IVariables parentVariables)
        {
            if (expressions.Length == 0)
            {
                return ArrayUtility.Empty<SolValue>();
            }
            var values = new SolValue[expressions.Length];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = expressions[i].Evaluate(context, parentVariables);
            }
            return values;
        }/// <summary>
         ///     Evaluates an array of expressions.
         /// </summary>
         /// <param name="expressions">The expression array.</param>
         /// <param name="context">The execution context.</param>
         /// <param name="parentVariables">The parent variables.</param>
         /// <returns>The value array.</returns>
        public static SolValue[] Evaluate(this IList<SolExpression> expressions, SolExecutionContext context, IVariables parentVariables)
        {
            if (expressions.Count == 0)
            {
                return ArrayUtility.Empty<SolValue>();
            }
            var values = new SolValue[expressions.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = expressions[i].Evaluate(context, parentVariables);
            }
            return values;
        }

        /*/// <summary>
         /// Helper method to obtain the
         /// </summary>
         /// <param name="assembly"></param>
         /// <param name="builder"></param>
         /// <returns></returns>
         internal static SolParameterInfo GetParameterInfo(SolAssembly assembly, SolFunctionBuilder builder)
         {
             var parameters = new SolParameter[builder.Parameters.Count];
             for (int i = 0; i < parameters.Length; i++) {
                 parameters[i] = builder.Parameters[i].Get(assembly);
             }
             if (builder.IsNative) {
                 return new SolParameterInfo.Native(parameters, builder.NativeMarshalTypes.ToArray(), builder.AllowOptionalParameters, builder.NativeSendContext);
             }
             return new SolParameterInfo(parameters, builder.AllowOptionalParameters);
         }

         /// <exception cref="SolMarshallingException">No matching SolType for a parameter type.</exception>
         internal static SolParameterInfo.Native GetParameterInfo(SolAssembly assembly, ParameterInfo[] parameterInfo)
         {
             if (parameterInfo.Length == 0) {
                 return EmptyNativeParameterInfo;
             }
             // If null     -> false 
             // if not null -> true (+ value = optional array element type)
             Type allowOptional = null;
             bool sendContext = false;
             int offsetStart = 0;
             int offsetEnd = 0;
             if (parameterInfo[0].ParameterType == typeof(SolExecutionContext)) {
                 sendContext = true;
                 offsetStart++;
             }
             if (parameterInfo[parameterInfo.Length - 1].GetCustomAttribute<ParamArrayAttribute>() != null) {
                 ParameterInfo paramsParameter = parameterInfo[parameterInfo.Length - 1];
                 allowOptional = paramsParameter.ParameterType.GetElementType();
                 offsetEnd++;
             }
             var solArray = new SolParameter[parameterInfo.Length - offsetStart - offsetEnd];
             var typeArray = new Type[solArray.Length + (allowOptional != null ? 1 : 0)];
             for (int i = offsetStart; i < parameterInfo.Length - offsetEnd; i++) {
                 // i is the index in the parameter info array.
                 ParameterInfo activeParameter = parameterInfo[i];
                 SolContractAttribute customContract = activeParameter.GetCustomAttribute<SolContractAttribute>();
                 SolLibraryNameAttribute customName = activeParameter.GetCustomAttribute<SolLibraryNameAttribute>();
                 solArray[i - offsetStart] = new SolParameter(
                     customName?.Name ?? activeParameter.Name,
                     customContract?.GetSolType() ?? SolMarshal.GetSolType(assembly, activeParameter.ParameterType)
                 );
                 typeArray[i - offsetStart] = activeParameter.ParameterType;
             }
             if (allowOptional != null) {
                 typeArray[typeArray.Length - 1] = allowOptional;
             }
             SolParameterInfo.Native infoClass = new SolParameterInfo.Native(solArray, typeArray, allowOptional != null, sendContext);
             return infoClass;
         }*/

        /*#region Nested type: StaticAttributeRef

        /// <summary>
        ///     Gets a static attribute from a type.
        /// </summary>
        private class StaticAttributeRef : DynamicReference
        {
            /// <summary>
            ///     Creates a new <see cref="StaticAttributeRef" /> instance.
            /// </summary>
            /// <param name="holder">The attribute holder.</param>
            /// <param name="attribute">The attribute type.</param>
            public StaticAttributeRef(ICustomAttributeProvider holder, Type attribute)
            {
                m_Holder = holder;
                m_Attribute = attribute;
            }

            // The attribute type.
            private readonly Type m_Attribute;
            // The attribute holder.
            private readonly ICustomAttributeProvider m_Holder;

            #region Overrides

            /// <inheritdoc />
            public override object GetReference(out GetState refState)
            {
                object[] objs;
                try {
                    objs = m_Holder.GetCustomAttributes(m_Attribute, true);
                } catch (TypeLoadException) {
                    refState = GetState.NotRetrieved;
                    return null;
                } catch (InvalidOperationException) {
                    refState = GetState.NotRetrieved;
                    return null;
                } catch (AmbiguousMatchException) {
                    refState = GetState.NotRetrieved;
                    return null;
                }
                if (objs.Length == 0) {
                    refState = GetState.NotRetrieved;
                    return null;
                }
                refState = GetState.Retrieved;
                return objs[0];
            }

            /// <inheritdoc />
            public override void SetReference(object value, out SetState refState)
            {
                // Cannot assign.
                refState = SetState.NotAssigned;
            }

            #endregion
        }

        #endregion*/


        /*internal static void GetParameterBuilders(ParameterInfo[] parameterInfo, out SolParameterBuilder[] builders, out Type[] marshalTypes, out bool allowOptional, out bool sendContext)
        {
            if (parameterInfo.Length == 0) {
                builders = EmptyArray<SolParameterBuilder>.Value;
                marshalTypes = EmptyArray<Type>.Value;
                allowOptional = false;
                sendContext = false;
                return;
            }
            // If null     -> false 
            // if not null -> true (+ value = optional array element type)
            Type allowOptionalType = null;
            int offsetStart = 0;
            int offsetEnd = 0;
            sendContext = false;
            if (parameterInfo[0].ParameterType == typeof(SolExecutionContext)) {
                sendContext = true;
                offsetStart++;
            }
            if (parameterInfo[parameterInfo.Length - 1].GetCustomAttribute<ParamArrayAttribute>() != null) {
                ParameterInfo paramsParameter = parameterInfo[parameterInfo.Length - 1];
                allowOptionalType = paramsParameter.ParameterType.GetElementType();
                offsetEnd++;
            }
            builders = new SolParameterBuilder[parameterInfo.Length - offsetStart - offsetEnd];
            marshalTypes = new Type[builders.Length + (allowOptionalType != null ? 1 : 0)];
            for (int i = offsetStart; i < parameterInfo.Length - offsetEnd; i++) {
                // i is the index in the parameter info array.
                ParameterInfo activeParameter = parameterInfo[i];
                SolContractAttribute customContract = activeParameter.GetCustomAttribute<SolContractAttribute>();
                SolLibraryNameAttribute customName = activeParameter.GetCustomAttribute<SolLibraryNameAttribute>();
                builders[i - offsetStart] = new SolParameterBuilder(
                    customName?.Name ?? activeParameter.Name,
                    customContract != null ? SolTypeBuilder.Fixed(customContract.GetSolType()) : SolTypeBuilder.Native(activeParameter.ParameterType)
                );
                marshalTypes[i - offsetStart] = activeParameter.ParameterType;
            }
            if (allowOptionalType != null) {
                marshalTypes[marshalTypes.Length - 1] = allowOptionalType;
            }
            allowOptional = allowOptionalType != null;
        }*/
    }
}