using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public class ClassInternalVariables : ClassVariables
    {
        public ClassInternalVariables(SolClass ofClass) : base(ofClass.Assembly)
        {
            m_OfClass = ofClass;
            Members.SetValue("self", ofClass, new SolType(ofClass.Type, false));
        }

        private readonly SolClass m_OfClass;

        public override SolClassDefinition Definition => m_OfClass.InheritanceChain.Definition;

        #region Overrides

        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            // Internal and globals are ok for internal scope.
            if (definition.AccessModifier == AccessModifier.Internal) {
                return true;
            }
            // Locals cannot be accessed outside of their inheritance scope.
            // Global will be treated by the global variables. They are not 
            // supported here to properly support overriding, since a global 
            // can be overridden at a "higher" level.
            return false;
        }

        protected override IVariables GetParent()
        {
            //return ((SolClass)Members.DirectRawGet("self")).GlobalVariables;
            return m_OfClass.GlobalVariables;
        }

        protected override SolClass GetInstance() => m_OfClass;

        #endregion
    }
}