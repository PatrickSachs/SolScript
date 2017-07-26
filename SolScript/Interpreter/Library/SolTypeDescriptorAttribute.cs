using System;

namespace SolScript.Interpreter.Library
{
    /// <summary>
    ///     Add this attributes to a type to describe it for SolScript. Most of the time the type wants to descibe itself. By
    ///     default all public members are visible, use the <see cref="SolLibraryVisibilityAttribute" /> to hide or expose a
    ///     single member. Make sure to expose a constructor.
    ///     <br />
    ///     You can also use the attribute to describe another type. In this case you want to implement the
    ///     <see cref="INativeClassSelf" /> interface to obtain a reference to the class instance you are describing.
    /// </summary>
    public class SolTypeDescriptorAttribute : Attribute
    {
        /// <summary>
        ///     Creates the attribute.
        /// </summary>
        /// <param name="libraryName">The name of the library to descibe the type to.</param>
        /// <param name="typeMode">The type mode of the type. You typically wish to use default or sealed.</param>
        /// <param name="describes">The type to descibe.</param>
        public SolTypeDescriptorAttribute(string libraryName, SolTypeMode typeMode, Type describes)
        {
            LibraryName = libraryName;
            TypeMode = typeMode;
            Describes = describes;
        }

        /// <summary>
        ///     The type to descibe.
        /// </summary>
        public Type Describes { get; }

        /// <summary>
        ///     The name of the library to descibe the type to.
        /// </summary>
        public string LibraryName { get; }

        /// <summary>
        ///     The type mode of the type.
        /// </summary>
        public SolTypeMode TypeMode { get; }
    }
}