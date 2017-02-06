using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Theses <see cref="ClassVariables" /> represent the local variables of a certain inheritance chain element of a
    ///     class. Local variables need multiple different variable conexts since they are isolated at each inheritance level.
    /// </summary>
    public class ClassInheritanceVariables : ClassVariables
    {
        internal ClassInheritanceVariables(SolClass ofClass, SolClass.Inheritance inheritance) : base(inheritance.Definition.Assembly)
        {
            // Note: ofClass is not fully created at this point. Most properties(incl. Assembly)
            // rely on the inheritance chain which is still being construced.
            m_OfClass = ofClass;
            m_Inheritance = inheritance;
        }

        /// <summary>
        ///     The inheritance these variables belong to.
        /// </summary>
        private readonly SolClass.Inheritance m_Inheritance;

        /// <summary>
        ///     The class these variables and inheritance belong to.
        /// </summary>
        // todo: class field inside inheritance. no additional overhead since the class would be stored in multiple other places anyways.
        private readonly SolClass m_OfClass;

        /// <inheritdoc />
        public override SolClassDefinition Definition => m_Inheritance.Definition;

        /// <inheritdoc />
        protected override bool OnlyUseDeclaredFunctions => true;

        #region Overrides

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override IVariables GetParent()
        {
            return m_OfClass.InternalVariables;
        }

        /// <inheritdoc />
        protected override SolClass GetInstance() => m_OfClass;

        #endregion
    }
}