using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Libraries.lang;

namespace SolScript.test
{
    [SolTypeDescriptor(test.NAME, SolTypeMode.Abstract, typeof(test_InheritanceTest1))]
    public class test_InheritanceTest1
    {
        public test_InheritanceTest1()
        {
            
        }

        public void func1(SolExecutionContext context)
        {
            lang_Globals.print(context, SolString.ValueOf("func1"));
        }
    }

    [SolTypeDescriptor(test.NAME, SolTypeMode.Sealed, typeof(test_InheritanceTest2))]
    public class test_InheritanceTest2 : test_InheritanceTest1
    {
        public void func2(SolExecutionContext context)
        {
            lang_Globals.print(context, SolString.ValueOf("func2"));
        }
    }
}