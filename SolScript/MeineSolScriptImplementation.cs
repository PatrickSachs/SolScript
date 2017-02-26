// ReSharper disable UnusedMember.Global
// ReSharper disable CheckNamespace
// ReSharper disable ExceptionNotDocumented
// ReSharper disable ExceptionNotDocumentedOptional

using SolScript.Interpreter;
using SolScript.Interpreter.Types;
using SolScript.Libraries.std;

namespace MyApplication
{
    public class MySolScriptImplementation
    {
        public void CreateScriptHost()
        {
            // Creates an assembly from the given code strings and includes the standard-library.
            SolAssembly solScript = SolAssembly.FromStrings(new SolAssemblyOptions("Demo Assembly"), "function say_hello() print('Hello World!') end")
                .IncludeLibrary(std.GetLibrary())
                .Create();
            // Gets the say_hello function & calls it.
            SolFunction sayHello = (SolFunction) solScript.GlobalVariables.Get("say_hello");
            sayHello.Call(new SolExecutionContext(solScript, "MySolScriptImplementation"));
        }
    }
}