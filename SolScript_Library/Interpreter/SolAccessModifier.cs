using System;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Access modifiers are used to represent from where a function or field may be accessed.
    /// </summary>
    public enum SolAccessModifier
    {
        /// <summary>
        ///     No access modifier has been specified. The variable may be accessed from everywhere.
        /// </summary>
        None = 0,

        /// <summary>
        ///     A local access modifier has been specified. The variable may only be accessed within its context. (e.g. from within
        ///     the same class inheritance level or for globals from other globals)
        /// </summary>
        Local = 1,

        /// <summary>
        ///     An internal access modifier has been specified. For classes the variable may only be accessed from inside the same
        ///     class.
        /// </summary>
        // todo: what does internal mean for globals?
        Internal = 2
    }
}