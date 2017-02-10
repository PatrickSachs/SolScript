using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Annotations
{
    [SolLibraryClass("test", SolTypeMode.Annotation)]
    [SolLibraryName("test_f")]
    public class TestF
    {
        public TestF(SolValue value)
        {
            m_Value = value;
        }

        private readonly SolValue m_Value;

        [SolContract("table", false)]
        public SolTable __a_set_variable(SolExecutionContext context, SolValue value, SolValue raw)
        {
            return new SolTable {
                [new SolString("override")] = m_Value
            };
        }
    }
}