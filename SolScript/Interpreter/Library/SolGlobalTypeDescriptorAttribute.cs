using System;

namespace SolScript.Interpreter.Library
{
    /// <summary>
    ///     This attribute is marked to create global fields and functions from the visible members of this class. All members
    ///     must be static.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SolGlobalTypeDescriptorAttribute : Attribute
    {
        /// <summary>
        ///     Creates a new global attribute.
        /// </summary>
        /// <param name="library">The library this attribute belongs to.</param>
        public SolGlobalTypeDescriptorAttribute(string library)
        {
            Library = library;
        }

        /// <summary>
        ///     The library this attribute belongs to.
        /// </summary>
        public string Library { get; }
    }
}