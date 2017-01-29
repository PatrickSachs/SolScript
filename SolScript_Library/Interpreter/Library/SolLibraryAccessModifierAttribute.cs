using System;

namespace SolScript.Interpreter.Library
{
    public class SolLibraryAccessModifierAttribute : Attribute
    {
        public SolLibraryAccessModifierAttribute(AccessModifier accessModifier)
        {
            AccessModifier = accessModifier;
        }

        public AccessModifier AccessModifier { get; }
    }
}