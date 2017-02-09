using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Library.Classes {
    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Default)]
    [SolLibraryName("TestClass")]
    public class TestClass {
        public int counter;

        public int test_func() {
            counter++;
            Console.WriteLine("Hello from C#! Counter: " + counter);
            return counter;
        }
    }

    [SolLibraryClass("test", SolTypeMode.Singleton)]
    [SolLibraryName("MarshalTest")]
    public class MarshalTest
    {
        public Dictionary<string, double> Dictionary(double[] array)
        {
            Dictionary<string, double> dic = new Dictionary<string, double>();
            foreach (double ae in array)
            {
                dic.Add("key_" + ae, ae * 7.47);
            }
            return dic;
        }
        public void Void(string test) {SolDebug.WriteLine(test); }
        public StringBuilder StringBuilder(char str)
        {
            return new StringBuilder(new string(str, 666));
        }
        // todo: delegate return values - inc endless delegate wrapping! (or wrapped delegate function?!)
        public bool? Delegate(SolFunction.AutoDelegate<int> delg)
        {
            int value = delg.Invoke("Boo!");
            if (value > 0) return true;
            if (value < 0) return false;
            return null;
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

        public int[] int_array()
        {
            return new[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
        }

        public string a_string()
        {
            return Guid.NewGuid().ToString();
        }

        private static void MyMethod()
        {
            Console.WriteLine("-- I am a native lamda function!");
        }

        private readonly MethodInfo m_Method = typeof(TestClassSingleton).GetMethod("MyMethod", BindingFlags.Static|BindingFlags.NonPublic);

        public SolFunction Lamda(SolExecutionContext context)
        {
            return new SolNativeLamdaFunction(context.Assembly, m_Method, DynamicReference.NullReference.Instance);
        }

        public MethodInfo MethodRaw()
        {
            return m_Method;
        }
    }
}