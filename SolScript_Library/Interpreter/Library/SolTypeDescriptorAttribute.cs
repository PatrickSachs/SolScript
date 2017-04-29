using System;

namespace SolScript.Interpreter.Library
{
    public class SolTypeDescriptorAttribute : Attribute
    {
        public SolTypeDescriptorAttribute(string libraryName, SolTypeMode typeMode, Type describes)
        {
            LibraryName = libraryName;
            TypeMode = typeMode;
            Describes = describes;
        }

        public string LibraryName { get; }
        public SolTypeMode TypeMode { get; }
        public Type Describes { get; }
    }
}