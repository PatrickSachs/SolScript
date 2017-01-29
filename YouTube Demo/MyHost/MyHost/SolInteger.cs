using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace MyHost
{
    public class SolInteger : SolValue
    {
        public readonly int Value;

        public SolInteger(int value)
        {
            Value = value;
        }

        public override object ConvertTo(Type type)
        {
            if (type == typeof(int)) return Value;
            if (type == typeof(SolNumber)) return new SolNumber(Value);
            throw new SolMarshallingException("i blew up");
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return Value.ToString();
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            SolInteger otherInteger = other as SolInteger;
            if (otherInteger == null) return false;
            return Value == otherInteger.Value;
        }

        public override string Type => "int";
    }
}
