using SolScript.Interpreter.Library;

// ReSharper disable InconsistentNaming

namespace UnitTests.TestLibarary
{
    public static class test
    {
        public const string NAME = "test";

        private static SolLibrary s_Library;

        public static SolLibrary GetLibrary()
        {
            return s_Library ?? (s_Library = new SolLibrary(NAME, typeof(test).Assembly));
        }
    }
}