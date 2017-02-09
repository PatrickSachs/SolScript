using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeAutoDelegateMarshaller : ISolNativeMarshaller
    {
        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(SolFunction.AutoDelegate);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolFunction.TYPE, true);
        }

        /// <inheritdoc />
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            SolFunction.AutoDelegate auto = (SolFunction.AutoDelegate)value;
            return new SolAutoDelegateWrapperFunction(assembly, auto);
        }
    }
}
