using System;

namespace SolScript.Interpreter.Library {
    public class SolLibraryNameAttribute : Attribute {
        public SolLibraryNameAttribute(string name) {
            Name = name;
        }

        public readonly string Name;
    }
}