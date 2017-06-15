using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using Irony;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Metadata;
using PSUtility.Reflection;
using SolScript.Compiler;
using SolScript.Compiler.Native;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Libraries.lang;
using SolScript.Parser;
using SolScript.Utility;
using gen = System.Collections.Generic;
using ps = PSUtility.Enumerables;
using Resources = SolScript.Properties.Resources;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolAssembly" /> is the heart of SolScript. An assembly is the collection of all classes and global
    ///     variables of an ongoing execution. From here you can create new classes, access global variables, include libraries
    ///     and generally start using SolScript.
    /// </summary>
    public sealed class SolAssembly : IMetaDataProvider
    {
        private SolAssembly(SolAssemblyOptions options)
        {
            Input = Console.OpenStandardInput();
            Output = Console.OpenStandardOutput();
            InputEncoding = Console.InputEncoding;
            OutputEncoding = Console.OutputEncoding;
            m_Options = options.Clone();
            Errors = SolErrorCollection.CreateCollection(out m_ErrorAdder, options.WarningsAreErrors);
        }

        /// <summary>
        ///     The bytecode version of this assembly. Bytecode of older versions may or may not be compatible.
        /// </summary>
        public const uint BYTECODE_VERSION = 0;

        #region IMetaDataProvider Members

        /// <inheritdoc />
        public bool TryGetMetaValue<T>(MetaKey<T> key, out T value)
        {
            object valueObj;
            if (!m_MetaCache.TryGetValue(key, out valueObj) || !(valueObj is T)) {
                value = default(T);
                return false;
            }
            value = (T) valueObj;
            return true;
        }

        /// <inheritdoc />
        public bool TrySetMetaValue<T>(MetaKey<T> key, T value)
        {
            m_MetaCache[key] = value;
            return true;
        }

        /// <inheritdoc />
        public bool HasMetaValue<T>(MetaKey<T> key, bool ignoreType = false)
        {
            object value;
            if (!m_MetaCache.TryGetValue(key, out value)) {
                return false;
            }
            return value is T;
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj == this;
        }


        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }

        #endregion

        /// <summary>
        ///     Creates a new assembly builder.
        /// </summary>
        /// <returns>The assembly builder.</returns>
        public static Builder Create()
        {
            return new Builder();
        }

        /// <summary>
        ///     Gets the global variables associated with the given access modifier.
        /// </summary>
        /// <param name="access">The access modifier.</param>
        /// <returns>The variable source.</returns>
        public IVariables GetVariables(SolAccessModifier access)
        {
            // todo: declared only global variables.
            switch (access) {
                case SolAccessModifier.Global:
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
        ///     Tries to get a global function with the given name.
        /// </summary>
        /// <param name="name">The name of the function to find.</param>
        /// <param name="definition">Outputs the definition. Only valid if the method returned true.</param>
        /// <returns>true if the function could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Invalid state. </exception>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetGlobalFunction(string name, out SolFunctionDefinition definition)
        {
            return m_GlobalFunctions.TryGetValue(name, out definition);
        }

        /// <summary>
        ///     Tries to get a global field with the given name.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        /// <param name="definition">Outputs the definition. Only valid if the method returned true.</param>
        /// <returns>true if the field could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Invalid state. </exception>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetGlobalField(string name, out SolFieldDefinition definition)
        {
            return m_GlobalFields.TryGetValue(name, out definition);
        }

        /// <summary>
        ///     Tries to get the class definition for the given native type.
        /// </summary>
        /// <param name="nativeType">The native type.</param>
        /// <param name="definition">This out value contains the found class, or null.</param>
        /// <param name="nativeIsDescriptor">Is the given native type a type descriptor(true) or the object type itself(false)?</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Invalid state.
        /// </exception>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(Type nativeType, [CanBeNull] out SolClassDefinition definition, bool nativeIsDescriptor = false)
        {
            if (nativeIsDescriptor) {
                return m_DescriptorClasses.TryGetValue(nativeType, out definition);
            }
            return m_DescribedClasses.TryGetValue(nativeType.NotNull(), out definition);
        }

        /// <summary>
        ///     Tries to get the class definition of the given name.
        /// </summary>
        /// <param name="className">The class name.</param>
        /// <param name="definition">This out value contains the found class, or null.</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Invalid state.
        /// </exception>
        /// />
        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(string className, [CanBeNull] out SolClassDefinition definition)
        {
            return m_ClassDefinitions.TryGetValue(className, out definition);
        }

        /// <exception cref="ArgumentException">The class cannot be instantiated and creation is not enforced.</exception>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the instance.</exception>
        private SolClass New_Impl(SolClassDefinition definition, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            //AssertState(AssemblyState.GeneratedClassBodies, AssertMatch.ExactOrHigher, "Cannot create class instances without having generated the class bodies.");
            if (!options.EnforceCreation && !definition.CanBeCreated()) {
                throw new ArgumentException($"The class \"{definition.Type}\" cannot be instantiated.");
            }
            var annotations = new ps.PSList<SolClass>();
            SolClass instance = new SolClass(definition);
            // The context is required to actually initialize the fields.
            SolExecutionContext creationContext = options.CallingContext ?? new SolExecutionContext(this, definition.Type + "#" + instance.Id + " creation context");
            SolClass.Inheritance activeInheritance = instance.InheritanceChain;
            // todo this needs to start at the base type in order for field inits to work? or does it not matter since field inits should not depend on members?!
            while (activeInheritance != null) {
                DynamicReference inheritanceDynRef = new DynamicReference.ClassDescriptorObject(instance);
                // Create Annotations
                if (options.CreateAnnotations) {
                    try {
                        IVariables variables = activeInheritance.GetVariables(SolAccessModifier.Local, SolVariableMode.All);
                        annotations.AddRange(activeInheritance.Definition.DeclaredAnnotations
                            .Select(a => New(a.Definition, ClassCreationOptions.Enforce(), a.Arguments.Evaluate(creationContext, variables)))
                        );
                        /*annotations.AddRange(InternalHelper.CreateAnnotations(
                            creationContext,
                            activeInheritance.GetVariables(SolAccessModifier.Local, SolVariableMode.All),
                            activeInheritance.Definition.DeclaredAnnotations,
                            activeInheritance.Definition.DescriptorType));*/
                    } catch (SolTypeRegistryException ex) {
                        throw new SolTypeRegistryException(activeInheritance.Definition.Location,
                            $"An error occured while initializing one of the annotation on class \"{instance.Type}\"(Inheritance Level: \"{activeInheritance.Definition.Type}\").",
                            ex);
                    }
                }
                foreach (gen.KeyValuePair<string, SolFieldDefinition> fieldPair in activeInheritance.Definition.DecalredFieldLookup) {
                    SolFieldDefinition fieldDefinition = fieldPair.Value;
                    IVariables variables = activeInheritance.GetVariables(fieldDefinition.AccessModifier, SolVariableMode.Declarations);
                    // Which variable context is this field declared in?
                    // Declare the field.  
                    bool wasDeclared = false;
                    switch (fieldDefinition.Initializer.FieldType) {
                        //    -> but check later on (after ctor) if they are actually inited. !! RESPECT DECLAREXYZFIELD OPTION!!
                        case SolFieldInitializerWrapper.Type.ScriptField:
                            if (options.DeclareScriptFields) {
                                variables.Declare(fieldDefinition.Name, fieldDefinition.Type);
                                wasDeclared = true;
                            }
                            break;
                        case SolFieldInitializerWrapper.Type.NativeField:
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
                                //variables.AssignAnnotations(fieldDefinition.Name, InternalHelper.CreateAnnotations(creationContext, localUsage, fieldDefinition.DeclaredAnnotations, provider));
                                SolClass[] fieldAnnotations = fieldDefinition.DeclaredAnnotations
                                    .Select(a => New(a.Definition, ClassCreationOptions.Enforce(), a.Arguments.Evaluate(creationContext, localUsage)))
                                    .ToArray();
                                variables.AssignAnnotations(fieldDefinition.Name, fieldAnnotations);
                            } catch (SolTypeRegistryException ex) {
                                throw new SolTypeRegistryException(activeInheritance.Definition.Location,
                                    $"An error occured while initializing one of the annotation on class field \"{instance.Type}.{fieldDefinition.Name}\"(Inheritance Level: \"{activeInheritance.Definition.Type}\").",
                                    ex);
                            }
                        }
                        // Assign the script fields.
                        if (options.AssignScriptFields && fieldDefinition.Initializer.FieldType == SolFieldInitializerWrapper.Type.ScriptField) {
                            // Evaluate in the variables of the inheritance since the field initializer of e.g. a global field may still refer to a local field.
                            SolExpression fieldExpression = fieldDefinition.Initializer.GetScriptField();
                            if (fieldExpression != null) {
                                try {
                                    variables.Assign(fieldDefinition.Name, fieldExpression.Evaluate(creationContext, localUsage));
                                } catch (SolVariableException ex) {
                                    throw new SolTypeRegistryException(fieldDefinition.Location,
                                        $"An error occured while initializing the field \"{fieldDefinition.Name}\" on class \"{definition.Type}\".", ex);
                                }
                            }
                        }
                    }
                }
                // todo: function annotations? should they be lazily created?(that stuff really does not belong into variables though?!)
                // or it is time to get rid of this stupid lazy function init stuff(but my sweet memory!)
                activeInheritance = activeInheritance.BaseInheritance;
            }
            instance.AnnotationsArray = new ps.Array<SolClass>(annotations.ToArray());
            if (options.CallConstructor) {
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
        /// <exception cref="ArgumentException">
        ///     No class with <paramref name="name" /> exists. -or- The class cannot be
        ///     instantiated and creation is not enforced.
        /// </exception>
        public SolClass New(string name, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            SolClassDefinition definition;
            if (!m_ClassDefinitions.TryGetValue(name, out definition)) {
                throw new ArgumentException($"The class \"{name}\" does not exist.");
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
        /// <exception cref="ArgumentException">
        ///     The <paramref name="definition" /> belongs to a different assembly. -or- The class
        ///     cannot be instantiated and creation is not enforced.
        /// </exception>
        public SolClass New(SolClassDefinition definition, ClassCreationOptions options, params SolValue[] constructorArguments)
        {
            if (!ReferenceEquals(definition.Assembly, this)) {
                throw new ArgumentException($"Cannot create class \"{definition.Type}\"(Assembly: \"{definition.Assembly.Name}\") in Assembly \"{Name}\".");
            }
            SolClass instance = New_Impl(definition, options, constructorArguments);
            return instance;
        }

        /// <summary>
        ///     Compiles the assembly to the given binary writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <exception cref="IOException">An I/O error occurs. </exception>
        public void Compile(BinaryWriter writer)
        {
            SolCompliationContext context = new SolCompliationContext {
                State = SolCompilationState.Preparing
            };
            // Get all used file names
            foreach (SolClassDefinition classDefinition in m_ClassDefinitions.Values) {
                context.RegisterFile(classDefinition.Location.File);
            }
            foreach (SolFunctionDefinition function in m_GlobalFunctions.Values) {
                context.RegisterFile(function.Location.File);
            }
            foreach (SolFieldDefinition field in m_GlobalFields.Values) {
                context.RegisterFile(field.Location.File);
            }
            context.State = SolCompilationState.Started;
            writer.Write(BYTECODE_VERSION);
            writer.Write(context.FileIndices.Count);
            foreach (gen.KeyValuePair<string, uint> indexPair in context.FileIndices) {
                writer.Write(indexPair.Key);
                writer.Write(indexPair.Value);
            }
        }

        #region Nested type: Builder

        /// <summary>
        ///     The assembly builder is used to create new assemblies.
        /// </summary>
        public class Builder
        {
            /// <summary>
            ///     Creates a new builder instance.
            /// </summary>
            internal Builder()
            {
                m_Libraries.Add(lang.GetLibrary());
            }

            private readonly ps.PSHashSet<SolLibrary> m_Libraries = new ps.PSHashSet<SolLibrary>();
            private readonly ps.PSHashSet<string> m_SrcFileNames = new ps.PSHashSet<string>();
            private readonly ps.PSList<string> m_SrcStrings = new ps.PSList<string>();
            private SolAssembly m_Assembly;

            private SolAssemblyOptions m_Options;

            /// <summary>
            ///     The source files referenced by this builder.
            /// </summary>
            public ps.ReadOnlyHashSet<string> SourceFiles => m_SrcFileNames.AsReadOnly();

            /// <summary>
            ///     The source strings referenced by this builder.
            /// </summary>
            public ps.ReadOnlyList<string> SourceStrings => m_SrcStrings.AsReadOnly();

            /// <summary>
            ///     Includes new source files in this builder.
            /// </summary>
            /// <param name="files">The files.</param>
            /// <returns>The builder.</returns>
            public Builder IncludeSourceFiles(params string[] files)
            {
                m_SrcFileNames.AddRange(files);
                return this;
            }

            /// <summary>
            ///     Includes new source strings in this builder.
            /// </summary>
            /// <param name="strings">The strings.</param>
            /// <returns>The builder.</returns>
            public Builder IncludeSourceStrings(params string[] strings)
            {
                m_SrcStrings.AddRange(strings);
                return this;
            }

            /// <summary>
            ///     Includes new libraries in this builder.
            /// </summary>
            /// <param name="libraries">The libraries.</param>
            /// <returns>The builder.</returns>
            /// <remarks>The <see cref="lang" /> library is included by default.</remarks>
            public Builder IncludeLibraries(params SolLibrary[] libraries)
            {
                m_Libraries.AddRange(libraries);
                return this;
            }

            /// <summary>
            ///     Tries to build the assembly. Check <see cref="Errors" /> for possible errors or warnings.
            /// </summary>
            /// <param name="options">The options for the assembly.</param>
            /// <param name="assembly">The assembly.</param>
            /// <returns>true if the assembly was successfully created, false if not.</returns>
            public bool TryBuild(SolAssemblyOptions options, out SolAssembly assembly)
            {
                m_Options = options;
                CurrentlyParsing = m_Assembly = assembly = new SolAssembly(options);
                if (!TryBuildLibraries()) {
                    CurrentlyParsing = null;
                    return false;
                }
                if (!TryBuildScripts()) {
                    CurrentlyParsing = null;
                    return false;
                }
                // todo: --!validate scripts !-- 
                TryCreateNativeMapping();
                if (!TryCreate()) {
                    CurrentlyParsing = null;
                    return false;
                }
                CurrentlyParsing = null;
                return true;
            }

            private bool TryCreateNativeMapping()
            {
                // TODO: it feels kind of hakcy to just override the previous values. maybe gen the native mappings in one go with the rest?
                ps.PSList<SolClassDefinition> requiresNativeMapping = new ps.PSList<SolClassDefinition>();
                foreach (SolClassDefinition definition in m_Assembly.m_ClassDefinitions.Values) {
                    if (definition.DescriptorType != null) {
                        // Native classes don't need a native binding.
                        continue;
                    }
                    if (definition.BaseClass?.DescriptorType == null) {
                        // We are not inheriting from a native class.
                        continue;
                    }
                    requiresNativeMapping.Add(definition);
                }
                NativeCompiler.CreateNativeClassForSolClass(requiresNativeMapping, new NativeCompiler.Context { AssemblyName = m_Assembly.Name });
                return true;
            }

            private bool TryCreate()
            {
                SolExecutionContext context = new SolExecutionContext(m_Assembly, m_Assembly.Name + " initialization context");
                m_Assembly.GlobalVariables = new GlobalVariable(m_Assembly);
                m_Assembly.InternalVariables = new InternalVariables(m_Assembly);
                m_Assembly.LocalVariables = new LocalVariables(m_Assembly);

                // ===========================================================================

                // Declare all global functions and fields but do not initialize them since their annotations or field
                // initializer might refer to an undeclared field/function.
                // todo: figure all this out. I dont think there will a proper way to never create a deadlock like situation.
                // A possible solution would be to do it like java and don't let initializers refer to members below them. 
                // Or like C# and only allow constants, but I'd REALLY(I mean really!) like to avoid that.
                // Declare Functions ... (AND ASSIGN!)
                foreach (gen.KeyValuePair<string, SolFunctionDefinition> funcPair in m_Assembly.GlobalFunctionPairs) {
                    SolDebug.WriteLine("Processing global function " + funcPair.Key + " ...");
                    SolFunctionDefinition funcDefinition = funcPair.Value;
                    IVariables declareInVariables = m_Assembly.GetVariables(funcDefinition.AccessModifier);
                    SolFunction function;
                    switch (funcDefinition.Chunk.ChunkType) {
                        case SolChunkWrapper.Type.ScriptChunk:
                            function = new SolScriptGlobalFunction(funcDefinition);
                            break;
                        case SolChunkWrapper.Type.NativeMethod:
                            function = new SolNativeGlobalFunction(funcDefinition, DynamicReference.NullReference.Instance);
                            break;
                        case SolChunkWrapper.Type.NativeConstructor:
                            m_Assembly.m_ErrorAdder.Add(new SolError(funcDefinition.Location, ErrorId.None,
                                Resources.Err_GlobalFunctionCannotBeNativeConstructor.ToString(funcPair, funcDefinition.Chunk.GetNativeConstructor().DeclaringType)));
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    try {
                        declareInVariables.Declare(funcPair.Key, new SolType(SolFunction.TYPE, false));
                        if (funcDefinition.DeclaredAnnotations.Count > 0) {
                            try {
                                declareInVariables.AssignAnnotations(funcPair.Key, funcDefinition.DeclaredAnnotations.CreateAnnotations(context, m_Assembly.LocalVariables));
                            } catch (SolTypeRegistryException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(funcDefinition.Location, ErrorId.None, Resources.Err_FailedToCreateGlobalFunctionAnnotations.ToString(funcDefinition.Name),
                                    false, ex));
                                return false;
                            }
                        }
                        declareInVariables.Assign(funcPair.Key, function);
                    } catch (SolVariableException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(funcDefinition.Location, ErrorId.None, Resources.Err_FailedToDeclareGlobalFunction.ToString(funcDefinition.Name), false, ex));
                        return false;
                    }
                }
                // Initialize global fields
                foreach (gen.KeyValuePair<string, SolFieldDefinition> fieldPair in m_Assembly.GlobalFieldPairs) {
                    SolDebug.WriteLine("Processing global field " + fieldPair.Key + " ...");
                    SolFieldDefinition fieldDefinition = fieldPair.Value;
                    IVariables declareInVariables = m_Assembly.GetVariables(fieldDefinition.AccessModifier);
                    switch (fieldDefinition.Initializer.FieldType) {
                        case SolFieldInitializerWrapper.Type.ScriptField:
                            try {
                                declareInVariables.Declare(fieldPair.Key, fieldDefinition.Type);
                                if (fieldDefinition.DeclaredAnnotations.Count > 0) {
                                    try {
                                        declareInVariables.AssignAnnotations(fieldPair.Key, fieldDefinition.DeclaredAnnotations.CreateAnnotations(context, m_Assembly.LocalVariables));
                                    } catch (SolVariableException ex) {
                                        m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.None, Resources.Err_FailedToCreateGlobalFieldAnnotations.ToString(fieldDefinition.Name), false, ex));
                                        return false;
                                    }
                                }
                                SolExpression scriptInitializer = fieldDefinition.Initializer.GetScriptField();
                                if (scriptInitializer != null) {
                                    declareInVariables.Assign(fieldPair.Key, scriptInitializer.Evaluate(context, m_Assembly.LocalVariables));
                                }
                            } catch (SolVariableException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(fieldDefinition.Location, ErrorId.None, Resources.Err_FailedToDeclareGlobalField.ToString(fieldDefinition.Name), false, ex));
                                return false;
                            }
                            break;
                        case SolFieldInitializerWrapper.Type.NativeField:
                            FieldOrPropertyInfo nativeField = fieldDefinition.Initializer.GetNativeField();
                            try {
                                declareInVariables.DeclareNative(fieldPair.Key, fieldDefinition.Type, nativeField, DynamicReference.NullReference.Instance);
                            } catch (SolVariableException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(fieldDefinition.Location, ErrorId.None, Resources.Err_FailedToDeclareGlobalField.ToString(fieldDefinition.Name), false, ex));
                                return false;
                            }
                            try {
                                declareInVariables.AssignAnnotations(fieldPair.Key, fieldDefinition.DeclaredAnnotations.CreateAnnotations(context, m_Assembly.LocalVariables));
                            } catch (SolVariableException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.None, Resources.Err_FailedToCreateGlobalFieldAnnotations.ToString(fieldDefinition.Name), false, ex));
                                return false;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                return true;
            }

            /// <summary>
            ///     Builds all scripts.
            /// </summary>
            /// <returns>true if everything worked as expected, false if an error occured.</returns>
            private bool TryBuildScripts()
            {
                var trees = new ps.PSList<ParseTree>();
                Irony.Parsing.Parser parser = new Irony.Parsing.Parser(Grammar);

                // Scan the source strings & files for code.
                for (int i = 0; i < m_SrcStrings.Count; i++) {
                    ParseTree tree = parser.Parse(m_SrcStrings[i], "Source:" + i.ToString(CultureInfo.InvariantCulture));
                    trees.Add(tree);
                }
                foreach (string fileName in m_SrcFileNames) {
                    string text;
                    try {
                        // todo: Parser Read File Encoding
                        text = File.ReadAllText(fileName);
                    } catch (ArgumentException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIsInvalid.ToString(fileName ?? "null"), false, ex));
                        return false;
                    } catch (NotSupportedException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIsInvalid.ToString(fileName ?? "null"), false, ex));
                        return false;
                    } catch (IOException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIOError.ToString(fileName), false, ex));
                        return false;
                    } catch (UnauthorizedAccessException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIOError.ToString(fileName), false, ex));
                        return false;
                    } catch (SecurityException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None, Resources.Err_SourceFileIOError.ToString(fileName), false, ex));
                        return false;
                    }
                    ParseTree tree = parser.Parse(text, fileName);
                    trees.Add(tree);
                }

                // ===========================================================================

                // All trees have been built. Time to check them for errors.
                // Parse all trees even if we have errors. This allows easier debugging for the user if there are errors
                // spread accross multiple files.
                bool errorInTrees = false;
                foreach (ParseTree tree in trees) {
                    foreach (LogMessage message in tree.ParserMessages) {
                        switch (message.Level) {
                            case ErrorLevel.Info:
                            case ErrorLevel.Warning:
                                m_Assembly.m_ErrorAdder.Add(new SolError(message.Location, ErrorId.SyntaxError, message.Message, true));
                                errorInTrees = errorInTrees || m_Options.WarningsAreErrors;
                                continue;
                            case ErrorLevel.Error:
                                m_Assembly.m_ErrorAdder.Add(new SolError(message.Location, ErrorId.SyntaxError, message.Message));
                                errorInTrees = true;
                                continue;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                if (errorInTrees) {
                    return false;
                }

                // ===========================================================================

                // At this point we can be sure that our parse trees are valid. Let's go ahead and transform them into
                // SolScript statements/classes and all the other fun!
                // Parse all trees even if we have errors. This allows easier debugging for the user if there are errors
                // spread accross multiple files.
                StatementFactory factory = m_Assembly.Factory;
                foreach (ParseTree tree in trees) {
                    try {
                        StatementFactory.TreeData treeData = factory.InterpretTree(tree);
                        // Register the parsed classes
                        foreach (SolClassDefinition classDefinition in treeData.Classes) {
                            try {
                                m_Assembly.m_ClassDefinitions.Add(classDefinition.Type, classDefinition);
                            } catch (ArgumentException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(classDefinition.Location, ErrorId.InterpreterError, Resources.Err_DuplicateClass.ToString(classDefinition.Type), false, ex));
                                errorInTrees = true;
                            }
                        }
                        // Register the parsed global fields
                        foreach (SolFieldDefinition fieldDefinition in treeData.Fields) {
                            try {
                                m_Assembly.m_GlobalFields.Add(fieldDefinition.Name, fieldDefinition);
                            } catch (ArgumentException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(fieldDefinition.Location, ErrorId.InterpreterError, Resources.Err_DuplicateGlobalField.ToString(fieldDefinition.Name), false,
                                    ex));
                                errorInTrees = true;
                            }
                        }
                        // Register the parsed global functions
                        foreach (SolFunctionDefinition functionDefinition in treeData.Functions) {
                            try {
                                m_Assembly.m_GlobalFunctions.Add(functionDefinition.Name, functionDefinition);
                            } catch (ArgumentException ex) {
                                m_Assembly.m_ErrorAdder.Add(new SolError(functionDefinition.Location, ErrorId.InterpreterError, Resources.Err_DuplicateGlobalFunction.ToString(functionDefinition.Name),
                                    false, ex));
                                errorInTrees = true;
                            }
                        }
                    } catch (SolInterpreterException ex) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.InterpreterError, Resources.Err_SourceFileIsInvalid.ToString(tree.FileName), false, ex));
                        errorInTrees = true;
                    }
                }
                if (errorInTrees) {
                    return false;
                }
                // ===========================================================================

                // And we've done it again! Time to finally move on to validating all of this.
                return true;
            }

            /// <summary>
            ///     Builds all native libraries.
            /// </summary>
            /// <returns>true if everything worked as expected, false if an error occured.</returns>
            private bool TryBuildLibraries()
            {
                var globals = new ps.PSList<Type>();
                // Build the raw definition hulls.
                foreach (SolLibrary library in m_Libraries) {
                    foreach (Assembly libraryAssembly in library.Assemblies) {
                        foreach (Type libraryType in libraryAssembly.GetTypes()) {
                            // Get descriptor
                            SolTypeDescriptorAttribute descriptor = libraryType.GetCustomAttribute<SolTypeDescriptorAttribute>();
                            if (descriptor != null && descriptor.LibraryName == library.Name) {
                                SolDebug.WriteLine(library.Name + " - " + libraryType + " describes " + descriptor.Describes);
                                // Get name
                                string name = libraryType.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? libraryType.Name;
                                // Create definition object
                                SolClassDefinition definition = new SolClassDefinition(m_Assembly, SolSourceLocation.Native(), true) {
                                    Type = name,
                                    TypeMode = descriptor.TypeMode,
                                    DescribedType = descriptor.Describes,
                                    DescriptorType = libraryType
                                };
                                // Register in ...
                                //   ... descibed type lookup
                                try {
                                    m_Assembly.m_DescribedClasses.Add(definition.DescribedType, definition);
                                } catch (ArgumentException ex) {
                                    m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None,
                                        Resources.Err_DuplicateClassDescribed.ToString(definition.DescribedType), false, ex));
                                    return false;
                                }
                                //   ... descriptor type lookup
                                try {
                                    m_Assembly.m_DescriptorClasses.Add(definition.DescriptorType, definition);
                                } catch (ArgumentException ex) {
                                    m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None,
                                        Resources.Err_DuplicateClassDescriptor.ToString(definition.DescriptorType), false, ex));
                                    return false;
                                }
                                //   ... class name lookup
                                try {
                                    m_Assembly.m_ClassDefinitions.Add(name, definition);
                                } catch (ArgumentException ex) {
                                    m_Assembly.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.None,
                                        Resources.Err_DuplicateClass.ToString(name), false, ex));
                                    return false;
                                }
                            }
                            // Get global
                            SolGlobalAttribute global = libraryType.GetCustomAttribute<SolGlobalAttribute>();
                            if (global != null && global.Library == library.Name) {
                                // Globals will be searched later since the type hierarchy needs to be
                                // built in order to determine their return types.
                                globals.Add(libraryType);
                            }
                        }
                    }
                }

                //          !! LIBRARIES HAVE NO MORE MEANING PAST THIS POINT !!
                // ===========================================================================

                // Figure out inheritance 
                // Need to be built in a different iteration since not all definitions may be
                // created while trying to access one in the previous.
                foreach (SolClassDefinition definition in m_Assembly.m_ClassDefinitions.Values) {
                    // Set base class
                    Type describedBaseType = definition.DescribedType.BaseType;
                    SolClassDefinition baseClassDefinition;
                    if (describedBaseType != null && m_Assembly.m_DescribedClasses.TryGetValue(describedBaseType, out baseClassDefinition)) {
                        definition.BaseClassReference = new SolClassDefinitionReference(m_Assembly, baseClassDefinition);
                    }
                }
                // ===========================================================================

                // Time to build functions and fields!
                // They need to be built in a different iteration since the marshaller needs
                // to access the inheritance chain in order to determine the sol types.
                // Also we will turn a constructor into a function.
                foreach (SolClassDefinition definition in m_Assembly.m_ClassDefinitions.Values) {
                    // Build native class functions
                    foreach (MethodInfo method in definition.DescriptorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (method.IsSpecialName) {
                            continue;
                        }
                        // Try to build it.
                        Result<SolFunctionDefinition> functionResult = BuildFunction(method);
                        if (!functionResult) {
                            // If it failed WITHOUT an exception it simply means that no script
                            // function can/should be generated.
                            if (functionResult.Exception == null) {
                                continue;
                            }
                            // If it fails WITH an exception it means that an actual error occured
                            // which will be reported back to the user.
                            // Additionally, assembly generation will be aborted.
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeFunction.ToString(method.FullName()),
                                false, functionResult.Exception)
                            );
                            return false;
                        }
                        SolFunctionDefinition function = functionResult.Value;
                        function.DefinedIn = definition;
                        // Guard against duplicate function names.
                        if (definition.DeclaredFunctions.Any(d => d.Name == function.Name)) {
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_DuplicateClassFunction.ToString(definition.Type, function.Name),
                                false, functionResult.Exception)
                            );
                            return false;
                        }
                        definition.AssignFunctionDirect(function);
                    }
                    // Build native class field. Same procedure as a function.
                    foreach (FieldOrPropertyInfo field in FieldOrPropertyInfo.Get(definition.DescriptorType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (field.IsSpecialName) {
                            continue;
                        }
                        Result<SolFieldDefinition> fieldResult = BuildField(field);
                        if (!fieldResult) {
                            if (fieldResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeField.ToString(field.FullName()),
                                false, fieldResult.Exception)
                            );
                            return false;
                        }
                        SolFieldDefinition fieldDef = fieldResult.Value;
                        fieldDef.DefinedIn = definition;
                        if (definition.DeclaredFields.Any(d => d.Name == fieldDef.Name)) {
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_DuplicateClassField.ToString(definition.Type, field.FullName()),
                                false, fieldResult.Exception)
                            );
                            return false;
                        }
                        definition.AssignFieldDirect(fieldDef);
                    }
                    bool hasConstructor = false;
                    // Find a class constructor ...
                    foreach (ConstructorInfo constructor in definition.DescriptorType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                        Result<SolFunctionDefinition> ctorResult = BuildConstructor(constructor);
                        if (!ctorResult) {
                            if (ctorResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeConstructor.ToString(constructor.FullName()),
                                false, ctorResult.Exception)
                            );
                            return false;
                        }
                        SolFunctionDefinition ctorDef = ctorResult.Value;
                        ctorDef.DefinedIn = definition;
                        definition.AssignFunctionDirect(ctorDef);
                        hasConstructor = true;
                        break;
                    }
                    // ... or raise an error if none could be found.
                    // todo: allow classes without constructor. Not sure how, but it seems like something which -could- be useful
                    if (!hasConstructor) {
                        m_Assembly.m_ErrorAdder.Add(new SolError(
                            SolSourceLocation.Native(), ErrorId.None,
                            Resources.Err_NoClassConstructor.ToString(definition.Type))
                        );
                        return false;
                    }
                }

                // ===========================================================================

                // Now that the entire class hierarchy has been built we can go ahead and build the globals
                foreach (Type globalType in globals) {
                    foreach (MethodInfo method in globalType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (method.IsSpecialName) {
                            continue;
                        }
                        Result<SolFunctionDefinition> functionResult = BuildFunction(method);
                        if (!functionResult) {
                            if (functionResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeFunction.ToString(method.FullName()),
                                false, functionResult.Exception)
                            );
                            return false;
                        }
                        SolFunctionDefinition function = functionResult.Value;
                        m_Assembly.m_GlobalFunctions.Add(function.Name, function);
                    }
                    foreach (FieldOrPropertyInfo field in FieldOrPropertyInfo.Get(globalType, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                        if (field.IsSpecialName) {
                            continue;
                        }
                        Result<SolFieldDefinition> fieldResult = BuildField(field);
                        if (!fieldResult) {
                            if (fieldResult.Exception == null) {
                                continue;
                            }
                            m_Assembly.m_ErrorAdder.Add(new SolError(
                                SolSourceLocation.Native(), ErrorId.None,
                                Resources.Err_FailedToBuildNativeField.ToString(field.FullName()),
                                false, fieldResult.Exception)
                            );
                            return false;
                        }
                        SolFieldDefinition fieldDef = fieldResult.Value;
                        m_Assembly.m_GlobalFields.Add(fieldDef.Name, fieldDef);
                    }
                }

                // ===========================================================================

                // Okay - We made it! 
                // The assembly is built. How will be epic adventure of assembly creation continue?
                // Find out right after this advertisement!
                // On a more serious note: Now that we have the native stuff let's parse the user scripts.
                return true;
            }

            /// <summary>
            ///     Tries to build a field definition from the given field/property wrapper.
            /// </summary>
            /// <param name="field">The field to build the ... other field from.</param>
            /// <returns>
            ///     a - Nothing if the creation failed gracefully. Execution can be continued.<br />b - An exception if a creation
            ///     failed
            ///     critically. Throw/Log an error.<br />c - The field if the creation succeeded. The field itself will be a global
            ///     function. Assign <see cref="SolFieldDefinition.DefinedIn" /> to make it a class field.
            /// </returns>
            private Result<SolFieldDefinition> BuildField(FieldOrPropertyInfo field)
            {
                SolLibraryVisibilityAttribute visibility = field.GetCustomAttribute<SolLibraryVisibilityAttribute>();
                if (visibility != null && !visibility.Visible || visibility == null && !field.IsPublic) {
                    return Result<SolFieldDefinition>.Failure();
                }
                NativeFieldPostProcessor postProcessor = m_Options.GetPostProcessor(field);
                if (postProcessor.DoesFailCreation(field)) {
                    return Result<SolFieldDefinition>.Failure();
                }
                string name = postProcessor.GetName(field);
                SolAccessModifier access = postProcessor.GetAccessModifier(field);
                SolType? remappedType = postProcessor.GetFieldType(field);
                if (remappedType == null) {
                    try {
                        remappedType = SolMarshal.GetSolType(m_Assembly, field.DataType);
                    } catch (SolMarshallingException ex) {
                        return Result<SolFieldDefinition>.Failure(ex);
                    }
                }
                SolFieldDefinition solfield = new SolFieldDefinition(m_Assembly, SolSourceLocation.Native()) {
                    Name = name,
                    AccessModifier = access,
                    Type = remappedType.Value,
                    Initializer = new SolFieldInitializerWrapper(field)
                };
                // todo: Native Annotations for Fields
                return Result<SolFieldDefinition>.Success(solfield);
            }

            private Result<SolFunctionDefinition> BuildConstructor(ConstructorInfo constructor)
            {
                // todo: flesh out ctors as well as functions
                // todo: annotations   
                SolLibraryVisibilityAttribute visibility = constructor.GetCustomAttribute<SolLibraryVisibilityAttribute>();
                if (!(visibility?.Visible ?? constructor.IsPublic)) {
                    return Result<SolFunctionDefinition>.Failure();
                }
                SolAccessModifier accessModifier = constructor.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? SolAccessModifier.Internal;
                SolParameterInfo.Native parameterInfo;
                try {
                    parameterInfo = InternalHelper.GetParameterInfo(m_Assembly, constructor.GetParameters());
                } catch (SolMarshallingException ex) {
                    return Result<SolFunctionDefinition>.Failure(ex);
                }
                SolFunctionDefinition solctor = new SolFunctionDefinition(m_Assembly, SolSourceLocation.Native()) {
                    Name = SolMetaFunction.__new.Name,
                    AccessModifier = accessModifier,
                    Type = new SolType(SolNil.TYPE, true),
                    Chunk = new SolChunkWrapper(constructor),
                    ParameterInfo = parameterInfo
                };
                return Result<SolFunctionDefinition>.Success(solctor);
            }

            /// <summary>
            ///     Tries to build a function definition from the given method info.
            /// </summary>
            /// <param name="method">The method to build the function from.</param>
            /// <returns>
            ///     a - Nothing if the creation failed gracefully. Execution can be continued.<br />b - An exception if a creation
            ///     failed
            ///     critically. Throw/Log an error.<br />c - The function if the creation succeeded. The function itself will be a
            ///     global
            ///     function. Assign <see cref="SolFunctionDefinition.DefinedIn" /> to make it a class function.
            /// </returns>
            private Result<SolFunctionDefinition> BuildFunction(MethodInfo method)
            {
                SolLibraryVisibilityAttribute visibility = method.GetCustomAttribute<SolLibraryVisibilityAttribute>();
                if (visibility != null && !visibility.Visible || visibility == null && !method.IsPublic) {
                    return Result<SolFunctionDefinition>.Failure();
                }
                NativeMethodPostProcessor postProcessor = m_Options.GetPostProcessor(method);
                if (postProcessor.DoesFailCreation(method)) {
                    return Result<SolFunctionDefinition>.Failure();
                }
                string name = postProcessor.GetName(method);
                SolAccessModifier access = postProcessor.GetAccessModifier(method);
                SolMemberModifier memberModifier;
                if (method.IsAbstract) {
                    // Abstract methods are abstract fields. No additional checks required since they cannot override.
                    memberModifier = SolMemberModifier.Abstract;
                } else if (!method.IsOverride() || !m_Assembly.m_DescriptorClasses.ContainsKey(method.GetBaseDefinition().DeclaringType.NotNull())) {
                    // If a method is not overriding anything it is a normal function in sol script.
                    // If a method is overriding something but the overridden method's declaring class is not known to SolScript we will ignore
                    // the override keyword.
                    // todo "new" keyword on native methods might fail.
                    memberModifier = SolMemberModifier.Default;
                } else {
                    // If a method overrides something and the overridden method's class is exposed to SolScript the method if an override function.
                    memberModifier = SolMemberModifier.Override;
                }
                SolType? remappedReturn = postProcessor.GetReturn(method);
                if (remappedReturn == null) {
                    try {
                        remappedReturn = SolMarshal.GetSolType(m_Assembly, method.ReturnType);
                    } catch (SolMarshallingException ex) {
                        return Result<SolFunctionDefinition>.Failure(ex);
                    }
                }
                SolParameterInfo.Native parmeterInfo;
                try {
                    parmeterInfo = InternalHelper.GetParameterInfo(m_Assembly, method.GetParameters());
                } catch (SolMarshallingException ex) {
                    return Result<SolFunctionDefinition>.Failure(ex);
                }
                SolFunctionDefinition function = new SolFunctionDefinition(m_Assembly, SolSourceLocation.Native()) {
                    Name = name,
                    AccessModifier = access,
                    Type = remappedReturn.Value,
                    Chunk = new SolChunkWrapper(method),
                    MemberModifier = memberModifier,
                    ParameterInfo = parmeterInfo
                };
                // todo: Native Annotations for Methods
                return Result<SolFunctionDefinition>.Success(function);
            }
        }

        #endregion

        #region Static Fields & Properties

        #region Public

        /// <summary>
        ///     Should the SolScript grammar rules be cached? This allows much faster parser startup, but the grammar object is
        ///     quite large(2-3MB). On the flipside, constantly creating objects this large and then releaseing them will be bad
        ///     for the GC. To summarize: Set this to true if you expect to generate more than one assembly, false if only one.
        /// </summary>
        public static bool CacheGrammar {
            get { return s_CacheGrammar; }
            set {
                s_CacheGrammar = value;
                if (!value) {
                    s_Grammar = null;
                }
            }
        }

        #endregion

        #region Non Public

        internal static SolAssembly CurrentlyParsing;

        // The Irony Grammar rules used for SolScript.
        private static SolScriptGrammar s_Grammar;
        // CacheGrammar backing field.
        private static bool s_CacheGrammar = true;

        internal static SolScriptGrammar Grammar {
            get {
                SolScriptGrammar gr = s_Grammar;
                if (gr == null) {
                    gr = new SolScriptGrammar();
                    if (CacheGrammar) {
                        s_Grammar = gr;
                    }
                }
                return gr;
            }
        }

        #endregion

        #endregion

        #region Member Fields & Properties

        #region Public

        /// <summary>
        ///     Contains the errors raised during the creation of the assembly.<br /> Runtime errors are NOT added to this error
        ///     collection. They are thrown as a <see cref="SolRuntimeException" /> instead.
        /// </summary>
        public SolErrorCollection Errors { get; }

        /// <summary>
        ///     All global fields in key value pairs.
        /// </summary>
        public ps.ReadOnlyDictionary<string, SolFieldDefinition> GlobalFieldPairs => m_GlobalFields.AsReadOnly();

        /// <summary>
        ///     All global functions in key value pairs.
        /// </summary>
        public ps.ReadOnlyDictionary<string, SolFunctionDefinition> GlobalFunctionPairs => m_GlobalFunctions.AsReadOnly();

        /// <summary>
        /// All classes in this assembly.
        /// </summary>
        public ps.ReadOnlyCollection<SolClassDefinition> Classes => m_ClassDefinitions.Values;

        /// <summary>
        ///     A descriptive name of this assembly(e.g. "Enemy AI Logic"). The name will be used during debugging and error
        ///     output.
        /// </summary>
        public string Name => m_Options.Name;

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
        ///     The output stream of this assembly. Used for writing data from SolScript. (Defaults to the console output)<br />
        ///     Although possible, it is not recommended to change this value during runtime.
        /// </summary>
        public Stream Output { get; set; }

        /// <summary>
        ///     The encoding of the <see cref="Output" /> stream. (Defaults to the console encoding)
        /// </summary>
        public Encoding OutputEncoding { get; set; }

        #endregion

        #region Non Public

        // Stores all meta data provider objects.
        private readonly ps.PSDictionary<MetaKeyBase, object> m_MetaCache = new ps.PSDictionary<MetaKeyBase, object>(MetaKeyBase.NameComparer);
        private readonly ps.PSDictionary<string, SolClassDefinition> m_ClassDefinitions = new ps.PSDictionary<string, SolClassDefinition>();
        // Errors can be added here.
        private readonly SolErrorCollection.Adder m_ErrorAdder;
        private readonly ps.PSDictionary<string, SolFieldDefinition> m_GlobalFields = new ps.PSDictionary<string, SolFieldDefinition>();
        private readonly ps.PSDictionary<string, SolFunctionDefinition> m_GlobalFunctions = new ps.PSDictionary<string, SolFunctionDefinition>();
        private readonly ps.PSDictionary<Type, SolClassDefinition> m_DescribedClasses = new ps.PSDictionary<Type, SolClassDefinition>();
        private readonly ps.PSDictionary<Type, SolClassDefinition> m_DescriptorClasses = new ps.PSDictionary<Type, SolClassDefinition>();
        // The options for creating this assembly.
        private readonly SolAssemblyOptions m_Options;
        // The lazy statement factory.
        private StatementFactory l_factory;


        // The statement factory is used for parsing raw source files.
        private StatementFactory Factory => l_factory ?? (l_factory = new StatementFactory(this));

        /// <summary>
        ///     The global variables of an assembly are exposed to everything. Other assemblies, classes and other globals can
        ///     access them.
        /// </summary>
        internal IVariables GlobalVariables;

        /// <summary>
        ///     Internal globals can only be accessed from other global variables and functions. They should only be used for meta
        ///     functions.
        /// </summary>
        internal IVariables InternalVariables;

        /// <summary>
        ///     The local variables of an assembly can only be accessed from other global variables and functions.
        /// </summary>
        internal IVariables LocalVariables;

        #endregion

        #endregion
    }
}