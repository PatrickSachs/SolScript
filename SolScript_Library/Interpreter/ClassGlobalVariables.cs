using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public class ClassGlobalVariables : ClassVariables
    {
        internal ClassGlobalVariables(SolClass ofClass) : base(ofClass.Assembly)
        {
            m_OfClass = ofClass;
        }

        private readonly SolClass m_OfClass;
        public override SolClassDefinition Definition => m_OfClass.InheritanceChain.Definition;

        #region Overrides

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

        protected override IVariables GetParent()
        {
            return Assembly.GlobalVariables;
        }

        protected override SolClass GetInstance()
        {
            return m_OfClass;
        }

        #endregion
    }
}