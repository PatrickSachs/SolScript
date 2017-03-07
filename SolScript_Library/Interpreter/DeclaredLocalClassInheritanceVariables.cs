using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Theses <see cref="DeclaredClassInheritanceVariables" /> represent the local variables of a certain inheritance chain element of a
    ///     class.
    /// </summary>
    public class DeclaredLocalClassInheritanceVariables : DeclaredClassInheritanceVariables
    {
        internal DeclaredLocalClassInheritanceVariables(SolClass.Inheritance inheritance) : base(inheritance)
        {
        }
        
        #region Overrides

        /// <inheritdoc />
        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            return definition.AccessModifier == SolAccessModifier.Local;
        }

        #endregion
    }
}