using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types.Interfaces;
using SolScript.Utility;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     This class represents a class in SolScript. If you wish to create you own classes have a look at the
    ///     <see cref="SolLibraryClassAttribute" /> attribute.<br />
    ///     Classes can be instantiated using the
    ///     <see cref="SolAssembly.New(string, ClassCreationOptions, SolValue[])" /> method.
    /// </summary>
    public sealed class SolClass : SolValue, IValueIndexable
    {
        /// <summary>
        ///     Creates a new class.
        /// </summary>
        /// <param name="definition">The definition to base if on.</param>
        internal SolClass(SolClassDefinition definition)
        {
            Id = s_NextId++;
            Stack<SolClassDefinition> inheritanceStack = definition.GetInheritanceReversed();
            Inheritance inheritance = null;
            while (inheritanceStack.Count > 0) {
                SolClassDefinition activeDefinition = inheritanceStack.Pop();
                inheritance = new Inheritance(this, activeDefinition, inheritance);
            }
            InheritanceChain = inheritance;
        }

        // The next class id.
        private static uint s_NextId;
        // The unique id of this class. (Until we overflow the uint max size at least.)
        public readonly uint Id;
        // The inheritance chain. This is where all the fancy magic happens.
        internal readonly Inheritance InheritanceChain;
        // Internal array containing the annotations.
        internal Array<SolClass> AnnotationsArray;

        /// <summary>
        ///     The annotations on this class instance.
        /// </summary>
        public IReadOnlyList<SolClass> Annotations => AnnotationsArray;

        /// <summary>
        ///     The assembly this class is in.
        /// </summary>
        public SolAssembly Assembly => InheritanceChain.Definition.Assembly;

        /// <inheritdoc />
        public override bool IsClass => true;

        /// <inheritdoc />
        public override string Type => InheritanceChain.Definition.Type;

        /// <summary>
        ///     The type mode of this class, explaining if the class is a singleton, sealed, class, etc.
        /// </summary>
        public SolTypeMode TypeMode => InheritanceChain.Definition.TypeMode;

        /// <summary>
        ///     Is this class initialized? A class counts as initialized as soon as
        ///     the constructor is called.
        /// </summary>
        public bool IsInitialized { get; internal set; }

        #region IValueIndexable Members

        /// <summary>
        ///     Gets or sets a global variable in this class.
        /// </summary>
        /// <param name="key">The key to index by.</param>
        /// <returns>The value assigned with the given key.</returns>
        /// <exception cref="SolVariableException">An error occured while getting/setting the value.</exception>
        /// <remarks>
        ///     A <see cref="SolClass" /> can only be indexed by a <see cref="SolString" />. All other attempts to index this
        ///     class will throw a <see cref="SolVariableException" />.
        /// </remarks>
        public SolValue this[SolValue key] {
            get {
                SolString keySolStr = key as SolString;
                if (keySolStr == null) {
                    throw new SolVariableException(InheritanceChain.Definition.Location,
                        $"Tried to get-index a variable in {Type} with a {key.Type} value. Classes can only be indexed directly or by strings.");
                }
                return GetVariables(SolAccessModifier.None, SolVariableMode.All).Get(keySolStr.Value);
            }
            set {
                SolString keySolStr = key as SolString;
                if (keySolStr == null) {
                    throw new SolVariableException(InheritanceChain.Definition.Location,
                        $"Tried to set-index a variable in {Type} with a {key.Type} value. Classes can only be indexed directly or by strings.");
                }
                GetVariables(SolAccessModifier.None, SolVariableMode.All).Assign(keySolStr.Value, value);
            }
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            Inheritance inheritance = FindInheritance(type);
            if (inheritance != null) {
                DynamicReference.GetState getState;
                object nativeObject = inheritance.NativeReference.GetReference(out getState);
                if (getState != DynamicReference.GetState.Retrieved) {
                    throw new SolMarshallingException(type, "The native reference of inheritance level \"" + inheritance.Definition.Type + "\" could not be resolved.");
                }
                if (nativeObject == null) {
                    return null;
                }
                // The value/class relation is cached in case the value will be marshalled back to SolScript.
                SolMarshal.GetAssemblyCache(Assembly).StoreReference(nativeObject, this);
                return nativeObject;
            }
            return base.ConvertTo(type);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        protected override string ToString_Impl(SolExecutionContext context)
        {
            context = context ?? new SolExecutionContext(Assembly, SolMetaKey.__to_string.Name + " native call");
            try {
                SolClassDefinition.MetaFunctionLink link;
                if (TryGetMetaFunction(SolMetaKey.__to_string, out link)) {
                    return SolMetaKey.__to_string.Cast(link.GetFunction(this).Call(context)).NotNull().Value;
                }
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to resolve the " + SolMetaKey.__to_string.Name + " meta function.", ex);
            }
            return "class#" + Id + "<" + Type + ">";
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolNumber GetN(SolExecutionContext context)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__getn, out link)) {
                return SolMetaKey.__getn.Cast(link.GetFunction(this).Call(context));
            }
            return base.GetN(context);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 20 + (int) Id + Type.GetHashCode();
            }
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__is_equal, out link)) {
                return SolMetaKey.__is_equal.Cast(link.GetFunction(this).Call(context, other)).NotNull().Value;
            }
            SolClass otherType = other as SolClass;
            return otherType != null && Id == otherType.Id;
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolString Concatenate(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__concat, out link)) {
                return SolMetaKey.__concat.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Concatenate(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolNumber Add(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__add, out link)) {
                return SolMetaKey.__add.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Add(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Subtract(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__sub, out link)) {
                return SolMetaKey.__sub.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Subtract(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Multiply(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__mul, out link)) {
                return SolMetaKey.__mul.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Multiply(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Divide(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__div, out link)) {
                return SolMetaKey.__div.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Divide(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Exponentiate(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__exp, out link)) {
                return SolMetaKey.__exp.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Exponentiate(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Modulo(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__mod, out link)) {
                return SolMetaKey.__mod.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Modulo(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override IEnumerable<SolValue> Iterate(SolExecutionContext context)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.__iterate, out link)) {
                return SolMetaKey.__iterate.Cast(link.GetFunction(this).Call(context)).NotNull().Iterate(context);
            }
            return base.Iterate(context);
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, this)) {
                return true;
            }
            if (ReferenceEquals(other, null)) {
                return false;
            }
            SolClass otherClass = other as SolClass;
            if (otherClass == null) {
                return false;
            }
            return Id == otherClass.Id;
        }

        #endregion

        /// <summary>
        ///     Gets a variable source from this class.
        /// </summary>
        /// <param name="access">The access modifier of the variable source.</param>
        /// <param name="mode">The variable mode.</param>
        /// <param name="level">
        ///     The inheritance level. (e.g. if a class named "MyClass" extends "Base" pass "Base" to obtain the
        ///     variables of "Base". Otherwise the most derived type will be used - in this example "MyClass".)
        /// </param>
        /// <returns>The variable source.</returns>
        /// <exception cref="SolVariableException">The class does not extend <paramref name="level" />.</exception>
        public IVariables GetVariables(SolAccessModifier access, SolVariableMode mode, string level)
        {
            Inheritance inheritance = level != null ? FindInheritance(level) : InheritanceChain;
            if (inheritance == null) {
                throw new SolVariableException(InheritanceChain.Definition.Location, "The class \"" + Type + "\" does not extend \"" + level + "\".");
            }
            return inheritance.GetVariables(access, mode);
        }

        /// <summary>
        ///     Gets a variable source from this class.
        /// </summary>
        /// <param name="access">The access modifier of the variable source.</param>
        /// <param name="mode">The variable mode.</param>
        /// <param name="level">
        ///     The inheritance level. (e.g. if a class named "MyClass" extends "Base" pass the declaration of "Base" to obtain the
        ///     variables of "Base". Otherwise the most derived type will be used - in this example "MyClass".)
        /// </param>
        /// <returns>The variable source.</returns>
        /// <exception cref="SolVariableException">The class does not extend <paramref name="level" />.</exception>
        public IVariables GetVariables(SolAccessModifier access, SolVariableMode mode, SolClassDefinition level = null)
        {
            Inheritance inheritance = level != null ? FindInheritance(level) : InheritanceChain;
            if (inheritance == null) {
                throw new SolVariableException(InheritanceChain.Definition.Location, "The class \"" + Type + "\" does not extend \"" + level + "\".");
            }
            return inheritance.GetVariables(access, mode);
        }


        /// <summary>
        ///     Tries to get a meta function of this class.
        /// </summary>
        /// <param name="meta">The meta key identifier.</param>
        /// <param name="link">The meta function link. Only valid if the method returned true.</param>
        /// <returns>true if the meta function could be found, false if not.</returns>
        /// <exception cref="SolVariableException">
        ///     The meta function could be found but was in an invalid state(e.g. wrong type, accessor...).
        /// </exception>
        [ContractAnnotation("link:null => false")]
        internal bool TryGetMetaFunction(SolMetaKey meta, out SolClassDefinition.MetaFunctionLink link)
        {
            return InheritanceChain.Definition.TryGetMetaFunction(meta, out link);
        }

        /// <summary> Finds the inheritance link that contains the given class definition. </summary>
        /// <param name="definition"> The class definition. </param>
        /// <returns> The inheritance link, or null. </returns>
        [CanBeNull]
        internal Inheritance FindInheritance(SolClassDefinition definition)
        {
            Inheritance active = InheritanceChain;
            while (active != null) {
                if (active.Definition == definition) {
                    return active;
                }
                active = active.BaseInheritance;
            }
            return null;
        }

        /// <summary>
        ///     Finds the inheritance link that is assignable to the given native type.
        /// </summary>
        /// <param name="nativeType">The native type.</param>
        /// <returns>The inheritance link, or null. </returns>
        internal Inheritance FindInheritance(Type nativeType)
        {
            Inheritance active = InheritanceChain;
            while (active != null) {
                if (nativeType.IsAssignableFrom(active.Definition.NativeType)) {
                    return active;
                }
                active = active.BaseInheritance;
            }
            return null;
        }

        /// <summary> Finds the inheritance link that which is linked to a class definition of the given class name. </summary>
        /// <param name="className"> The class name. </param>
        /// <returns> The inheritance link, or null. </returns>
        [CanBeNull]
        internal Inheritance FindInheritance(string className)
        {
            Inheritance active = InheritanceChain;
            while (active != null) {
                if (active.Definition.Type == className) {
                    return active;
                }
                active = active.BaseInheritance;
            }
            return null;
        }

        /// <summary> Calls the constructor for this class. </summary>
        /// <param name="callingContext">The context to use for calling the constructor.</param>
        /// <param name="args">The constructor arguments.</param>
        /// <exception cref="InvalidOperationException">The class is already initialized.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        /// <seealso cref="IsInitialized" />
        internal void CallConstructor(SolExecutionContext callingContext, params SolValue[] args)
        {
            if (IsInitialized) {
                throw new InvalidOperationException($"Tried to call the constructor of a class instance of type \"{Type}\" after the class has already been initialized.");
            }
            // ===========================================
            // Prepare
            // The function is already received at this point
            // so that we can create a fake stack-frame helping
            // out with error reporting.
            SolFunction ctorFunction;
            try {
                SolClassDefinition.MetaFunctionLink link;
                // If the constructor could not be found, we add a dummy function in order to have a stack trace.
                ctorFunction = TryGetMetaFunction(SolMetaKey.__new, out link) ? link.GetFunction(this) : SolFunction.Dummy(Assembly);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(callingContext, $"The constructor of \"{Type}\" was in an invalid state.", ex);
            }
            callingContext.PushStackFrame(ctorFunction);
            // ===========================================
            // __a_pre_new
            foreach (SolClass annotation in AnnotationsArray) {
                SolValue[] rawArgs = args;
                try {
                    SolClassDefinition.MetaFunctionLink preLink;
                    if (annotation.TryGetMetaFunction(SolMetaKey.__a_pre_new, out preLink)) {
                        SolTable metaTable = SolMetaKey.__a_pre_new.Cast(preLink.GetFunction(annotation).Call(callingContext, new SolTable(args), new SolTable(rawArgs))).NotNull();
                        SolValue metaNewArgsRaw;
                        if (metaTable.TryGet(SolString.ValueOf("new_args"), out metaNewArgsRaw)) {
                            SolTable metaNewArgs = metaNewArgsRaw as SolTable;
                            if (metaNewArgs == null) {
                                throw new SolRuntimeException(callingContext,
                                    $"The annotation \"{annotation}\" tried to override the constructor arguments of a class instance of type \"{Type}\" with a \"{metaNewArgsRaw.Type}\" value. Expected a \"table!\" value.");
                            }
                            args = metaNewArgs.IterateArray().ToArray();
                        }
                    }
                } catch (SolVariableException ex) {
                    throw new InvalidOperationException($"The pre-constructor function of annotation \"{annotation.Type}\" on \"{Type}\" was in an invalid state.", ex);
                }
            }
            // ===========================================
            // Call actual constructor function
            // Remove the previously added fake stack frame.
            callingContext.PopStackFrame();
            ctorFunction.Call(callingContext, args);
            // ===========================================
            // __a_post_new
            foreach (SolClass annotation in AnnotationsArray) {
                try {
                    SolClassDefinition.MetaFunctionLink postLink;
                    if (annotation.TryGetMetaFunction(SolMetaKey.__a_post_new, out postLink)) {
                        postLink.GetFunction(annotation).Call(callingContext, new SolTable(args));
                    }
                } catch (SolVariableException ex) {
                    throw new InvalidOperationException($"The post-constructor function of annotation \"{annotation.Type}\" on \"{Type}\" was in an invalid state.", ex);
                }
            }
        }

        #region Nested type: Inheritance

        /// <summary>
        ///     The <see cref="Inheritance" /> type is used to represent data related to a certain class in the inheritance chain
        ///     of a class(such as the local variables).
        /// </summary>
        internal class Inheritance
        {
            // Creates the ... creators. Yay.
            static Inheritance()
            {
                s_Creators[(int) SolVariableMode.All + (int) SolAccessModifier.None] = i => new All_Globals(i);
                s_Creators[(int) SolVariableMode.All + (int) SolAccessModifier.Internal] = i => new All_Internals(i);
                s_Creators[(int) SolVariableMode.All + (int) SolAccessModifier.Local] = i => new All_Locals(i);
                s_Creators[(int) SolVariableMode.Base + (int) SolAccessModifier.None] = i => new Base_Globals(i);
                s_Creators[(int) SolVariableMode.Base + (int) SolAccessModifier.Internal] = i => new Base_Internals(i);
                s_Creators[(int) SolVariableMode.Base + (int) SolAccessModifier.Local] = i => new Base_Locals(i);
            }

            /// <summary>
            ///     Creates a new <see cref="Inheritance" /> object.
            /// </summary>
            /// <param name="instance"> The class instance this <see cref="Inheritance" /> belongs to.</param>
            /// <param name="definition">
            ///     The definition of this exact <see cref="Inheritance" /> element. Not just the parent class
            ///     definition of the class.
            /// </param>
            /// <param name="baseInheritance">The base inheritance(The inheritance this one extends).</param>
            public Inheritance(SolClass instance, SolClassDefinition definition, [CanBeNull] Inheritance baseInheritance)
            {
                // The globals and internals are separated for each inheritance level in 
                // order to be able to access the base variables.
                Instance = instance;
                BaseInheritance = baseInheritance;
                Definition = definition;
                NativeReference = DynamicReference.FailedReference.Instance;
                m_DeclaredLocalVariables = new DeclaredLocalClassInheritanceVariables(this);
                m_DeclaredInternalVariables = new DeclaredInternalClassInheritanceVariables(this);
                m_DeclaredGlobalVariables = new DeclaredGlobalClassInheritanceVariables(this);
            }

            private static readonly Func<Inheritance, IVariables>[] s_Creators = new Func<Inheritance, IVariables>[6];

            /// <summary>
            ///     The base inheritance(The inheritance this one extends).
            /// </summary>
            [CanBeNull] public readonly Inheritance BaseInheritance;

            /// <summary>
            ///     The definition of this exact <see cref="Inheritance" /> element. Not just the parent class definition of the class.
            /// </summary>
            public readonly SolClassDefinition Definition;

            /// <summary>
            ///     The class instance this <see cref="Inheritance" /> belongs to.
            /// </summary>
            public readonly SolClass Instance;

            /// <summary>
            ///     The native object representing this exact <see cref="Inheritance" />.
            /// </summary>
            public DynamicReference NativeReference;

            // Indexing works by (int)SolVariableMode + (int)AccessModifier
            private readonly IVariables[] l_variables = new IVariables[6];
            // The global variables declared at this inheritance level.
            private readonly DeclaredGlobalClassInheritanceVariables m_DeclaredGlobalVariables;
            // The internal variables declared at this inheritance level.
            private readonly DeclaredInternalClassInheritanceVariables m_DeclaredInternalVariables;
            // The local variables declared at this inheritance level.
            private readonly DeclaredLocalClassInheritanceVariables m_DeclaredLocalVariables;

            public IVariables GetVariables(SolAccessModifier access, SolVariableMode mode)
            {
                int index = (int) access + (int) mode;
                switch (index) {
                    case (int) SolAccessModifier.None + (int) SolVariableMode.Declarations:
                        return m_DeclaredGlobalVariables;
                    case (int) SolAccessModifier.Local + (int) SolVariableMode.Declarations:
                        return m_DeclaredLocalVariables;
                    case (int) SolAccessModifier.Internal + (int) SolVariableMode.Declarations:
                        return m_DeclaredInternalVariables;
                    default:
                        return l_variables[index] ?? (l_variables[index] = s_Creators[index](this));
                }
            }

            #region Nested type: All_Globals

            /// <summary>
            ///     All inheritance elements, Global variables.
            /// </summary>
            private class All_Globals : InheritanceVarBase
            {
                /// <inheritdoc />
                public All_Globals(Inheritance inheritance) : base(inheritance) {}

                #region Overrides

                /// <inheritdoc />
                /// <remarks>Sources include ALL global variables in the entire inheritance chain.</remarks>
                protected override IEnumerable<IVariables> GetVariableSources()
                {
                    Inheritance active = VarInheritance.Instance.InheritanceChain;
                    while (active != null) {
                        yield return active.m_DeclaredGlobalVariables;
                        active = active.BaseInheritance;
                    }
                    yield return Assembly.GlobalVariables;
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">
                ///     A variable with this name has already been declared.
                /// </exception>
                public override void Declare(string name, SolType type)
                {
                    VarInheritance.m_DeclaredGlobalVariables.Declare(name, type);
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
                public override void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
                {
                    VarInheritance.m_DeclaredGlobalVariables.DeclareNative(name, type, field, fieldReference);
                }

                #endregion
            }

            #endregion

            #region Nested type: All_Internals

            private class All_Internals : InheritanceVarBase
            {
                /// <inheritdoc />
                public All_Internals(Inheritance inheritance) : base(inheritance) {}

                #region Overrides

                /// <inheritdoc />
                /// <remarks>Sources include ALL global variables in the entire inheritance chain.</remarks>
                protected override IEnumerable<IVariables> GetVariableSources()
                {
                    Inheritance active = VarInheritance.Instance.InheritanceChain;
                    while (active != null) {
                        yield return active.m_DeclaredInternalVariables;
                        yield return active.m_DeclaredGlobalVariables;
                        active = active.BaseInheritance;
                    }
                    yield return Assembly.GlobalVariables;
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">
                ///     A variable with this name has already been declared.
                /// </exception>
                public override void Declare(string name, SolType type)
                {
                    VarInheritance.m_DeclaredGlobalVariables.Declare(name, type);
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
                public override void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
                {
                    VarInheritance.m_DeclaredGlobalVariables.DeclareNative(name, type, field, fieldReference);
                }

                #endregion
            }

            #endregion

            #region Nested type: All_Locals

            private class All_Locals : InheritanceVarBase
            {
                /// <inheritdoc />
                public All_Locals(Inheritance inheritance) : base(inheritance) {}

                #region Overrides

                /// <inheritdoc />
                /// <exception cref="SolVariableException">
                ///     A variable with this name has already been declared.
                /// </exception>
                public override void Declare(string name, SolType type)
                {
                    VarInheritance.m_DeclaredLocalVariables.Declare(name, type);
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
                public override void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
                {
                    VarInheritance.m_DeclaredLocalVariables.DeclareNative(name, type, field, fieldReference);
                }

                /// <inheritdoc />
                protected override IEnumerable<IVariables> GetVariableSources()
                {
                    yield return VarInheritance.m_DeclaredLocalVariables;
                    Inheritance active = VarInheritance.Instance.InheritanceChain;
                    while (active != null) {
                        yield return active.m_DeclaredGlobalVariables;
                        yield return active.m_DeclaredInternalVariables;
                        active = active.BaseInheritance;
                    }
                    yield return Assembly.GlobalVariables;
                }

                #endregion
            }

            #endregion

            #region Nested type: Base

            private abstract class Base : InheritanceVarBase
            {
                /// <inheritdoc />
                public Base(Inheritance inheritance) : base(inheritance) {}

                /// <exception cref="SolVariableException" accessor="get">No base class exists.</exception>
                protected Inheritance BaseInheritance {
                    get {
                        Inheritance theBase = VarInheritance.BaseInheritance;
                        if (theBase == null) {
                            throw new SolVariableException(VarInheritance.Definition.Location, "The class \"" + VarInheritance.Definition.Type + "\" has no base class.");
                        }
                        return theBase;
                    }
                }

                /// <summary>
                ///     Checks if this inheritance has a base class.
                /// </summary>
                protected bool HasDirectBase => VarInheritance.BaseInheritance != null;
            }

            #endregion

            #region Nested type: Base_Globals

            private class Base_Globals : Base
            {
                /// <inheritdoc />
                public Base_Globals(Inheritance inheritance) : base(inheritance) {}

                #region Overrides

                /// <inheritdoc />
                /// <exception cref="SolVariableException">
                ///     A variable with this name has already been declared.
                /// </exception>
                public override void Declare(string name, SolType type)
                {
                    if (HasDirectBase) {
                        BaseInheritance.m_DeclaredGlobalVariables.Declare(name, type);
                    } else {
                        Assembly.GlobalVariables.Declare(name, type);
                    }
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
                public override void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
                {
                    if (HasDirectBase) {
                        BaseInheritance.m_DeclaredGlobalVariables.DeclareNative(name, type, field, fieldReference);
                    } else {
                        Assembly.GlobalVariables.DeclareNative(name, type, field, fieldReference);
                    }
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">An error occured.</exception>
                protected override IEnumerable<IVariables> GetVariableSources()
                {
                    if (HasDirectBase) {
                        Inheritance active = BaseInheritance;
                        while (active != null) {
                            yield return active.m_DeclaredGlobalVariables;
                            active = active.BaseInheritance;
                        }
                    }
                    yield return Assembly.GlobalVariables;
                }

                #endregion
            }

            #endregion

            #region Nested type: Base_Internals

            private class Base_Internals : Base
            {
                /// <inheritdoc />
                public Base_Internals(Inheritance inheritance) : base(inheritance) {}

                #region Overrides

                /// <inheritdoc />
                /// <exception cref="SolVariableException">
                ///     A variable with this name has already been declared.
                /// </exception>
                public override void Declare(string name, SolType type)
                {
                    if (HasDirectBase) {
                        BaseInheritance.m_DeclaredInternalVariables.Declare(name, type);
                    } else {
                        Assembly.GlobalVariables.Declare(name, type);
                    }
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
                public override void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
                {
                    if (HasDirectBase) {
                        BaseInheritance.m_DeclaredInternalVariables.DeclareNative(name, type, field, fieldReference);
                    } else {
                        Assembly.GlobalVariables.DeclareNative(name, type, field, fieldReference);
                    }
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">An error occured.</exception>
                protected override IEnumerable<IVariables> GetVariableSources()
                {
                    if (HasDirectBase) {
                        Inheritance active = BaseInheritance;
                        while (active != null) {
                            yield return active.m_DeclaredInternalVariables;
                            yield return active.m_DeclaredGlobalVariables;
                            active = active.BaseInheritance;
                        }
                    }
                    yield return Assembly.GlobalVariables;
                }

                #endregion
            }

            #endregion

            #region Nested type: Base_Locals

            private class Base_Locals : Base
            {
                /// <inheritdoc />
                public Base_Locals(Inheritance inheritance) : base(inheritance) {}

                #region Overrides

                /// <inheritdoc />
                /// <exception cref="SolVariableException">
                ///     A variable with this name has already been declared.
                /// </exception>
                public override void Declare(string name, SolType type)
                {
                    if (HasDirectBase) {
                        BaseInheritance.m_DeclaredLocalVariables.Declare(name, type);
                    } else {
                        Assembly.GlobalVariables.Declare(name, type);
                    }
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
                public override void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
                {
                    if (HasDirectBase) {
                        BaseInheritance.m_DeclaredLocalVariables.DeclareNative(name, type, field, fieldReference);
                    } else {
                        Assembly.GlobalVariables.DeclareNative(name, type, field, fieldReference);
                    }
                }

                /// <inheritdoc />
                /// <exception cref="SolVariableException">An error occured.</exception>
                protected override IEnumerable<IVariables> GetVariableSources()
                {
                    if (HasDirectBase) {
                        Inheritance active = BaseInheritance;
                        yield return active.m_DeclaredLocalVariables;
                        while (active != null) {
                            yield return active.m_DeclaredInternalVariables;
                            yield return active.m_DeclaredGlobalVariables;
                            active = active.BaseInheritance;
                        }
                    }
                    yield return Assembly.GlobalVariables;
                }

                #endregion
            }

            #endregion

            #region Nested type: InheritanceVarBase

            /// <summary>
            ///     Base class for creating per-inheritance element variable sources.
            /// </summary>
            private abstract class InheritanceVarBase : VarBase
            {
                /// <inheritdoc />
                protected InheritanceVarBase(Inheritance inheritance)
                {
                    VarInheritance = inheritance;
                }

                /// <summary>
                ///     The inheritance element this variable source is on.
                /// </summary>
                protected readonly Inheritance VarInheritance;

                /// <inheritdoc />
                protected override SolClass Instance => VarInheritance.Instance;

                #region Overrides

                /// <inheritdoc />
                protected override SolSourceLocation GetLocation()
                {
                    return VarInheritance.Definition.Location;
                }

                #endregion
            }

            #endregion
        }

        #endregion

        #region Nested type: VarBase

        /// <summary>
        ///     Base class for all special variable sources regarding classes.
        /// </summary>
        private abstract class VarBase : IVariables
        {
            /// <summary>
            ///     The class instance this variable source is on.
            /// </summary>
            protected abstract SolClass Instance { get; }

            #region IVariables Members

            /// <inheritdoc />
            /// <exception cref="SolVariableException">Failed to get the value.</exception>
            public SolValue Get(string name)
            {
                SolValue value;
                VariableState s = TryGet(name, out value);
                if (value == null || s != VariableState.Success) {
                    throw InternalHelper.CreateVariableGetException(name, s, null, GetLocation());
                }
                return value;
            }

            /// <inheritdoc />
            public virtual VariableState TryGet(string name, out SolValue value)
            {
                foreach (IVariables source in GetVariableSources()) {
                    VariableState s = source.TryGet(name, out value);
                    switch (s) {
                        case VariableState.Success:
                        case VariableState.FailedCouldNotResolveNativeReference:
                        case VariableState.FailedNotAssigned:
                        case VariableState.FailedTypeMismatch:
                        case VariableState.FailedNativeException:
                        case VariableState.FailedRuntimeError:
                            // We either found it, or something went wrong.
                            return s;
                        case VariableState.FailedNotDeclared:
                            // Nope, isn't here - let's go on.
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                // Phew, okay we checked all of 'em and found nothing. It's not there.
                value = null;
                return VariableState.FailedNotDeclared;
            }

            /// <inheritdoc />
            /// <exception cref="SolVariableException">Failed to get the annotations.</exception>
            public IReadOnlyList<SolClass> GetAnnotations(string name)
            {
                foreach (IVariables source in GetVariableSources()) {
                    if (source.IsDeclared(name)) {
                        return source.GetAnnotations(name);
                    }
                }
                return EmptyReadOnlyList<SolClass>.Value;
            }

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     No variable with this name has been declared.
            /// </exception>
            public virtual void AssignAnnotations(string name, params SolClass[] annotations)
            {
                foreach (IVariables source in GetVariableSources()) {
                    if (source.IsDeclared(name)) {
                        source.AssignAnnotations(name, annotations);
                    }
                }
                throw new SolVariableException(GetLocation(), "Tried to assign annotations to class field \"" + name + "\". No such field exists.");
            }

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     No variable with this name has been declared.
            /// </exception>
            public virtual SolValue Assign(string name, SolValue value)
            {
                foreach (IVariables source in GetVariableSources()) {
                    if (source.IsDeclared(name)) {
                        return source.Assign(name, value);
                    }
                }
                throw new SolVariableException(GetLocation(), "Tried to assign class field \"" + name + "\". No such field exists.");
            }

            /// <inheritdoc />
            public virtual bool IsDeclared(string name)
            {
                foreach (IVariables source in GetVariableSources()) {
                    if (source.IsDeclared(name)) {
                        return true;
                    }
                }
                return false;
            }

            /// <inheritdoc />
            public virtual bool IsAssigned(string name)
            {
                foreach (IVariables source in GetVariableSources()) {
                    if (source.IsAssigned(name)) {
                        return true;
                    }
                }
                return false;
            }

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     A variable with this name has already been declared.
            /// </exception>
            /// <exception cref="SolVariableException">The <see cref="GetVariableSources" /> enumerable is empty.</exception>
            public virtual void Declare(string name, SolType type)
            {
                using (IEnumerator<IVariables> enumerator = GetVariableSources().GetEnumerator()) {
                    if (!enumerator.MoveNext()) {
                        throw new SolVariableException(GetLocation(), "Cannot declare class field \"" + name + "\". No native variable sources exist.");
                    }
                    enumerator.Current.Declare(name, type);
                }
            }

            /// <inheritdoc />
            /// <exception cref="SolVariableException">
            ///     A variable with this name has already been declared.
            /// </exception>
            /// <exception cref="SolVariableException">The <see cref="GetVariableSources" /> enumerable is empty.</exception>
            public virtual void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
            {
                using (IEnumerator<IVariables> enumerator = GetVariableSources().GetEnumerator()) {
                    if (!enumerator.MoveNext()) {
                        throw new SolVariableException(GetLocation(), "Cannot declare native class field \"" + name + "\". No native variable sources exist.");
                    }
                    enumerator.Current.DeclareNative(name, type, field, fieldReference);
                }
            }

            /// <inheritdoc />
            public SolAssembly Assembly => Instance.Assembly;

            #endregion

            /// <summary>
            ///     Gets the location of this variable source used for errors.
            /// </summary>
            /// <returns>The location.</returns>
            protected abstract SolSourceLocation GetLocation();

            /// <summary>
            ///     Gets all <see cref="IVariables" />s that declare the values.
            /// </summary>
            /// <returns>The variable source enumerable.</returns>
            protected abstract IEnumerable<IVariables> GetVariableSources();
        }

        #endregion
    }
}