using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="ClassInternalVariables" /> are used to represent the variables only visible inside the respective
    ///     class.
    /// </summary>
    public class ClassInternalVariables : ClassVariables
    {
        /// <summary>
        ///     Creates the <see cref="ClassInheritanceVariables" /> instance and declares the "self" field, a reference to
        ///     <paramref name="ofClass" />.
        /// </summary>
        /// <param name="ofClass">The class theses variables belong to.</param>
        public ClassInternalVariables(SolClass ofClass) : base(ofClass.Assembly)
        {
            m_OfClass = ofClass;
            // ReSharper disable once ExceptionNotDocumented
            // As this is the very first operation no exception will actually occur.
            Members.SetValue("self", ofClass, new SolType(ofClass.Type, false));
        }

        /// <summary>
        ///     The class these variables belong to.
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
            // Internal and globals are ok for internal scope.
            if (definition.AccessModifier == SolAccessModifier.Internal) {
                return true;
            }
            // Locals cannot be accessed outside of their inheritance scope.
            // Global will be treated by the global variables. They are not 
            // supported here to properly support overriding, since a global 
            // can be overridden at a "higher" level.
            return false;
        }

        /// <inheritdoc />
        protected override IVariables GetParent()
        {
            return m_OfClass.GlobalVariables;
        }

        /// <inheritdoc />
        protected override SolClass GetInstance() => m_OfClass;

        #endregion
    }
}