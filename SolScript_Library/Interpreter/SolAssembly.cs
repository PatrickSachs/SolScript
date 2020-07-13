using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Ionic.Zip;
using Irony;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Parser;

namespace SolScript.Interpreter {
    public class SolAssembly {
        public SolAssembly(string entryPoint = "Main", string name = "Unnamed SolAssembly") {
            m_EntryPoint = entryPoint;
            Name = name;
            TypeRegistry = new TypeRegistry(this);
        }

        private StatementFactory l_factory;
        private StatementFactory Factory => l_factory ?? (l_factory = new StatementFactory(this));

        private static readonly SolScriptGrammar s_Grammar = new SolScriptGrammar();
        private readonly string m_EntryPoint;
        private readonly List<SolLibrary> m_Libraries = new List<SolLibrary>();
        internal readonly TypeRegistry TypeRegistry;
        public IVariables GlobalVariables { get; private set; }

        public string Name { get; }

        public static SolAssembly FromDirectory(string sourceDir) {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                SolDebug.WriteLine("Creating Parser ...");
                Irony.Parsing.Parser parser = new Irony.Parsing.Parser(s_Grammar);
                SolDebug.WriteLine("Loading Trees ...");
                var trees = new List<ParseTree>(100);
                foreach (string dir in Directory.GetFiles(sourceDir)) {
                    ParseTree tree = parser.Parse(File.ReadAllText(dir), dir);
                    trees.Add(tree);
                    SolDebug.WriteLine("  ... Loaded " + dir);
                    foreach (LogMessage message in tree.ParserMessages) {
                        SolDebug.WriteLine("   " + message.Location + " : " + message);
                    }
                }
                return FromTrees(trees);
            } finally {
                stopwatch.Stop();
                SolDebug.WriteLine("StopWatch: " + stopwatch.ElapsedMilliseconds);
            }
        }

