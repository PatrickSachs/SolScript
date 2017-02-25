// ReSharper disable InconsistentNaming

using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The SolScript standard library implements a wide variety of general purpose functionality.
    /// </summary>
    [PublicAPI]
    public static class std
    {
        /// <summary>
        ///     The library name is "std".
        /// </summary>
        public const string NAME = nameof(std);

        private static SolLibrary Library;

        /// <summary>
        ///     Gets the standard library for usage in your <see cref="SolAssembly" />. Unless you wish to provide your own custom
        ///     standard library you should always include this library.
        /// </summary>
        /// <returns>The library.</returns>
        public static SolLibrary GetLibrary()
        {
            return Library ?? (Library = new SolLibrary(NAME, typeof(std).Assembly));
        }
    }
}