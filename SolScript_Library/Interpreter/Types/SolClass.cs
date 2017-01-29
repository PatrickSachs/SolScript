using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Types
{
    public class SolClass : SolValue, IValueIndexable
    {
        internal SolClass(SolClassDefinition definition)
        {
            Id = s_NextId++;
            InheritanceChain = new Inheritance(this, definition, null);
            GlobalVariables = new ClassGlobalVariables(this);
            InternalVariables = new ClassInternalVariables(this);
        }

        private static uint s_NextId;
        public readonly ClassGlobalVariables GlobalVariables;
        public readonly uint Id;
        internal readonly Inheritance InheritanceChain;
        public readonly ClassInternalVariables InternalVariables;

        private SolClass[] m_Annotations;

        public IReadOnlyList<SolClass> Annotations => m_Annotations;

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

        public override object ConvertTo(Type type)
        {
            if (type == typeof(SolClass)) {
                return this;
            }
            /* for (int i = 0; i < m_Mixins.Length; i++) {
                object clrObj = m_Mixins[i].Native;
                if (clrObj == null) {
                    continue;
                }
                if (clrObj.GetType() == type || clrObj.GetType().IsSubclassOf(type)) {
                    return clrObj;
                }
            }*/
            return null;
        }

        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        protected override string ToString_Impl(SolExecutionContext context)
        {
            SolClassDefinition.MetaFunctionLink link;
            if (context != null && TryGetMetaFunction(SolMetaKey.AsString, out link)) {
                return SolMetaKey.AsString.Cast(link.GetFunction(this).Call(context)).NotNull().Value;
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

        public override bool Equals(object other)
        {
#if DEBUG
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
            if (otherClass.Id == Id) {
                throw new InvalidOperationException("One instance of class " + Type + " seems to have multiple instances.");
            }
            return false;
#else
            return other == this;
#endif
        }

        #endregion

        internal bool TryGetMetaFunction(SolMetaKey meta, out SolClassDefinition.MetaFunctionLink link)
        {
            return InheritanceChain.Definition.TryGetMetaFunction(meta, out link);
        }

        /*/// <summary>
        ///     Returns a stack of the inheritances starting at the given definition, and including the derived inheritance.<br />
        ///     <c>(Uber:Medium:Base:Slave).FindInheritancesUpward(Base) -> (Base, Medium, Uber)</c>
        /// </summary>
        /// <param name="definition">The definition</param>
        /// <returns>The stack.</returns>
        internal Stack<Inheritance> FindInheritancesUpward(SolClassDefinition definition)
        {
            var stack = new Stack<Inheritance>();
            Inheritance active = InheritanceChain;
            while (active != null) {
                stack.Push(active);
                if (active.Definition == definition) {
                    return stack;
                }
                active = active.BaseClass;
            }
            return null;
        }*/

        /// <summary>
        ///     Gets the inheritance chain in reversed order. The base base class comes first, while the most derived class(this
        ///     one) comes last.
        /// </summary>
        /// <returns>A stack containing the inheritance.</returns>
        internal Stack<Inheritance> GetInheritanceChainReversed()
        {
            var stack = new Stack<Inheritance>();
            Inheritance active = InheritanceChain;
            while (active != null) {
                stack.Push(active);
                active = active.BaseClass;
            }
            return stack;
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
                active = active.BaseClass;
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
                active = active.BaseClass;
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
                active = active.BaseClass;
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
            // todo: add dummy function if no ctor could be found? (or somehow else help with debugging if no ctor)
            SolFunction ctorFunction = null;
            SolClassDefinition.MetaFunctionLink link;
            if (TryGetMetaFunction(SolMetaKey.Constructor, out link)) {
                ctorFunction = link.GetFunction(this);
                callingContext.PushStackFrame(ctorFunction);
            }
            // ===========================================
            // __a_pre_new
            foreach (SolClass annotation in m_Annotations) {
                SolValue[] rawArgs = args;
                SolClassDefinition.MetaFunctionLink preLink;
                if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationPreConstructor, out preLink)) {
                    SolTable metaTable = SolMetaKey.AnnotationPreConstructor.Cast(preLink.GetFunction(annotation).Call(callingContext, new SolTable(args), new SolTable(rawArgs))).NotNull();
                    SolValue metaNewArgsRaw;
                    if (metaTable.TryGet("new_args", out metaNewArgsRaw)) {
                        SolTable metaNewArgs = metaNewArgsRaw as SolTable;
                        if (metaNewArgs == null) {
                            throw new SolRuntimeException(callingContext, $"The annotation \"{annotation}\" tried to override the constructor arguments of a class instance of type \"{Type}\" with a \"{metaNewArgsRaw.Type}\" value. Expected a \"table!\" value.");
                        }
                        args = metaNewArgs.ToArray();
                    }
                }
            }
            // ===========================================
            // Call actual constructor function
            if (ctorFunction != null) {
                // Remove the previously added fake stack frame.
                callingContext.PopStackFrame();
                ctorFunction.Call(callingContext, args);
            }
            IsInitialized = true;
            // ===========================================
            // __a_post_new
            foreach (SolClass annotation in m_Annotations) {
                SolClassDefinition.MetaFunctionLink postLink;
                if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationPostConstructor, out postLink)) {
                    postLink.GetFunction(annotation).Call(callingContext, new SolTable(args));
                }
            }
        }

        #region Nested type: Inheritance

        internal class Inheritance
        {
            public Inheritance(SolClass classInstance, SolClassDefinition definition, [CanBeNull] Inheritance baseClass)
            {
                BaseClass = baseClass;
                Definition = definition;
                Variables = new ClassInheritanceVariables(classInstance, this);
            }

            public readonly SolClassDefinition Definition;

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

            [CanBeNull] public Inheritance BaseClass;
            [CanBeNull] public object NativeObject;
        }

        #endregion

        #region Nested type: Initializer

        /// <summary> The initializer class is used to initialize SolClasses. </summary>
        public sealed class Initializer
        {
            internal Initializer(SolClass forClass)
            {
                m_ForClass = forClass;
            }

            private readonly SolClass m_ForClass;

            /// <summary>
            ///     Creates the instance of this SolClass by assigning the default (or
            ///     specified) values to the fields and then calls the constructor.
            /// </summary>
            /// <exception cref="SolRuntimeException">An error occured while initializing the class.</exception>
            public SolClass Create(SolExecutionContext context, params SolValue[] args)
            {
                // todo: annotations during class initialization
                Stack<Inheritance> inheritanceStack = m_ForClass.GetInheritanceChainReversed();
                var annotations = new List<SolClass>();
                while (inheritanceStack.Count != 0) {
                    Inheritance inheritance = inheritanceStack.Pop();
                    foreach (KeyValuePair<string, SolFieldDefinition> fieldPair in inheritance.Definition.FieldPairs) {
                        if (fieldPair.Value.FieldInitializer != null) {
                            SolValue initialValue = fieldPair.Value.FieldInitializer.Evaluate(context, inheritance.Variables);
                            SolDebug.WriteLine("Initializing field " + fieldPair.Key + " to " + initialValue);
                            try {
                                inheritance.Variables.Assign(fieldPair.Key, initialValue);
                            } catch (SolVariableException ex) {
                                throw new SolRuntimeException(context,
                                    $"An error occured while initializing the field \"{fieldPair.Key}\" of class \"{m_ForClass.Type}\"(Inheritance Level: \"{inheritance.Definition.Type}\"). {ex.Message}",
                                    ex);
                            }
                        }
                    }
                    foreach (SolAnnotationDefinition annotation in inheritance.Definition.Annotations) {
                        var annotationArgs = new SolValue[annotation.Arguments.Length];
                        for (int i = 0; i < annotationArgs.Length; i++) {
                            annotationArgs[i] = annotation.Arguments[i].Evaluate(context, inheritance.Variables);
                        }
                        try {
                            SolClass annotationInstance = m_ForClass.Assembly.TypeRegistry.PrepareInstance(annotation.Definition, true).Create(context, annotationArgs);
                            annotations.Add(annotationInstance);
                        } catch (SolTypeRegistryException ex) {
                            throw new SolRuntimeException(context,
                                $"An error occured while initializing the annotation \"{annotation.Definition.Type}\" of class \"{m_ForClass.Type}\"(Inheritance Level: \"{inheritance.Definition.Type}\").",
                                ex);
                        }
                    }
                }
                m_ForClass.m_Annotations = annotations.ToArray();
                m_ForClass.CallConstructor(context, args);
                return m_ForClass;
            }

            /// <summary>
            ///     Warning: NO finds of this class have been assigned and the ctor will
            ///     NOT be called. The IsInitialized variable of the class is set to FALSE. You
            ///     are responsible for making sure that this class can be used.
            /// </summary>
            /// <remarks>
            ///     The runtime internally uses this method to marshal already existing
            ///     native objects to new SolClasses.
            /// </remarks>
            [NotNull]
            public SolClass CreateWithoutInitialization()
            {
                m_ForClass.m_Annotations = Array.Empty<SolClass>();
                return m_ForClass;
            }
        }

        #endregion
    }
}