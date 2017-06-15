using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;

namespace SolScript.Interpreter
{
    public abstract class SolMemberDefinition : SolAnnotateableDefinitionBase
    {
        /// <inheritdoc />
        public SolMemberDefinition(SolAssembly assembly, SourceLocation location) : base(assembly, location) { }

        internal SolMemberDefinition() { }


        /// <summary>
        ///     The access modifier for this function which decide from where the function can be accessed.
        /// </summary>
        public SolAccessModifier AccessModifier { get; internal set; }

        /// <summary>
        ///     The name of the field.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     The data type of the field.
        /// </summary>
        public SolType Type { get; internal set; }
        /// <summary>
        ///     The class this field was defined in. This is null for global fields.
        /// </summary>
        [CanBeNull]
        public SolClassDefinition DefinedIn { get; internal set; }

        private readonly PSList<SolAnnotationDefinition> m_DeclaredAnnotations = new PSList<SolAnnotationDefinition>();

        /// <inheritdoc />
        public override ReadOnlyList<SolAnnotationDefinition> DeclaredAnnotations => m_DeclaredAnnotations.AsReadOnly();

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            if (DefinedIn != null)
            {
                return DefinedIn.Type + "." + Name;
            }
            return Name;
        }

        /// <inheritdoc />
        internal override void AddAnnotation(SolAnnotationDefinition annotation)
        {
            m_DeclaredAnnotations.Add(annotation);
        }

        #endregion

    }
}