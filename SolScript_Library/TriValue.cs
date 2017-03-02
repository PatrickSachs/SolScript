namespace SolScript
{
    /// <summary>
    ///     A TriValue is a three valued value(-1, 0, 1). The closest comparison is a bool which is a two values value(0, 1).
    /// </summary>
    public enum TriValue : sbyte
    {
        /// <summary>
        ///     No specific value has been defined. (Value 0)
        /// </summary>
        Undefined = 0,

        /// <summary>
        ///     True. (Value 1)
        /// </summary>
        True = 1,

        /// <summary>
        ///     False. (Value -1)
        /// </summary>
        False = -1
    }
}