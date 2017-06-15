using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This is the base class for all native class functions.
    /// </summary>
    public abstract class SolNativeClassFunction : SolClassFunction
    {
        // No thrid party primitives allowed.
        internal SolNativeClassFunction([NotNull] SolClass instance, SolFunctionDefinition definition)
        {
            Definition = definition;
            ClassInstance = instance;
        }

        /// <inheritdoc cref="SolFunction.ParameterInfo" />
        public new SolParameterInfo.Native ParameterInfo => (SolParameterInfo.Native) base.ParameterInfo;

        /// <inheritdoc />
        public override SolFunctionDefinition Definition { get; }

        /// <inheritdoc />
        public override SolClass ClassInstance { get; }

        #region Overrides

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other == this;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 10 + (int) Id;
            }
        }

        #endregion
    }
}