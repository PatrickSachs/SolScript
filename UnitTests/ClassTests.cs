using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolScript.Interpreter;
using SolScript.Interpreter.Types;
using SolScript.Libraries.std;

namespace UnitTests
{
    [TestClass]
    public class ClassTests
    {
        /// <summary>
        ///     Tests the most basic class construct avilable.
        /// </summary>
        [TestMethod]
        public void BasicClass()
        {
            SolValue value = TestHelper.NewAssemblyAndRun(nameof(BasicClass), @"
class Test
end

global function test() : Test!
    return new Test()
end");
            Assert.IsTrue(value.Type == "Test");
        }

        /// <summary>
        /// Tests basic inheriting classes.
        /// </summary>
        [TestMethod]
        public void BasicClassInheritance()
        {
            SolClass value = (SolClass)TestHelper.NewAssemblyAndRun(nameof(BasicClassInheritance), @"
abstract class Base
end
class Impl extends Base
end

global function test() : Impl!
    return new Impl()
end", std.GetLibrary());

            Assert.IsTrue(value.Type == "Impl");
            Assert.IsTrue(value.Definition.Type == "Impl");
            Assert.IsTrue(value.Definition.Extends("Base"));
            Assert.IsTrue(value.Definition.BaseClass.Type == "Base");
            Assert.IsFalse(value.Definition.Extends(std_Stream.TYPE));
            Assert.IsFalse(value.Definition.Extends(std_BinaryStream.TYPE));
        }

        [TestMethod]
        public void Self()
        {
            SolClass value = (SolClass)TestHelper.NewAssemblyAndRun(nameof(BasicClassInheritance), @"
abstract class Base
    global field : string?
    function set_field(field : string?) self.field = field print('Set \'field\' to: ' .. field .. '.') end
end
class Impl extends Base
    global success : bool!
    internal function __new(field : string?) 
        set_field(field) 
        success = self.field == field
    end
end

global function test() : Impl!
    return new Impl('String Value')
end", std.GetLibrary());
            SolBool success = (SolBool)value.GetVariables(SolAccessModifier.Global, SolVariableMode.All).Get("success");
            SolString field = (SolString)value.GetVariables(SolAccessModifier.Global, SolVariableMode.All).Get("field");
            
            Assert.IsTrue(success);
            Assert.IsTrue(field == "String Value");
        }

        /*[TestMethod]
        public void InheritanceAccessAndOverride()
        {
            SolTable value = (SolTable)TestHelper.NewAssemblyAndRun(nameof(BasicClassInheritance), @"
abstract class Base
    internal function __new() 
        add_data('Base#__new')
    end

    global function base_func() 
        add_data('Base#base_func')
    end

    internal function override_func()
        add_data('Base#override_func')
    end
end
class Impl extends Base
    internal function __new() 
        // Calling an internal function
        base.__new()
        add_data('Impl#__new')
        base_func()
        impl_func()
        override_func()
    end

    global function impl_func()
        add_data('Impl#impl_func')
    end

    internal override function override_func()
        add_data('Impl#override_func')
    end
end

global function add_data(value) 
    Table.append(data, value)
end
local data = {}

global function test() : table!
    new Impl()
    return data
end", std.GetLibrary());
        }*/
    }
}