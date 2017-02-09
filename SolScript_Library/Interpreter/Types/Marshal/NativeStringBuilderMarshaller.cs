using System;
using System.Text;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeStringBuilderMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(StringBuilder);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolString.TYPE, true);
        }

        /// <inheritdoc />
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            StringBuilder builder = (StringBuilder) value;
            return new SolString(builder.ToString());
        }

        #endregion
    }
}