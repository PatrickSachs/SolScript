using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace SolScript
{
    /// <summary>
    ///     The <see cref="SolDebug" />  class is used to internal debugging purposes.
    /// </summary>
    internal static class SolDebug
    {
        /// <summary>
        ///     By how many characters should the message be indented?
        /// </summary>
        public static int Indent { get; set; } = 30;

        /// <summary>
        ///     Writes a line to the console and debug output.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="caller">Do not set - The caller file name.</param>
        /// <param name="member">Do not set - The caller member name.</param>
        /// <remarks>Only included in <c>DEBUG</c> builds.</remarks>
        [Conditional("DEBUG")]
        public static void WriteLine(string message,
#if DEBUG
            [CallerFilePath]
#endif
        string caller = "?",
#if DEBUG
            [CallerMemberName]
#endif
        string member = "?")
        {
            string file = Path.GetFileNameWithoutExtension(caller) ?? "?";
            string prefix = file + "." + member + "()";
            int indent = Indent - prefix.Length;
            prefix = prefix + new string(' ', indent < 0 ? 0 : indent);
            string str = prefix + (indent > 0 ? " -> " : "->") + message;
            Debug.WriteLine(str);
            //Console.WriteLine(str);
        }

        // Should the Stack Trace be debgged? (That's just one of things you don't ever expect having to debug, but as it turns out: you do!)
        [Conditional("DEBUG_STACKTRACE")]
        public static void StackTrace(string message,
#if DEBUG
            [CallerFilePath]
#endif
        string caller = "?",
#if DEBUG
            [CallerMemberName]
#endif
        string member = "?")
        {
            WriteLine("[STACK TRACE] " + message, caller, member);
        }
    }
}