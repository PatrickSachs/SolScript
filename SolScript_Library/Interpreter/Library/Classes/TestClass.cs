using System;

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
    }
}