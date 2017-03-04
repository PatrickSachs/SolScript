using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    public sealed class SolClassDefinition : SolAnnotateableDefinitionBase
    {
        // todo: type registry state assertions in this class
        internal SolClassDefinition(SolAssembly assembly, SolSourceLocation location, string type, SolTypeMode typeMode) : base(assembly, location)
        {
            Type = type;
            TypeMode = typeMode;
        }

        private readonly Utility.Dictionary<string, SolFieldDefinition> m_Fields = new Utility.Dictionary<string, SolFieldDefinition>();
        private readonly Utility.Dictionary<string, SolFunctionDefinition> m_Functions = new Utility.Dictionary<string, SolFunctionDefinition>();
        public readonly string Type;
        public readonly SolTypeMode TypeMode;
        public SolClassDefinition BaseClass;

        // Lazily generated
        private System.Collections.Generic.Dictionary<SolMetaKey, MetaFunctionLink> l_MetaFunctions;

        private Array<SolAnnotationDefinition> m_Annotations;

        public Type NativeType;

        private bool DidBuildMetaFunctions => l_MetaFunctions != null;

        /// <inheritdoc />
        public override IReadOnlyList<SolAnnotationDefinition> Annotations {
            get { return m_Annotations; }
            protected set { m_Annotations = new Array<SolAnnotationDefinition>(value.ToArray()); }
        }

        public IReadOnlyCollection<SolFieldDefinition> Fields => m_Fields.Values;
        public IReadOnlyCollection<SolFunctionDefinition> Functions => m_Functions.Values;
        public IReadOnlyCollection<KeyValuePair<string, SolFieldDefinition>> FieldPairs => m_Fields;
        public IReadOnlyCollection<KeyValuePair<string, SolFunctionDefinition>> FunctionPairs => m_Functions;
        public IReadOnlyCollection<string> FieldNames => m_Fields.Keys;
        public IReadOnlyCollection<string> FunctionNames => m_Functions.Keys;

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return $"SolClassDefinition(Type={Type}, Fields={m_Fields.Count}, Functions={m_Functions.Count})";
        }

        #endregion

        /// <summary>
        ///     Gets the reversed inheritance chain of this definition, first containing the most abstract definition and last the
        ///     most derived one(this one).
        /// </summary>
        /// <returns>A stack containing the class definitions.</returns>
        public Stack<SolClassDefinition> GetInheritanceReversed()
        {
            var stack = new Stack<SolClassDefinition>();
            SolClassDefinition definition = this;
            while (definition != null) {
                stack.Push(definition);
                definition = definition.BaseClass;
            }
            return stack;
        }

        /// <summary>
        /// Sets the annotations of this class definition.
        /// </summary>
        /// <param name="annotations">The annotations.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassHulls"/> state.</remarks>
        internal void SetAnnotations(params SolAnnotationDefinition[] annotations)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassHulls, SolAssembly.AssertMatch.Exact, "Class annotations can only be set during the generation of class bodies.");
            m_Annotations = new Array<SolAnnotationDefinition>(annotations);
        }

        /// <summary>Creates the meta function lookup for the class definition.</summary>
        /// <exception cref="SolVariableException">The meta function could be found but was in an invalid state(e.g. wrong type, accessor...).</exception>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.</remarks>
        private void BuildMetaFunctions()
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Class meta functions can only be built once the class bodies have been generated.");
            l_MetaFunctions = new System.Collections.Generic.Dictionary<SolMetaKey, MetaFunctionLink>();
            FindAndRegisterMetaFunction(SolMetaKey.__new);
            FindAndRegisterMetaFunction(SolMetaKey.__to_string);
            FindAndRegisterMetaFunction(SolMetaKey.__getn);
            FindAndRegisterMetaFunction(SolMetaKey.__is_equal);
            FindAndRegisterMetaFunction(SolMetaKey.__iterate);
            FindAndRegisterMetaFunction(SolMetaKey.__mod);
            FindAndRegisterMetaFunction(SolMetaKey.__exp);
            FindAndRegisterMetaFunction(SolMetaKey.__div);
            FindAndRegisterMetaFunction(SolMetaKey.__add);
            FindAndRegisterMetaFunction(SolMetaKey.__sub);
            FindAndRegisterMetaFunction(SolMetaKey.__mul);
            FindAndRegisterMetaFunction(SolMetaKey.__concat);
            if (TypeMode == SolTypeMode.Annotation) {
                FindAndRegisterMetaFunction(SolMetaKey.__a_pre_new);
                FindAndRegisterMetaFunction(SolMetaKey.__a_post_new);
                FindAndRegisterMetaFunction(SolMetaKey.__a_get_variable);
                FindAndRegisterMetaFunction(SolMetaKey.__a_set_variable);
                FindAndRegisterMetaFunction(SolMetaKey.__a_call_function);
            }
        }

        /// <summary>Tries to get the meta function for the given key.</summary>
        /// <param name="meta">The meta function key.</param>
        /// <param name="link">The meta function linker. Only valid if method returned true.</param>
        /// <returns>true if the meta function exists, false if not.</returns>
        /// <exception cref="SolVariableException"> The meta function could be found but was in an invalid state(e.g. wrong type, accessor...). </exception>
        /// <exception cref="ArgumentNullException"><paramref name="meta" /> is null.</exception>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.</remarks>
        [ContractAnnotation("link:null => false")]
        public bool TryGetMetaFunction([NotNull] SolMetaKey meta, [CanBeNull] out MetaFunctionLink link)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Class meta functions can only be obtained once the class bodies have been generated.");
            if (!DidBuildMetaFunctions) {
                BuildMetaFunctions();
            }
            return l_MetaFunctions.TryGetValue(meta, out link);
        }

        /// <summary>
        ///     Tries to find and add a meta function.
        /// </summary>
        /// <param name="meta">The meta function helper.</param>
        /// <param name="parameters">The parameter types of the meta function.</param>
        /// <returns>true if said meta function could be found, false if not.</returns>
        /// <exception cref="SolVariableException">
        ///     The meta function could be found but was in an invalid state(e.g. wrong type,
        ///     accessor...).
        /// </exception>
        /// <remarks>This method does NOT assert state!</remarks>
        // This method is meant for usage from inside BuildMetaFunctions.
        private bool FindAndRegisterMetaFunction(SolMetaKey meta, params SolType[] parameters)
        {
            // todo: find meta function parameters
            SolFunctionDefinition definition;
            if (TryGetFunction(meta.Name, false, out definition)) {
                if (definition.AccessModifier != SolAccessModifier.Local && definition.AccessModifier != SolAccessModifier.Internal) {
                    throw new SolVariableException($"The meta function \"{meta.Name}\" of class \"{Type}\" must either be local or internal.");
                }
                if (!meta.Type.IsCompatible(Assembly, definition.ReturnType)) {
                    throw new SolVariableException(
                        $"The return type \"{definition.ReturnType}\" of meta function \"{meta.Name}\" in class \"{Type}\" is not compatible with the required return type \"{meta.Type}\"");
                }
                l_MetaFunctions.Add(meta, new MetaFunctionLink(meta, definition));
                return true;
            }
            return false;
        }

        /// <summary> Checks if the class does extend the given class at some point. </summary>
        /// <param name="className">The class name.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>
        ///     Warning: A class does not extend itself and will thus return false if
        ///     the own class name is passed.<br/>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.
        /// </remarks>
        public bool DoesExtendInHierarchy(string className)
        {
            SolClassDefinition definedIn;
            return DoesExtendInHierarchy(className, out definedIn);
        }

        /// <summary> Checks if the class does extend the given class at some point. </summary>
        /// <param name="className">The class name.</param>
        /// <param name="definedIn">The class definition of the extended class. Only valid if the method returned true.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>
        ///     Warning: A class does not extend itself and will thus return false if
        ///     the own class name is passed.<br></br>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.
        /// </remarks>
        [ContractAnnotation("definedIn:null => false")]
        public bool DoesExtendInHierarchy(string className, [CanBeNull]out SolClassDefinition definedIn)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Class bodies need to be generated before inheritance can be checked.");
            definedIn = BaseClass;
            while (definedIn != null) {
                if (definedIn.Type == className) {
                    return true;
                }
                definedIn = definedIn.BaseClass;
            }
            return false;
        }

        /// <summary>
        ///     Can an instance of this class be created by the user? Only
        ///     <see cref="SolTypeMode.Default" /> and <see cref="SolTypeMode.Sealed" />
        ///     classes can be created using the "new" keyword.
        /// </summary>
        /// <returns>True if the class be created using new, false if not.</returns>
        public bool CanBeCreated()
        {
            switch (TypeMode) {
                case SolTypeMode.Default:
                case SolTypeMode.Sealed:
                    return true;
                case SolTypeMode.Singleton:
                case SolTypeMode.Annotation:
                case SolTypeMode.Abstract:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Can the class be inherited?
        /// </summary>
        /// <returns>True if the class can be inherited, false if not.</returns>
        public bool CanBeInherited()
        {
            // todo: better inheritance. e.g. allow annotations to extend annotations.
            switch (TypeMode) {
                case SolTypeMode.Default:
                case SolTypeMode.Abstract:
                    return true;
                case SolTypeMode.Annotation:
                case SolTypeMode.Singleton:
                case SolTypeMode.Sealed:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Gets a function definition in this class definition. </summary>
        /// <param name="name"> The name of the function. </param>
        /// <param name="declaredOnly">
        ///     If this parameter is true only functions declared
        ///     directly in this class will be searched.
        /// </param>
        /// <param name="definition">
        ///     The returned function. - Only valid if the method
        ///     returned true.
        /// </param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>This does not account for the <see cref="SolAccessModifier" /> of the function. Use the overload accepting a
        ///     delegate if you wish to provide custom matching behavior.<br/>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.</remarks>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetFunction(string name, bool declaredOnly, [CanBeNull] out SolFunctionDefinition definition)
        {
            return TryGetFunction(name, declaredOnly, out definition, functionDefinition => true);
        }

        /// <summary> Gets a function definition in this class definition. </summary>
        /// <param name="name"> The name of the function. </param>
        /// <param name="declaredOnly">
        ///     If this parameter is true only functions declared
        ///     directly in this class will be searched.
        /// </param>
        /// <param name="definition">
        ///     The returned function. - Only valid if the method
        ///     returned true.
        /// </param>
        /// <param name="validator">
        ///     This delegate is used to validate the function. If the delegate returns true the function will be
        ///     treated as matching, if false then not.
        /// </param>
        /// <returns>True if the function could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.</remarks>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetFunction(string name, bool declaredOnly, [CanBeNull] out SolFunctionDefinition definition, Func<SolFunctionDefinition, bool> validator)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Class bodies need to be generated before class functions can be used.");
            SolClassDefinition activeClassDefinition = this;
            while (activeClassDefinition != null) {
                if (activeClassDefinition.m_Functions.TryGetValue(name, out definition) && validator(definition)) {
                    return true;
                }
                activeClassDefinition = declaredOnly ? null : activeClassDefinition.BaseClass;
            }
            definition = null;
            return false;
        }

        /// <summary> Gets a field definition in this class definition. </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="declaredOnly">
        ///     If this parameter is true only field declared
        ///     directly in this class will be searched.
        /// </param>
        /// <param name="definition">
        ///     The returned field. - Only valid if the method
        ///     returned true.
        /// </param>
        /// <returns>True if the function could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>
        ///     This does not account for the <see cref="SolAccessModifier" /> of the function. Use the overload accepting a
        ///     delegate if you wish to provide custom matching behavior.<br/>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.
        /// </remarks>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetField(string name, bool declaredOnly, [CanBeNull] out SolFieldDefinition definition)
        {
            return TryGetField(name, declaredOnly, out definition, fieldDefinition => true);
        }

        /// <summary> Gets a field definition in this class definition. </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="declaredOnly">
        ///     If this parameter is true only field declared
        ///     directly in this class will be searched.
        /// </param>
        /// <param name="definition">
        ///     The returned field. - Only valid if the method
        ///     returned true.
        /// </param>
        /// <param name="validator">
        ///     This function is used to validate the field. If the delegate returns true the field will be
        ///     treated as matching, if false then not.
        /// </param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassBodies"/> or higher state.</remarks>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetField(string name, bool declaredOnly, [CanBeNull] out SolFieldDefinition definition, Func<SolFieldDefinition, bool> validator)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Class bodies need to be generated before class fields can be used.");
            SolClassDefinition activeClassDefinition = this;
            while (activeClassDefinition != null) {
                if (activeClassDefinition.m_Fields.TryGetValue(name, out definition) && validator(definition)) {
                    return true;
                }
                activeClassDefinition = declaredOnly ? null : activeClassDefinition.BaseClass;
            }
            definition = null;
            return false;
        }

        /// <summary>
        /// Directs sets a field for this definition.
        /// </summary>
        /// <param name="field">The field to set.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassHulls"/> or higher state.</remarks>
        internal void AssignFieldDirect(SolFieldDefinition field)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassHulls, SolAssembly.AssertMatch.Exact, "Class definition fields can only be set during the generation of class bodies.");
            m_Fields[field.Name] = field;
        }

        /// <summary>
        /// Directs sets a function for this definition.
        /// </summary>
        /// <param name="function">The function to set.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassHulls"/> or higher state.</remarks>
        internal void AssignFunctionDirect(SolFunctionDefinition function)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassHulls, SolAssembly.AssertMatch.Exact, "Class definition functions can only be set during the generation of class bodies.");
            m_Functions[function.Name] = function;
        }

        #region Nested type: MetaFunctionLink

        public class MetaFunctionLink
        {
            internal MetaFunctionLink(SolMetaKey meta, SolFunctionDefinition definition)
            {
                Meta = meta;
                Definition = definition;
            }

            public readonly SolFunctionDefinition Definition;
            public readonly SolMetaKey Meta;

            #region Overrides

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                if (ReferenceEquals(this, obj)) {
                    return true;
                }
                if (obj.GetType() != GetType()) {
                    return false;
                }
                return Equals((MetaFunctionLink) obj);
            }

            public override int GetHashCode()
            {
                unchecked {
                    return ((Meta?.GetHashCode() ?? 0) * 397) ^ (Definition?.GetHashCode() ?? 0);
                }
            }

            #endregion

            /// <exception cref="SolVariableException"> The value has not been declared, assigned or was of an invalid type. </exception>
            /// <exception cref="SolVariableException">
            ///     Could not find the class in the inheritance chain of <see cref="Definition" />.
            ///     <see cref="SolFunctionDefinition.DefinedIn" />.
            /// </exception>
            public SolFunction GetFunction(SolClass instance)
            {
                SolClass.Inheritance inheritance = instance.FindInheritance(Definition.DefinedIn);
                if (inheritance == null) {
                    throw new SolVariableException($"Could not find the class \"{instance.Type}\" in the inheritance chain of class \"{Definition.DefinedIn.NotNull().Type}\".");
                }
                SolValue value = inheritance.Variables.Get(Meta.Name);
                SolFunction function = value as SolFunction;
                if (function == null) {
                    throw new SolVariableException("Tried to get meta function \"" + Meta.Name + "\" from a \"" + instance.Type + "\" instance. Expected a function value, recceived a \"" + value.Type +
                                                   "\" value.");
                }
                return function;
            }

            protected bool Equals(MetaFunctionLink other)
            {
                return Meta.Equals(other.Meta) && Equals(Definition, other.Definition);
            }
        }

        #endregion
    }
}