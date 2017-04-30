using System.Collections.Generic;
using System.Reflection;
using PSUtility.Enumerables;

namespace SolScript.Interpreter.Library
{
    /// <summary>
    ///     A libary defines which native types should be used as SolScript classes or globals.
    /// </summary>
    public class SolLibrary
    {
        /// <summary>
        ///     Creates a new library.
        /// </summary>
        /// <param name="libraryName">The libarary name.</param>
        /// <param name="sourceAssemblies">The assemblies scanned by this library.</param>
        public SolLibrary(string libraryName, params Assembly[] sourceAssemblies)
        {
            Name = libraryName;
            m_Assemblies = new Array<Assembly>(sourceAssemblies);
        }

        // Assemblies backing field.
        private readonly Array<Assembly> m_Assemblies;

        /// <summary>
        ///     All assemblies registered for this libarary.
        /// </summary>
        public IReadOnlyList<Assembly> Assemblies => m_Assemblies;

        /// <summary>
        ///     The name of the library.
        /// </summary>
        public string Name { get; }
    }
}