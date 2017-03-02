namespace SolScript.Interpreter
{
    /// <summary>
    ///     A member modifier is used to specify the type of a function.
    /// </summary>
    public enum SolMemberModifier
    {
        /// <summary>
        ///     The function is a "normal" function.
        /// </summary>
        None,

        /// <summary>
        ///     The function as an abstract function, requiring implementation in a subclass.
        /// </summary>
        Abstract,

        /// <summary>
        ///     The function overrides a function at a lower level in the class hierarchy(and thus possibly implementing an
        ///     abstract function).
        /// </summary>
        Override
    }
}