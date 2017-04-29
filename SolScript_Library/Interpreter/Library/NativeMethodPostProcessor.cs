using System;
using System.Reflection;
using PSUtility.Reflection;

namespace SolScript.Interpreter.Library
{
    /// <summary>
    ///     A <see cref="NativeMethodPostProcessor" /> is used to modify certain aspects of a native function during creation.
    ///     This for example allows to remap the name of the method(e.g this is internally used to remap ToString() to
    ///     __to_string()).
    /// </summary>
    public abstract class NativeMethodPostProcessor
    { 
        // todo: provide a way for the implementation to specify which aspects should even be touched by the post processor. 

        public abstract bool AppliesTo(MethodInfo method);

        // This may not be needed now, but could be important as the post processor scales.
        /// <summary>
        ///     By default an explicit <see cref="SolLibraryNameAttribute" /> overrides the result of the <see cref="GetName" />
        ///     method, and so on. If this value is true however, the explict arguments are ignored and the results of the method
        ///     calls of this post processor take precedence.
        /// </summary>
        /// <param name="method">The method referene.</param>
        /// <returns>If the attributes on this method should be overridden.</returns>
        public virtual bool OverridesExplicitAttributes(MethodInfo method) => false;

        /// <summary>
        ///     If this returns true no function for this method can be created. By default all methods can be created.
        /// </summary>
        public virtual bool DoesFailCreation(MethodInfo method) => false;

        /// <summary>
        ///     Gets the remapped function name. The default is <see cref="MethodInfo.Name" />.
        /// </summary>
        /// <param name="method">The method referene.</param>
        /// <returns>The new function name to use in SolScript.</returns>
        public virtual string GetName(MethodInfo method) => method.GetCustomAttribute<SolLibraryNameAttribute>()?.Name ?? method.Name;

        /// <summary>
        ///     Gets the remapped return type. The default is either marshalled from the actual native return type or inferred from
        ///     one of its attributes(but they will be determined at a later stage; once the definitions are being generated).
        /// </summary>
        /// <param name="method">The method referene.</param>
        /// <returns>The remapped return type, or null if you do not wish to remap.</returns>
        /// <remarks>
        ///     Very important: If you do not wish to remap the return type you must return null and NOT the default SolType
        ///     value.
        /// </remarks>
        public virtual SolType? GetReturn(MethodInfo method) => method.GetCustomAttribute<SolContractAttribute>()?.GetSolType();

        /// <summary>
        ///     Gets the remapped function <see cref="SolAccessModifier" />. Default is <see cref="SolAccessModifier.Global" />.
        /// </summary>
        /// <param name="method">The method referene.</param>
        /// <returns>The new function <see cref="SolAccessModifier" /> to use in SolScript.</returns>
        public virtual SolAccessModifier GetAccessModifier(MethodInfo method) => method.GetCustomAttribute<SolLibraryAccessModifierAttribute>()?.AccessModifier ?? SolAccessModifier.Global;

        #region Nested type: Access

        public class Access : NativeMethodPostProcessor
        {
            public Access(Predicate<MethodInfo> matcher, SolAccessModifier access)
            {
                m_Access = access;
                m_Matcher = matcher;
            }

            private readonly SolAccessModifier m_Access;
            private readonly Predicate<MethodInfo> m_Matcher;

            #region Overrides

            public override SolAccessModifier GetAccessModifier(MethodInfo method) => m_Access;

            /// <inheritdoc />
            public override bool AppliesTo(MethodInfo method)
            {
                return m_Matcher(method);
            }

            #endregion
        }

        #endregion

        #region Nested type: Default

        public sealed class Default : NativeMethodPostProcessor
        {
            public Default(Predicate<MethodInfo> matcher)
            {
                m_Matcher = matcher;
            }

            private readonly Predicate<MethodInfo> m_Matcher;

            #region Overrides

            /// <inheritdoc />
            public override bool AppliesTo(MethodInfo method)
            {
                return m_Matcher(method);
            }

            #endregion
        }

        #endregion

        #region Nested type: Fail

        public sealed class Fail : NativeMethodPostProcessor
        {
            public Fail(Predicate<MethodInfo> matcher)
            {
                m_Matcher = matcher;
            }

            private readonly Predicate<MethodInfo> m_Matcher;

            #region Overrides

            /// <inheritdoc />
            public override bool DoesFailCreation(MethodInfo method) => true;

            /// <inheritdoc />
            public override bool AppliesTo(MethodInfo method)
            {
                return m_Matcher(method);
            }

            #endregion
        }

        #endregion

        #region Nested type: Rename

        public class Rename : NativeMethodPostProcessor
        {
            public Rename(Predicate<MethodInfo> matcher, string name)
            {
                m_Matcher = matcher;
                m_Name = name;
            }

            private readonly Predicate<MethodInfo> m_Matcher;

            private readonly string m_Name;

            #region Overrides

            public override string GetName(MethodInfo method) => m_Name;

            /// <inheritdoc />
            public override bool AppliesTo(MethodInfo method)
            {
                return m_Matcher(method);
            }

            #endregion
        }

        #endregion

        #region Nested type: RenameAccess

        public class RenameAccess : Rename
        {
            public RenameAccess(Predicate<MethodInfo> matcher, string name, SolAccessModifier access) : base(matcher, name)
            {
                m_Access = access;
            }

            private readonly SolAccessModifier m_Access;

            #region Overrides

            public override SolAccessModifier GetAccessModifier(MethodInfo method) => m_Access;

            #endregion
        }

        #endregion

        #region Nested type: RenameAccessReturn

        public sealed class RenameAccessReturn : RenameAccess
        {
            /// <inheritdoc />
            public RenameAccessReturn(Predicate<MethodInfo> matcher, string name, SolAccessModifier access, SolType returnType) : base(matcher, name, access)
            {
                m_ReturnType = returnType;
            }

            private readonly SolType m_ReturnType;

            #region Overrides

            /// <inheritdoc />
            public override SolType? GetReturn(MethodInfo method) => m_ReturnType;

            #endregion
        }

        #endregion
    }
}