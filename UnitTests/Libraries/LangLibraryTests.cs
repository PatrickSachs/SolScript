using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolScript.Interpreter;
using SolScript.Interpreter.Types;

namespace UnitTests.Libraries
{
    [TestClass]
    public class LangLibraryTests
    {
        /// <summary>
        ///     Tests the to_string function.
        /// </summary>
        [TestMethod]
        public new void ToString()
        {
            SolExecutionContext context;
            SolTable value = (SolTable)TestHelper.NewAssemblyAndRun(nameof(ToString), 
                @"
global function test() 
    return { to_string(10), to_string(true), to_string('string'), to_string(4.2) }
end", out context);

            Assert.IsTrue(value[0].IsEqual(context, SolString.ValueOf("10")));
            Assert.IsTrue(value[1].IsEqual(context, SolString.ValueOf("true")));
            Assert.IsTrue(value[2].IsEqual(context, SolString.ValueOf("string")));
            Assert.IsTrue(value[3].IsEqual(context, SolString.ValueOf("4.2")));
        }

        /// <summary>
        ///     Tests the equals function.
        /// </summary>
        [TestMethod]
        public void Equals()
        {
            SolExecutionContext context;
            SolTable value = (SolTable)TestHelper.NewAssemblyAndRun(nameof(ToString),
                @"
class Class1
    global my_field : string!
    internal function __new(my_field : string!) self.my_field = my_field end
    internal function __is_equal(other : any?) 
        if (type(other) != 'Class1') then 
            return false   
        end
        return other.my_field == my_field
    end
end

global function test() : table!
    var cls1 = new Class1('my_string_value öäüß\nâáà€\r$')
    var cls2 = new Class1('my_string_value öäüß\nâáà€\r$')
    return { 
        equals(cls1, cls2), equals(true, false), equals(123, 123.00), equals({}, {}),
        reference_equals(cls1, cls2), reference_equals(true, false), reference_equals(123, 123.00), reference_equals({}, {})
    }
end", out context);

            Assert.IsTrue(value[0].IsTrue(context));
            Assert.IsTrue(value[1].IsFalse(context));
            Assert.IsTrue(value[2].IsTrue(context));
            Assert.IsTrue(value[3].IsFalse(context));

            Assert.IsTrue(value[4].IsFalse(context));
            Assert.IsTrue(value[5].IsFalse(context));
            Assert.IsTrue(value[6].IsTrue(context));
            Assert.IsTrue(value[7].IsFalse(context));
        }
    }
}