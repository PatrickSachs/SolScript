using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{

    #region SolVariableMode enum

    /// <summary>
    ///     The <see cref="SolVariableMode" /> enum is used to retrieve <see cref="IVariables" /> for a certain variable mode using
    ///     <see cref="SolClass.GetVariables(SolAccessModifier, SolVariableMode, string)" />.
    /// </summary>
    /// <remarks>Be cautious when using the ordinal values of this enum as if does not follow the typical 0, 1, 2 pattern!</remarks>
    public enum SolVariableMode
    {
        /// <summary>
        ///     The variables of all inheritance elements of this class will be regarded. In some cases(such as for
        ///     <see cref="SolAccessModifier.Local" /> access) the inheritance element on which the variables are received from
        ///     might take precedence.
        /// </summary>
        All = 0,

        /// <summary>
        ///     Only the variables of the base elements of this class will be regarded. If no base class exists this will simply
        ///     contain the global assembly variables.
        /// </summary>
        Base = All + 3,

        /// <summary>
        ///     Only the variables directly declared in this inheritance element and given access modifier will be obtained.
        /// </summary>
        Declarations = Base + 3
    }

    #endregion
}