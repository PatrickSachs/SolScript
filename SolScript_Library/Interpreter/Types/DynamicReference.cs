using JetBrains.Annotations;

namespace SolScript.Interpreter.Types {
    /// <summary> A dynamic reference is used to store a reference to a potentially
    ///     not-yet existing reference, or to a dynamically changing reference by
    ///     declaring a reference getter function. </summary>
    public abstract class DynamicReference {
        #region ReferenceState enum

        public enum ReferenceState {
            Retrieved,
            NotRetrieved
        }

        #endregion

        /// <summary> Gets the reference, or null if the reference does not exist/cannot be
        ///     retrieved. </summary>
        /// <returns> The reference, or null. </returns>
        [CanBeNull]
        public abstract object GetReference(out ReferenceState refState);

        #region Nested type: CustomTypeMixinClr

        internal sealed class CustomTypeMixinClr : DynamicReference {
            public CustomTypeMixinClr(SolCustomType type, int mixinId) {
                Type = type;
                MixinId = mixinId;
            }

            public readonly int MixinId;
            public readonly SolCustomType Type;

            [CanBeNull]
            public override object GetReference(out ReferenceState refState) {
                if (Type.ClrObjects.Length > MixinId) {
                    refState = ReferenceState.Retrieved;
                    return Type.ClrObjects[MixinId];
                }
                refState = ReferenceState.NotRetrieved;
                return null;
            }

            public override string ToString() {
                return Type.Type + "<mixin#" + Type.Context.Assembly.TypeRegistry.Types[Type.Type].Mixins[MixinId] + ">";
            }
        }

        #endregion

        #region Nested type: NullReference

        /// <summary> Super dynamic reference that always returns a resolved null
        ///     reference. Useful for static invocation targets. </summary>
        public sealed class NullReference : DynamicReference {
            private NullReference() {
            }

            public static readonly NullReference Instance = new NullReference();

            /// <summary> Gets the reference, or null if the reference does not exist/cannot be
            ///     retrieved. </summary>
            /// <returns> The reference, or null. </returns>
            [CanBeNull]
            public override object GetReference(out ReferenceState refState) {
                refState = ReferenceState.Retrieved;
                return null;
            }
        }

        #endregion
    }
}