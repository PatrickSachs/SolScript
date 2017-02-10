using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

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

        private readonly Dictionary<string, SolFieldDefinition> m_Fields = new Dictionary<string, SolFieldDefinition>();
        private readonly Dictionary<string, SolFunctionDefinition> m_Functions = new Dictionary<string, SolFunctionDefinition>();
        public readonly string Type;
        public readonly SolTypeMode TypeMode;
        public SolClassDefinition BaseClass;

        // Lazily generated
        private Dictionary<SolMetaKey, MetaFunctionLink> l_MetaFunctions;

        private SolAnnotationDefinition[] m_Annotations;

        public Type NativeType;

        private bool DidBuildMetaFunctions => l_MetaFunctions != null;

        /// <inheritdoc />
        public override IReadOnlyList<SolAnnotationDefinition> Annotations {
            get { return m_Annotations; }
            protected set { m_Annotations = value.ToArray(); }
        }

        public IReadOnlyCollection<SolFieldDefinition> Fields => m_Fields.Values;
        public IReadOnlyCollection<SolFunctionDefinition> Functions => m_Functions.Values;
        public IReadOnlyCollection<KeyValuePair<string, SolFieldDefinition>> FieldPairs => m_Fields;
        public IReadOnlyCollection<KeyValuePair<string, SolFunctionDefinition>> FunctionPairs => m_Functions;
        public IReadOnlyCollection<string> FieldNames => m_Fields.Keys;
        public IReadOnlyCollection<string> FunctionNames => m_Functions.Keys;

        #region Overrides

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

        internal void SetAnnotations(params SolAnnotationDefinition[] annotations)
        {
            Assembly.TypeRegistry.AssetStateExact(TypeRegistry.State.GeneratedClassHulls, "Class annotations can only be set during the generation of class bodies.");
            m_Annotations = annotations;
        }

        /// <exception cref="SolVariableException">
        ///     The meta function could be found but was in an invalid state(e.g. wrong type, accessor...).
        /// </exception>
        private void BuildMetaFunctions()
        {
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassHulls, "Class meta functions can only be built once the generation of class bodies hsa completed.");
            l_MetaFunctions = new Dictionary<SolMetaKey, MetaFunctionLink>();
            FindMetaFunction(SolMetaKey.Constructor);
            FindMetaFunction(SolMetaKey.Stringify);
            FindMetaFunction(SolMetaKey.GetN);
            FindMetaFunction(SolMetaKey.IsEqual);
            FindMetaFunction(SolMetaKey.Iterate);
            FindMetaFunction(SolMetaKey.Modulo);
            FindMetaFunction(SolMetaKey.Expotentiate);
            FindMetaFunction(SolMetaKey.Divide);
            FindMetaFunction(SolMetaKey.Add);
            FindMetaFunction(SolMetaKey.Subtract);
            FindMetaFunction(SolMetaKey.Multiply);
            FindMetaFunction(SolMetaKey.Concatenate);
            if (TypeMode == SolTypeMode.Annotation) {
                FindMetaFunction(SolMetaKey.AnnotationPreConstructor);
                FindMetaFunction(SolMetaKey.AnnotationPostConstructor);
                FindMetaFunction(SolMetaKey.AnnotationGetVariable);
                FindMetaFunction(SolMetaKey.AnnotationSetVariable);
                FindMetaFunction(SolMetaKey.AnnotationCallFunction);
            }
        }

        /// <exception cref="SolVariableException">
        ///     The meta function could be found but was in an invalid state(e.g. wrong type, accessor...).
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="meta" /> is null.</exception>
        [ContractAnnotation("link:null => false")]
        public bool TryGetMetaFunction([NotNull] SolMetaKey meta, [CanBeNull] out MetaFunctionLink link)
        {
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
        private bool FindMetaFunction(SolMetaKey meta, params SolType[] parameters)
        {
            // todo: find meta function parameters
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassHulls, "Class meta functions can only be built once the generation of class bodies hsa completed.");
            SolFunctionDefinition definition;
            if (TryGetFunction(meta.Name, false, out definition)) {
                if (definition.AccessModifier != AccessModifier.Local && definition.AccessModifier != AccessModifier.Internal) {
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
        /// <remarks>
        ///     Warning: A class does not extend itself and will thus return false if
        ///     the own class name is passed.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="TypeRegistry" /> is in a state lower than  <see cref="TypeRegistry.State.GeneratedClassBodies" />
        /// </exception>
        public bool DoesExtendInHierarchy(string className)
        {
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassBodies, "Class bodies need to be generated before inheritance can be checked.");
            SolClassDefinition definedIn;
            return DoesExtendInHierarchy(className, out definedIn);
        }

        /// <summary> Checks if the class does extend the given class at some point. Also returns the definition of the class. </summary>
        /// <remarks>
        ///     Warning: A class does not extend itself and will thus return false if
        ///     the own class name is passed.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="TypeRegistry" /> is in a state lower than  <see cref="TypeRegistry.State.GeneratedClassBodies" />
        /// </exception>
        public bool DoesExtendInHierarchy(string className, out SolClassDefinition definedIn)
        {
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassBodies, "Class bodies need to be generated before inheritance can be checked.");
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
        ///     classes can be created using the new keyword.
        /// </summary>
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

        public bool CanBeInherited()
        {
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

        public bool HasFunction(string name, bool declaredOnly)
        {
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassBodies, "Class bodies need to be generated before class functions can be used.");
            return m_Functions.ContainsKey(name);
        }

        /// <summary>
        ///     Checks if the class definition has a field of the given name.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="declaredOnly">
        ///     If this parameter is true only functions declared
        ///     directly in this class will be searched and possible fields in inherited class ignored.
        /// </param>
        /// <returns>true if a field with this name could be found, false if not.</returns>
        /// <remarks>
        ///     This method will not take the <see cref="AccessModifier" /> of the field into account. Use one of the
        ///     <see cref="TryGetField(string,bool,out SolFieldDefinition)" /> methods for more sophistcated behaviour.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="TypeRegistry" /> is in a state lower than  <see cref="TypeRegistry.State.GeneratedClassBodies" />
        /// </exception>
        public bool HasField(string name, bool declaredOnly)
        {
            //<bubble>InvalidOperationException</bubble>
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassBodies, "Class bodies need to be generated before class fields can be used.");
            return m_Fields.ContainsKey(name);
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
        /// <remarks>
        ///     This does not account for the <see cref="AccessModifier" /> of the function. Use the overload accepting a
        ///     delegate if you wish to provide custom matching behaviour.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="TypeRegistry" /> is in a state lower than  <see cref="TypeRegistry.State.GeneratedClassBodies" />
        /// </exception>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetFunction(string name, bool declaredOnly, [CanBeNull] out SolFunctionDefinition definition)
        {
            //<bubble>InvalidOperationException</bubble>
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
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="TypeRegistry" /> is in a state lower than  <see cref="TypeRegistry.State.GeneratedClassBodies" />
        /// </exception>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetFunction(string name, bool declaredOnly, [CanBeNull] out SolFunctionDefinition definition, Func<SolFunctionDefinition, bool> validator)
        {
            //<bubble>InvalidOperationException</bubble>
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassBodies, "Class bodies need to be generated before class functions can be used.");
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
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="TypeRegistry" /> is in a state lower than
        ///     <see cref="TypeRegistry.State.GeneratedClassBodies" />.
        /// </exception>
        /// <remarks>
        ///     This function will not take accessors into account. Use the overload accepting a validator more more
        ///     sophsticated behaviour.
        /// </remarks>
        public bool TryGetField(string name, bool declaredOnly, [CanBeNull] out SolFieldDefinition definition)
        {
            //<bubble>InvalidOperationException</bubble>
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
        /// <exception cref="InvalidOperationException">
        ///     The <see cref="TypeRegistry" /> is in a state lower than
        ///     <see cref="TypeRegistry.State.GeneratedClassBodies" />.
        /// </exception>
        public bool TryGetField(string name, bool declaredOnly, [CanBeNull] out SolFieldDefinition definition, Func<SolFieldDefinition, bool> validator)
        {
            //<bubble>InvalidOperationException</bubble>
            Assembly.TypeRegistry.AssetStateExactAndHigher(TypeRegistry.State.GeneratedClassBodies, "Class bodies need to be generated before class fields can be used.");
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

        internal void SetField(string name, SolFieldDefinition field)
        {
            Assembly.TypeRegistry.AssetStateExact(TypeRegistry.State.GeneratedClassHulls, "Class definition fields can only be set during the generation of class bodies.");
            m_Fields[name] = field;
        }

        internal void SetFunction(string name, SolFunctionDefinition function)
        {
            Assembly.TypeRegistry.AssetStateExact(TypeRegistry.State.GeneratedClassHulls, "Class definition functions can only be set during the generation of class bodies.");
            m_Functions[name] = function;
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