using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     These <see cref="DeclaredClassInheritanceVariables" /> are used to represent the variables of an inheritance
    ///     element which are accessible outside of the class.
    /// </summary>
    public class DeclaredGlobalClassInheritanceVariables : DeclaredClassInheritanceVariables
    {
        /// <summary>
        ///     Creates a new <see cref="DeclaredGlobalClassInheritanceVariables" /> instance for a given inheritance level.
        /// </summary>
        /// <param name="inheritance">The linked inheritance element.</param>
        internal DeclaredGlobalClassInheritanceVariables(SolClass.Inheritance inheritance) : base(inheritance) {}

        #region Overrides

        /// <inheritdoc />
        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            // Globals can be accessed.
            return definition.AccessModifier == SolAccessModifier.Global;
        }

        #endregion
    }
}