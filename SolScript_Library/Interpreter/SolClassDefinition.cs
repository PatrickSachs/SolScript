using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Compiler;
using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolClassDefinition" /> is a "prefab" for classes. They contain all non-instance bound data about a
    ///     class. The definitions are created from <see cref="SolClassBuilder" /> by the <see cref="SolAssembly" /> during the
    ///     assembly creation.
    /// </summary>
    public sealed class SolClassDefinition : SolAnnotateableDefinitionBase
    {
        // No 3rd party implementations.
        internal SolClassDefinition(SolAssembly assembly, SolSourceLocation location, string type, SolTypeMode typeMode) : base(assembly, location)
        {
            Type = type;
            TypeMode = typeMode;
        }

        /// <summary>
        ///     The type name of this class definition.
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     The type mode of this class definition.
        /// </summary>
        public readonly SolTypeMode TypeMode;

        /// <summary>
        ///     The native type represented by this class definition.
        /// </summary>
        public Type NativeType;

        /// <summary>
        ///     Raw access to the annotations of this class.
        /// </summary>
        [CanBeNull] internal Array<SolAnnotationDefinition> AnnotationsArray;

        // The fields of this definition.
        private readonly PSUtility.Enumerables.Dictionary<string, SolFieldDefinition> m_Fields = new PSUtility.Enumerables.Dictionary<string, SolFieldDefinition>();
        // The functions of this definition.
        private readonly PSUtility.Enumerables.Dictionary<string, SolFunctionDefinition> m_Functions = new PSUtility.Enumerables.Dictionary<string, SolFunctionDefinition>();

        // Backing field for base class.
        private SolClassDefinition l_base_class;

        // Lazily generated meta functions.
        private PSUtility.Enumerables.Dictionary<SolMetaKey, MetaFunctionLink> l_meta_functions;

        /// <summary>
        ///     All meta functions on this class. This includes meta functions declared at all inheritance levels.
        /// </summary>
        public IReadOnlyDictionary<SolMetaKey, MetaFunctionLink> AllMetaFunctions {
            get {
                if (!DidBuildMetaFunctions) {
                    BuildMetaFunctions();
                }
                return l_meta_functions;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<SolAnnotationDefinition> DeclaredAnnotations {
            get {
                Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Annotations can only be accessed once the class body has been generated.");
                return AnnotationsArray;
            }
        }

        /// <summary>
        ///     The meta functions only declared in this class definition.
        /// </summary>
        public IReadOnlyDictionary<SolMetaKey, MetaFunctionLink> DeclaredMetaFunctions {
            get {
                if (!DidBuildMetaFunctions) {
                    BuildMetaFunctions();
                }
                var dic = new PSUtility.Enumerables.Dictionary<SolMetaKey, MetaFunctionLink>();
                foreach (KeyValuePair<SolMetaKey, MetaFunctionLink> m in l_meta_functions.Where(p => p.Value.Definition.DefinedIn == this)) {
                    dic.Add(m.Key, m.Value);
                }
                return dic;
            }
        }

        /// <summary>
        ///     A lookup of the fields declared in this class. Consider using one of the
        ///     <see cref="TryGetField(string,bool,out SolScript.Interpreter.SolFieldDefinition)" /> overloads if you simply want a
        ///     handle on a field definition.
        /// </summary>
        public IReadOnlyDictionary<string, SolFieldDefinition> FieldLookup {
            get {
                Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Fields can only be accessed once the class body has been generated.");
                return m_Fields;
            }
        }

        /// <summary>
        ///     All fields declared in this class.
        /// </summary>
        public IReadOnlyCollection<SolFieldDefinition> Fields {
            get {
                Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Fields can only be accessed once the class body has been generated.");
                return m_Fields.Values;
            }
        }

        /// <summary>
        ///     A lookup of the functions declared in this class. Consider using one of the
        ///     <see cref="TryGetFunction(string,bool,out SolScript.Interpreter.SolFunctionDefinition)" /> overloads if you simply
        ///     want a handle on a function definition.
        /// </summary>
        public IReadOnlyDictionary<string, SolFunctionDefinition> FunctionLookup {
            get {
                Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Functions can only be accessed once the class body has been generated.");
                return m_Functions;
            }
        }

        /// <summary>
        ///     All functions declared in this class.
        /// </summary>
        public IReadOnlyCollection<SolFunctionDefinition> Functions {
            get {
                Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher, "Functions can only be accessed once the class body has been generated.");
                return m_Functions.Values;
            }
        }

        /// <summary>
        ///     The base class of this class definition.
        /// </summary>
        /// <remarks>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> state.</remarks>
        public SolClassDefinition BaseClass {
            get {
                Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher,
                    "The base class can only be obtained after then class bodies have been generated.");
                return l_base_class;
            }
            set { l_base_class = value; }
        }

        // Are the meta functions generated yet? This allows us to access meta functions.
        // Meta functions are generated if you try to access them while they not created.
        private bool DidBuildMetaFunctions => l_meta_functions != null;

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
        /// <remarks>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> state.</remarks>
        public Stack<SolClassDefinition> GetInheritanceReversed()
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher,
                "The inheritance chain can only be obtained after the class bodies have been generated.");
            var stack = new Stack<SolClassDefinition>();
            SolClassDefinition definition = this;
            while (definition != null) {
                stack.Push(definition);
                definition = definition.BaseClass;
            }
            return stack;
        }

        /// <summary>Creates the meta function lookup for the class definition.</summary>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or higher state.</remarks>
        private void BuildMetaFunctions()
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher,
                "Class meta functions can only be built once the class bodies have been generated.");
            l_meta_functions = new PSUtility.Enumerables.Dictionary<SolMetaKey, MetaFunctionLink>();
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
            }
        }

        /// <summary>Tries to get the meta function for the given key.</summary>
        /// <param name="meta">The meta function key.</param>
        /// <param name="link">The meta function linker. Only valid if method returned true.</param>
        /// <returns>true if the meta function exists, false if not.</returns>
        /// <exception cref="SolVariableException">
        ///     The meta function could be found but was in an invalid state(e.g. wrong type,
        ///     accessor...).
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="meta" /> is null.</exception>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or higher state.</remarks>
        [ContractAnnotation("link:null => false")]
        public bool TryGetMetaFunction([NotNull] SolMetaKey meta, [CanBeNull] out MetaFunctionLink link)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassBodies, SolAssembly.AssertMatch.ExactOrHigher,
                "Class meta functions can only be obtained once the class bodies have been generated.");
            if (!DidBuildMetaFunctions) {
                BuildMetaFunctions();
            }
            return l_meta_functions.TryGetValue(meta, out link);
        }

        /// <summary>
        ///     Tries to find and add a meta function.
        /// </summary>
        /// <param name="meta">The meta function helper.</param>
        /// <returns>true if said meta function could be found, false if not.</returns>
        /// <remarks>This method does NOT assert state! Type checks, etc. should be performed by the <see cref="SolCompiler" />.</remarks>
        // This method is meant for usage from inside BuildMetaFunctions.
        private bool FindAndRegisterMetaFunction(SolMetaKey meta)
        {
            SolFunctionDefinition definition;
            if (TryGetFunction(meta.Name, false, out definition)) {
                l_meta_functions.Add(meta, new MetaFunctionLink(meta, definition));
                return true;
            }
            return false;
        }

        /// <summary> Checks if the class does extend the given class at some point. </summary>
        /// <param name="className">The class name.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>
        ///     Warning: A class does not extend itself and will thus return false if
        ///     the own class name is passed.<br />Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or higher
        ///     state.
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
        ///     the own class name is passed.<br></br>Requires <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or
        ///     higher state.
        /// </remarks>
        [ContractAnnotation("definedIn:null => false")]
        public bool DoesExtendInHierarchy(string className, [CanBeNull] out SolClassDefinition definedIn)
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
        ///     Can the class be inherited?
        /// </summary>
        /// <param name="by">The class that wants to extend this one.</param>
        /// <returns>True if the class can be inherited, false if not.</returns>
        public bool CanBeInherited(SolClassDefinition by)
        {
            switch (TypeMode) {
                case SolTypeMode.Default:
                case SolTypeMode.Abstract:
                    return true;
                case SolTypeMode.Annotation:
                    return by.TypeMode == SolTypeMode.Annotation;
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
        /// <remarks>
        ///     This does not account for the <see cref="SolAccessModifier" /> of the function. Use the overload accepting a
        ///     delegate if you wish to provide custom matching behavior.<br />Only valid in
        ///     <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or higher state.
        /// </remarks>
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
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or higher state.</remarks>
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
        ///     delegate if you wish to provide custom matching behavior.<br />Only valid in
        ///     <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or higher state.
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
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassBodies" /> or higher state.</remarks>
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
        ///     Directs sets a field for this definition.
        /// </summary>
        /// <param name="field">The field to set.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassHulls" /> or higher state.</remarks>
        internal void AssignFieldDirect(SolFieldDefinition field)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassHulls, SolAssembly.AssertMatch.Exact, "Class definition fields can only be set during the generation of class bodies.");
            m_Fields[field.Name] = field;
        }

        /// <summary>
        ///     Directs sets a function for this definition.
        /// </summary>
        /// <param name="function">The function to set.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Only valid in <see cref="SolAssembly.AssemblyState.GeneratedClassHulls" /> or higher state.</remarks>
        internal void AssignFunctionDirect(SolFunctionDefinition function)
        {
            Assembly.AssertState(SolAssembly.AssemblyState.GeneratedClassHulls, SolAssembly.AssertMatch.Exact, "Class definition functions can only be set during the generation of class bodies.");
            m_Functions[function.Name] = function;
        }

        #region Nested type: MetaFunctionLink

        /// <summary>
        ///     The meta function link is used to provide quick access to meta functions.
        /// </summary>
        /// <remarks>
        ///     The main advantage of the meta function linker is that the "final location" of the meta function has already
        ///     been determined. This means that the meta function can be accessed from a flat variable source instead of having to
        ///     scan all class internals.
        /// </remarks>
        public class MetaFunctionLink
        {
            internal MetaFunctionLink(SolMetaKey meta, SolFunctionDefinition definition)
            {
                Meta = meta;
                Definition = definition;
            }

            /// <summary>
            ///     The function definitions.
            /// </summary>
            public readonly SolFunctionDefinition Definition;

            /// <summary>
            ///     The meta key of this meta function.
            /// </summary>
            public readonly SolMetaKey Meta;

            #region Overrides

            /// <inheritdoc />
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

            /// <inheritdoc />
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
                    throw new SolVariableException(Definition.Location, $"Could not find the class \"{instance.Type}\" in the inheritance chain of class \"{Definition.DefinedIn.NotNull().Type}\".");
                }
                SolValue value = inheritance.GetVariables(Definition.AccessModifier, SolVariableMode.Declarations).Get(Meta.Name);
                SolFunction function = value as SolFunction;
                if (function == null) {
                    throw new SolVariableException(Definition.Location,
                        "Tried to get meta function \"" + Meta.Name + "\" from a \"" + instance.Type + "\" instance. Expected a function value, received a \"" + value.Type +
                        "\" value.");
                }
                return function;
            }

            /// <inheritdoc cref="Equals(object)" />
            protected bool Equals(MetaFunctionLink other)
            {
                return Meta.Equals(other.Meta) && Equals(Definition, other.Definition);
            }
        }

        #endregion
    }
}