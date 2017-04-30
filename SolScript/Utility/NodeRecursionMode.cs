namespace SolScript.Utility
{
    /// <summary>
    ///     Signifies the type of recursion used in some cases.
    /// </summary>
    public enum NodeRecursionMode
    {
        /// <summary>
        ///     No recursion will be used.
        /// </summary>
        None,

        /// <summary>
        ///     All root elements will be searched before progressing to the child elements.
        /// </summary>
        Layer,
        /*/// <summary>
        ///     All root elements will be searched before progressing to the child elements.
        /// </summary>
        LayerBackwards,*/

        /// <summary>
        ///     All child elements will be searched before progression to the next root element.
        /// </summary>
        Direct,

        /*/// <summary>
        ///     All child elements will be searched before progression to the next root element.
        /// </summary>
        DirectBackwards*/
    }
}