using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     This class represents a class in SolScript. If you wish to create you own classes have a look at the
    ///     <see cref="SolLibraryClassAttribute" /> attribute.<br />
    ///     Classes can be instantiated using the
    ///     <see cref="TypeRegistry.CreateInstance(string, ClassCreationOptions, SolValue[])" /> method.
    /// </summary>
    public sealed class SolClass : SolValue, IValueIndexable
    {
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
            GlobalVariables = new ClassGlobalVariables(this);
            InternalVariables = new ClassInternalVariables(this);
        }

        private static uint s_NextId;
        public readonly ClassGlobalVariables GlobalVariables;
        public readonly uint Id;
        internal readonly Inheritance InheritanceChain;
        public readonly ClassInternalVariables InternalVariables;

        internal SolClass[] AnnotationsArray;

        public IReadOnlyList<SolClass> Annotations => AnnotationsArray;

        // todo: investigate if this wont cause problems. A class may be in a different assembly that the one it was declared in.
        public SolAssembly Assembly => InheritanceChain.Definition.Assembly;
        public SolTypeMode TypeMode => InheritanceChain.Definition.TypeMode;
        public override bool IsClass => true;
        public override string Type => InheritanceChain.Definition.Type;

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
                    throw new SolVariableException($"Tried to get-index a variable in {Type} with a {key.Type} value. Classes can only be indexed directly or by strings.");
                }
                SolValue value = GlobalVariables.Get(keySolStr.Value);
                if (value == null) {
                    throw new SolVariableException($"Tried to get the non-assinged variable {keySolStr.Value} in {Type}.");
                }
                return value;
            }
            set {
                SolString keySolStr = key as SolString;
                if (keySolStr == null) {
                    throw new SolVariableException($"Tried to set-index a variable in {Type} with a {key.Type} value. Classes can only be indexed directly or by strings.");
                }
                GlobalVariables.Assign(keySolStr.Value, value);
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
                object nativeObject = inheritance.NativeObject;
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
            SolClassDefinition.MetaFunctionLink link;
            if (context != null && TryGetMetaFunction(SolMetaKey.Stringify, out link)) {
                return SolMetaKey.Stringify.Cast(link.GetFunction(this).Call(context)).NotNull().Value;
            }
            return "class#" + Id + "<" + Type + ">";
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolNumber GetN(SolExecutionContext context)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.GetN, out link)) {
                return SolMetaKey.GetN.Cast(link.GetFunction(this).Call(context));
            }
            return base.GetN(context);
        }

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
            if (TryGetMetaFunction(SolMetaKey.IsEqual, out link)) {
                return SolMetaKey.IsEqual.Cast(link.GetFunction(this).Call(context, other)).NotNull().Value;
            }
            SolClass otherType = other as SolClass;
            return otherType != null && Id == otherType.Id;
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolString Concatenate(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Concatenate, out link)) {
                return SolMetaKey.Concatenate.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Concatenate(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolNumber Add(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Add, out link)) {
                return SolMetaKey.Add.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Add(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Subtract(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Subtract, out link)) {
                return SolMetaKey.Subtract.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Subtract(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Multiply(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Multiply, out link)) {
                return SolMetaKey.Multiply.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Multiply(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Divide(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Divide, out link)) {
                return SolMetaKey.Divide.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Divide(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Exponentiate(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Expotentiate, out link)) {
                return SolMetaKey.Expotentiate.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Exponentiate(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override SolValue Modulo(SolExecutionContext context, SolValue other)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Modulo, out link)) {
                return SolMetaKey.Modulo.Cast(link.GetFunction(this).Call(context, other));
            }
            return base.Modulo(context, other);
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        public override IEnumerable<SolValue> Iterate(SolExecutionContext context)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Iterate, out link)) {
                return SolMetaKey.Iterate.Cast(link.GetFunction(this).Call(context)).NotNull().Iterate(context);
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
        /// <param name="args">The constrcutor arguments.</param>
        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        internal void CallConstructor(SolExecutionContext callingContext, params SolValue[] args)
        {
            if (IsInitialized) {
                throw new InvalidOperationException($"Tried to call the constructor of a class instance of type \"{Type}\" after the class has already been initialized.");
            }
            // ===========================================
            // Prepare
            // The function is already recceived at this point
            // so that we can create a fake stack-frame helping
            // out with error reporting.
            SolFunction ctorFunction;
            try {
                SolClassDefinition.MetaFunctionLink link;
                // If the constructor could not be found, we add a dummy function in order to have a stack trace.
                ctorFunction = TryGetMetaFunction(SolMetaKey.Constructor, out link) ? link.GetFunction(this) : SolFunction.Dummy(Assembly);
            } catch (SolVariableException ex) {
                throw new InvalidOperationException($"The constructor of \"{Type}\" was in an invalid state.", ex);
            }
            callingContext.PushStackFrame(ctorFunction);
            // ===========================================
            // __a_pre_new
            foreach (SolClass annotation in AnnotationsArray) {
                SolValue[] rawArgs = args;
                try {
                    SolClassDefinition.MetaFunctionLink preLink;
                    if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationPreConstructor, out preLink)) {
                        SolTable metaTable = SolMetaKey.AnnotationPreConstructor.Cast(preLink.GetFunction(annotation).Call(callingContext, new SolTable(args), new SolTable(rawArgs))).NotNull();
                        SolValue metaNewArgsRaw;
                        if (metaTable.TryGet("new_args", out metaNewArgsRaw)) {
                            SolTable metaNewArgs = metaNewArgsRaw as SolTable;
                            if (metaNewArgs == null) {
                                throw new SolRuntimeException(callingContext,
                                    $"The annotation \"{annotation}\" tried to override the constructor arguments of a class instance of type \"{Type}\" with a \"{metaNewArgsRaw.Type}\" value. Expected a \"table!\" value.");
                            }
                            args = metaNewArgs.ToArray();
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
                    if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationPostConstructor, out postLink)) {
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
                Instance = instance;
                BaseInheritance = baseInheritance;
                Definition = definition;
                Variables = new ClassInheritanceVariables(this);
            }

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
            ///     The local variables of this inheritance level. Uses the class global variables as parent.
            /// </summary>
            /// <remarks>
            ///     Each inheritance link has their own variables representing the non-global variables. These variables have the class
            ///     internal variables as parent, thus accessing globals and internals if no local exists.<br />
            ///     Furthermore new variables will be declared in the local scope, while still having the ability to set values to
            ///     global ones.
            /// </remarks>
            public readonly ClassInheritanceVariables Variables;

            /// <summary>
            ///     The native object representing this exact <see cref="Inheritance" />.
            /// </summary>
            [CanBeNull] public object NativeObject;
        }

        #endregion
    }
}