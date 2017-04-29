using System;
using System.Linq;

namespace SolScript.Interpreter.Library
{
    /// <summary>
    ///     A <see cref="NativeFieldPostProcessor" /> is used to modify certain aspects of a native field(or potentially a
    ///     property) during creation.
    /// </summary>
    public abstract class NativeFieldPostProcessor
    {
        public abstract bool AppliesTo(FieldOrPropertyInfo field);

        /*/// <summary>
            ///     Returns a default implementation of the <see cref="NativeFieldPostProcessor" />, returning all values according to
            ///     the specified default values.
            /// </summary>
            /// <returns>The post processor instance.</returns>
            public static NativeFieldPostProcessor GetDefault()
            {
                return Default.Instance;
            }*/

        // todo: provide a way for the implementation to specify which aspects should even be touched by the post processor. 
        // This may not be needed now, but could be important as the post processor scales.
        /// <summary>
        ///     By default an explicit <see cref="SolLibraryNameAttribute" /> overrides the result of the <see cref="GetName" />
        ///     method, and so on. If this value is true however, the explict arguments are ignored and the results of the method
        ///     calls of this post processor take precedence.
        /// </summary>
        /// <param name="field">The field referene.</param>
        /// <returns>If the attributes on this field should be overridden.</returns>
        public virtual bool OverridesExplicitAttributes(FieldOrPropertyInfo field) => false;

        /// <summary>
        ///     If this returns true no field for this method can be created. By default all fields can be created.
        /// </summary>
        public virtual bool DoesFailCreation(FieldOrPropertyInfo field) => false;

        /// <summary>
        ///     Gets the remapped function name. The default is <see cref="FieldOrPropertyInfo.Name" />.
        /// </summary>
        /// <param name="field">The field referene.</param>
        /// <returns>The new field name to use in SolScript.</returns>
        public virtual string GetName(FieldOrPropertyInfo field) => field.Name;

        /// <summary>
        ///     Gets the remapped field type. The default is either marshalled from the actual native field type or inferred from
        ///     one of its attributes(but they will be determined at a later stage; once the definitions are being generated).
        /// </summary>
        /// <param name="field">The field referene.</param>
        /// <returns>The remapped field type, or null if you do not wish to remap.</returns>
        /// <remarks>
        ///     Very important: If you do not wish to remap the field type you must return null and NOT the default SolType
        ///     value.
        /// </remarks>
        public virtual SolType? GetFieldType(FieldOrPropertyInfo field) => null;

        /// <summary>
        ///     Gets the remapped field <see cref="SolAccessModifier" />. Default is <see cref="SolAccessModifier.Global" />.
        /// </summary>
        /// <param name="field">The field referene.</param>
        /// <returns>The new field <see cref="SolAccessModifier" /> to use in SolScript.</returns>
        public virtual SolAccessModifier GetAccessModifier(FieldOrPropertyInfo field) => SolAccessModifier.Global;

        #region Nested type: Default

        public sealed class Default : NativeFieldPostProcessor
        {
            private readonly Predicate<FieldOrPropertyInfo> m_Matcher;
            public Default(Predicate<FieldOrPropertyInfo> matcher)
            {
                m_Matcher = matcher;
            }

            /// <inheritdoc />
            public override bool AppliesTo(FieldOrPropertyInfo field)
            {
                return m_Matcher(field);
            }
        }

        #endregion

        #region Nested type: FailOnInterface

        /// <summary>
        ///     This post processor fails creation of fields that were declared in a type implementing the given interface.
        /// </summary>
        public class FailOnInterface : NativeFieldPostProcessor
        {
            private readonly Predicate<FieldOrPropertyInfo> m_Matcher;
               
            /// <summary>
            ///     Creates a new <see cref="NativeFieldPostProcessor.FailOnInterface" /> instance.
            /// </summary>
            /// <param name="interfce">The interface type to fail on.</param>
            /// <exception cref="ArgumentException"><paramref name="interfce" /> is not an interface.</exception>
            public FailOnInterface(Predicate<FieldOrPropertyInfo> matcher, Type interfce)
            {
                if (!interfce.IsInterface) {
                    throw new ArgumentException("The given type \"" + interfce + "\" is not an interface.", nameof(interfce));
                }
                m_Type = interfce;
                m_Matcher = matcher;
            }

            private readonly Type m_Type;

            #region Overrides

            /// <inheritdoc />
            public override bool DoesFailCreation(FieldOrPropertyInfo field)
            {
                if (field.DeclaringType == null) {
                    return false;
                }

                return field.DeclaringType.GetInterfaces().Contains(m_Type);
            }

            /// <inheritdoc />
            public override bool AppliesTo(FieldOrPropertyInfo field)
            {
                return m_Matcher(field);
            }

            #endregion
        }

        #endregion

        #region Nested type: FailOnType

        /// <summary>
        ///     This post processors fails creation of fields in the given type.
        /// </summary>
        public class FailOnType : NativeFieldPostProcessor
        {
            /// <summary>
            ///     Creates a new <see cref="NativeFieldPostProcessor.FailOnType" /> instance.
            /// </summary>
            /// <param name="type">The type to fail on.</param>
            public FailOnType(Predicate<FieldOrPropertyInfo> matcher, Type type)
            {
                m_Matcher = matcher;
                m_Type = type;
            }

            private readonly Predicate<FieldOrPropertyInfo> m_Matcher;
            private readonly Type m_Type;

            #region Overrides

            /// <inheritdoc />
            public override bool DoesFailCreation(FieldOrPropertyInfo field)
            {
                if (field.DeclaringType == null) {
                    return false;
                }
                return field.DeclaringType == m_Type || field.DeclaringType.IsSubclassOf(m_Type);
            }

            /// <inheritdoc />
            public override bool AppliesTo(FieldOrPropertyInfo field)
            {
                return m_Matcher(field);
            }

            #endregion
        }

        #endregion
    }
}