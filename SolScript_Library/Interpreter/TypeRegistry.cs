using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public class TypeRegistry
    {
        #region State enum

        public enum State
        {
            Registry = 0,
            GenerationStarted = 2,
            GeneratedClassHulls = 3,
            GeneratedClassBodies = 4,
            GeneratedGlobals = 5
        }

        #endregion

        public TypeRegistry(SolAssembly linkedAssembly)
        {
            LinkedAssembly = linkedAssembly;
            m_GlobalsBuilder = new SolGlobalsBuilder();
        }

        public readonly SolAssembly LinkedAssembly;
        private readonly Dictionary<string, SolClassBuilder> m_ClassBuilders = new Dictionary<string, SolClassBuilder>();
        private readonly Dictionary<string, SolClassDefinition> m_ClassDefinitions = new Dictionary<string, SolClassDefinition>();
        private readonly Dictionary<string, SolFieldDefinition> m_GlobalFields = new Dictionary<string, SolFieldDefinition>();
        private readonly Dictionary<string, SolFunctionDefinition> m_GlobalFunctions = new Dictionary<string, SolFunctionDefinition>();

        private readonly Dictionary<Type, SolClassDefinition> m_NativeClasses = new Dictionary<Type, SolClassDefinition>();
        private SolGlobalsBuilder m_GlobalsBuilder;

        public IReadOnlyCollection<KeyValuePair<string, SolFieldDefinition>> GlobalFieldPairs => m_GlobalFields;
        public IReadOnlyCollection<KeyValuePair<string, SolFunctionDefinition>> GlobalFunctionPairs => m_GlobalFunctions;

        public State CurrentState { get; private set; }

        /// <summary>
        ///     The builder used to create the global fields and functions.
        /// </summary>
        /// <exception cref="InvalidOperationException">The Type Registry is not in <see cref="State.Registry" /> state.</exception>
        /// <seealso cref="CurrentState" />
        public SolGlobalsBuilder GlobalsBuilder {
            get {
                // <bubble>InvalidOperationException</bubble>
                AssetStateExact(State.Registry, "Can only access the globals builder during registry state.");
                return m_GlobalsBuilder;
            }
            set {
                // <bubble>InvalidOperationException</bubble>
                AssetStateExact(State.Registry, "Can only access the globals builder during registry state.");
                m_GlobalsBuilder = value;
            }
        }

        /// <summary>
        ///     All class definitions.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Class definitions have not been <see cref="State.GeneratedClassHulls" /> yet.
        /// </exception>
        /// <seealso cref="HasReachedState" />
        public IReadOnlyCollection<SolClassDefinition> ClassDefinitions {
            get {
                // <bubble>InvalidOperationException</bubble>
                AssetStateExactAndHigher(State.GeneratedClassHulls, "Cannot receive class definitions if they aren't generated yet.");
                return m_ClassDefinitions.Values;
            }
        }

        /// <summary>
        ///     Tries to get a global function with the given name.
        /// </summary>
        /// <param name="name">The name of the function to find.</param>
        /// <param name="definition">Outputs the definition. Only valid if the method returned true.</param>
        /// <returns>true if the function could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Class definitions have not been <see cref="State.GeneratedClassBodies" /> yet.
        /// </exception>
        /// <seealso cref="HasReachedState" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetGlobalFunction(string name, out SolFunctionDefinition definition)
        {
            // <bubble>InvalidOperationException</bubble>
            AssetStateExactAndHigher(State.GeneratedGlobals, "Cannot receive global function definitions if they aren't generated yet.");
            return m_GlobalFunctions.TryGetValue(name, out definition);
        }

        /// <summary>
        ///     Tries to get a global field with the given name.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        /// <param name="definition">Outputs the definition. Only valid if the method returned true.</param>
        /// <returns>true if the field could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Class definitions have not been <see cref="State.GeneratedClassBodies" /> yet.
        /// </exception>
        /// <seealso cref="HasReachedState" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetGlobalField(string name, out SolFieldDefinition definition)
        {
            // <bubble>InvalidOperationException</bubble>
            AssetStateExactAndHigher(State.GeneratedGlobals, "Cannot receive global function definitions if they aren't generated yet.");
            return m_GlobalFields.TryGetValue(name, out definition);
        }

        /// <summary>
        ///     Checks if the Type Registry is in the given state or a higher state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>true if it is in the given state or a higher state.</returns>
        public bool HasReachedState(State state)
        {
            return (int) CurrentState >= (int) state;
        }

        /// <summary>
        ///     Checks if the Type Registry has not yet reached the given state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>true if it is in a lower state than the given one.</returns>
        public bool HasNotReachedState(State state)
        {
            return (int) CurrentState < (int) state;
        }

        /// <summary>
        ///     Tries to get the class definition for the given native type.
        /// </summary>
        /// <param name="nativeType">The native type.</param>
        /// <param name="definition">This out value contains the found class, or null.</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Class definitions have not been <see cref="State.GeneratedClassHulls" /> yet.
        /// </exception>
        /// <seealso cref="HasReachedState" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(Type nativeType, [CanBeNull] out SolClassDefinition definition)
        {
            // <bubble>InvalidOperationException</bubble>
            AssetStateExactAndHigher(State.GeneratedClassHulls, "Cannot receive class definitions if they aren't generated yet.");
            return m_NativeClasses.TryGetValue(nativeType, out definition);
        }

        /// <summary>
        ///     Tries to get the class definition of the given name.
        /// </summary>
        /// <param name="className">The class name.</param>
        /// <param name="definition">This out value contains the found class, or null.</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Class definitions have not been <see cref="State.GeneratedClassHulls" /> yet.
        /// </exception>
        /// <seealso cref="HasReachedState" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(string className, [CanBeNull] out SolClassDefinition definition)
        {
            // <bubble>InvalidOperationException</bubble>
            AssetStateExactAndHigher(State.GeneratedClassHulls, "Cannot receive class definitions if they aren't generated yet.");
            return m_ClassDefinitions.TryGetValue(className, out definition);
        }

        internal void AssetStateExactAndHigher(State state, string message = "Invalid State!")
        {
            if (!HasReachedState(state)) {
                throw new InvalidOperationException($"{message} - In State: {CurrentState}({(int) CurrentState}); Required State: {state}({(int) state}) or higher.");
            }
        }

        internal void AssetStateExactAndLower(State state, string message = "Invalid State!")
        {
            if (!HasNotReachedState(state)) {
                throw new InvalidOperationException($"{message} - In State: {CurrentState}({(int) CurrentState}); Required State: {state}({(int) state}) or lower.");
            }
        }

        internal void AssetStateExact(State state, string message = "Invalid State!")
        {
            if (CurrentState != state) {
                throw new InvalidOperationException($"{message} - In State: {CurrentState}({(int) CurrentState}); Required State: {state}({(int) state}) exactly.");
            }
        }

        /// <summary>
        ///     Registers a new class builder in the type registry.
        /// </summary>
        /// <param name="builder">The class builder.</param>
        /// <exception cref="InvalidOperationException">The Type Registry is not in <see cref="State.Registry" /> state.</exception>
        /// <exception cref="SolTypeRegistryException">
        ///     Another class with the same <see cref="SolClassBuilder.Name" /> already
        ///     exists.
        /// </exception>
        /// <seealso cref="CurrentState" />
        public void RegisterClass(SolClassBuilder builder)
        {
            // <bubble>InvalidOperationException</bubble>
            AssetStateExact(State.Registry, "Can only register classes during registry state. New classes cannot be registered once the definitions have been generated.");
            try {
                m_ClassBuilders.Add(builder.Name, builder);
            } catch (ArgumentException ex) {
                throw new SolTypeRegistryException("Another class with the name \"" + builder.Name + "\" already exists.", ex);
            }
        }

        /// <summary>
        ///     Registers multiple new class builders in the type registry.
        /// </summary>
        /// <param name="builders">The class builders.</param>
        /// <exception cref="InvalidOperationException">The Type Registry is not in <see cref="State.Registry" /> state.</exception>
        /// <seealso cref="CurrentState" />
        public void RegisterClasses(IEnumerable<SolClassBuilder> builders)
        {
            foreach (SolClassBuilder builder in builders) {
                // <bubble>InvalidOperationException</bubble>
                RegisterClass(builder);
            }
        }

        /// <summary>
        ///     Creates the definitions(classes, global functions + fields) for this type registry. This will advance the state to
        ///     <see cref="State.GeneratedClassBodies" />.
        /// </summary>
        /// <exception cref="InvalidOperationException">The Type Registry is not in the <see cref="State.Registry" /> state.</exception>
        /// <exception cref="SolTypeRegistryException">An error generating the class definitions occured.</exception>
        /// <seealso cref="CurrentState" />
        internal void GenerateDefinitions()
        {
            // <bubble>InvalidOperationException</bubble>
            AssetStateExact(State.Registry, "Generating class definitions will advance the state to GeneratedClassBodies, and thus can only be called in Registry state.");
            CurrentState = State.GenerationStarted;
            foreach (SolClassBuilder builder in m_ClassBuilders.Values) {
                SolClassDefinition def = new SolClassDefinition(LinkedAssembly, builder.Name, builder.TypeMode);
                if (builder.NativeType != null) {
                    def.NativeType = builder.NativeType;
                    m_NativeClasses.Add(builder.NativeType, def);
                }
                m_ClassDefinitions.Add(builder.Name, def);
            }
            CurrentState = State.GeneratedClassHulls;
            foreach (SolClassBuilder builder in m_ClassBuilders.Values) {
                SolClassDefinition def = m_ClassDefinitions[builder.Name];
                if (builder.BaseClass != null) {
                    SolClassDefinition baseDef = m_ClassDefinitions[builder.BaseClass];
                    if (!baseDef.CanBeInherited()) {
                        throw new SolTypeRegistryException("Class \"" + def.Type + "\" tried to inherit from class \"" + baseDef.Type + "\", which does not allow inheritance.");
                    }
                    def.BaseClass = baseDef;
                }
                foreach (SolFieldBuilder fieldBuilder in builder.Fields) {
                    SolFieldDefinition fieldDefinition = new SolFieldDefinition(def) {
                        Type = fieldBuilder.Type,
                        Modifier = fieldBuilder.AccessModifier,
                        FieldInitializer = fieldBuilder.ScriptField,
                        NativeBackingField = fieldBuilder.NativeField
                    };
                    def.SetField(fieldBuilder.Name, fieldDefinition);
                }
                foreach (SolFunctionBuilder functionBuilder in builder.Functions) {
                    SolFunctionDefinition functionDefinition = new SolFunctionDefinition(LinkedAssembly, def, functionBuilder);
                    def.SetFunction(functionDefinition.Name, functionDefinition);
                }
                SolAnnotationDefinition[] annotations = new SolAnnotationDefinition[builder.Annotations.Count];
                int i = 0;
                foreach (SolAnnotationData builderAnnotation in builder.Annotations) {
                    SolClassDefinition annotationClass;
                    if (!TryGetClass(builderAnnotation.Name, out annotationClass)) {
                        throw new SolTypeRegistryException("Could not find class \"" + builderAnnotation.Name + "\" which was required for an annotation on class \"" + def.Type + "\".");
                    }
                    if (annotationClass.TypeMode != SolTypeMode.Annotation) {
                        throw new SolTypeRegistryException("The class \"" + builderAnnotation.Name + "\" which was used as an annotation on class \"" + def.Type + "\" is no annotation.");
                    }
                    annotations[i] = new SolAnnotationDefinition(annotationClass, builderAnnotation.Arguments);
                    i++;
                }
                def.SetAnnotations(annotations);
            }
            CurrentState = State.GeneratedClassBodies;
            foreach (SolFunctionBuilder globalFunctionBuilder in m_GlobalsBuilder.Functions) {
                SolFunctionDefinition functionDefinition = new SolFunctionDefinition(LinkedAssembly, globalFunctionBuilder);
                m_GlobalFunctions.Add(globalFunctionBuilder.Name, functionDefinition);
            }
            foreach (SolFieldBuilder globalFieldBuilder in m_GlobalsBuilder.Fields) {
                SolFieldDefinition fieldDefinition = new SolFieldDefinition {
                    Type = globalFieldBuilder.Type,
                    Modifier = globalFieldBuilder.AccessModifier,
                    FieldInitializer = globalFieldBuilder.ScriptField,
                    NativeBackingField = globalFieldBuilder.NativeField
                };
                m_GlobalFields.Add(globalFieldBuilder.Name, fieldDefinition);
            }
            CurrentState = State.GeneratedGlobals;
        }

        /// <exception cref="InvalidOperationException">State is lower than GeneratedClassBodies</exception>
        private SolClass PrepareInstance_Impl(SolClassDefinition definition)
        {
            // <bubble>InvalidOperationException</bubble>
            AssetStateExactAndHigher(State.GeneratedClassBodies, "Class instances can only be created once the class definitions have been generated.");
            SolClass instance = new SolClass(definition);
            {
                // Build inheritance tree and declare fields
                SolClassDefinition activeDefinition = definition;
                SolClass.Inheritance activeInheritance = null;
                while (activeDefinition != null) {
                    if (activeInheritance == null) {
                        activeInheritance = instance.InheritanceChain;
                    } else {
                        SolClass.Inheritance newInheritance = new SolClass.Inheritance(instance, activeDefinition, null);
                        activeInheritance.BaseClass = newInheritance;
                        activeInheritance = newInheritance;
                    }
                    foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in activeDefinition.FieldPairs) {
                        switch (fieldPair.Value.Modifier) {
                            case AccessModifier.None:
                                instance.GlobalVariables.Declare(fieldPair.Key, fieldPair.Value.Type);
                                break;
                            case AccessModifier.Local:
                                activeInheritance.Variables.Declare(fieldPair.Key, fieldPair.Value.Type);
                                break;
                            case AccessModifier.Internal:
                                instance.InternalVariables.Declare(fieldPair.Key, fieldPair.Value.Type);
                                break;
                        }
                    }
                    activeDefinition = activeDefinition.BaseClass;
                }
            }
            return instance;
        }

        /// <summary>
        ///     Prepares a new class instance for usage inside SolScript.
        /// </summary>
        /// <param name="name">The name of the class to create.</param>
        /// <param name="enforce">
        ///     Enforce creation of the class. Passing true will allow you to create instances for e.g.
        ///     annotations or abstract classes.
        /// </param>
        /// <returns>A class initializer which gives you some further options on how the class will be created.</returns>
        /// <exception cref="InvalidOperationException">
        ///     The class definitions have not been <see cref="State.GeneratedClassBodies" />
        ///     yet.
        /// </exception>
        /// <exception cref="SolTypeRegistryException">An error occured while preparing the class initializer.</exception>
        public SolClass.Initializer PrepareInstance(string name, bool enforce = false)
        {
            AssetStateExactAndHigher(State.GeneratedClassBodies, "Cannot create class instances without having generated the class definitions.");
            SolClassDefinition definition;
            if (!m_ClassDefinitions.TryGetValue(name, out definition)) {
                throw new SolTypeRegistryException($"The class \"{name}\" does not exist.");
            }
            // <bubble>SolTypeRegistryException</bubble>
            // <bubble>InvalidOperationException</bubble>
            return PrepareInstance(definition, enforce);
        }

        /// <summary>
        ///     Prepares a new class instance for usage inside SolScript.
        /// </summary>
        /// <param name="definition">The class definition you wish to create class for.</param>
        /// <param name="enforce">
        ///     Enforce creation of the class. Passing true will allow you to create instances for e.g.
        ///     annotations or abstract classes.
        /// </param>
        /// <returns>A class initializer which gives you some further options on how the class will be created.</returns>
        /// <exception cref="SolTypeRegistryException">An error occured while preparing the class initializer.</exception>
        public SolClass.Initializer PrepareInstance(SolClassDefinition definition, bool enforce = false)
        {
            if (definition.Assembly != LinkedAssembly) {
                throw new SolTypeRegistryException($"Cannot create class \"{definition.Type}\"(Assembly: \"{definition.Assembly.Name}\") in the Type Registry for Assembly \"{LinkedAssembly.Name}\".");
            }
            if (!enforce && !definition.CanBeCreated()) {
                throw new SolTypeRegistryException($"The class \"{definition.Type}\" cannot be instantiated.");
            }
            // <bubble>InvalidOperationException</bubble>
            SolClass instance = PrepareInstance_Impl(definition);
            return new SolClass.Initializer(instance);
        }
    }
}