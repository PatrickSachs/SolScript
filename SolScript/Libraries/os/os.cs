// ReSharper disable InconsistentNaming

using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;

namespace SolScript.Libraries.os
{
    /// <summary>
    ///     The OS-library is used to give the users operating-system level access. This enables file access and retrieving
    ///     data about the operating system itself.
    /// </summary>
    [PublicAPI]
    public static class os
    {
        /// <summary>
        ///     The library name is "os".
        /// </summary>
        public const string NAME = nameof(os);

        private static SolLibrary Library;

        /// <summary>
        ///     Gets the os library for usage in your <see cref="SolAssembly" />.
        /// </summary>
        /// <returns>The library.</returns>
        public static SolLibrary GetLibrary()
        {
            return Library ?? (Library = new SolLibrary(NAME, typeof(os).Assembly));
        }
    }
}