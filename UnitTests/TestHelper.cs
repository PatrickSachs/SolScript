using System.Diagnostics;
using SolScript;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

namespace UnitTests
{
    internal static class TestHelper
    {
        public static SolAssembly NewEmptyAssembly(string name, params SolLibrary[] libraries)
        {
            SolAssembly assembly;
            SolAssembly.Create()
                .IncludeLibraries(libraries)
                .TryBuild(new SolAssemblyOptions(name), out assembly);
            return assembly;
        }

        public static SolAssembly NewAssembly(string name, string code, params SolLibrary[] libraries)
        {
            SolAssembly assembly;
            SolAssembly.Create()
                .IncludeLibraries(libraries)
                .IncludeSourceStrings(code)
                .TryBuild(new SolAssemblyOptions(name), out assembly);
            return assembly;
        }

        public static SolValue NewAssemblyAndRun(string name, string code, params SolLibrary[] libraries)
        {
            SolExecutionContext context;
            return NewAssemblyAndRun(name, code, out context, libraries);
        }

        public static SolValue NewAssemblyAndRun(string name, string code, out SolExecutionContext context, params SolLibrary[] libraries)
        {
            SolAssembly assembly;
            SolAssembly.Create()
                .IncludeLibraries(libraries)
                .IncludeSourceStrings(code)
                .TryBuild(new SolAssemblyOptions(name), out assembly);
            if (assembly.Errors.Count != 0) {
                Debug.WriteLine(' ' + new string('=', 15));
                Debug.WriteLine(name + " has " + assembly.Errors.Count + " error(s):");
                foreach (SolError error in assembly.Errors) {
                    Debug.WriteLine("* " + error);
                }
                Debug.WriteLine(' ' + new string('=', 15));
            }
            SolFunction value = (SolFunction) assembly.GetVariables(SolAccessModifier.Global).Get("test");
            return value.Call(context = new SolExecutionContext(assembly, name + " (Test Context)"));
        }

        public static SolValue NewAssemblyAndRun(string name, string code, out SolAssembly assembly, params SolLibrary[] libraries)
        {
            SolExecutionContext context;
            SolValue value = NewAssemblyAndRun(name, code, out context, libraries);
            assembly = context.Assembly;
            return value;
        }
    }
}