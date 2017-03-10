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

    [SolGlobal("test")]
    public static class test_G
    {
        [SolGlobal("test")]
        public static string Field;
    }
}