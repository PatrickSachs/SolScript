using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public struct SolType
    {
        /// <inheritdoc cref="PrimitiveTypeNameOf" />
        /// <typeparam name="T">The primitive type.</typeparam>
        public static string PrimitiveTypeNameOf<T>() where T : SolValue
        {
            return PrimitiveTypeNameOf(typeof(T));
        }

        /// <summary>
        ///     Gets the primitive type name of the given native SolScript type.
        /// </summary>
        /// <param name="type">The primitive type.</param>
        /// <returns>The primitive type name.</returns>
        /// <remarks>
        ///     This does not support your own custom classes, only primitive classes are supported(General rule of thumb:
        ///     Everything that extends the <see cref="SolValue" /> native class works).
        /// </remarks>
        /// <exception cref="InvalidOperationException">Not a valid primitive type.</exception>
        public static string PrimitiveTypeNameOf(Type type)
        {
            string name;
            if (!TryGetPrimitiveTypeNameOf(type, out name)) {
                throw new InvalidOperationException("The native type \"" + type + "\" is not a legal primitive SolScript type.");
            }
            return name;
        }

        /// <summary>
        ///     Checks if the given type name is a primitive type.
        /// </summary>
        /// <param name="name">The type name.</param>
        /// <param name="includeWildcards">Should wildcards such as <c>any</c> and <c>class</c> be considered as primitive type?</param>
        /// <returns>true if type is primitive, false if not.</returns>
        public static bool IsPrimitiveType(string name, bool includeWildcards = true)
        {
            switch (name) {
                case SolNil.TYPE:
                    return true;
                case SolNumber.TYPE:
                    return true;
                case SolBool.TYPE:
                    return true;
                case SolString.TYPE:
                    return true;
                case SolTable.TYPE:
                    return true;
                case SolFunction.TYPE:
                    return true;
                default:
                    if (includeWildcards) {
                        if (name == SolValue.ANY_TYPE) {
                            return true;
                        }
                        if (name == SolValue.CLASS_TYPE) {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>
        ///     Gets the primitive type name of the given native SolScript type.
        /// </summary>
        /// <param name="type">The primitive type.</param>
        /// <param name="name">The primitive type name. Only valid if the method returned true. </param>
        /// <returns>true if the type represents a primitive type, false if not.</returns>
        /// <remarks>
        ///     This does not support your own custom classes, only primitive classes are supported(General rule of thumb:
        ///     Everything that extends the <see cref="SolValue" /> native class works).
        /// </remarks>
        [ContractAnnotation("name:null => false")]
        public static bool TryGetPrimitiveTypeNameOf(Type type, [CanBeNull] out string name)
        {
            if (type == typeof(SolValue)) {
                name = SolValue.ANY_TYPE;
                return true;
            }
            if (type == typeof(SolClass)) {
                name = SolValue.CLASS_TYPE;
                return true;
            }
            if (type == typeof(SolNil)) {
                name = SolNil.TYPE;
                return true;
            }
            if (type == typeof(SolNumber)) {
                name = SolNumber.TYPE;
                return true;
            }
            if (type == typeof(SolBool)) {
                name = SolBool.TYPE;
                return true;
            }
            if (type == typeof(SolString)) {
                name = SolString.TYPE;
                return true;
            }
            if (type == typeof(SolTable)) {
                name = SolTable.TYPE;
                return true;
            }
            if (type == typeof(SolFunction) || type.IsSubclassOf(typeof(SolFunction))) {
                name = SolFunction.TYPE;
                return true;
            }
            name = null;
            return false;
        }

        public static SolType AnyNil => new SolType("any", true);

        public SolType(string type, bool canBeNil)
        {
            Type = type;
            CanBeNil = canBeNil;
        }

        public string Type { get; [UsedImplicitly] internal set; }
        public bool CanBeNil { get; [UsedImplicitly] internal set; }

        /// <summary>
        ///     Checks if this type is compatible with another type. Note: If you
        ///     want to treat types in the style of "string!" check the overload.
        /// </summary>
        /// <param name="assembly">
        ///     The assembly to check for validity in. The assembly is
        ///     required in order to check for mixins.
        /// </param>
        /// <param name="other"> The type (incl. nilability) to check. </param>
        /// <returns>
        ///     true if a value of the given type can be assigned to a variable of
        ///     this type.
        /// </returns>
        [Pure]
        public bool IsCompatible(SolAssembly assembly, string other)
        {
            // If both types are the same the types are compatible.
            if (Type == other) {
                return true;
            }
            // If the other type is nil and nil CAN be assigned to this type the
            // types ARE compatible.
            // If the other type is nil and nil CANNOT be assigned to this type the
            // types ARE NOT compatible.
            if (other == SolNil.TYPE) {
                return CanBeNil;
            }
            // If we only accept nil the other type should have identify matched 
            // in the first check.
            if (Type == SolNil.TYPE) {
                return false;
            }
            // If any type can be assigned to this value the other value is compatible.
            if (Type == SolValue.ANY_TYPE) {
                return true;
            }
            // The following checks only apply if the other type is a class definition.
            SolClassDefinition classDef;
            if (assembly.TryGetClass(other, out classDef)) {
                // If we accept any class the other type is compatible.
                if (Type == SolValue.CLASS_TYPE) {
                    return true;
                }
                // If we only accept a specific class we check if the class extends the
                // type somewhere in the inheritance hierarchy. We do not check for
                // exact type equality since this was already done in step one.
                if (classDef.DoesExtendInHierarchy(Type)) {
                    return true;
                }
            }
            // If none of these checks worked the types are not compatible.
            return false;
        }

        /// <summary>
        ///     Checks if the type of another SolType is valid for the
        ///     type/nullability of this instance.
        /// </summary>
        /// <param name="assembly">
        ///     The assembly to check for validity in. The assembly is
        ///     required in order to check for mixins.
        /// </param>
        /// <param name="type"> The type (incl. nilablity) to check. </param>
        /// <returns>
        ///     true if a value of the given type can be assigned to a variable of
        ///     this type.
        /// </returns>
        [Pure]
        public bool IsCompatible(SolAssembly assembly, SolType type)
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
                // If the types are not the same the values is not compatible unless it is a mixin of another @class.
                SolClassDefinition classDef;
                if (assembly.TryGetClass(type.Type, out classDef)) {
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

        [Pure]
        public static bool operator ==(SolType t1, SolType t2)
        {
            return t1.Equals(t2);
        }

        [Pure]
        public static bool operator !=(SolType t1, SolType t2)
        {
            return !t1.Equals(t2);
        }

        [Pure]
        public bool Equals(SolType other)
        {
            return string.Equals(Type, other.Type) && CanBeNil == other.CanBeNil;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is SolType && Equals((SolType) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((Type?.GetHashCode() ?? 0) * 397) ^ CanBeNil.GetHashCode();
            }
        }

        public override string ToString()
        {
            return Type + (CanBeNil ? "?" : "!");
        }
    }
}