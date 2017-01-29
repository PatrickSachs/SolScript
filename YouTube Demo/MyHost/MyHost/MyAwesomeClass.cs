using System;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

namespace MyHost
{
    [SolLibraryClass("mylib", SolTypeMode.Sealed)]
    [SolLibraryName("Thing")]
    public class MyAwesomeClass
    {
        [SolLibraryVisibility("mylib", true)]
        [SolLibraryName("say_hello")]
        [SolContract("Thing", false)]
        public MyAwesomeClass SayHello([SolContract("string", false)] string boo)
        {
            Console.WriteLine("Hello from .Net, " + boo + "!");
            return new MyAwesomeClass();
        }

        [SolLibraryVisibility("mylib", true)]
        [SolLibraryName("say_hello_table")]
        private MyAwesomeClass SayHelloFromATable(SolTable table)
        {
            return SayHello((table[new SolNumber(24)] as SolString)?.Value ?? "error");
        }

        public SolInteger get_int()
        {
            return new SolInteger(2423);
        }
    }
}