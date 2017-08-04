// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Official repository: https://bitbucket.org/PatrickSachs/solscript/
// ---------------------------------------------------------------------
// Copyright 2017 Patrick Sachs
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using PSUtility.Metadata;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Parser;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolAssembly" /> is the heart of SolScript. An assembly is the collection of all classes and global
    ///     variables of an ongoing execution. From here you can create new classes, access global variables, include libraries
    ///     and generally start using SolScript.
    /// </summary>
    public sealed partial class SolAssembly : IMetaDataProvider
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
        /// <param name="options">Some options for getting the class, will use the default ones if null.</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
        /// <seealso cref="GetNativeClassOptions"/>
        [ContractAnnotation("definition:null => false")]
        public bool TryGetClass(Type nativeType, [CanBeNull] out SolClassDefinition definition, GetNativeClassOptions options = null)
        {
            options = options ?? GetNativeClassOptions.Default;
            if (options.Descriptor) {
                if (m_DescriptorClasses.TryGetValue(nativeType, out definition)) {
                    return !options.AllowDerived || definition.DescriptorType == nativeType;
                }
                if (options.AllowDerived) {
                    Type currentBaseClass = nativeType.BaseType;
                    while (currentBaseClass != null) {
                        if (m_DescribedClasses.TryGetValue(currentBaseClass, out definition)) {
                            m_DescribedClasses.Add(nativeType, definition);
                            return true;
                        }
                        currentBaseClass = currentBaseClass.BaseType;
                    }
                }
                return false;
            }
            if (m_DescribedClasses.TryGetValue(nativeType.NotNull(), out definition)) {
                return !options.AllowDerived || definition.DescribedType == nativeType;
            }
            if (options.AllowDerived) {
                Type currentBaseClass = nativeType.BaseType;
                while (currentBaseClass != null) {
                    if (m_DescribedClasses.TryGetValue(currentBaseClass, out definition)) {
                        m_DescribedClasses.Add(nativeType, definition);
                        return true;
                    }
                    currentBaseClass = currentBaseClass.BaseType;
                }
            }
            return false;
        }

        /// <summary>
        ///     Tries to get the class definition of the given name.
        /// </summary>
        /// <param name="className">The class name.</param>
        /// <param name="definition">This out value contains the found class, or null.</param>
        /// <returns>true if a class for the native type could be found, false otherwise.</returns>
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
            var annotations = new PSList<SolClass>();
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
                foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in activeInheritance.Definition.DecalredFieldLookup) {
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
            instance.AnnotationsArray = new Array<SolClass>(annotations.ToArray());
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
            foreach (KeyValuePair<string, uint> indexPair in context.FileIndices) {
                writer.Write(indexPair.Key);
                writer.Write(indexPair.Value);
            }
        }

        #region Nested type: GetNativeClassOptions

        /// <summary>
        ///     Some options for trying to get a native class using
        ///     <see cref="SolAssembly.TryGetClass(System.Type,out SolScript.Interpreter.SolClassDefinition,GetNativeClassOptions)" />.
        /// </summary>
        public class GetNativeClassOptions
        {
            /// <summary>
            /// The default options.
            /// </summary>
            public static readonly GetNativeClassOptions Default = new GetNativeClassOptions(true, false);

            /// <inheritdoc />
            public GetNativeClassOptions(bool allowDerived, bool descriptor)
            {
                AllowDerived = allowDerived;
                Descriptor = descriptor;
            }

            /// <summary>
            ///     Should we also allow derived classes(true) or only exact matches(false)?
            /// </summary>
            public bool AllowDerived { get; }

            /// <summary>
            ///     Should the descripor or the descibed clas be searched for? (Default: false=described)
            /// </summary>
            public bool Descriptor { get; }
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

        [ThreadStatic]
        internal static SolAssembly CurrentlyParsingThreadStatic;

        // The Irony Grammar rules used for SolScript.
        private static SolScriptNodeGrammar s_Grammar;
        // CacheGrammar backing field.
        private static bool s_CacheGrammar = true;

        internal static SolScriptNodeGrammar Grammar {
            get {
                SolScriptNodeGrammar gr = s_Grammar;
                if (gr == null) {
                    gr = new SolScriptNodeGrammar();
                    s_Grammar = gr;
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
        public ReadOnlyDictionary<string, SolFieldDefinition> GlobalFieldPairs => m_GlobalFields.AsReadOnly();

        /// <summary>
        ///     All global functions in key value pairs.
        /// </summary>
        public ReadOnlyDictionary<string, SolFunctionDefinition> GlobalFunctionPairs => m_GlobalFunctions.AsReadOnly();

        /// <summary>
        ///     All classes in this assembly.
        /// </summary>
        public ReadOnlyCollection<SolClassDefinition> Classes => m_ClassDefinitions.Values;

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
        private readonly PSDictionary<MetaKeyBase, object> m_MetaCache = new PSDictionary<MetaKeyBase, object>(MetaKeyBase.NameComparer);
        private readonly PSDictionary<string, SolClassDefinition> m_ClassDefinitions = new PSDictionary<string, SolClassDefinition>();
        // Errors can be added here.
        private readonly SolErrorCollection.Adder m_ErrorAdder;
        private readonly PSDictionary<string, SolFieldDefinition> m_GlobalFields = new PSDictionary<string, SolFieldDefinition>();
        private readonly PSDictionary<string, SolFunctionDefinition> m_GlobalFunctions = new PSDictionary<string, SolFunctionDefinition>();
        private readonly PSDictionary<Type, SolClassDefinition> m_DescribedClasses = new PSDictionary<Type, SolClassDefinition>();
        private readonly PSDictionary<Type, SolClassDefinition> m_DescriptorClasses = new PSDictionary<Type, SolClassDefinition>();
        // The options for creating this assembly.
        private readonly SolAssemblyOptions m_Options;

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