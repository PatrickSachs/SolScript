using System;
using System.Threading;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library {
    [SolLibraryClass("std", TypeDef.TypeMode.Singleton)]
    [SolLibraryName("IO")]
    public class IOModule {
        // ReSharper disable InconsistentNaming
        public void sol_out(SolValue value)
        {
            Console.Write(value);
        }

        public void sol_outln(SolValue value)
        {
            Console.WriteLine(value);
        }

        public SolString sol_in(SolValue message) {
            if (!message.Equals(SolNil.Instance)) {
                Console.WriteLine(message);
            }
            Console.Write(" > ");
            return new SolString(Console.ReadLine() ?? string.Empty);
        }

        public void wait(int time) {
            Thread.Sleep(time);
        }

        // ReSharper restore InconsistentNaming
    }
}