namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     Base class for all global functions.
    /// </summary>
    public abstract class GlobalSolFunction : DefinedSolFunction
    {
        /// <inheritdoc />
        public override IClassLevelLink DefinedIn => null;
    }
}