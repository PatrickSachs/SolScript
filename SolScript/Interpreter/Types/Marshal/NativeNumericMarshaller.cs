using System;
using SolScript.Exceptions;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeNumericMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_VERY_HIGH;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            if (type == typeof(double)) {
                return true;
            }
            if (type == typeof(int)) {
                return true;
            }
            if (type == typeof(float)) {
                return true;
            }
            if (type == typeof(long)) {
                return true;
            }
            if (type == typeof(byte)) {
                return true;
            }
            if (type == typeof(short)) {
                return true;
            }
            if (type == typeof(ushort)) {
                return true;
            }
            if (type == typeof(uint)) {
                return true;
            }
            if (type == typeof(ulong)) {
                return true;
            }
            if (type == typeof(decimal)) {
                return true;
            }
            // "char" is a numeric type, but SolScript will treat it as string.
            return false;
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">
        ///     Could not cast value to a double or the represented value is too big or small
        ///     for a double.
        /// </exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            try {
                return new SolNumber(Convert.ToDouble(value));
            } catch (InvalidCastException ex) {
                throw new SolMarshallingException("Failed to cast type \"" + type.Name + "\" to a double required for the creation of a number.", ex);
            } catch (OverflowException ex) {
                throw new SolMarshallingException("The number is outside the bounds of a double.", ex);
            }
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolNumber.TYPE, false);
        }

        #endregion
    }
}