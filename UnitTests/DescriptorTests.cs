using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using UnitTests.TestLibarary;

namespace UnitTests
{
    [TestClass]
    public class DescriptorTests
    {
        /// <summary>
        ///     Tests marshalling of externally described classes between SolScript and native code. Also extracts the descriptor
        ///     explictly.
        /// </summary>
        [TestMethod]
        public void Marshalling()
        {
            // See test_DescriptorTests.cs
            SolAssembly assembly = TestHelper.NewAssembly(nameof(Marshalling), @"
function test(val : Descriptor!) : Descriptor?
    val.Set(true)
    if val.GetString() != 'true' then
        return nil
    end
    return val
end", test.GetLibrary());
            SolFunction func = (SolFunction) assembly.GetVariables(SolAccessModifier.Global).Get("test");
            SolExecutionContext context = new SolExecutionContext(assembly, "Test");
            Described described = new Described();
            SolValue value = func.Call(context, SolMarshal.MarshalFromNative(assembly, described));
            Described describedMarshalledBack = SolMarshal.MarshalFromSol<Described>(value);
            Descriptor descriptor = SolMarshal.MarshalFromSol<Descriptor>(value);

            Assert.IsTrue(value.Type == "Descriptor");
            Assert.IsTrue(described.String == SolBool.TRUE_STRING);
            Assert.IsTrue(described.Bool);

            Assert.IsTrue(ReferenceEquals(described, describedMarshalledBack));
            Assert.IsTrue(descriptor.Get() == described.Bool);
            Assert.IsTrue(ReferenceEquals(descriptor.Self, value));
        }

        /// <summary>
        /// Tests if the marshalled classes are properly cached to ensure that one object is only represented by one class.
        /// </summary>
        [TestMethod]
        public void MarshallingCache()
        {
            SolAssembly assembly = TestHelper.NewAssembly(nameof(MarshallingCache), @"
function test(val1, val2)
    return {
        equals(val1, val2), 
        reference_equals(val1, val2),
        val1, val2
    }
end", test.GetLibrary());
            SolFunction func = (SolFunction)assembly.GetVariables(SolAccessModifier.Global).Get("test");
            SolExecutionContext context = new SolExecutionContext(assembly, "Test");
            Described described = new Described();
            
            // Marshals twice, should rely on internal marshaller cache.
            SolValue val1 = SolMarshal.MarshalFromNative(assembly, described);
            SolValue val2 = SolMarshal.MarshalFromNative(assembly, described);
            SolTable value = (SolTable)func.Call(context, val1, val2);

            Assert.IsTrue((SolBool)value[0]);
            Assert.IsTrue((SolBool)value[1]);
            Assert.IsTrue(value[2].IsReferenceEqual(context, val1));
            Assert.IsTrue(value[2].IsReferenceEqual(context, val2));
            Assert.IsTrue(value[2].ConvertTo<Described>() == described);
            Assert.IsTrue(value[3].IsReferenceEqual(context, val1));
            Assert.IsTrue(value[3].IsReferenceEqual(context, val2));
            Assert.IsTrue(value[3].ConvertTo<Described>() == described);
            Assert.IsTrue(value[3].IsReferenceEqual(context, value[2]));
            Assert.IsTrue(value[3].IsEqual(context, value[2]));
            Assert.IsTrue(value[3].ConvertTo<Described>() == value[2].ConvertTo<Described>());
        }

        #region Test SolScript Classes

        private class Described
        {
            public bool Bool;
            public string String;

            public void SetStringToBool()
            {
                String = Bool ? SolBool.TRUE_STRING : SolBool.FALSE_STRING;
            }
        }

        [SolTypeDescriptor(test.NAME, SolTypeMode.Default, typeof(Described))]
        private class Descriptor : INativeClassSelf
        {
            private Described Described => (Described) Self.DescribedNativeObject;

            #region INativeClassSelf Members

            /// <inheritdoc />
            public SolClass Self { get; set; }

            #endregion

            public void Set(bool value)
            {
                Described.Bool = value;
                Described.SetStringToBool();
            }

            public bool Get()
            {
                return Described.Bool;
            }

            public string GetString()
            {
                return Described.String;
            }
        }

        #endregion
    }
}