using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Library {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SolLibraryClassAttribute : Attribute {
        public SolLibraryClassAttribute([NotNull] string libName, SolTypeMode mode) {
            LibraryName = libName;
            Mode = mode;
        }

        public readonly string LibraryName;
        public readonly SolTypeMode Mode;
    }
}