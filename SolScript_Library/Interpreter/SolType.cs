using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public struct SolType {
        public static SolType Default => new SolType("any", true);

        public SolType(string type) {
            Type = type;
            CanBeNil = false;
        }

        public SolType(string type, bool canBeNil) {
            Type = type;
            CanBeNil = canBeNil;
        }

        public readonly string Type;
        public readonly bool CanBeNil;

        public bool IsCompatible(string type) {
            if (Type == "any") {
                return type != "nil" || CanBeNil;
            }
            if (Type == "nil") {
                return type == "nil";
            }
            if (type == "nil") {
                return CanBeNil;
            }
            // todo: mixins!
            return Type == type;
        }
        
        /// <summary> Checks if the type of another SolType is valid for the
        ///     type/nullability of this instance. </summary>
        public bool IsCompatible(SolType type)
        {
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
                // todo: mixins!
                // If the types are not the same the values is not compatible.
                return false;
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