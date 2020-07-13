using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Library {
    /// <summary> This attribute defines a type contraint on a
    ///     method/parameter/variable/etc. Items without this attribute will have their
    ///     SolType inferred from their CLR-Type(e.g System.String becomes string?),
    ///     which is not always the desired behaviour. You can use this attribute to
    ///     manually define the type. </summary>
    public class SolContract : Attribute {
        /// <summary> Creates a new contract from a given type name and explicit
        ///     nilability. </summary>
        /// <param name="typeName"> The name of the type </param>
        /// <param name="canBeNil"> Can this value be nil? </param>
        public SolContract([NotNull] string typeName, bool canBeNil) {
            TypeName = typeName;
            CanBeNil = canBeNil;
        }

        /// <summary> Creates a new contract from a given type name. Values can be nil. </summary>
        /// <param name="typeName"> The name of the type </param>
        public SolContract([NotNull] string typeName) {
            TypeName = typeName;
            CanBeNil = true;
        }

        /// <summary> Creates a new contract valid for all types and nil values. </summary>
        public SolContract() {
            TypeName = "any";
            CanBeNil = true;
        }

        /// <summary> Can this value be nil? </summary>
        public readonly bool CanBeNil;

        /// <summary> The type name. </summary>
        public readonly string TypeName;

        /// <summary> Generates the SolType from the provided type name and nilability. </summary>
        /// <returns> An explicit SolType </returns>
        public SolType GetSolType() {
            return new SolType(TypeName, CanBeNil);
        }
    }
}