        public static SolAssembly FromStrings(params string[] strings) {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
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
                return FromTrees(trees);
            } finally {
                stopwatch.Stop();
                SolDebug.WriteLine("StopWatch: " + stopwatch.ElapsedMilliseconds);
            }
        }

        private static SolAssembly FromTrees(IEnumerable<ParseTree> trees) {
            SolDebug.WriteLine("Building Trees ...");
            SolAssembly script = new SolAssembly();
            StatementFactory factory = script.Factory;
            foreach (ParseTree tree in trees) {
                SolDebug.WriteLine("  ... Building File " + tree.FileName);
                var typeDefs = factory.GetClassDefinitions(tree.Root);
                foreach (SolClassBuilder typeDef in typeDefs) {
                    script.TypeRegistry.RegisterClass(typeDef);
                    SolDebug.WriteLine("    ... Type " + typeDef.Name);
                }
            }
            return script;
        }

        public static SolAssembly FromFile(string file) {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
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
                                    string str = stream.ReadToEnd();
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
                return FromTrees(trees);
            } finally {
                stopwatch.Stop();
                SolDebug.WriteLine("StopWatch: " + stopwatch.ElapsedMilliseconds);
            }
        }

        public void IncludeLibrary(SolLibrary library) {
            m_Libraries.Add(library);
        }

        private void RegisterGlobal(string name, SolValue value, SolType type) {
            GlobalVariables.Declare(name, type);
            GlobalVariables.Assign(name, value);
        }

        public SolClass Run(params SolValue[] args) {
            SolExecutionContext context = new SolExecutionContext(this);
            GlobalVariables = new ChunkVariables(this);
            // Build language functions/values
            SolType fType = new SolType(SolFunction.TYPE);
            DynamicReference fRef = DynamicReference.NullReference.Instance;
            RegisterGlobal("nil", SolNil.Instance, new SolType(SolNil.TYPE, true));
            RegisterGlobal("false", SolBool.False, new SolType(SolBool.TYPE, true));
            RegisterGlobal("true", SolBool.True, new SolType(SolBool.TYPE, true));
            // issue: create non class functions for globals (investigate class free functions for language aswell)
            RegisterGlobal("error", SolCSharpStaticFunction.CreateFrom(this, SolAssemblyGlobals.error_Method, fRef), fType);
            RegisterGlobal("print", SolCSharpStaticFunction.CreateFrom(this, SolAssemblyGlobals.print_Method, fRef), fType);
            RegisterGlobal("type", SolCSharpStaticFunction.CreateFrom(this, SolAssemblyGlobals.type_Method, fRef), fType);
            RegisterGlobal("to_string", SolCSharpStaticFunction.CreateFrom(this, SolAssemblyGlobals.to_string_Method, fRef), fType);
            RegisterGlobal("assert", SolCSharpStaticFunction.CreateFrom(this, SolAssemblyGlobals.assert_Method, fRef), fType);
            RegisterGlobal("default", SolCSharpStaticFunction.CreateFrom(this, SolAssemblyGlobals.default_Method, fRef), fType);
            // todo: investigate either merging type registry and library or making it clearer, the separation seems a bit arbitrary.
            // Build class hulls, required in order to be able to have access too all types when building the bodies.
            foreach (SolLibrary library in m_Libraries) {
                SolDebug.WriteLine("Building class hulls of library " + library.Name);
                var classHulls = library.BuildClassHulls(this);
                TypeRegistry.RegisterClasses(classHulls);
            }
            // Build class bodies
            foreach (SolLibrary library in m_Libraries) {
                SolDebug.WriteLine("Building class bodies of library " + library.Name);
                library.BuildClassBodies(this);
            }
            // Create definitions.
            TypeRegistry.CreateClassDefinitions();
            foreach (SolClassDefinition classDef in TypeRegistry.ClassDefinitions.Where(c => c.TypeMode == SolTypeMode.Singleton)) {
                SolDebug.WriteLine("Singleton: " + classDef.Type);
                SolClass instance = TypeRegistry.CreateInstance(classDef, true).Create(context);
                GlobalVariables.Declare(instance.Type, new SolType(classDef.Type));
                GlobalVariables.Assign(instance.Type, instance);
            }
            return TypeRegistry.CreateInstance(m_EntryPoint, false, args).Create(context);
        }
    }

    internal static class SolAssemblyGlobals {
        static SolAssemblyGlobals() {
            error_Method = typeof (SolAssemblyGlobals).GetMethod("error", BindingFlags.Public | BindingFlags.Static);
            print_Method = typeof (SolAssemblyGlobals).GetMethod("print", BindingFlags.Public | BindingFlags.Static);
            type_Method = typeof (SolAssemblyGlobals).GetMethod("type", BindingFlags.Public | BindingFlags.Static);
            to_string_Method = typeof (SolAssemblyGlobals).GetMethod("to_string", BindingFlags.Public | BindingFlags.Static);
            assert_Method = typeof (SolAssemblyGlobals).GetMethod("assert", BindingFlags.Public | BindingFlags.Static);
            default_Method = typeof (SolAssemblyGlobals).GetMethod("default", BindingFlags.Public | BindingFlags.Static);
        }

        public static MethodInfo error_Method;
        public static MethodInfo print_Method;
        public static MethodInfo type_Method;
        public static MethodInfo to_string_Method;
        public static MethodInfo assert_Method;
        public static MethodInfo default_Method;

        [UsedImplicitly]
        public static void error(SolExecutionContext context, string message) {
            throw SolScriptInterpreterException.Raw(context, message);
        }

        [UsedImplicitly]
        public static void print(SolExecutionContext context, params SolValue[] values) {
            Console.WriteLine(context.CurrentLocation + " : " + string.Join(", ", (object[]) values));
        }

        [UsedImplicitly]
        public static string type(SolValue value) {
            return value.Type;
        }

        [UsedImplicitly]
        public static string to_string(SolValue value) {
            return value.ToString();
        }

        // ReSharper disable once UnusedParameter.Global
        [UsedImplicitly]
        public static bool assert(SolExecutionContext context, SolValue value, string message = "Assertion failed!") {
            if (value.IsEqual(context, SolNil.Instance) || value.IsEqual(context, SolBool.False)) {
                throw SolScriptInterpreterException.Raw(context, message);
            }
            return true;
        }

        [UsedImplicitly]
        public static SolValue @default(SolExecutionContext context, string type/*, params SolValue[] ctorArgs*/) {
            /*char lastChar = type[type.Length - 1];
            if (lastChar == '?') {
                return SolNil.Instance;
            }
            if (lastChar == '!') {
                type = type.Substring(0, type.Length - 1);
            }*/
            switch (type) {
                case "nil": {
                    return SolNil.Instance;
                }
                case "bool": {
                    return SolBool.False;
                }
                case "number": {
                    return new SolNumber(0);
                }
                case "table": {
                    return new SolTable();
                }
                case "string": {
                    return SolString.Empty;
                }
                default: {
                    SolClassDefinition classDef;
                    if (!context.Assembly.TypeRegistry.TryGetClass(type, out classDef)) {
                            // todo: more exceptions
                        throw new NotImplementedException("class does not exist");
                    }
                    return SolNil.Instance;
                    /*switch (classDef.TypeMode) {
                        case SolTypeMode.Sealed:
                        case SolTypeMode.Default:
                            return context.Assembly.TypeRegistry.CreateInstance(classDef, false, ctorArgs);
                        case SolTypeMode.Singleton:
                            // This should return the singleton object.
                            return context.Assembly.RootContext.VariableContext.GetValue(context, classDef.Name);
                        case SolTypeMode.Annotation:
                            throw new NotImplementedException();
                        case SolTypeMode.Abstract:
                            throw new NotImplementedException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }*/
                }
            }
        }
    }
}