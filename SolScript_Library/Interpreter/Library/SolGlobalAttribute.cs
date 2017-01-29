using System;

namespace SolScript.Interpreter.Library
{
    /// <summary>
    ///     The <see cref="SolGlobalAttribute" /> is used to mark a method as a global function in SolScript.
    /// </summary>
    /// <remarks>Only classes annotated with this attribute are scanned for global methods.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SolGlobalAttribute : Attribute
    {
        /// <summary>
        ///     Creates a new global attribute.
        /// </summary>
        /// <param name="library">The library this attribute belongs to.</param>
        public SolGlobalAttribute(string library)
        {
            Library = library;
        }

        /// <summary>
        ///     The library this attribute belongs to.
        /// </summary>
        public string Library { get; }
    }
}