using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolScript.Interpreter;
using SolScript.Interpreter.Types;
using SolScript.Libraries.std;

namespace UnitTests
{
    [TestClass]
    public class TypeTests
    {
        /// <summary>
        ///     Tests which types match the any!/any? wildcard.
        /// </summary>
        [TestMethod]
        public void AnyWildcard()
        {
            SolAssembly assembly = TestHelper.NewEmptyAssembly(nameof(AnyWildcard));
            SolType anyNil = SolType.AnyNil;
            SolType anyNotNill = new SolType(SolValue.ANY_TYPE, false);

            Assert.IsTrue(anyNil.IsCompatible(assembly, SolString.TYPE));
            Assert.IsTrue(anyNil.IsCompatible(assembly, SolValue.CLASS_TYPE));
            Assert.IsTrue(anyNil.IsCompatible(assembly, SolNil.TYPE));

            Assert.IsTrue(anyNotNill.IsCompatible(assembly, SolString.TYPE));
            Assert.IsTrue(anyNotNill.IsCompatible(assembly, SolValue.CLASS_TYPE));
            Assert.IsFalse(anyNotNill.IsCompatible(assembly, SolNil.TYPE));
        }

        /// <summary>
        ///     Tests which types match the class!/class? wildcard.
        /// </summary>
        [TestMethod]
        public void ClassWildcard()
        {
            SolAssembly assembly = TestHelper.NewEmptyAssembly(nameof(ClassWildcard), std.GetLibrary());
            SolType classNil = new SolType(SolValue.CLASS_TYPE, true);
            SolType classNotNil = new SolType(SolValue.CLASS_TYPE, false);

            Assert.IsTrue(classNil.IsCompatible(assembly, std_Stream.TYPE));
            Assert.IsTrue(classNil.IsCompatible(assembly, std_Math.TYPE));
            Assert.IsTrue(classNil.IsCompatible(assembly, SolNil.TYPE));
            Assert.IsFalse(classNil.IsCompatible(assembly, SolString.TYPE));

            Assert.IsTrue(classNotNil.IsCompatible(assembly, std_Stream.TYPE));
            Assert.IsTrue(classNotNil.IsCompatible(assembly, std_Math.TYPE));
            Assert.IsFalse(classNotNil.IsCompatible(assembly, SolNil.TYPE));
            Assert.IsFalse(classNotNil.IsCompatible(assembly, SolString.TYPE));
        }

        /// <summary>
        ///     Tests which types match an explicit wildcard.
        /// </summary>
        [TestMethod]
        public void ExplicitType()
        {
            SolAssembly assembly = TestHelper.NewEmptyAssembly(nameof(ClassWildcard));
            SolType typeNil = new SolType(SolString.TYPE, true);
            SolType typeNotNil = new SolType(SolString.TYPE, false);

            Assert.IsTrue(typeNil.IsCompatible(assembly, SolString.TYPE));
            Assert.IsFalse(typeNil.IsCompatible(assembly, SolBool.TYPE));
            Assert.IsTrue(typeNil.IsCompatible(assembly, SolNil.TYPE));

            Assert.IsTrue(typeNotNil.IsCompatible(assembly, SolString.TYPE));
            Assert.IsFalse(typeNotNil.IsCompatible(assembly, SolBool.TYPE));
            Assert.IsFalse(typeNotNil.IsCompatible(assembly, SolNil.TYPE));
        }

        /// <summary>
        ///     Tests which types match an explicit wildcard by using inheriting classes.
        /// </summary>
        [TestMethod]
        public void ExplcitTypeInheritance()
        {
            SolAssembly assembly = TestHelper.NewEmptyAssembly(nameof(ClassWildcard), std.GetLibrary());
            SolType typeNil = new SolType(std_Stream.TYPE, true);
            SolType typeNotNil = new SolType(std_Stream.TYPE, false);

            Assert.IsTrue(typeNil.IsCompatible(assembly, std_Stream.TYPE));
            Assert.IsTrue(typeNil.IsCompatible(assembly, std_BinaryStream.TYPE));
            Assert.IsFalse(typeNil.IsCompatible(assembly, SolString.TYPE));
            Assert.IsTrue(typeNil.IsCompatible(assembly, SolNil.TYPE));

            Assert.IsTrue(typeNotNil.IsCompatible(assembly, std_Stream.TYPE));
            Assert.IsTrue(typeNotNil.IsCompatible(assembly, std_BinaryStream.TYPE));
            Assert.IsFalse(typeNil.IsCompatible(assembly, SolString.TYPE));
            Assert.IsFalse(typeNotNil.IsCompatible(assembly, SolNil.TYPE));
        }
    }
}