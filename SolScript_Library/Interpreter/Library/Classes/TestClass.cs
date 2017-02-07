using System;
using System.Reflection;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Library.Classes {
    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Default)]
    [SolLibraryName("TestClass")]
    public class TestClass {
        public int counter;

        public void test_func() {
            counter++;
            Console.WriteLine("Hello from C#! Counter: " + counter);
        }
    }

    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Singleton)]
    [SolLibraryName("TestClassSingleton")]
    public class TestClassSingleton {
        public TestClass get_test_class() {
            return new TestClass();
        }

        public TestClass marshal_it(TestClass testClass) {
            SolDebug.WriteLine("marshalling it!");
            return testClass;
        }

        private void MyMethod()
        {
            Console.WriteLine("-- I am a native lamda function!");
        }

        private readonly MethodInfo m_Method = typeof(TestClassSingleton).GetMethod("MyMethod", BindingFlags.Instance|BindingFlags.NonPublic);

        public SolFunction Lamda(SolExecutionContext context)
        {
            return new SolNativeLamdaFunction(context.Assembly, m_Method, new DynamicReference.FixedReference(this));
        }
    }
}