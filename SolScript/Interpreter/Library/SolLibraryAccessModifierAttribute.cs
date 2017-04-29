using System;

namespace SolScript.Interpreter.Library
{
    public class SolLibraryAccessModifierAttribute : Attribute
    {
        public SolLibraryAccessModifierAttribute(SolAccessModifier accessModifier)
        {
            AccessModifier = accessModifier;
        }

        public SolAccessModifier AccessModifier { get; }
    }
}