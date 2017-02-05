using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    /// <summary> A dynamic reference is used to store a reference to a potentially
    ///     not-yet existing reference, or to a dynamically changing reference by
    ///     declaring a reference getter function. </summary>
    public abstract class DynamicReference {
        #region GetState enum

        public enum GetState {
            Retrieved,
            NotRetrieved
        }

        #endregion

        #region SetState enum

        public enum SetState {
            Assigned,
            NotAssigned
        }

        #endregion

        /// <summary> Gets the reference, or null if the reference does not exist/cannot be
        ///     retrieved. </summary>
        /// <returns> The reference, or null. </returns>
        /// <param name="refState"> Contains detailed information about the success of the
        ///     retrieval. </param>
        [CanBeNull]
        public abstract object GetReference(out GetState refState);

        /// <summary> Sets the value associated with this reference, or nothing if the
        ///     reference cannot be resolved. </summary>
        /// <param name="value"> The value to assign. </param>
        /// <param name="refState"> Contains detailed information about the success of the
        ///     assignment. </param>
        public abstract void SetReference([CanBeNull] object value, out SetState refState);

        internal class InheritanceNative : DynamicReference
        {
            private readonly SolClass.Inheritance m_Inheritance;

            public InheritanceNative(SolClass.Inheritance inheritance)
            {
                m_Inheritance = inheritance;
            }

            /// <inheritdoc />
            public override object GetReference(out GetState refState)
            {
                object obj = m_Inheritance.NativeObject;
                refState = obj != null ? GetState.Retrieved : GetState.NotRetrieved;
                return obj;
            }

            /// <inheritdoc />
            public override void SetReference(object value, out SetState refState)
            {
                if (m_Inheritance.NativeObject != null) {
                    refState = SetState.NotAssigned;
                } else {
                    m_Inheritance.NativeObject = value;
                    refState = SetState.Assigned;                 
                }
            }
        }

        /*#region Nested type: CustomTypeMixinClr

       public sealed class CustomTypeMixinClr : DynamicReference {
            public CustomTypeMixinClr(SolClass type, MixinId mixinId) {
                Type = type;
                MixinId = mixinId;
            }

            public readonly MixinId MixinId;
            public readonly SolClass Type;

            #region Overrides

            [CanBeNull]
            public override object GetReference(out GetState refState) {

            }

            /// <summary> Sets the value associated with this reference. </summary>
            public override void SetReference([CanBeNull] object value, out SetState refState)
            {

            }

            public override string ToString() {
                return Type.Type + "<mixin#" + MixinId + ">";
            }

            #endregion
        }

        #endregion*/

        #region Nested type: NullReference

        /// <summary> Super dynamic reference that always returns a resolved null
        ///     reference. Useful for static invocation targets. </summary>
        public sealed class NullReference : DynamicReference {
            private NullReference() {
            }

            public static readonly NullReference Instance = new NullReference();

            #region Overrides

            /// <summary> Gets the reference, or null if the reference does not exist/cannot be
            ///     retrieved. </summary>
            /// <returns> The reference, or null. </returns>
            [CanBeNull]
            public override object GetReference(out GetState refState) {
                refState = GetState.Retrieved;
                return null;
            }

            /// <summary> Sets the value associated with this reference. </summary>
            public override void SetReference([CanBeNull] object value, out SetState refState) {
                refState = SetState.NotAssigned;
            }

            #endregion
        }

        #endregion
    }
}