using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     A dynamic reference is used to store a reference to a potentially
    ///     not-yet existing reference, or to a dynamically changing reference by
    ///     declaring a reference getter function.
    /// </summary>
    public abstract class DynamicReference
    {
        #region GetState enum

        public enum GetState
        {
            Retrieved,
            NotRetrieved
        }

        #endregion

        #region SetState enum

        /// <summary>
        ///     Has the reference been set?
        /// </summary>
        public enum SetState
        {
            /// <summary>
            ///     The variable has been set.
            /// </summary>
            Assigned,

            /// <summary>
            ///     The variables has not been set.
            /// </summary>
            NotAssigned
        }

        #endregion

        /// <summary>
        ///     Gets the reference, or null if the reference does not exist/cannot be
        ///     retrieved.
        /// </summary>
        /// <returns> The reference, or null. </returns>
        /// <param name="refState">
        ///     Contains detailed information about the success of the
        ///     retrieval.
        /// </param>
        [CanBeNull]
        public abstract object GetReference(out GetState refState);

        /// <summary>
        ///     Sets the value associated with this reference, or nothing if the
        ///     reference cannot be resolved.
        /// </summary>
        /// <param name="value"> The value to assign. </param>
        /// <param name="refState">
        ///     Contains detailed information about the success of the
        ///     assignment.
        /// </param>
        public abstract void SetReference([CanBeNull] object value, out SetState refState);

        #region Nested type: FailedReference

        /// <summary>
        ///     This reference always fails at whatever it tries to do. I almost feel a bit sorry for it.
        /// </summary>
        public sealed class FailedReference : DynamicReference
        {
            private FailedReference() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly FailedReference Instance = new FailedReference();

            #region Overrides

            /// <inheritdoc />
            public override object GetReference(out GetState refState)
            {
                refState = GetState.NotRetrieved;
                return null;
            }

            /// <inheritdoc />
            public override void SetReference(object value, out SetState refState)
            {
                refState = SetState.NotAssigned;
            }

            #endregion
        }

        #endregion

        #region Nested type: FixedReference

        /// <summary>
        ///     A fixed reference always points to the same object. It is essentially just a wrapper around a "normal" reference.
        /// </summary>
        public class FixedReference : DynamicReference
        {
            /// <summary>
            ///     Crates a new fixed reference for the given value.
            /// </summary>
            /// <param name="value">The value to reference to.</param>
            public FixedReference(object value)
            {
                m_Value = value;
            }

            private object m_Value;

            #region Overrides

            /// <inheritdoc />
            public override object GetReference(out GetState refState)
            {
                refState = GetState.Retrieved;
                return m_Value;
            }

            /// <inheritdoc />
            public override void SetReference(object value, out SetState refState)
            {
                refState = SetState.Assigned;
                m_Value = value;
            }

            #endregion
        }

        #endregion

        #region Nested type: InheritanceNative

        internal class InheritanceNative : DynamicReference
        {
            public InheritanceNative(SolClass.Inheritance inheritance)
            {
                m_Inheritance = inheritance;
            }

            private readonly SolClass.Inheritance m_Inheritance;

            #region Overrides

            /// <inheritdoc />
            public override object GetReference(out GetState refState)
            {
                return m_Inheritance.NativeReference.GetReference(out refState);
            }

            /// <inheritdoc />
            public override void SetReference(object value, out SetState refState)
            {
                m_Inheritance.NativeReference = new FixedReference(value);
                refState = SetState.Assigned;
            }

            #endregion
        }

        #endregion

        #region Nested type: NullReference

        /// <summary>
        ///     Super dynamic reference that always returns a resolved null
        ///     reference. Useful for static invocation targets.
        /// </summary>
        public sealed class NullReference : DynamicReference
        {
            private NullReference() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly NullReference Instance = new NullReference();

            #region Overrides

            /// <summary>
            ///     Gets the reference, or null if the reference does not exist/cannot be
            ///     retrieved.
            /// </summary>
            /// <returns> The reference, or null. </returns>
            [CanBeNull]
            public override object GetReference(out GetState refState)
            {
                refState = GetState.Retrieved;
                return null;
            }

            /// <summary> Sets the value associated with this reference. </summary>
            public override void SetReference([CanBeNull] object value, out SetState refState)
            {
                refState = SetState.NotAssigned;
            }

            #endregion
        }

        #endregion
    }
}