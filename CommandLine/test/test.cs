using System;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;

namespace SolScript.test
{
    /// <summary>
    ///     The test library is used for ... well testing purposes. Hooray!
    /// </summary>
    public static class test
    {
        /// <summary>
        ///     The library name is test. Such creativity, such wow. (That joke isn't funny anymore, is it?)
        /// </summary>
        public const string NAME = "test";

        private static SolLibrary l_library;

        /// <summary>
        ///     Gets the <see cref="test" /> library.
        /// </summary>
        /// <returns>The library.</returns>
        public static SolLibrary GetLibrary()
        {
            return l_library ?? (l_library = new SolLibrary(NAME, typeof(test).Assembly));
        }
    }

    [SolGlobalTypeDescriptor("test")]
    public static class test_G
    {
        [SolVisibility( true)]
        public static string Field;

        [SolVisibility(true)]
        public static void BREAK()
        {
            Console.WriteLine("> BREAKPOINT <");
        }
    }
}