using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using Irony;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Compiler;
using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Libraries.lang;
using SolScript.Parser;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolAssembly" /> is the heart of SolScript. An assembly is the collection of all classes and global
    ///     variables of an ongoing execution. From here you can create new classes, access global variables, include libraries
    ///     and generally start using SolScript.
    /// </summary>
    public sealed class SolAssembly
    {
        #region AssemblyState enum

        /// <summary>
        ///     An enum specifying the current state of the assembly. Some methods are only available during certain states.
        /// </summary>
        public enum AssemblyState
        {
            /// <summary>
            ///     So,ething went wrong. Check <see cref="Errors" /> for more details.
            /// </summary>
            Error = -1,

            /// <summary>
            ///     No state. The assembly should never be in this state.
            /// </summary>
            None = 0,

            /// <summary>
            ///     The assembly is currently waiting for libraries and classes to be registered.
            /// </summary>
            Registry = 1,

            /// <summary>
            ///     The registry is done, the builders can now be built into definitions.
            /// </summary>
            AllRegistered = 2,

            /// <summary>
            ///     The class hulls have been built. All class definitions exist, but they are empty shells. No globals have been
            ///     built.
            /// </summary>
            GeneratedClassHulls = 3,

            /// <summary>
            ///     All classes have been fully built. Their members, inheritance chain, etc. are ready for usage. No globals have been
            ///     built.
            /// </summary>
            GeneratedClassBodies = 4,

            /// <summary>
            ///     The assembly has been fully built.
            /// </summary>
            GeneratedAll = 5,

            /// <summary>
            ///     The assembly is running. Hooray - we made it!
            /// </summary>
            Running = 6
        }

        #endregion

        private SolAssembly(SolAssemblyOptions options)
        {
            Input = Console.OpenStandardInput();
            Output = Console.OpenStandardOutput();
            InputEncoding = Console.InputEncoding;
            OutputEncoding = Console.OutputEncoding;
            m_Options = (SolAssemblyOptions) options.Clone();
            m_Compiler = new SolCompiler(this);
            m_Libraries.Add(lang.GetLibrary());
            Errors = SolErrorCollection.CreateCollection(out m_ErrorAdder, options.WarningsAreErrors);
        }

        // The Irony Grammar rules used for SolScript.
        private static readonly SolScriptGrammar s_Grammar = new SolScriptGrammar();


        private readonly PSUtility.Enumerables.Dictionary<string, SolClassDefinition> m_ClassDefinitions = new PSUtility.Enumerables.Dictionary<string, SolClassDefinition>();
        // The compiler used to validate and compile the assembly.
        private readonly SolCompiler m_Compiler;
        // Errors can be added here.
        private readonly SolErrorCollection.Adder m_ErrorAdder;
        private readonly PSUtility.Enumerables.Dictionary<string, SolFieldDefinition> m_GlobalFields = new PSUtility.Enumerables.Dictionary<string, SolFieldDefinition>();
        private readonly PSUtility.Enumerables.Dictionary<string, SolFunctionDefinition> m_GlobalFunctions = new PSUtility.Enumerables.Dictionary<string, SolFunctionDefinition>();
        // All libraries registered in this Assembly.
        private readonly PSUtility.Enumerables.List<SolLibrary> m_Libraries = new PSUtility.Enumerables.List<SolLibrary>();
        private readonly PSUtility.Enumerables.Dictionary<Type, SolClassDefinition> m_NativeClasses = new PSUtility.Enumerables.Dictionary<Type, SolClassDefinition>();
        // The options for creating this assembly.
        private readonly SolAssemblyOptions m_Options;
        // A helper lookup containing a type/definition map of all singleton definitions.
        private readonly PSUtility.Enumerables.Dictionary<string, SolClassDefinition> m_SingletonLookup = new PSUtility.Enumerables.Dictionary<string, SolClassDefinition>();

        // The lazy builders. Only available until everything has been parsed.
        private BuildersContainer l_builders;
        // The lazy statement factory.
        private StatementFactory l_factory;

        /// <summary>
        ///     Contains the errors raised during the creation of the assembly.<br /> Runtime errors are NOT added to this error
        ///     collection. They are thrown as a <see cref="SolRuntimeException" /> instead.
        /// </summary>
        public SolErrorCollection Errors { get; }

        /// <summary>
        ///     All global fields in key value pairs.
        /// </summary>
        /// <exception cref="InvalidOperationException">Invalid state. </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedAll" /> state.</remarks>
        /// <seealso cref="State" />
        public IReadOnlyCollection<KeyValuePair<string, SolFieldDefinition>> GlobalFieldPairs {
            get {
                AssertState(AssemblyState.AllRegistered, AssertMatch.ExactOrHigher, "Cannot receive global field definitions if they aren't generated yet.");
                return m_GlobalFields;
            }
        }

        /// <summary>
        ///     All global functions in key value pairs.
        /// </summary>
        /// <exception cref="InvalidOperationException">Invalid state. </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedAll" /> state.</remarks>
        /// <seealso cref="State" />
        public IReadOnlyCollection<KeyValuePair<string, SolFunctionDefinition>> GlobalFunctionPairs {
            get {
                AssertState(AssemblyState.AllRegistered, AssertMatch.ExactOrHigher, "Cannot receive global function definitions if they aren't generated yet.");
                return m_GlobalFunctions;
            }
        }

        /// <summary>
        ///     A descriptive name of this assembly(e.g. "Enemy AI Logic"). The name will be used during debugging and error
        ///     output.
        /// </summary>
        public string Name => m_Options.Name;

        /// <summary>
        ///     The global variables of an assembly are exposed to everything. Other assemblies, classes and other globals can
        ///     access them.
        /// </summary>
        public IVariables GlobalVariables { get; private set; }

        /// <summary>
        ///     The input stream of this assembly. Used for reading data from SolScript. (Defaults to the console input)<br />
        ///     Although possible, it is not recommended to change this value during runtime.
        /// </summary>
        public Stream Input { get; set; }

        /// <summary>
        ///     The encoding of the <see cref="Input" /> stream. (Defaults to the console encoding)
        /// </summary>
        public Encoding InputEncoding { get; set; }

        /// <summary>
        ///     Internal globals can only be accessed from other global variables and functions. They should only be used for meta
        ///     functions.
        /// </summary>
        public IVariables InternalVariables { get; private set; }

        /// <summary>
        ///     The local variables of an assembly can only be accessed from other global variables and functions.
        /// </summary>
        public IVariables LocalVariables { get; private set; }

        /// <summary>
        ///     The output stream of this assembly. Used for writing data from SolScript. (Defaults to the console output)<br />
        ///     Although possible, it is not recommended to change this value during runtime.
        /// </summary>
        public Stream Output { get; set; }

        /// <summary>
        ///     The encoding of the <see cref="Output" /> stream. (Defaults to the console encoding)
        /// </summary>
        public Encoding OutputEncoding { get; set; }

        /// <summary>
        ///     The current state of this assembly. Some methods may only work in a certain state.
        /// </summary>
        /// <seealso cref="AssemblyState" />
        public AssemblyState State { get; private set; }

        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>Only available in states lower than <see cref="AssemblyState.GeneratedAll" />.</remarks>
        private BuildersContainer Builders {
            get {
                AssertState(AssemblyState.GeneratedAll, AssertMatch.LowerNoError, "Can only access builders until everything has been fully interpreted.");
                return l_builders;
            }
        }

        // The statement factory is used for parsing raw source files.
        private StatementFactory Factory => l_factory ?? (l_factory = new StatementFactory(this));

        /// <summary>
        ///     Gets the global variables associated with the given access modifier.
        /// </summary>
        /// <param name="access">The access modifier.</param>
        /// <returns>The variable source.</returns>
        public IVariables GetVariables(SolAccessModifier access)
        {
            // todo: declared only global variables.
            switch (access) {
                case SolAccessModifier.None:
                    return GlobalVariables;
                case SolAccessModifier.Local:
                    return LocalVariables;
                case SolAccessModifier.Internal:
                    return InternalVariables;
                default:
                    throw new ArgumentOutOfRangeException(nameof(access), access, null);
            }
        }

        /// <summary>
        ///     Inclused a <see cref="SolLibrary" /> which will be included in the library upon creation.
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        ///     Libraries can only be registered during the
        ///     <see cref="AssemblyState.Registry" /> state.
        /// </exception>
        /// <seealso cref="State" />
        public SolAssembly IncludeLibrary(SolLibrary library)
        {
            AssertState(AssemblyState.Registry, AssertMatch.Exact, "Libraries can only be registered during the library registry state.");
            m_Libraries.Add(library);
            return this;
        }

        /// <summary>
        ///     Stops the assembly from allowing the registry of the libraries, classes and globals and creates the builders for
        ///     these, which can later be built using the <see cref="GenerateDefinitions" /> method.
        /// </summary>
        /// <returns>The assembly.</returns>
        /// <remarks>
        ///     Requires the <see cref="AssemblyState.Registry" /> state, advances to
        ///     <see cref="AssemblyState.AllRegistered" /> state.
        /// </remarks>
        public SolAssembly FinalizeRegistry()
        {
            AssertState(AssemblyState.Registry, AssertMatch.Exact, "Can only interpret in registry state.");
            bool hadError = false;
            foreach (SolLibrary library in m_Libraries) {
                SolDebug.WriteLine("Building class hulls of library " + library.Name);
                if (!library.HasBeenCreated) {
                    library.Create();
                }
                try {
                    RegisterClassBuilders(library.Classes.Values);
                    foreach (SolFieldBuilder field in library.GlobalFields) {
                        Builders.AddField(field);
                    }
                    foreach (SolFunctionBuilder function in library.GlobalFunctions) {
                        Builders.AddFunction(function);
                    }
                } catch (SolTypeRegistryException ex) {
                    m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.ClassRegistry, ex.RawMessage, false, ex));
                    hadError = true;
                }
            }
            State = hadError
                ? AssemblyState.Error
                : AssemblyState.AllRegistered;
            return this;
        }

        /// <summary>
        ///     Builds all class definitions, globals and libraries.
        /// </summary>
        /// <returns>The assembly itself.</returns>
        /// <remarks>
        ///     Requires the assembly to be in the <see cref="AssemblyState.AllRegistered" /> state. Advances to
        ///     <see cref="AssemblyState.GeneratedAll" /> state.
        /// </remarks>
        /// <seealso cref="State" />
        public SolAssembly GenerateDefinitions()
        {
            AssertState(AssemblyState.AllRegistered, AssertMatch.Exact, "Can only interpret in registry state.");
            bool hadError = false;
            foreach (SolClassBuilder builder in Builders.ClassBuilders.Values) {
                SolClassDefinition def = new SolClassDefinition(this, builder.Location, builder.Name, builder.TypeMode);
                if (builder.NativeType != null) {
                    def.NativeType = builder.NativeType;
                    m_NativeClasses.Add(builder.NativeType, def);
                }
                if (def.TypeMode == SolTypeMode.Singleton) {
                    m_SingletonLookup.Add(def.Type, def);
                }
                m_ClassDefinitions.Add(builder.Name, def);
            }
            State = AssemblyState.GeneratedClassHulls;
            foreach (SolClassBuilder builder in Builders.ClassBuilders.Values) {
                SolClassDefinition def = m_ClassDefinitions[builder.Name];
                if (builder.BaseClass != null) {
                    SolClassDefinition baseDef = m_ClassDefinitions[builder.BaseClass];
                    if (!baseDef.CanBeInherited(def)) {
                        m_ErrorAdder.Add(new SolError(def.Location, ErrorId.InvalidInheritance,
                            "Class \"" + def.Type + "\" tried to inherit from class \"" + baseDef.Type + "\" which does not allow inheritance."));
                        hadError = true;
                    }
                    def.BaseClass = baseDef;
                }
                foreach (SolFieldBuilder fieldBuilder in builder.Fields) {
                    try {
                        SolFieldDefinition fieldDefinition = new SolFieldDefinition(this, def, fieldBuilder);
                        def.AssignFieldDirect(fieldDefinition);
                    } catch (SolMarshallingException ex) {
                        m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.ClassFieldRegistry,
                            "Failed to get field type for field \"" + fieldBuilder.Name + "\" in class \"" + def.Type + "\": " + ex.RawMessage, false, ex));
                        hadError = true;
                    }
                }
                foreach (SolFunctionBuilder functionBuilder in builder.Functions) {
                    try {
                        SolFunctionDefinition functionDefinition = new SolFunctionDefinition(this, def, functionBuilder);
                        def.AssignFunctionDirect(functionDefinition);
                    } catch (SolMarshallingException ex) {
                        m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.ClassFunctionRegistry,
                            "Failed to get return type for function \"" + functionBuilder.Name + "\" in class \"" + def.Type + "\": " + ex.RawMessage, false, ex));
                        hadError = true;
                    }
                }
                try {
                    def.AnnotationsArray = InternalHelper.AnnotationsFromData(this, builder.Annotations);
                } catch (SolMarshallingException ex) {
                    m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.InvalidAnnotationType,
                        "Failed to create an annotation definition for class \"" + def.Type + "\": " + ex.RawMessage, false, ex));
                    hadError = true;
                }
                // We cannot validate the class at this point since the base class may not be built yet.
            }
            State = AssemblyState.GeneratedClassBodies;
            foreach (SolFieldBuilder globalFieldBuilder in Builders.Fields) {
                try {
                    SolFieldDefinition fieldDefinition = new SolFieldDefinition(this, globalFieldBuilder);
                    m_GlobalFields.Add(globalFieldBuilder.Name, fieldDefinition);
                } catch (SolMarshallingException ex) {
                    m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.GlobalFieldRegistry,
                        "Failed to get return type for global function \"" + globalFieldBuilder.Name + "\": " + ex.RawMessage, false, ex));
                    hadError = true;
                }
            }
            foreach (SolFunctionBuilder globalFunctionBuilder in Builders.Functions) {
                try {
                    SolFunctionDefinition functionDefinition = new SolFunctionDefinition(this, globalFunctionBuilder);
                    m_GlobalFunctions.Add(globalFunctionBuilder.Name, functionDefinition);
                } catch (SolMarshallingException ex) {
                    m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.GlobalFunctionRegistry,
                        "Failed to get field type for global field \"" + globalFunctionBuilder.Name + "\": " + ex.RawMessage, false, ex));
                    hadError = true;
                }
            }
            // Validate
            SolDebug.WriteLine("Validating ...");
            foreach (SolClassDefinition definition in m_ClassDefinitions.Values) {
                try {
                    SolDebug.WriteLine("   ... Class " + definition.Type);
                    m_Compiler.ValidateClass(definition);
                } catch (SolCompilerException ex) {
                    m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.CompilerError, ex.RawMessage, false, ex));
                    hadError = true;
                }
            }
            foreach (SolFunctionDefinition definition in m_GlobalFunctions.Values) {
                try {
                    SolDebug.WriteLine("   ... Global Function " + definition.Name);
                    m_Compiler.ValidateFunction(definition);
                } catch (SolCompilerException ex) {
                    m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.CompilerError, ex.RawMessage, false, ex));
                    hadError = true;
                }
            }
            State = hadError ? AssemblyState.Error : AssemblyState.GeneratedAll;
            return this;
        }

        /// <summary>
        ///     This method truly creates the assembly. Creates global functions and fields from the generated definitions and
        ///     allows you to start using the assembly for whatever you need it afterwards.
        /// </summary>
        /// <returns>The created assembly; ready for usage.</returns>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the class or global definitions.</exception>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <remarks>
        ///     Requires the <see cref="AssemblyState.GeneratedAll" /> state, advances to <see cref="AssemblyState.Running" />
        ///     state. Last initialization method.
        /// </remarks>
        public SolAssembly Create()
        {
            AssertState(AssemblyState.GeneratedAll, AssertMatch.Exact, "The assembly must be generated to create it.");
            State = AssemblyState.Running;
            SolExecutionContext context = new SolExecutionContext(this, Name + " initialization context");
            GlobalVariables = new GlobalVariable(this);
            InternalVariables = new InternalVariables(this);
            LocalVariables = new LocalVariables(this);
            // Create global functions
            foreach (KeyValuePair<string, SolFunctionDefinition> funcPair in GlobalFunctionPairs) {
                SolFunctionDefinition funcDefinition = funcPair.Value;
                IVariables declareInVariables = GetVariablesForModifier(funcDefinition.AccessModifier);
                SolFunction function;
                ICustomAttributeProvider provider;
                switch (funcDefinition.Chunk.ChunkType) {
                    case SolChunkWrapper.Type.ScriptChunk:
                        function = new SolScriptGlobalFunction(funcDefinition);
                        provider = null;
                        break;
                    case SolChunkWrapper.Type.NativeMethod:
                        function = new SolNativeGlobalFunction(funcDefinition, DynamicReference.NullReference.Instance);
                        provider = funcDefinition.Chunk.GetNativeMethod();
                        break;
                    case SolChunkWrapper.Type.NativeConstructor:
                        provider = funcDefinition.Chunk.GetNativeConstructor();
                        throw new SolTypeRegistryException(funcDefinition.Location,
                            "Tried to make a native constructor the global function \"" + funcPair.Key + "\". Native constructors cannot be global functions.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                try {
                    declareInVariables.Declare(funcPair.Key, new SolType(SolFunction.TYPE, false));
                    if (funcDefinition.DeclaredAnnotations.Count > 0) {
                        try {
                            declareInVariables.AssignAnnotations(funcPair.Key, InternalHelper.CreateAnnotations(context, LocalVariables, funcDefinition.DeclaredAnnotations, provider));
                        } catch (SolTypeRegistryException ex) {
                            throw new SolTypeRegistryException(funcDefinition.Location,
                                $"An error occured while initializing one of the annotations on global function \"{funcDefinition.Name}\".",
                                ex);
                        }
                    }
                    declareInVariables.Assign(funcPair.Key, function);
                } catch (SolVariableException ex) {
                    throw new SolTypeRegistryException(funcDefinition.Location, "Failed to register global function \"" + funcDefinition.Name + "\"", ex);
                }
            }
            // Initialize global fields
            foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in GlobalFieldPairs) {
                SolFieldDefinition fieldDefinition = fieldPair.Value;
                IVariables declareInVariables = GetVariablesForModifier(fieldDefinition.AccessModifier);
                switch (fieldDefinition.Initializer.FieldType) {
                    case SolFieldInitializerWrapper.Type.ScriptField:
                        SolExpression scriptInitializer = fieldDefinition.Initializer.GetScriptField();
                        try {
                            declareInVariables.Declare(fieldPair.Key, fieldDefinition.Type);
                            if (fieldDefinition.DeclaredAnnotations.Count > 0) {
                                try {
                                    declareInVariables.AssignAnnotations(fieldPair.Key, InternalHelper.CreateAnnotations(context, LocalVariables, fieldDefinition.DeclaredAnnotations, null));
                                } catch (SolVariableException ex) {
                                    throw new SolTypeRegistryException(fieldDefinition.Location,
                                        $"An error occured while initializing one of the annotations on global field \"{fieldDefinition.Name}\".",
                                        ex);
                                }
                            }
                            declareInVariables.Assign(fieldPair.Key, scriptInitializer.Evaluate(context, LocalVariables));
                        } catch (SolVariableException ex) {
                            throw new SolTypeRegistryException(fieldDefinition.Location, "Failed to register global field \"" + fieldDefinition.Name + "\"", ex);
                        }
                        break;
                    case SolFieldInitializerWrapper.Type.NativeField:
                        FieldOrPropertyInfo nativeField = fieldDefinition.Initializer.GetNativeField();
                        try {
                            declareInVariables.DeclareNative(fieldPair.Key, fieldDefinition.Type, nativeField, DynamicReference.NullReference.Instance);
                        } catch (SolVariableException ex) {
                            throw new SolTypeRegistryException(fieldDefinition.Location, "Failed to register native global field \"" + fieldDefinition.Name + "\"", ex);
                        }
                        try {
                            declareInVariables.AssignAnnotations(fieldPair.Key, InternalHelper.CreateAnnotations(context, LocalVariables, fieldDefinition.DeclaredAnnotations, nativeField));
                        } catch (SolVariableException ex) {
                            throw new SolTypeRegistryException(fieldDefinition.Location,
                                $"An error occured while initializing one of the annotations on global field \"{fieldDefinition.Name}\".",
                                ex);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return this;
        }

        /// <summary>
        ///     Gets the variables matching the given access modifier.
        /// </summary>
        private IVariables GetVariablesForModifier(SolAccessModifier modifier)
        {
            switch (modifier) {
                case SolAccessModifier.None:
                    return GlobalVariables;
                case SolAccessModifier.Local:
                    return LocalVariables;
                case SolAccessModifier.Internal:
                    return InternalVariables;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Tries to get a global function with the given name.
        /// </summary>
        /// <param name="name">The name of the function to find.</param>
        /// <param name="definition">Outputs the definition. Only valid if the method returned true.</param>
        /// <returns>true if the function could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Invalid state. </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedAll" /> state.</remarks>
        /// <seealso cref="State" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetGlobalFunction(string name, out SolFunctionDefinition definition)
        {
            AssertState(AssemblyState.GeneratedAll, AssertMatch.ExactOrHigher, "Cannot receive global function definitions if they aren't generated yet.");
            return m_GlobalFunctions.TryGetValue(name, out definition);
        }

        /// <summary>
        ///     Tries to get a global field with the given name.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        /// <param name="definition">Outputs the definition. Only valid if the method returned true.</param>
        /// <returns>true if the field could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Invalid state. </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedAll" /> state.</remarks>
        /// <seealso cref="State" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetGlobalField(string name, out SolFieldDefinition definition)
        {
            AssertState(AssemblyState.GeneratedAll, AssertMatch.ExactOrHigher, "Cannot receive global field definitions if they aren't generated yet.");
            return m_GlobalFields.TryGetValue(name, out definition);
        }

        /// <summary>
        ///     Tries to get the class definition for the given native type.
        /// </summary>
        /// <param name="nativeType">The native type.</param>
        /// <param name="definition">This out value contains the found class, or null.</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Invalid state.
        /// </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedClassHulls" /> state.</remarks>
        /// <seealso cref="State" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(Type nativeType, [CanBeNull] out SolClassDefinition definition)
        {
            AssertState(AssemblyState.GeneratedClassHulls, AssertMatch.ExactOrHigher, "Cannot receive class definitions if they aren't generated yet.");
            return m_NativeClasses.TryGetValue(nativeType, out definition);
        }

        /// <summary>
        ///     Tries to get the class definition of the given name.<br /> Keep in mind that depending on the current
        ///     <see cref="State" /> the obtained definition may only be skeleton. Check the documentation of the
        ///     <see cref="AssemblyState" /> values for more info.
        /// </summary>
        /// <param name="className">The class name.</param>
        /// <param name="definition">This out value contains the found class, or null.</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Invalid state.
        /// </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedClassHulls" /> state.</remarks>
        /// <seealso cref="State" />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(string className, [CanBeNull] out SolClassDefinition definition)
        {
            AssertState(AssemblyState.GeneratedClassHulls, AssertMatch.ExactOrHigher, "Cannot receive class definitions if they aren't generated yet.");
            return m_ClassDefinitions.TryGetValue(className, out definition);
        }

        /// <summary>
        ///     Registers a new class builder in the type registry.
        /// </summary>
        /// <param name="builder">The class builder.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <exception cref="SolTypeRegistryException">
        ///     Another class with the same <see cref="SolClassBuilder.Name" /> already
        ///     exists.
        /// </exception>
        /// <remarks>Requires the <see cref="AssemblyState.Registry" /> state.</remarks>
        /// <seealso cref="State" />
        public void RegisterClassBuilder(SolClassBuilder builder)
        {
            AssertState(AssemblyState.Registry, AssertMatch.Exact, "Can only register class builders in registry state.");
            try {
                Builders.ClassBuilders.Add(builder.Name, builder);
            } catch (ArgumentException ex) {
                throw new SolTypeRegistryException(builder.Location, "Another class with the name \"" + builder.Name + "\" already exists.", ex);
            }
        }

        /// <summary>
        ///     Registers multiple new class builders in the type registry.
        /// </summary>
        /// <param name="builders">The class builders.</param>
        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <seealso cref="State" />
        /// <exception cref="SolTypeRegistryException">
        ///     Another class with the same <see cref="SolClassBuilder.Name" /> already
        ///     exists.
        /// </exception>
        /// <remarks>Only available in the <see cref="AssemblyState.Registry" /> state.</remarks>
        /// <seealso cref="State" />
        public void RegisterClassBuilders(IEnumerable<SolClassBuilder> builders)
        {
            AssertState(AssemblyState.Registry, AssertMatch.Exact, "Can only register class builders in registry state.");
            foreach (SolClassBuilder builder in builders) {
                RegisterClassBuilder(builder);
            }
        }

        /// <summary>
        ///     Checks if the given name is a singleton type and returns the singleton definition if it is.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="definition">The singleton definition. Only valid if the method returned true.</param>
        /// <returns>true if the class with the given name is a singleton, false if not, or no class with this name exists at all.</returns>
        [ContractAnnotation("definition:null => false")]
        public bool IsSingleton(string name, out SolClassDefinition definition)
        {
            return m_SingletonLookup.TryGetValue(name, out definition);
        }

        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        private SolClass New_Impl(SolClassDefinition definition, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            AssertState(AssemblyState.GeneratedClassBodies, AssertMatch.ExactOrHigher, "Cannot create class instances without having generated the class bodies.");
            if (!options.EnforceCreation && !definition.CanBeCreated()) {
                throw new InvalidOperationException($"The class \"{definition.Type}\" cannot be instantiated.");
            }
            var annotations = new PSUtility.Enumerables.List<SolClass>();
            SolClass instance = new SolClass(definition);
            // The context is required to actually initialize the fields.
            SolExecutionContext creationContext = options.CallingContext ?? new SolExecutionContext(this, definition.Type + "#" + instance.Id + " creation context");
            SolClass.Inheritance activeInheritance = instance.InheritanceChain;
            // todo this needs to start at the base type in order for field inits to work? or does it not matter since field inits should not depend on members?!
            while (activeInheritance != null) {
                DynamicReference inheritanceDynRef = new DynamicReference.InheritanceNative(activeInheritance);
                // Create Annotations
                if (options.CreateAnnotations) {
                    try {
                        annotations.AddRange(InternalHelper.CreateAnnotations(creationContext, activeInheritance.GetVariables(SolAccessModifier.Local, SolVariableMode.All),
                            activeInheritance.Definition.DeclaredAnnotations, activeInheritance.Definition.NativeType));
                    } catch (SolTypeRegistryException ex) {
                        throw new SolTypeRegistryException(activeInheritance.Definition.Location,
                            $"An error occured while initializing one of the annotation on class \"{instance.Type}\"(Inheritance Level: \"{activeInheritance.Definition.Type}\").",
                            ex);
                    }
                }
                foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in activeInheritance.Definition.FieldLookup) {
                    SolFieldDefinition fieldDefinition = fieldPair.Value;
                    IVariables variables = activeInheritance.GetVariables(fieldDefinition.AccessModifier, SolVariableMode.Declarations);
                    // Which variable context is this field declared in?
                    // Declare the field.  
                    bool wasDeclared = false;
                    ICustomAttributeProvider provider;
                    switch (fieldDefinition.Initializer.FieldType) {
                        // todo: allow to not init fields - e.g. not null types will probably need to be init from the ctor.
                        //    -> but check later on (after ctor) if they are actually inited. !! RESPECT DECLAREXYZFIELD OPTION!!
                        case SolFieldInitializerWrapper.Type.ScriptField:
                            provider = null;
                            if (options.DeclareScriptFields) {
                                variables.Declare(fieldDefinition.Name, fieldDefinition.Type);
                                wasDeclared = true;
                            }
                            break;
                        case SolFieldInitializerWrapper.Type.NativeField:
                            provider = fieldDefinition.Initializer.GetNativeField();
                            if (options.DeclareNativeFields) {
                                variables.DeclareNative(fieldDefinition.Name, fieldDefinition.Type, fieldDefinition.Initializer.GetNativeField(), inheritanceDynRef);
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
                        // Annotations and fields are initialized using local access.
                        IVariables localUsage = activeInheritance.GetVariables(SolAccessModifier.Local, SolVariableMode.All);
                        // Let's create the field annotations(If we actually have some to create).
                        if (options.CreateFieldAnnotations && fieldDefinition.DeclaredAnnotations.Count > 0) {
                            try {
                                variables.AssignAnnotations(fieldDefinition.Name, InternalHelper.CreateAnnotations(creationContext, localUsage, fieldDefinition.DeclaredAnnotations, provider));
                            } catch (SolTypeRegistryException ex) {
                                throw new SolTypeRegistryException(activeInheritance.Definition.Location,
                                    $"An error occured while initializing one of the annotation on class field \"{instance.Type}.{fieldDefinition.Name}\"(Inheritance Level: \"{activeInheritance.Definition.Type}\").",
                                    ex);
                            }
                        }
                        // Assign the script fields.
                        if (options.AssignScriptFields && fieldDefinition.Initializer.FieldType == SolFieldInitializerWrapper.Type.ScriptField) {
                            // Evaluate in the variables of the inheritance since the field initializer of e.g. a global field may still refer to a local field.
                            try {
                                variables.Assign(fieldDefinition.Name, fieldDefinition.Initializer.GetScriptField().Evaluate(creationContext, localUsage));
                            } catch (SolVariableException ex) {
                                throw new SolTypeRegistryException(fieldDefinition.Location, $"An error occured while initializing the field \"{fieldDefinition.Name}\" on class \"{definition.Type}\".",
                                    ex);
                            }
                        }
                    }
                }
                // todo: function annotations? should they be lazily created?(that stuff really does not belong into variables though?!)
                // or it is time to get rid of this stupid lazy function init stuff(but my sweet memory!)
                activeInheritance = activeInheritance.BaseInheritance;
            }
            instance.AnnotationsArray = new Array<SolClass>(annotations.ToArray());
            if (options.CallConstructor) {
                /*if (definition.NativeType != null && definition.NativeType.IsSubclassOf(typeof(Attribute))) {
                    // We cannot create the instance of new native attributes(or should not). Use special wrappers for them.
                    throw new SolTypeRegistryException(definition.Location, "The class \"" + definition.Type + "\" wraps the native type \"" + definition.NativeType + "\", which is an attribute. Attributes cannot be created using the new keyword.");
                }*/
                try {
                    instance.CallConstructor(creationContext, constructorArguments);
                } catch (SolRuntimeException ex) {
                    throw new SolTypeRegistryException(definition.Location, $"An error occured while calling the constructor of class \"{definition.Type}\".", ex);
                }
            }
            instance.IsInitialized = options.MarkAsInitialized;
            return instance;
        }

        /// <summary>
        ///     Creates a new class instance.
        /// </summary>
        /// <param name="name">The name of the class to instantiate.</param>
        /// <param name="options">
        ///     The options for the instance creation. If you are unsure about what this is, passing
        ///     <see cref="ClassCreationOptions.Default()" /> is typically a good idea.
        /// </param>
        /// <param name="constructorArguments">The arguments for the constructor function call.</param>
        /// <returns>The created class instance.</returns>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        /// <exception cref="InvalidOperationException"> The class definitions have not been generated yet. </exception>
        /// <exception cref="InvalidOperationException"> No class with <paramref name="name" /> exists. </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedClassBodies" /> state.</remarks>
        public SolClass New(string name, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            AssertState(AssemblyState.GeneratedClassBodies, AssertMatch.ExactOrHigher, "Cannot create class instances without having generated the class bodies.");
            SolClassDefinition definition;
            if (!m_ClassDefinitions.TryGetValue(name, out definition)) {
                throw new InvalidOperationException($"The class \"{name}\" does not exist.");
            }
            return New(definition, options, constructorArguments);
        }

        /// <summary>
        ///     Creates a new class instance.
        /// </summary>
        /// <param name="definition">The class definition to generate.</param>
        /// <param name="options">
        ///     The options for the instance creation. If you are unsure about what this is, passing
        ///     <see cref="ClassCreationOptions.Default()" /> is typically a good idea.
        /// </param>
        /// <param name="constructorArguments">The arguments for the constructor function call.</param>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        /// <exception cref="InvalidOperationException"> The class definitions have not been generated yet. </exception>
        /// <exception cref="InvalidOperationException"> The <paramref name="definition" /> belongs to a different assembly. </exception>
        /// <remarks>Requires the <see cref="AssemblyState.GeneratedClassBodies" /> state.</remarks>
        public SolClass New(SolClassDefinition definition, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            AssertState(AssemblyState.GeneratedClassBodies, AssertMatch.ExactOrHigher, "Cannot create class instances without having generated the class bodies.");
            if (definition.Assembly != this) {
                throw new InvalidOperationException($"Cannot create class \"{definition.Type}\"(Assembly: \"{definition.Assembly.Name}\") in Assembly \"{Name}\".");
            }
            SolClass instance = New_Impl(definition, options, constructorArguments);
            return instance;
        }

        #region Nested type: BuildersContainer

        /// <summary>
        ///     A data container to encapsulate builders. This allows us to easily null the builders out once they are no longer
        ///     needed, saving us memory.
        /// </summary>
        private sealed class BuildersContainer : SolConstructWithMembersBuilder.Generic<BuildersContainer>
        {
            public readonly PSUtility.Enumerables.Dictionary<string, SolClassBuilder> ClassBuilders = new PSUtility.Enumerables.Dictionary<string, SolClassBuilder>();
        }

        #endregion

        #region From XYZ

        /// <summary>
        ///     Creates a new <see cref="SolAssembly" /> from the parsed contents of the given directory.
        /// </summary>
        /// <param name="sourceDir">The directory to scan for source files.</param>
        /// <param name="options">
        ///     The options for creating this assembly. - The options will be cloned in the assembly and further
        ///     changes to the options will thus have no effect.
        /// </param>
        /// <returns>
        ///     The assembly which can directly be created(<see cref="Create" />) or first have some libraries included(
        ///     <see cref="IncludeLibrary" />).
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">The directory (or a file) does not exist.</exception>
        /// <exception cref="IOException">An IO exception occured while trying to read the files.</exception>
        /// <exception cref="UnauthorizedAccessException">Cannot access a file within the directory or the directory itself.</exception>
        public static SolAssembly FromDirectory(SolAssemblyOptions options, string sourceDir)
        {
            if (!Directory.Exists(sourceDir)) {
                throw new DirectoryNotFoundException("The directory \"" + sourceDir + "\" does not exist.");
            }
#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
#endif
            SolDebug.WriteLine("Creating Parser ...");
            Irony.Parsing.Parser parser = new Irony.Parsing.Parser(s_Grammar);
            SolDebug.WriteLine("Loading Trees ...");
            var trees = new PSUtility.Enumerables.List<ParseTree>();
            string[] files;
            try {
                files = Directory.GetFiles(sourceDir, options.SourceFilePattern);
            } catch (DirectoryNotFoundException ex) {
                throw new DirectoryNotFoundException(
                    "The directory \"" + sourceDir + "\" does not exist. However a previous check indicated that the directory exists. Do you have other threads messing with the directory?",
                    ex);
            }
            foreach (string dir in files) {
                ParseTree tree;
                try {
                    tree = parser.Parse(File.ReadAllText(dir), dir);
                } catch (FileNotFoundException ex) {
                    throw new DirectoryNotFoundException(
                        "The file \"" + dir + "\" does not exist. However the OS told us about the existence of this file. Do you have other threads messing with the file?", ex);
                } catch (SecurityException ex) {
                    throw new UnauthorizedAccessException("Cannot access file \"" + dir + "\".", ex);
                }
                trees.Add(tree);
                SolDebug.WriteLine("  ... Loaded " + dir);
            }
            SolAssembly a = FromTrees(trees, options);
            return a;
#if DEBUG
            } finally {
                stopwatch.Stop();
                SolDebug.WriteLine("StopWatch: " + stopwatch.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        ///     Creates an <see cref="SolAssembly" /> from the given code strings.
        /// </summary>
        /// <param name="strings">The code strings.</param>
        /// <param name="options">
        ///     The options for creating this assembly. - The options will be cloned in the assembly and further
        ///     changes to the options will thus have no effect.
        /// </param>
        /// <returns>
        ///     The assembly which can directly be created(<see cref="Create" />) or first have some libraries included(
        ///     <see cref="IncludeLibrary" />).
        /// </returns>
        /// <exception cref="SolInterpreterException">An error occured while interpreting the syntax tree.</exception>
        public static SolAssembly FromStrings(SolAssemblyOptions options, params string[] strings)
        {
#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
#endif
            SolDebug.WriteLine("Creating Parser ...");
            Irony.Parsing.Parser parser = new Irony.Parsing.Parser(s_Grammar);
            var trees = new PSUtility.Enumerables.List<ParseTree>(strings.Length);
            SolDebug.WriteLine("Loading Trees ...");
            for (int i = 0; i < strings.Length; i++) {
                string s = strings[i];
                ParseTree tree = parser.Parse(s, "Source:" + i.ToString(CultureInfo.InvariantCulture));
                trees.Add(tree);
                SolDebug.WriteLine("  ... Loaded " + s);
            }
            SolAssembly a = FromTrees(trees, options);
            return a;
#if DEBUG
            } finally {
                stopwatch.Stop();
                SolDebug.WriteLine("StopWatch: " + stopwatch.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        ///     Builds the assembly from the irony parse trees.
        /// </summary>
        private static SolAssembly FromTrees(IEnumerable<ParseTree> trees, SolAssemblyOptions options)
        {
            SolDebug.WriteLine("Building Trees ...");
            SolAssembly script = new SolAssembly(options) {
                l_builders = new BuildersContainer(),
                State = AssemblyState.Registry
            };
            StatementFactory factory = script.Factory;
            foreach (ParseTree tree in trees) {
                SolDebug.WriteLine("  ... Checking tree " + tree.FileName);
                foreach (LogMessage message in tree.ParserMessages) {
                    switch (message.Level) {
                        case ErrorLevel.Info:
                        case ErrorLevel.Warning:
                            script.m_ErrorAdder.Add(new SolError(new SolSourceLocation(tree.FileName, message.Location), ErrorId.SyntaxError, message.Message, true));
                            // Parse all trees even if we have errors. This allows easier debugging for the user if there are errors
                            // spread accross multiple files.
                            continue;
                        case ErrorLevel.Error:
                            script.m_ErrorAdder.Add(new SolError(new SolSourceLocation(tree.FileName, message.Location), ErrorId.SyntaxError, message.Message));
                            // Parse all trees even if we have errors. This allows easier debugging for the user if there are errors
                            // spread accross multiple files.
                            continue;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            if (!script.Errors.HasErrors) {
                foreach (ParseTree tree in trees) {
                    SolDebug.WriteLine("  ... Building File " + tree.FileName);
                    IReadOnlyList<SolClassBuilder> classBuilders;
                    try {
                        factory.InterpretTree(tree, script.Builders, out classBuilders);
                    } catch (SolInterpreterException ex) {
                        script.m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.InterpreterError, ex.RawMessage, false, ex));
                        // Parse all trees even if we have errors. This allows easier debugging for the user if there are errors
                        // spread accross multiple files.
                        continue;
                    }
                    foreach (SolClassBuilder typeDef in classBuilders) {
                        script.RegisterClassBuilder(typeDef);
                        SolDebug.WriteLine("    ... Type " + typeDef.Name);
                    }
                }
            } else {
                script.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.InternalSecurityMeasure, "Not interpreting the syntax trees since one or more of them had errors.", true));
            }
            if (script.Errors.HasErrors) {
                script.State = AssemblyState.Error;
            }
            return script;
        }

        #endregion

        #region Assertion

        internal enum AssertMatch
        {
            Exact,
            ExactOrLower,
            ExactOrLowerNoError,
            Lower,
            LowerNoError,
            ExactOrHigher,
            Higher
        }

        /// <exception cref="InvalidOperationException">Invalid state.</exception>
        internal void AssertState(AssemblyState state, AssertMatch match, string message = "")
        {
            string str1 = null;
            switch (match) {
                case AssertMatch.Exact:
                    if (state == State) {
                        return;
                    }
                    break;
                case AssertMatch.ExactOrLower:
                    if ((int) State <= (int) state) {
                        return;
                    }
                    str1 = "(or lower)";
                    break;
                case AssertMatch.ExactOrLowerNoError:
                    if ((int) State <= (int) state) {
                        if (State != AssemblyState.Error) {
                            return;
                        }
                    }
                    str1 = "(or lower)";
                    break;
                case AssertMatch.Lower:
                    if ((int) State < (int) state) {
                        return;
                    }
                    str1 = "(only lower)";
                    break;
                case AssertMatch.LowerNoError:
                    if ((int) State < (int) state) {
                        if (State != AssemblyState.Error) {
                            return;
                        }
                    }
                    str1 = "(only lower)";
                    break;
                case AssertMatch.ExactOrHigher:
                    if ((int) State >= (int) state) {
                        return;
                    }
                    str1 = "(or higher)";
                    break;
                case AssertMatch.Higher:
                    if ((int) State > (int) state) {
                        return;
                    }
                    str1 = "(only higher)";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(match), match, null);
            }
            throw new InvalidOperationException("Invalid state! Expected: " + state + (str1 != null ? " " + str1 : string.Empty) + ", but was: " + State + ". " + message);
        }

        #endregion
    }
}