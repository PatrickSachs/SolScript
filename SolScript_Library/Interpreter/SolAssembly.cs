using System;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using Irony;
using Irony.Parsing;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Parser;

namespace SolScript.Interpreter {
    public class SolAssembly {
        private static readonly SolScriptGrammar s_Grammar = new SolScriptGrammar();
        private readonly List<SolLibrary> m_Libraries = new List<SolLibrary>();
        internal readonly TypeRegistry TypeRegistry = new TypeRegistry();
        public SolExecutionContext RootContext { get; private set; }

        public string Name { get; private set; } = "Unnamed SolAssembly";

        public static SolAssembly FromDirectory(string sourceDir) {
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
        }

        public static SolAssembly FromStrings(params string[] strings) {
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
        }

        private static SolAssembly FromTrees(IEnumerable<ParseTree> trees) {
            SolDebug.WriteLine("Building Trees ...");
            SolAssembly script = new SolAssembly();
            foreach (ParseTree tree in trees) {
                SolDebug.WriteLine("  ... Building File " + tree.FileName);
                var typeDefs = StatementFactory.GetClassDefinitions(tree.Root);
                foreach (TypeDef typeDef in typeDefs) {
                    script.TypeRegistry.Types.Add(typeDef.Name, typeDef);
                    SolDebug.WriteLine("    ... Type " + typeDef.Name);
                }
            }
            return script;
        }

        public static SolAssembly FromFile(string file) {
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
                                SolDebug.WriteLine("  ... Loaded " + entry.FileName + " in " + tree.ParseTimeMilliseconds + "ms");
                                foreach (LogMessage message in tree.ParserMessages) {
                                    SolDebug.WriteLine("   " + message.Location + " : " + message);
                                }
                            }
                        }
                    }
                }
            }
            return FromTrees(trees);
        }

        public void IncludeLibrary(SolLibrary library) {
            m_Libraries.Add(library);
        }

        private void RegisterLibraries() {
            foreach (SolLibrary library in m_Libraries) {
                foreach (var globalFunc in library.GlobalFuncs)
                {
                    SolDebug.WriteLine("Registered Global Function " + globalFunc.Key + " from library " + library.Name);
                    SolCSharpFunction function =
                        SolCSharpFunction.CreateFrom(
                            globalFunc.Value.Creator2.NotNull("Script global funcs in library?!"),
                            DynamicReference.NullReference.Instance);
                    RootContext.VariableContext.DeclareVariable(globalFunc.Key, function, new SolType("function", false),
                        false);
                }
                foreach (var globalField in library.GlobalFields)
                {
                    SolDebug.WriteLine("Registered Global Field " + globalField.Key + " from library " + library.Name);
                    RootContext.VariableContext.DeclareVariable(globalField.Key, globalField.Value.Creator2,
                        DynamicReference.NullReference.Instance, new SolType("function", false), false);
                }
                foreach (string typeName in library.Types) {
                    TypeDef def = library.GetType(typeName);
                    TypeRegistry.Types.Add(typeName, def);
                    SolDebug.WriteLine("Registered Type " + typeName + " from library " + library.Name);
                    switch (def.Mode) {
                        case TypeDef.TypeMode.Default:
                            // Nothing, the class is created by the user.
                            break;
                        case TypeDef.TypeMode.Singleton:
                            RootContext.VariableContext.DeclareVariable(def.Name,
                                TypeRegistry.CreateInstance(this, def.Name, new SolValue[0]),
                                new SolType("function", false), false);
                            SolDebug.WriteLine("  ... Created " + typeName + " singleton.");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(def), "Invalid class mode " + def + ".");
                    }
                }
            }
        }

        public void Run() {
            RootContext = new SolExecutionContext(this);
            //RootContext.VariableContext.SetValue("nil", SolNil.Instance, new SolType("nil", true), false);
            //RootContext.VariableContext.SetValue("false", SolBoolean.False, new SolType("bool", false), false);
            //RootContext.VariableContext.SetValue("true", SolBoolean.True, new SolType("bool", false), false);
            RegisterLibraries();
            SolCustomType mainInstance = TypeRegistry.CreateInstance(this, "Main",
                new SolValue[] {new SolString("Argument Test!"), new SolNumber(666)});
        }
    }
}