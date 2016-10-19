using System;

namespace SolScript.Interpreter.Library {
    [AttributeUsage(AttributeTargets.Class)]
    public class SolLibraryGlobalsAttribute : Attribute {
        public SolLibraryGlobalsAttribute(string libName) {
            LibraryName = libName;
        }

        public readonly string LibraryName;
    }
}