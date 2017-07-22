namespace SolScript.Parser
{
    /// <summary>
    ///     The type of a token in SolScript.
    /// </summary>
    public enum SolTokenType
    {
        /// <summary>
        ///     Defines a class. "class"
        /// </summary>
        ClassDefinition,

        /// <summary>
        ///     Modifies a cass. "abstract, sealed, singleton, annotations, ..."
        /// </summary>
        ClassModifier,

        /// <summary>
        ///     The token for the name of a class.
        /// </summary>
        ClassName
    }
}