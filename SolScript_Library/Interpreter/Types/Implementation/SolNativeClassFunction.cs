using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

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

        /// <summary>
        ///     Tries to convert the local value into a value of a C# type. May
        ///     return null.
        /// </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            if (type.IsAssignableFrom(typeof(SolNativeClassFunction))) {
                return this;
            }
            throw new SolMarshallingException("function", type);
        }

        #endregion
    }
}