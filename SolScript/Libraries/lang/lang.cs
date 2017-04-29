using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.lang
{
    /// <summary>
    ///     This <see cref="SolLibrary" /> contains functions and classes shipped with SolScript by default. <br />This library
    ///     will be included by default and must be removed if you truly wish to remove even the most basic api(e.g. when
    ///     implementing your own).
    /// </summary>
    [PublicAPI]
    public static class lang
    {
        /// <summary>
        ///     The library name is "lang".
        /// </summary>
        public const string NAME = nameof(lang);

        private static SolLibrary Library;

        /// <summary>
        ///     Gets the os library for usage in your <see cref="SolAssembly" />.
        /// </summary>
        /// <returns>The library.</returns>
        public static SolLibrary GetLibrary()
        {
            return Library ?? (Library = new SolLibrary(NAME, typeof(lang).Assembly));
        }
    }
}