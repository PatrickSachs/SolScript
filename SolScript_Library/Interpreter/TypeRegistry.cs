using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter.Builders;
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

        private static readonly ClassCreationOptions AnnotationClassCreationOptions = new ClassCreationOptions.Customizable().SetEnforceCreation(true);

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
                AssetStateExact(State.Registry, "Can only access the globals builder during registry state.");
                return m_GlobalsBuilder;
            }
            set {
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
                AssertStateExactAndHigher(State.GeneratedClassHulls, "Cannot receive class definitions if they aren't generated yet.");
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
            AssertStateExactAndHigher(State.GeneratedGlobals, "Cannot receive global function definitions if they aren't generated yet.");
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
            AssertStateExactAndHigher(State.GeneratedGlobals, "Cannot receive global function definitions if they aren't generated yet.");
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
            AssertStateExactAndHigher(State.GeneratedClassHulls, "Cannot receive class definitions if they aren't generated yet.");
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
            AssertStateExactAndHigher(State.GeneratedClassHulls, "Cannot receive class definitions if they aren't generated yet.");
            return m_ClassDefinitions.TryGetValue(className, out definition);
        }

        /// <exception cref="InvalidOperationException">The TypeRegistry is not in the required state.</exception>
        internal void AssertStateExactAndHigher(State state, string message = "Invalid State!")
        {
            if (!HasReachedState(state)) {
                throw new InvalidOperationException($"{message} - In State: {CurrentState}({(int) CurrentState}); Required State: {state}({(int) state}) or higher.");
            }
        }

        /// <exception cref="InvalidOperationException">The TypeRegistry is not in the required state.</exception>
        internal void AssetStateExactAndLower(State state, string message = "Invalid State!")
        {
            if ((int)CurrentState > (int)state) {
                throw new InvalidOperationException($"{message} - In State: {CurrentState}({(int) CurrentState}); Required State: {state}({(int) state}) or lower.");
            }
        }

        /// <exception cref="InvalidOperationException">The TypeRegistry is not in the required state.</exception>
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
        /// <exception cref="SolTypeRegistryException">
        ///     Another class with the same <see cref="SolClassBuilder.Name" /> already
        ///     exists.
        /// </exception>
        public void RegisterClasses(IEnumerable<SolClassBuilder> builders)
        {
            foreach (SolClassBuilder builder in builders) {
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
            AssetStateExact(State.Registry, "Generating class definitions will advance the state to GeneratedClassBodies, and thus can only be called in Registry state.");
            CurrentState = State.GenerationStarted;
            foreach (SolClassBuilder builder in m_ClassBuilders.Values) {
                SolClassDefinition def = new SolClassDefinition(LinkedAssembly, builder.Location, builder.Name, builder.TypeMode);
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
                    SolFieldDefinition fieldDefinition;
                    try {
                        fieldDefinition = new SolFieldDefinition(LinkedAssembly, def, fieldBuilder);
                    } catch (SolMarshallingException ex) {
                        throw new SolTypeRegistryException("Failed to get field type for field \"" + fieldBuilder.Name + "\" in class \"" + def.Type + "\": " + ex.Message, ex);
                    }
                    def.SetField(fieldBuilder.Name, fieldDefinition);
                }
                foreach (SolFunctionBuilder functionBuilder in builder.Functions) {
                    SolFunctionDefinition functionDefinition;
                    try {
                        functionDefinition = new SolFunctionDefinition(LinkedAssembly, def, functionBuilder);
                    } catch (SolMarshallingException ex) {
                        throw new SolTypeRegistryException("Failed to get return type for function \"" + functionBuilder.Name + "\" in class \"" + def.Type + "\": " + ex.Message, ex);
                    }
                    def.SetFunction(functionDefinition.Name, functionDefinition);
                }
                var annotations = new SolAnnotationDefinition[builder.Annotations.Count];
                for (int i = 0; i < builder.Annotations.Count; i++) {
                    SolAnnotationData builderAnnotation = builder.Annotations[i];
                    SolClassDefinition annotationClass;
                    if (!TryGetClass(builderAnnotation.Name, out annotationClass)) {
                        throw new SolTypeRegistryException("Could not find class \"" + builderAnnotation.Name + "\" which was required for an annotation on class \"" + def.Type + "\".");
                    }
                    if (annotationClass.TypeMode != SolTypeMode.Annotation) {
                        throw new SolTypeRegistryException("The class \"" + builderAnnotation.Name + "\" which was used as an annotation on class \"" + def.Type + "\" is no annotation.");
                    }
                    annotations[i] = new SolAnnotationDefinition(builderAnnotation.Location, annotationClass, builderAnnotation.Arguments);
                }
                def.SetAnnotations(annotations);
            }
            CurrentState = State.GeneratedClassBodies;
            foreach (SolFieldBuilder globalFieldBuilder in m_GlobalsBuilder.Fields) {
                SolFieldDefinition fieldDefinition;
                try {
                    fieldDefinition = new SolFieldDefinition(LinkedAssembly, globalFieldBuilder);
                } catch (SolMarshallingException ex) {
                    throw new SolTypeRegistryException("Failed to get return type for global function \"" + globalFieldBuilder.Name + "\": " + ex.Message, ex);
                }
                m_GlobalFields.Add(globalFieldBuilder.Name, fieldDefinition);
            }
            foreach (SolFunctionBuilder globalFunctionBuilder in m_GlobalsBuilder.Functions) {
                SolFunctionDefinition functionDefinition;
                try {
                    functionDefinition = new SolFunctionDefinition(LinkedAssembly, globalFunctionBuilder);
                } catch (SolMarshallingException ex) {
                    throw new SolTypeRegistryException("Failed to get field type for global field \"" + globalFunctionBuilder.Name + "\": " + ex.Message, ex);
                }
                m_GlobalFunctions.Add(globalFunctionBuilder.Name, functionDefinition);
            }
            CurrentState = State.GeneratedGlobals;
        }

        /// <exception cref="InvalidOperationException">State is lower than GeneratedClassBodies</exception>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        private SolClass PrepareInstance_Impl(SolClassDefinition definition, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            AssertStateExactAndHigher(State.GeneratedClassBodies, "Class instances can only be created once the class definitions have been generated.");
            if (!options.EnforceCreation && !definition.CanBeCreated()) {
                throw new InvalidOperationException($"The class \"{definition.Type}\" cannot be instantiated.");
            }
            var annotations = new List<SolClass>();
            SolClass instance = new SolClass(definition);
            // The context is required to actually initialize the fields.
            SolExecutionContext creationContext = options.CallingContext ?? new SolExecutionContext(LinkedAssembly, definition.Type + "#" + instance.Id + " creation context");
            SolClass.Inheritance activeInheritance = instance.InheritanceChain;
            while (activeInheritance != null) {
                // Create Annotations
                if (options.CreateAnnotations) {
                    foreach (SolAnnotationDefinition annotation in activeInheritance.Definition.Annotations) {
                        var annotationArgs = new SolValue[annotation.Arguments.Length];
                        for (int i = 0; i < annotationArgs.Length; i++) {
                            annotationArgs[i] = annotation.Arguments[i].Evaluate(creationContext, activeInheritance.Variables);
                        }
                        try {
                            SolClass annotationInstance = annotation.Definition.Assembly.TypeRegistry.CreateInstance(annotation.Definition, AnnotationClassCreationOptions, annotationArgs);
                            annotations.Add(annotationInstance);
                        } catch (SolRuntimeException ex) {
                            throw new SolTypeRegistryException(
                                $"An error occured while initializing the annotation \"{annotation.Definition.Type}\" of class \"{instance.Type}\"(Inheritance Level: \"{activeInheritance.Definition.Type}\").",
                                ex);
                        }
                    }
                }
                foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in activeInheritance.Definition.FieldPairs) {
                    SolFieldDefinition fieldDefinition = fieldPair.Value;
                    ClassVariables variables;
                    // Which variable context is this field declared in?
                    // todo: check if already declared somewhere else with same name -> create proper overriding system for fields and functions.
                    switch (fieldDefinition.Modifier) {
                        case SolAccessModifier.None:
                            variables = instance.GlobalVariables;
                            break;
                        case SolAccessModifier.Local:
                            variables = activeInheritance.Variables;
                            break;
                        case SolAccessModifier.Internal:
                            variables = instance.InternalVariables;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    // Declare the field.
                    bool wasDeclared = false;
                    switch (fieldDefinition.Initializer.FieldType) {
                        case SolFieldInitializerWrapper.Type.ScriptField:
                            if (options.DeclareScriptFields) {
                                variables.Declare(fieldDefinition.Name, fieldDefinition.Type);
                                wasDeclared = true;
                            }
                            break;
                        case SolFieldInitializerWrapper.Type.NativeField:
                            if (options.DeclareNativeFields) {
                                variables.DeclareNative(fieldDefinition.Name, fieldDefinition.Type, fieldDefinition.Initializer.GetNativeField(),
                                    new DynamicReference.InheritanceNative(activeInheritance));
                                wasDeclared = true;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    // If the field was declared, let's carry on.
                    // At this point not all fields have fully been declared, which is fine since field
                    // initializers are not supposed/allowed to reference other members anyways.
                    if (wasDeclared) {
                        // Let's create the field annotations(If we actually have some to create).
                        if (options.CreateFieldAnnotations && fieldDefinition.Annotations.Count > 0) {
                            SolClass[] fieldAnnotationInstances = new SolClass[fieldDefinition.Annotations.Count];
                            for (int i = 0; i < fieldAnnotationInstances.Length; i++) {
                                SolAnnotationDefinition fieldAnnotation = fieldDefinition.Annotations[i];
                                SolValue[] values = new SolValue[fieldAnnotation.Arguments.Length];
                                for (int v = 0; v < values.Length; v++) {
                                    values[v] = fieldAnnotation.Arguments[v].Evaluate(creationContext, activeInheritance.Variables);
                                }
                                fieldAnnotationInstances[i] = CreateInstance(fieldAnnotation.Definition, AnnotationClassCreationOptions, values);
                            }
                            variables.AssignAnnotations(fieldDefinition.Name, fieldAnnotationInstances);
                        }
                        // Assign the script fields.
                        if (options.AssignScriptFields && fieldDefinition.Initializer.FieldType == SolFieldInitializerWrapper.Type.ScriptField) {
                            // Evaluate in the variables of the inheritance since the field initializer of e.g. a global field may still refer to a local field.
                            try {
                                variables.Assign(fieldDefinition.Name, fieldDefinition.Initializer.GetScriptField().Evaluate(creationContext, activeInheritance.Variables));
                            } catch (SolVariableException ex) {
                                throw new SolTypeRegistryException($"An error occured while initializing the field \"{fieldDefinition.Name}\" on class \"{definition.Type}\".", ex);
                            }
                        }
                    }
                }
                // todo: function annotations? should they lazily be created?(that stuff really does not belong into variables though?!)
                // or it is time to get rid of this stupid lazy function init stuff(but my sweet memory!)
                activeInheritance = activeInheritance.BaseInheritance;
            }
            instance.AnnotationsArray = annotations.ToArray();
            if (options.CallConstructor) {
                try {
                    instance.CallConstructor(creationContext, constructorArguments);
                } catch (SolRuntimeException ex) {
                    throw new SolTypeRegistryException($"An error occured while calling the constructor of class \"{definition.Type}\".", ex);
                }
            }
            instance.IsInitialized = true;
            return instance;
        }

        /// <summary>
        ///     Creates a new class instance.
        /// </summary>
        /// <param name="name">The name of the class to instantiate.</param>
        /// <param name="options">
        ///     The otpions for the instance creation. If you are unsure about what this is, passing
        ///     <see cref="ClassCreationOptions.Default()" /> is typically a good idea.
        /// </param>
        /// <param name="constructorArguments">The arguments for the constructor function call.</param>
        /// <returns>The created class instance.</returns>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        /// <exception cref="InvalidOperationException">
        ///     The class definitions have not been
        ///     <see cref="State.GeneratedClassBodies" /> yet or the class definition cannot be created for another reason.
        /// </exception>
        public SolClass CreateInstance(string name, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            AssertStateExactAndHigher(State.GeneratedClassBodies, "Cannot create class instances without having generated the class definitions.");
            SolClassDefinition definition;
            if (!m_ClassDefinitions.TryGetValue(name, out definition)) {
                throw new InvalidOperationException($"The class \"{name}\" does not exist.");
            }
            return CreateInstance(definition, options, constructorArguments);
        }

        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        /// <exception cref="InvalidOperationException">
        ///     The class definitions have not been
        ///     <see cref="State.GeneratedClassBodies" /> yet or the class definition cannot be created for another reason.
        /// </exception>
        public SolClass CreateInstance(SolClassDefinition definition, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            AssertStateExactAndHigher(State.GeneratedClassBodies, "Cannot create class instances without having generated the class definitions.");
            if (definition.Assembly != LinkedAssembly) {
                throw new InvalidOperationException($"Cannot create class \"{definition.Type}\"(Assembly: \"{definition.Assembly.Name}\") in the Type Registry for Assembly \"{LinkedAssembly.Name}\".");
            }
            SolClass instance = PrepareInstance_Impl(definition, options, constructorArguments);
            return instance;
        }
    }
}