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
        /// <summary>
        ///     Gets the reference, or null if the reference does not exist/cannot be
        ///     retrieved.
        /// </summary>
        /// <returns>
        ///     Contains information about the success of the
        ///     retrieval.
        /// </returns>
        /// <param name="obj">
        ///     The reference, or null.
        /// </param>
        public abstract bool TryGet([CanBeNull] out object obj);

        /// <summary>
        ///     Sets the value associated with this reference, or nothing if the
        ///     reference cannot be resolved.
        /// </summary>
        /// <param name="value"> The value to assign. </param>
        /// <returns>
        ///     Contains information about the success of the
        ///     assignment.
        /// </returns>
        public abstract bool TrySet([CanBeNull] object value);

        #region Nested type: ClassDescribedObject

        public class ClassDescribedObject : DynamicReference
        {
            public ClassDescribedObject(SolClass theClass)
            {
                m_TheClass = theClass;
            }

            private readonly SolClass m_TheClass;

            #region Overrides

            /// <inheritdoc />
            public override bool TryGet(out object obj)
            {
                return m_TheClass.DescribedObjectReference.TryGet(out obj);
            }

            /// <inheritdoc />
            public override bool TrySet(object value)
            {
                return m_TheClass.DescribedObjectReference.TrySet(value);
            }

            #endregion
        }

        #endregion

        #region Nested type: ClassDescriptorObject

        public class ClassDescriptorObject : DynamicReference
        {
            public ClassDescriptorObject(SolClass theClass)
            {
                m_TheClass = theClass;
            }

            private readonly SolClass m_TheClass;

            #region Overrides

            /// <inheritdoc />
            public override bool TryGet(out object obj)
            {
                return m_TheClass.DescriptorObjectReference.TryGet(out obj);
            }

            /// <inheritdoc />
            public override bool TrySet(object value)
            {
                return m_TheClass.DescriptorObjectReference.TrySet(value);
            }

            #endregion
        }

        #endregion

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
            public override bool TryGet(out object obj)
            {
                obj = null;
                return false;
            }

            /// <inheritdoc />
            public override bool TrySet(object value)
            {
                return false;
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
            public override bool TryGet(out object obj)
            {
                obj = m_Value;
                return true;
            }

            /// <inheritdoc />
            public override bool TrySet(object value)
            {
                m_Value = value;
                return true;
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
            ///     The singleton instance of the null reference.
            /// </summary>
            public static readonly NullReference Instance = new NullReference();

            #region Overrides

            /// <inheritdoc />
            public override bool TryGet(out object obj)
            {
                obj = null;
                return true;
            }

            /// <inheritdoc />
            public override bool TrySet(object value)
            {
                return false;
            }

            #endregion
        }

        #endregion

        /*#region Nested type: InheritanceNative

        internal class InheritanceNative : DynamicReference
        {
            public InheritanceNative(SolClass.Inheritance inheritance)
            {
                m_Inheritance = inheritance;
            }

            private readonly SolClass.Inheritance m_Inheritance;

            #region Overrides

            /// <inheritdoc />
            public override object TryGet(out GetState refState)
            {
                return m_Inheritance.NativeReference.TryGet(out refState);
            }

            /// <inheritdoc />
            public override void TrySet(object value, out SetState refState)
            {
                m_Inheritance.NativeReference = new FixedReference(value);
                refState = SetState.Assigned;
            }

            #endregion
        }

        #endregion*/
    }
}