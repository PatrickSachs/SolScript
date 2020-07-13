using System.Linq;
using JetBrains.Annotations;

namespace SolScript.Interpreter {
    public struct SolType {
        public static SolType AnyNil => new SolType("any", true);

        public SolType(string type, bool canBeNil = false) {
            Type = type;
            CanBeNil = canBeNil;
        }

        public readonly string Type;
        public readonly bool CanBeNil;

        /// <summary> Checks if this type is compatible with another type. Note: If you
        ///     want to treat types in the style of "string!" check the overload. </summary>
        /// <param name="assembly"> The assembly to check for validity in. The assembly is
        ///     required in order to check for mixins. </param>
        /// <param name="type"> The type (incl. nilablity) to check. </param>
        /// <returns> true if a value of the given type can be assigned to a variable of
        ///     this type. </returns>
        [Pure]
        public bool IsCompatible(SolAssembly assembly, string type) {
            if (Type == "any") {
                return type != "nil" || CanBeNil;
            }
            if (Type == "nil") {
                return type == "nil";
            }
            if (type == "nil") {
                return CanBeNil;
            }
            SolClassDefinition classDef;
            if (assembly.TypeRegistry.TryGetClass(type, out classDef)) {
                if (classDef.DoesExtendInHierarchy(type)) {
                    return true;
                }
            }
            return Type == type;
        }

        /// <summary> Checks if the type of another SolType is valid for the
        ///     type/nullability of this instance. </summary>
        /// <param name="assembly"> The assembly to check for validity in. The assembly is
        ///     required in order to check for mixins. </param>
        /// <param name="type"> The type (incl. nilablity) to check. </param>
        /// <returns> true if a value of the given type can be assigned to a variable of
        ///     this type. </returns>
        public bool IsCompatible(SolAssembly assembly, SolType type) {
            if (Type == "any") {
                if (CanBeNil) {
                    // If the local type can be nil and of any type all values are legal.
                    return true;
                }
                // If the local type if of any type but not nil, all non nil values are legal.
                return !type.CanBeNil;
            }
            if (Type == "nil") {
                // I don't really like the nil handling, but, if the local type is 
                // nil, the other type must be nil aswell. Simply being able to potentially
                // be nil is not enough.
                return type.Type == "nil";
            }
            if (Type != type.Type) {
                // If the types are not the same the values is not compatible unless it is a mixin of another @class.
                SolClassDefinition classDef;
                if (assembly.TypeRegistry.TryGetClass(type.Type, out classDef)) {
                    if (classDef.DoesExtendInHierarchy(type.Type)) {
                        return false;
                    }
                } else {
                    return false;
                }
            }
            if (CanBeNil) {
                // If the types are the same and the value can be nil all remaining values are
                // legal.
                return true;
            }
            // If the types are the same but the value cannot be nil only non nil remaining
            // values are legal.
            return !type.CanBeNil;
        }

        public bool Equals(SolType other) {
            return string.Equals(Type, other.Type) && CanBeNil == other.CanBeNil;
        }

        public override bool Equals([CanBeNull] object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SolType && Equals((SolType) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Type?.GetHashCode() ?? 0)*397) ^ CanBeNil.GetHashCode();
            }
        }

        public override string ToString() {
            return Type + (CanBeNil ? "?" : "!");
        }
    }
}