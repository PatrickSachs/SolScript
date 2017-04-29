using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Library {
    /// <summary> This attribute defines a type contraint on a
    ///     method/parameter/variable/etc. Items without this attribute will have their
    ///     SolType inferred from their CLR-Type(e.g System.String becomes string?),
    ///     which is not always the desired behaviour. You can use this attribute to
    ///     manually define the type. </summary>
    public class SolContractAttribute : Attribute {
        /// <summary> Creates a new contract from a given type name and explicit
        ///     nilability. </summary>
        /// <param name="typeName"> The name of the type </param>
        /// <param name="canBeNil"> Can this value be nil? </param>
        public SolContractAttribute([NotNull] string typeName, bool canBeNil) {
            TypeName = typeName;
            CanBeNil = canBeNil;
        }

        /// <summary> Can this value be nil? (Default: true) </summary>
        public bool CanBeNil { get; }

        /// <summary> The type name. (Default: any) </summary>
        public string TypeName { get; }
        
        /// <summary> Generates the SolType from the provided type name and nilability. </summary>
        /// <returns> An explicit SolType </returns>
        public SolType GetSolType() {
            return new SolType(TypeName, CanBeNil);
        }
    }
}