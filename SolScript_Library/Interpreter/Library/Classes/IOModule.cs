using System;
using System.Threading;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Classes {
    // ReSharper disable InconsistentNaming
    [SolLibraryClass("std", SolTypeMode.Singleton)]
    [SolLibraryName("IO")]
    [UsedImplicitly]
    public class IOModule {
        [UsedImplicitly]
        public void sol_out(SolValue value) {
            Console.Write(value);
        }

        [UsedImplicitly]
        public void sol_outln(SolValue value) {
            Console.WriteLine(value);
        }

        [UsedImplicitly]
        public SolString sol_in(SolExecutionContext context, SolValue message) {
            if (!message.IsEqual(context, SolNil.Instance)) {
                Console.WriteLine(message);
            }
            Console.Write(" > ");
            return SolString.ValueOf(Console.ReadLine() ?? string.Empty);
        }

        [UsedImplicitly]
        public void wait(int time) {
            Thread.Sleep(time);
        }

        public override string ToString() {
            return "IO Module";
        }
    }

    // ReSharper restore InconsistentNaming
}