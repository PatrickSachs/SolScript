using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace SolScript {
    public static class SolDebug {
        public static int Indent { get; set; } = 30;

        [Conditional("DEBUG")]
        public static void WriteLine(string message, [CallerFilePath] string caller = "?",
            [CallerMemberName] string member = "?") {
            string file = Path.GetFileNameWithoutExtension(caller) ?? "?";
            string prefix = file + "." + member + "()";
            int indent = Indent - prefix.Length;
            prefix = prefix + new string(' ', indent < 0 ? 0 : indent);
            string str = prefix + (indent > 0 ? " -> " : "->") + message;
            Debug.WriteLine(str);
            Console.WriteLine(str);
        }
    }
}