using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public class ClassInheritanceVariables : ClassVariables
    {
        internal ClassInheritanceVariables(SolClass ofClass, SolClass.Inheritance inheritance) : base(inheritance.Definition.Assembly)
        {
            // Note: ofClass is not fully created at this point. Most properties(incl. Assembly)
            // rely on the inheritance chain which is still being construced.
            m_OfClass = ofClass;
            m_Inheritance = inheritance;
        }

        private readonly SolClass.Inheritance m_Inheritance;
        private readonly SolClass m_OfClass;

        public override SolClassDefinition Definition => m_Inheritance.Definition;

        #region Overrides

        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            // Local functions can only be accessed from where they are defined in.
            if (definition.AccessModifier == AccessModifier.Local) {
                return definition.DefinedIn == Definition;
            }
            // Internal and global will be treated by the internal/global variables.
            // They are not supported here to properly support overriding, since an 
            // internal/global can be overridden at a "higher" level.
            return false;
        }

        protected override IVariables GetParent()
        {
            return m_OfClass.InternalVariables;
        }

        protected override SolClass GetInstance() => m_OfClass;

        #endregion
    }
}