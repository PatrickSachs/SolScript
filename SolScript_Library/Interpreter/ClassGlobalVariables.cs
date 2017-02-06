using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     These <see cref="ClassVariables"/> are used to represent the variables of a class which are accessible outside of the class.
    /// </summary>
    public class ClassGlobalVariables : ClassVariables
    {
        /// <summary>
        ///     Creates a new <see cref="ClassGlobalVariables" /> instance for a given class.
        /// </summary>
        /// <param name="ofClass">The class linked to theses variables.</param>
        internal ClassGlobalVariables(SolClass ofClass) : base(ofClass.Assembly)
        {
            m_OfClass = ofClass;
        }

        /// <summary>
        ///     The linked class.
        /// </summary>
        private readonly SolClass m_OfClass;

        /// <inheritdoc />
        public override SolClassDefinition Definition => m_OfClass.InheritanceChain.Definition;

        /// <inheritdoc />
        protected override bool OnlyUseDeclaredFunctions => false;

        #region Overrides

        /// <inheritdoc />
        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            // Globals can be accessed.
            if (definition.AccessModifier == AccessModifier.None) {
                return true;
            }
            // Locals cannot be accessed outside of their inheritance scope.
            // Internals cannot be accessed outside of the class.
            return false;
        }

        /// <inheritdoc />
        protected override IVariables GetParent()
        {
            return Assembly.GlobalVariables;
        }

        /// <inheritdoc />
        protected override SolClass GetInstance()
        {
            return m_OfClass;
        }

        #endregion
    }
}