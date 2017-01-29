using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

namespace MyHost
{
    class Program
    {
        static void Main(string[] args)
        {
            SolAssembly assembly = SolAssembly.FromDirectory(@"E:\Data\Development\SolScript\YouTube Demo\Demo1");
            assembly.Name = "A wonderful and useful assembly";
            assembly.IncludeLibrary(SolLibrary.StandardLibrary);
            assembly.IncludeLibrary(new SolLibrary("mylib", typeof(Program).Assembly));
            assembly.Create();
            SolValue value = assembly.GlobalVariables.Get("__main");
            var ctx = new SolExecutionContext(assembly, assembly.Name + " context");
            SolFunction function = value as SolFunction;
            if (function != null) {
                function.Call(ctx, SolMarshal.MarshalFromCSharp(assembly, typeof(string[]), new [] {
                    "key1", "key2", "another key"
                }));
            }
            SolClass mySolClass = assembly.TypeRegistry.PrepareInstance("Main").Create(new SolExecutionContext(assembly, "another context") {ParentContext = ctx});
            Console.ReadLine();
        }
    }
}
