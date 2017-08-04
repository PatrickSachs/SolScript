using NodeParser;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     Base class for all functions that were defined by a <see cref="SolFunctionDefinition" />.
    /// </summary>
    public abstract class DefinedSolFunction : SolFunction
    {
        /// <inheritdoc />
        public override SolAssembly Assembly => Definition.Assembly;

        /// <summary>
        ///     The definition of this function.
        /// </summary>
        public abstract SolFunctionDefinition Definition { get; }

        /// <inheritdoc />
        public override NodeLocation Location => Definition.Location;

        /// <inheritdoc />
        public override string Name {
            get {
                if (DefinedIn != null) {
                    return DefinedIn.InheritanceLevel.Type + "." + Definition.Name;
                }
                return Definition.Name;
            }
        }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo => Definition.ParameterInfo;

        /// <inheritdoc />
        public override SolType ReturnType => Definition.Type;

        #region Overrides

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Definition.Name + "<" + (DefinedIn?.ToString() ?? "global") + ">";
        }

        #endregion
    }
}