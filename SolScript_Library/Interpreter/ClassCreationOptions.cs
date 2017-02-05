﻿using System;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This class is used to provide some customization features when creation the class. You can either use the
    ///     <see cref="Default" /> options which will create the class as you would expect, or further hook into the
    ///     functionality by either creating a new instance of the <see cref="Customizable" /> subclass providing a
    ///     builder-style class.
    /// </summary>
    public abstract class ClassCreationOptions
    {
        // todo: hooks? callbacks? (ClassCreationOptions)

        /// <summary>
        ///     Should native fields be declared? Native fields do not need to be assigned since the native part of the class takes
        ///     care of that part. The declaration only informs SolScript about the existance of those fields.
        /// </summary>
        public abstract bool DeclareNativeFields { get; }

        /// <summary>
        ///     Should fields defined in the script be declared?
        /// </summary>
        public abstract bool DeclareScriptFields { get; }

        /// <summary>
        ///     Should fields defined in the script be assigned? Note that leaving this true this will lead to crashes or bugs if
        ///     <see cref="DeclareScriptFields" /> was set to false.
        /// </summary>
        public abstract bool AssignScriptFields { get; }

        /// <summary>
        ///     Sould be annotations on the class be created?
        /// </summary>
        public abstract bool CreateAnnotations { get; }

        /// <summary>
        ///     Should be annotations on the fields be created? Note that only annotations for the fields that will be declared
        ///     will be created(Everythign else would make very little sense). The annotations are created before the field gets
        ///     assigned, but after it gets declared.
        /// </summary>
        public abstract bool CreateFieldAnnotations { get; }

        /// <summary>
        ///     Should the constructor of this class be called?
        /// </summary>
        /// <remarks>
        ///     Not calling the constrcutor is extremely dangerous and can lead to hard-to-track bugs(e.g.: If the class is a
        ///     native class the constrcutor does cretae the native backing instance. If the constrcutor is not called, the native
        ///     object will thus be null and need to be set manually.).
        /// </remarks>
        public abstract bool CallConstructor { get; }

        /// <summary>
        ///     Should the creation of this class be enforced? By default some classes such as annotations or abstract classes
        ///     cannot be instantiated. If this property is true they can be created regardless.
        /// </summary>
        public abstract bool EnforceCreation { get; }

        /// <summary>
        ///     The default creation options, returning true for every property except <see cref="EnforceCreation" />.
        /// </summary>
        public static ClassCreationOptions Default => DefaultOptions.Instance;

        #region Nested type: Customizable

        /// <summary>
        ///     This class allows you to customize every avilable aspect of class creation.
        /// </summary>
        public class Customizable : ClassCreationOptions
        {
            private bool m_AssignScriptFields = true;
            private bool m_CallConstructor = true;
            private bool m_CreateAnnotations = true;
            private bool m_CreateFieldAnnotations = true;
            private bool m_DeclareNativeFields = true;
            private bool m_DeclareScriptFields = true;
            private bool m_EnforceCreation;

            /// <inheritdoc />
            public override bool DeclareNativeFields => m_DeclareNativeFields;

            /// <inheritdoc />
            public override bool DeclareScriptFields => m_DeclareScriptFields;

            /// <inheritdoc />
            public override bool AssignScriptFields => m_AssignScriptFields;

            /// <inheritdoc />
            public override bool CreateAnnotations => m_CreateAnnotations;

            /// <inheritdoc />
            public override bool CreateFieldAnnotations => m_CreateFieldAnnotations;

            /// <inheritdoc />
            public override bool CallConstructor => m_CallConstructor;

            /// <inheritdoc />
            public override bool EnforceCreation => m_EnforceCreation;

            /// <inheritdoc cref="DeclareNativeFields" />
            public Customizable SetDeclareNativeFields(bool value)
            {
                m_DeclareNativeFields = value;
                return this;
            }

            /// <inheritdoc cref="DeclareScriptFields" />
            public Customizable SetDeclareScriptFields(bool value)
            {
                m_DeclareScriptFields = value;
                if (!value) {
                    m_AssignScriptFields = false;
                    m_CreateFieldAnnotations = false;
                }
                return this;
            }

            /// <inheritdoc cref="AssignScriptFields" />
            /// <exception cref="InvalidOperationException">
            ///     Script fields cannot be assigned if they won't be declared(=
            ///     <paramref name="value" /> was true while <see cref="DeclareScriptFields" /> false).
            /// </exception>
            public Customizable SetAssignScriptFields(bool value)
            {
                if (value && !DeclareScriptFields) {
                    throw new InvalidOperationException("Script fields cannot be assigned if they won't be declared.");
                }
                m_AssignScriptFields = value;
                return this;
            }

            /// <inheritdoc cref="CreateAnnotations" />
            public Customizable SetCreateAnnotations(bool value)
            {
                m_CreateAnnotations = value;
                return this;
            }

            /// <inheritdoc cref="CreateFieldAnnotations" />
            public Customizable SetCreateFieldAnnotations(bool value)
            {
                m_CreateFieldAnnotations = value;
                return this;
            }

            /// <inheritdoc cref="CallConstructor" />
            public Customizable SetCallConstructor(bool value)
            {
                m_CallConstructor = value;
                return this;
            }

            /// <inheritdoc cref="EnforceCreation" />
            public Customizable SetEnforceCreation(bool value)
            {
                m_EnforceCreation = value;
                return this;
            }
        }

        #endregion

        #region Nested type: DefaultOptions

        /// <summary>
        ///     The default and immutable class creation options.
        /// </summary>
        private sealed class DefaultOptions : ClassCreationOptions
        {
            private DefaultOptions() {}
            public static readonly DefaultOptions Instance = new DefaultOptions();

            /// <inheritdoc />
            public override bool DeclareNativeFields => true;

            /// <inheritdoc />
            public override bool DeclareScriptFields => true;

            /// <inheritdoc />
            public override bool AssignScriptFields => true;

            /// <inheritdoc />
            public override bool CreateAnnotations => true;

            /// <inheritdoc />
            public override bool CreateFieldAnnotations => true;

            /// <inheritdoc />
            public override bool CallConstructor => true;

            /// <inheritdoc />
            public override bool EnforceCreation => false;
        }

        #endregion
    }
}