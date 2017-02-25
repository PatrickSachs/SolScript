using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using Ionic.Zip;
using Irony;
using Irony.Parsing;
using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Libraries.lang;
using SolScript.Parser;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolAssembly" /> is the heart of SolScript. An assembly is the collection of all classes and global
    ///     variables of an ongoing exceution. From here you can create new classes, access global variables, include libraries
    ///     and generally start using SolScript.
    /// </summary>
    public sealed class SolAssembly
    {
        private SolAssembly(SolAssemblyOptions options)
        {
            m_Options = (SolAssemblyOptions) options.Clone();
            TypeRegistry = new TypeRegistry(this);
            m_Libraries.Add(lang.GetLibrary());
            Errors = SolErrorCollection.CreateCollection(out m_ErrorAdder, options.WarningsAreErrors);
        }

        // The Irony Grammar rules used for SolScript.
        private static readonly SolScriptGrammar s_Grammar = new SolScriptGrammar();
        // These options will be used for creating a singeton. Their creation will need to be enforced since they are not creatable.
        private static readonly ClassCreationOptions s_SingletonClassCreationOptions = new ClassCreationOptions.Customizable().SetEnforceCreation(true);
        // Errors can be added here.
        private readonly SolErrorCollection.Adder m_ErrorAdder;
        // All libraries registered in this Assembly.
        private readonly List<SolLibrary> m_Libraries = new List<SolLibrary>();
        // The options for creating this assembly.
        private readonly SolAssemblyOptions m_Options;
        // The lazy statement factory.
        private StatementFactory l_factory;
        // The statement factory is used for parsing raw source files.
        private StatementFactory Factory => l_factory ?? (l_factory = new StatementFactory(this));

        /// <summary>
        ///     The <see cref="TypeRegistry" /> is used to access the classes in a <see cref="SolAssembly" />. You can create new
        ///     class instances or simply look up the <see cref="SolClassDefinition" /> of a class.
        /// </summary>
        public TypeRegistry TypeRegistry { get; }

        /// <summary>
        ///     The global variables of an assembly are exposed to everything. Other assemblies, clases and other globals can
        ///     access them.
        /// </summary>
        public IVariables GlobalVariables { get; private set; }

        /// <summary>
        ///     The local variables of an assembly can only be accessed from other global variables and functions.
        /// </summary>
        public IVariables LocalVariables { get; private set; }

        /// <summary>
        ///     todo: figure out what internal globals should be used for or if they should be allowed at all.
        /// </summary>
        public IVariables InternalVariables { get; private set; }

        /// <summary>
        ///     A descriptivate name of this assembly(e.g. "Enemy AI Logic"). The name will be used during debugging and error
        ///     output.
        /// </summary>
        public string Name => m_Options.Name;

        /// <summary>
        ///     Contains the errors raised during the creation of the assembly.<br /> Runtime errors are NOT added to this error
        ///     collection. They are thrown as a <see cref="SolRuntimeException" /> instead.
        /// </summary>
        public SolErrorCollection Errors { get; }

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
                var trees = new List<ParseTree>(100);
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
                            "The file \"" + dir + "\" does not exist. However the OS told us about the existance of this file. Do you have other threads messing with the file?", ex);
                    } catch (SecurityException ex) {
                        throw new UnauthorizedAccessException("Cannot access file \"" + dir + "\".", ex);
                    }
                    trees.Add(tree);
                    SolDebug.WriteLine("  ... Loaded " + dir);
                    foreach (LogMessage message in tree.ParserMessages) {
                        SolDebug.WriteLine("   " + message.Location + " : " + message);
                    }
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
                var trees = new List<ParseTree>(strings.Length);
                SolDebug.WriteLine("Loading Trees ...");
                foreach (string s in strings) {
                    ParseTree tree = parser.Parse(s);
                    trees.Add(tree);
                    SolDebug.WriteLine("  ... Loaded " + s);
                    foreach (LogMessage message in tree.ParserMessages) {
                        SolDebug.WriteLine("   " + message.Location + " : " + message);
                    }
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
        ///     Creates a <see cref="SolAssembly" /> by interpreting the contents of a single zip file.
        /// </summary>
        /// <param name="options">
        ///     The options for creating this assembly. - The options will be cloned in the assembly and further
        ///     changes to the options will thus have no effect.
        /// </param>
        /// <param name="file">The file to interpret.</param>
        /// <returns>
        ///     The assembly which can directly be created(<see cref="Create" />) or first have some libraries included(
        ///     <see cref="IncludeLibrary" />).
        /// </returns>
        /// <exception cref="FileNotFoundException">The file does not exist. <paramref name="file" /></exception>
        /// <exception cref="IOException">An I/O error occurs. </exception>
        public static SolAssembly FromFile(SolAssemblyOptions options, string file)
        {
#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
#endif
                if (!File.Exists(file)) {
                    throw new FileNotFoundException("Tried to load SolAssembly from file " + file +
                                                    ", which does not exist.");
                }
                SolDebug.WriteLine("Creating Parser ...");
                Irony.Parsing.Parser parser = new Irony.Parsing.Parser(s_Grammar);
                List<ParseTree> trees;
                using (ZipFile zip = ZipFile.Read(file)) {
                    trees = new List<ParseTree>(zip.Count);
                    SolDebug.WriteLine("Loading Trees ...");
                    foreach (ZipEntry entry in zip) {
                        using (MemoryStream entryStream = new MemoryStream()) {
                            if (entry.FileName.EndsWith(".sol")) {
                                entry.Extract(entryStream);
                                entryStream.Position = 0;
                                using (StreamReader stream = new StreamReader(entryStream)) {
                                    string str;
                                    try {
                                        str = stream.ReadToEnd();
                                    } catch (OutOfMemoryException ex) {
                                        throw new IOException("The internal buffer ran out of memory.", ex);
                                    }
                                    //SolDebug.WriteLine(str);
                                    ParseTree tree = parser.Parse(str, entry.FileName);
                                    trees.Add(tree);
                                    SolDebug.WriteLine("  ... Loaded " + entry.FileName + " in " +
                                                       tree.ParseTimeMilliseconds + "ms");
                                    foreach (LogMessage message in tree.ParserMessages) {
                                        SolDebug.WriteLine("   " + message.Location + " : " + message);
                                    }
                                }
                            }
                        }
                    }
                }
                return FromTrees(trees, options);
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
            SolAssembly script = new SolAssembly(options);
            StatementFactory factory = script.Factory;
            foreach (ParseTree tree in trees) {
                SolDebug.WriteLine("  ... Checking tree " + tree.FileName);
                foreach (LogMessage message in tree.ParserMessages) {
                    switch (message.Level) {
                        case ErrorLevel.Info:
                        case ErrorLevel.Warning:
                            script.m_ErrorAdder.Add(new SolError(new SolSourceLocation(tree.FileName, message.Location), ErrorId.None, message.Message, true));
                            break;
                        case ErrorLevel.Error:
                            // todo: properly warning through the parser states in message.
                            script.m_ErrorAdder.Add(new SolError(new SolSourceLocation(tree.FileName, message.Location), ErrorId.None, message.Message));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            if (!script.Errors.HasErrors) {
                foreach (ParseTree tree in trees) {
                    SolDebug.WriteLine("  ... Building File " + tree.FileName);
                    IReadOnlyCollection<SolClassBuilder> classBuilders;
                    try {
                        factory.InterpretTree(tree, script.TypeRegistry.GlobalsBuilder, out classBuilders);
                    } catch (SolInterpreterException ex) {
                        // todo: error id on exception (or just one error in general since its probably always an internal error?)
                        script.m_ErrorAdder.Add(new SolError(ex.Location, ErrorId.InternalFailedToResolve, ex.Message, false, ex));
                        return script;
                    }
                    foreach (SolClassBuilder typeDef in classBuilders) {
                        script.TypeRegistry.RegisterClass(typeDef);
                        SolDebug.WriteLine("    ... Type " + typeDef.Name);
                    }
                }
            } else {
                script.m_ErrorAdder.Add(new SolError(SolSourceLocation.Native(), ErrorId.InternalSecurityMeasure, "Not interpreting the syntax trees since one or more of them had errors.", true));
            }
            return script;
        }

        /// <summary>
        ///     Inclused a <see cref="SolLibrary" /> which will be included in the library upon creation.
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        ///     Libraries can only be registered during the
        ///     <see cref="Interpreter.TypeRegistry.State.Registry" /> state.
        /// </exception>
        /// <seealso cref="TypeRegistry.CurrentState" />
        public SolAssembly IncludeLibrary(SolLibrary library)
        {
            TypeRegistry.AssetStateExactAndLower(TypeRegistry.State.Registry, "Libraries can only be registered during the registry state.");
            m_Libraries.Add(library);
            return this;
        }

        /// <summary>
        ///     Quick helper to register globals.
        /// </summary>
        /// <exception cref="SolVariableException">An error occured while registering the variables.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterGlobal(string name, SolValue value, SolType type)
        {
            GlobalVariables.Declare(name, type);
            GlobalVariables.Assign(name, value);
        }

        /// <summary>
        ///     This method truly creates the assembly. Everything is prepared for actual usage, the classes are created and global
        ///     variables registered. You can start using the <see cref="SolAssembly" /> after calling this method(although you
        ///     probably want to check for errors and warnings first!).
        /// </summary>
        /// <returns>The created assembly; ready for usage.</returns>
        /// <exception cref="SolTypeRegistryException">An error occured while creating the class or global definitions.</exception>
        public SolAssembly Create()
        {
            SolExecutionContext context = new SolExecutionContext(this, Name + " initialization context");
            GlobalVariables = new GlobalVariables(this);
            InternalVariables = new GlobalInternalVariables(this);
            LocalVariables = new GlobalLocalVariables(this);
            // todo: investigate either merging type registry and library or making it clearer, the separation seems a bit arbitrary.
            // Build class hulls, required in order to be able to have access too all types when building the bodies.
            foreach (SolLibrary library in m_Libraries) {
                SolDebug.WriteLine("Building class hulls of library " + library.Name);
                IReadOnlyCollection<SolClassBuilder> classHulls = library.BuildClassHulls(this);
                TypeRegistry.RegisterClasses(classHulls);
            }
            // Build class bodies
            foreach (SolLibrary library in m_Libraries) {
                SolDebug.WriteLine("Building class bodies of library " + library.Name);
                library.BuildClassBodies(this);
            }
            // Create definitions.
            TypeRegistry.GenerateDefinitions();
            // Initialize global fields
            foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in TypeRegistry.GlobalFieldPairs) {
                SolFieldDefinition fieldDefinition = fieldPair.Value;
                IVariables declareInVariables;
                switch (fieldDefinition.Modifier) {
                    case SolAccessModifier.None:
                        declareInVariables = GlobalVariables;
                        break;
                    case SolAccessModifier.Local:
                        declareInVariables = LocalVariables;
                        break;
                    case SolAccessModifier.Internal:
                        declareInVariables = InternalVariables;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                switch (fieldDefinition.Initializer.FieldType) {
                    case SolFieldInitializerWrapper.Type.ScriptField:
                        SolExpression scriptInitializer = fieldDefinition.Initializer.GetScriptField();
                        try {
                            declareInVariables.Declare(fieldPair.Key, fieldDefinition.Type);
                            declareInVariables.Assign(fieldPair.Key, scriptInitializer.Evaluate(context, LocalVariables));
                        } catch (SolVariableException ex) {
                            throw new SolTypeRegistryException("Failed to register global field \"" + fieldDefinition.Name + "\"", ex);
                        }
                        break;
                    case SolFieldInitializerWrapper.Type.NativeField:
                        FieldOrPropertyInfo nativeField = fieldDefinition.Initializer.GetNativeField();
                        try {
                            declareInVariables.DeclareNative(fieldPair.Key, fieldDefinition.Type, nativeField, DynamicReference.NullReference.Instance);
                        } catch (SolVariableException ex) {
                            throw new SolTypeRegistryException("Failed to register native global field \"" + fieldDefinition.Name + "\"", ex);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            // Create singleton instances
            // todo: investigate to create them lazily as as of now they are unable to use other singletons in their ctor.
            foreach (SolClassDefinition classDef in TypeRegistry.ClassDefinitions.Where(c => c.TypeMode == SolTypeMode.Singleton)) {
                SolDebug.WriteLine("Singleton: " + classDef.Type);
                SolClass instance = TypeRegistry.CreateInstance(classDef, s_SingletonClassCreationOptions);
                try {
                    RegisterGlobal(instance.Type, instance, new SolType(classDef.Type, false));
                } catch (SolVariableException ex) {
                    throw new SolTypeRegistryException("Failed to register singleton instance \"" + classDef.Type + "\" as global variable.", ex);
                }
            }
            return this;
        }
    }
}