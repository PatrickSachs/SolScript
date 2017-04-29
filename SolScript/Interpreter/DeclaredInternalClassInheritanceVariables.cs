using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     These <see cref="DeclaredInternalClassInheritanceVariables" /> are used to represent the variables of an
    ///     inheritance
    ///     element which are accessible only from the inside of the class.
    /// </summary>
    public class DeclaredInternalClassInheritanceVariables : DeclaredClassInheritanceVariables
    {
        /// <summary>
        ///     Creates a new <see cref="DeclaredInternalClassInheritanceVariables" /> instance for a given inheritance level.
        /// </summary>
        /// <param name="inheritance">The linked inheritance element.</param>
        internal DeclaredInternalClassInheritanceVariables(SolClass.Inheritance inheritance) : base(inheritance)
        {
            /*// ReSharper disable once ExceptionNotDocumented
            // As this is the very first operation no exception will actually occur.
            Members.SetValue("self", ofClass, new SolType(ofClass.Type, false));*/
        }

        #region Overrides

        /// <inheritdoc />
        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            return definition.AccessModifier == SolAccessModifier.Internal;
        }

        #endregion
    }
}