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