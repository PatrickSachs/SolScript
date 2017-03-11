using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Standard <see cref="IVariables" /> implementation. Supports every basic operation. Most
    ///     other <see cref="IVariables" /> implementations are wrappers around this implementation.<br />Has support for
    ///     parenting the variables.
    /// </summary>
    public sealed class Variables : IVariables
    {
        /// <summary>
        ///     Creates a new <see cref="Variables" /> instance for the given <paramref name="assembly" />.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public Variables([NotNull] SolAssembly assembly)
        {
            Assembly = assembly;
        }

        private readonly Utility.Dictionary<string, Base> m_Variables = new Utility.Dictionary<string, Base>();

        [CanBeNull] private IVariables m_ParentContext;

        /// <inheritdoc />
        /// <exception cref="ArgumentException" accessor="set">
        ///     The <paramref name="value" /> assembly differs from the
        ///     <see cref="Assembly" /> of this context or is a cyclic reference.
        /// </exception>
        public IVariables Parent {
            get { return m_ParentContext; }
            set {
                if (IsCyclicReferenceTo(value)) {
                    throw new ArgumentException("Tried to set a cyclic reference to the Parent of a Variables class.", nameof(value));
                }
                if (value != null && value.Assembly != Assembly) {
                    // Other assemblies are not allowed since we may or may not have the classes of that assembly in our assembly.
                    throw new ArgumentException("Cannot parent variable context from different assemblies!", nameof(value));
                }
                m_ParentContext = value;
            }
        }

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly { get; }

        /// <inheritdoc />
        /// <exception cref="SolVariableException">
        ///     All possible exceptions are catched and wrapped inside
        ///     SolVariableExceptions. (Some cases may directly throw a SolVariableException without wrapping anything.)
        /// </exception>
        public SolValue Get(string name)
        {
            Base valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                VarOperation operation = valueInfo.TryGetValue();
                if (operation.State != VariableState.Success) {
                    throw InternalHelper.CreateVariableGetException(name, operation.State, operation.Exception, SolSourceLocation.Native());
                }
                return operation.Value.NotNull();
            }
            if (Parent != null) {
                return Parent.Get(name);
            }
            throw new SolVariableException(SolSourceLocation.Native(), $"Cannot get the value of variable '{name}' - The variable has not been declared.");
        }

        /// <summary>
        ///     Tries to get the value assigned to the given name. The result is only
        ///     valid if the method returned <see cref="VariableState.Success" />.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="value"> A pointer to where the variable should be saved. </param>
        public VariableState TryGet(string name, out SolValue value)
        {
            Base valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                VarOperation operation = valueInfo.TryGetValue();
                value = operation.Value;
                return operation.State;
            }
            if (Parent != null) {
                return Parent.TryGet(name, out value);
            }
            value = null;
            return VariableState.FailedNotDeclared;
        }

        /// <inheritdoc />
        /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
        public void Declare(string name, SolType type)
        {
            if (m_Variables.ContainsKey(name)) {
                throw new SolVariableException(SolSourceLocation.Native(), "Tried to declare variable \"" + name + "\", but it already existed.");
            }
            m_Variables[name] = new Script(Assembly, name, type);
        }

        /// <inheritdoc />
        /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
        public void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
        {
            if (m_Variables.ContainsKey(name)) {
                throw new SolVariableException(SolSourceLocation.Native(), "Tried to declare variable \"" + name + "\", but it already existed.");
            }
            m_Variables[name] = new Native(Assembly, name, type, field, fieldReference);
        }

        /// <summary> Assigns annotations to a given variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="annotations"> The annotations to assign to the variable. </param>
        /// <exception cref="SolVariableException">Could not assign the annotations.</exception>
        public void AssignAnnotations(string name, params SolClass[] annotations)
        {
            Base valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                try {
                    valueInfo.AssignAnnotations(annotations);
                } catch (InvalidOperationException ex) {
                    throw new SolVariableException(SolSourceLocation.Native(), $"Cannot assign annotations to variable \"{name}\".", ex);
                }
            } else if (Parent != null) {
                Parent.AssignAnnotations(name, annotations);
            } else {
                throw new SolVariableException(SolSourceLocation.Native(), $"Cannot assign annotations to variable \"{name}\" - No variable with the given name has been declared.");
            }
        }

        /// <summary>
        ///     Safely assigns a value to this var context. This method will fail if
        ///     no variable of with this name is present of the type of the passed value is
        ///     not compatible with the assigned type of the variable.
        /// </summary>
        /// <param name="name"> The variable name </param>
        /// <param name="value"> The value </param>
        /// <exception cref="SolVariableException">Could not assign the value.</exception>
        public SolValue Assign(string name, SolValue value)
        {
            Base valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                VarOperation operation = valueInfo.TryAssignValue(value);
                if (operation.Value == null || operation.State != VariableState.Success) {
                    throw InternalHelper.CreateVariableSetException(name, operation.State, operation.Exception, SolSourceLocation.Native());
                }
                return operation.Value;
            }
            if (Parent != null) {
                return Parent.Assign(name, value);
            }
            throw new SolVariableException(SolSourceLocation.Native(), $"Cannot assign variable '{name}' - No variable with the given name has been declared.");
        }

        /// <summary> Is a variable with this name declared? </summary>
        public bool IsDeclared(string name)
        {
            if (m_Variables.ContainsKey(name)) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsDeclared(name);
            }
            return false;
        }

        /// <summary>
        ///     Is a variable with this name assigned(Also returns false if the
        ///     variable is not declared)?
        /// </summary>
        public bool IsAssigned(string name)
        {
            Base info;
            if (m_Variables.TryGetValue(name, out info)) {
                return info.IsAssigned();
            }
            if (Parent != null) {
                return Parent.IsAssigned(name);
            }
            return false;
        }

        #endregion

        /// <summary>
        ///     Checks if this <see cref="Variables" /> at some point is parented to <paramref name="variables" />(Or even is
        ///     <paramref name="variables" />).
        /// </summary>
        internal bool IsCyclicReferenceTo(IVariables variables)
        {
            Variables active = this;
            while (active != null) {
                if (active == variables) {
                    return true;
                }
                active = active.Parent as Variables;
            }
            return false;
        }

        /// <summary>
        ///     This method forcibly sets a value in this VarContext, overriding the
        ///     previously existing value(if any) regardless of its type and possible type
        ///     compatibility. It is discouraged the use this method in unexpected
        ///     circumstances since a variable seemingly randomly changing its type will
        ///     catch a lot of users offguard and may lead to bugs. This method can only
        ///     fail in the inital types are not compatible.
        /// </summary>
        /// <param name="name"> The variable name </param>
        /// <param name="value"> The variable value </param>
        /// <param name="type"> The variable type </param>
        /// <exception cref="SolVariableException">The type of the value and the field are not compatible.</exception>
        public void SetValue([NotNull] string name, [NotNull] SolValue value, SolType type)
        {
            if (!type.IsCompatible(Assembly, value.Type)) {
                throw new SolVariableException(SolSourceLocation.Native(), "Cannot set variable '" + name + "' of type '" + type + "' to a value of type '" + value.Type + "'. - The types are not compatible.");
            }
            try {
                m_Variables[name] = new Script(Assembly, name, type, value);
            } catch (ArgumentException ex) {
                throw new SolVariableException(SolSourceLocation.Native(), "Could not set variable \"" + name + "\".", ex);
            }
        }

        #region Nested type: Base

        /// <summary>
        ///     Base class for variable wrappers.
        /// </summary>
        private abstract class Base
        {
            /// <summary>
            ///     Creates the wrapper.
            /// </summary>
            /// <param name="assembly">The assembly of this variable.</param>
            /// <param name="name">The name of this variable.</param>
            /// <param name="type">The declared type.</param>
            protected Base(SolAssembly assembly, string name, SolType type)
            {
                Assembly = assembly;
                Name = name;
                Type = type;
            }

            // Lazy annotations.
            [CanBeNull] private Array<SolClass> l_annotations;

            /// <summary>
            ///     The assembly of this variable.
            /// </summary>
            public SolAssembly Assembly { get; }

            /// <summary>
            ///     The name of this variable.
            /// </summary>
            public string Name { get; }

            /// <summary>
            ///     The declared type of this variable.
            /// </summary>
            public SolType Type { get; }

            /// <summary>
            ///     The annotations on this variable.
            /// </summary>
            public IReadOnlyList<SolClass> Annotations => l_annotations ?? EmptyReadOnlyList<SolClass>.Value;

            /// <summary>
            ///     Does this variable wrapper have annotations? Only wrappers without annotations can be assigned new variables.
            /// </summary>
            public bool HasAnnotations => l_annotations != null;

            /// <summary>
            ///     Assigns annotations to this variable wrapper.
            /// </summary>
            /// <param name="annotations">The annotations.</param>
            /// <exception cref="InvalidOperationException">Annotations have already been assigned.</exception>
            /// <exception cref="InvalidOperationException">One or more classes are not annotations.</exception>
            /// <seealso cref="HasAnnotations" />
            public void AssignAnnotations(params SolClass[] annotations)
            {
                if (HasAnnotations) {
                    throw new InvalidOperationException("Annotations have already been assigned to variable \"" + Name + "\".");
                }
                foreach (SolClass annotation in annotations) {
                    if (annotation.TypeMode != SolTypeMode.Annotation) {
                        throw new InvalidOperationException("Tried to assign class \"" + annotation.Type + "\" to variable \"" + Name + "\" as annotations. The class is not an annotation.");
                    }
                }
                l_annotations = new Array<SolClass>(annotations);
            }

            /// <summary>
            ///     Checks if a value has been assigned to this variable.
            /// </summary>
            /// <returns>true if a value has been assigned, false if not.</returns>
            public abstract bool IsAssigned();

            /// <inheritdoc cref="TryGetValue" />
            protected abstract VarOperation TryGetValue_Impl();

            /// <inheritdoc cref="TryAssignValue" />
            protected abstract VarOperation TryAssignValue_Impl(SolValue value);

            /// <inheritdoc cref="AssignAnnotations(SolScript.Interpreter.Types.SolClass[])" />
            /// <exception cref="InvalidOperationException">Annotations have already been assigned.</exception>
            /// <exception cref="InvalidOperationException">One or more classes are not annotations.</exception>
            public void AssignAnnotations(IEnumerable<SolClass> annotations)
            {
                AssignAnnotations(annotations.ToArray());
            }

            /// <summary>
            ///     Tries to assign the given value to this wrapper.
            /// </summary>
            /// <param name="value">The value to assign.</param>
            /// <returns>Information about the success of the operation.</returns>
            public VarOperation TryAssignValue(SolValue value)
            {
                if (!Type.IsCompatible(Assembly, value.Type)) {
                    return new VarOperation(null, VariableState.FailedTypeMismatch,
                        new SolVariableException(SolSourceLocation.Native(), "The type \"" + Type + "\" of variable \"" + Name + "\" is not compatible with the given value of type \"" + value.Type + "\"."));
                }
                if (HasAnnotations) {
                    SolExecutionContext context = new SolExecutionContext(Assembly, $"Variable \"{Name}\" setter annotation resolver");
                    SolValue rawValue = value;
                    foreach (SolClass annotation in Annotations) {
                        try {
                            // Get Variable Annotation Function
                            SolClassDefinition.MetaFunctionLink link;
                            if (annotation.TryGetMetaFunction(SolMetaKey.__a_set_variable, out link)) {
                                SolTable table;
                                try {
                                    table = SolMetaKey.__a_set_variable.Cast(link.GetFunction(annotation).Call(context, value, rawValue)).NotNull();
                                } catch (SolRuntimeException ex) {
                                    return new VarOperation(null, VariableState.FailedRuntimeError, ex);
                                }
                                SolValue metaOverride;
                                if (table.TryGet(SolString.ValueOf("override"), out metaOverride)) {
                                    if (!Type.IsCompatible(Assembly, metaOverride.Type)) {
                                        return new VarOperation(null, VariableState.FailedTypeMismatch,
                                            new SolVariableException(SolSourceLocation.Native(), "The annotation \"" + annotation.Type + "\" tried to override the value set to the variable \"" + Name +
                                                                     "\" with a value of type \"" + metaOverride.Type + "\". The type is not compatible with the variable type \"" + Type + "\"."));
                                    }
                                    value = metaOverride;
                                }
                            }
                        } catch (SolVariableException ex) {
                            return new VarOperation(null, VariableState.FailedNativeException, ex);
                        }
                    }
                }
                return TryAssignValue_Impl(value);
            }

            /// <summary>
            ///     Tried to get the current value from this value info.
            /// </summary>
            /// <returns>Information about the success of the get operation.</returns>
            public VarOperation TryGetValue()
            {
                VarOperation result = TryGetValue_Impl();
                if (result.State != VariableState.Success) {
                    return result;
                }
                if (HasAnnotations) {
                    SolValue value = result.Value;
                    SolExecutionContext context = new SolExecutionContext(Assembly, $"Variable \"{Name}\" getter annotation resolver");
                    foreach (SolClass annotation in Annotations) {
                        // Get Variable Annotation Function
                        try {
                            SolClassDefinition.MetaFunctionLink link;
                            if (annotation.TryGetMetaFunction(SolMetaKey.__a_get_variable, out link)) {
                                SolTable table;
                                try {
                                    table = SolMetaKey.__a_get_variable.Cast(link.GetFunction(annotation).Call(context, value, result.Value)).NotNull();
                                } catch (SolRuntimeException ex) {
                                    return new VarOperation(null, VariableState.FailedRuntimeError, ex);
                                }
                                SolValue metaOverride;
                                if (table.TryGet(SolString.ValueOf("override"), out metaOverride)) {
                                    if (!Type.IsCompatible(Assembly, metaOverride.Type)) {
                                        return new VarOperation(value, VariableState.FailedTypeMismatch, null);
                                    }
                                    value = metaOverride;
                                }
                            }
                        } catch (SolVariableException ex) {
                            return new VarOperation(value, VariableState.FailedNativeException, ex);
                        }
                    }
                    if (!ReferenceEquals(value, result.Value)) {
                        return new VarOperation(value, VariableState.Success, null);
                    }
                }
                return result;
            }
        }

        #endregion

        #region Nested type: Native

        /// <summary>
        ///     This wrapper is used for native variables.
        /// </summary>
        private class Native : Base
        {
            /// <inheritdoc />
            public Native(SolAssembly assembly, string name, SolType type, FieldOrPropertyInfo field, DynamicReference reference) : base(assembly, name, type)
            {
                m_Field = field;
                m_Reference = reference;
            }

            private readonly FieldOrPropertyInfo m_Field;
            private readonly DynamicReference m_Reference;

            #region Overrides

            /// <inheritdoc />
            public override bool IsAssigned()
            {
                // Native FIELDS/PROPERTIES are always assigned.
                return true;
            }

            /// <inheritdoc />
            protected override VarOperation TryGetValue_Impl()
            {
                DynamicReference.GetState referenceState;
                object reference = m_Reference.GetReference(out referenceState);
                if (referenceState != DynamicReference.GetState.Retrieved) {
                    return new VarOperation(null, VariableState.FailedCouldNotResolveNativeReference, null);
                }
                object nativeValue;
                try {
                    nativeValue = m_Field.GetValue(reference);
                } catch (TargetException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (NotSupportedException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (FieldAccessException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (TargetParameterCountException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (ArgumentException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (MethodAccessException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (TargetInvocationException ex) {
                    // We only need the inner exception if an error occured in a property getter.
                    return new VarOperation(null, VariableState.FailedNativeException, ex.InnerException);
                }
                try {
                    SolValue value = SolMarshal.MarshalFromNative(Assembly, m_Field.DataType, nativeValue);
                    return new VarOperation(value, VariableState.Success, null);
                } catch (SolMarshallingException ex) {
                    return new VarOperation(null, VariableState.FailedTypeMismatch, ex);
                }
            }

            /// <inheritdoc />
            protected override VarOperation TryAssignValue_Impl(SolValue value)
            {
                DynamicReference.GetState referenceState;
                object reference = m_Reference.GetReference(out referenceState);
                if (referenceState != DynamicReference.GetState.Retrieved) {
                    return new VarOperation(null, VariableState.FailedCouldNotResolveNativeReference, null);
                }
                object nativeValue;
                try {
                    nativeValue = SolMarshal.MarshalFromSol(value, m_Field.DataType);
                } catch (SolMarshallingException ex) {
                    return new VarOperation(null, VariableState.FailedTypeMismatch, ex);
                }
                try {
                    m_Field.SetValue(reference, nativeValue);
                } catch (FieldAccessException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (TargetParameterCountException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (MethodAccessException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (TargetException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (ArgumentException ex) {
                    return new VarOperation(null, VariableState.FailedNativeException, ex);
                } catch (TargetInvocationException ex) {
                    // We only need the inner exception if an error occured in a property setter.
                    return new VarOperation(null, VariableState.FailedNativeException, ex.InnerException);
                }
                return new VarOperation(value, VariableState.Success, null);
            }

            #endregion
        }

        #endregion

        #region Nested type: Script

        /// <summary>
        ///     This wrapper is used for script variables.
        /// </summary>
        private class Script : Base
        {
            /// <inheritdoc />
            /// <exception cref="ArgumentException">The initial value is not compatible.</exception>
            public Script(SolAssembly assembly, string name, SolType type, SolValue start = null) : base(assembly, name, type)
            {
                if (start == null) {
                    return;
                }
                if (!Type.IsCompatible(Assembly, start.Type)) {
                    throw new ArgumentException("The initial value of type \"" + start.Type + "\" is not compatible with the type \"" + Type + "\" of field \"" + Name + "\".", nameof(start));
                }
                m_AssignedValue = start;
            }

            [CanBeNull] private SolValue m_AssignedValue;

            #region Overrides

            /// <inheritdoc />
            public override bool IsAssigned()
            {
                return m_AssignedValue != null;
            }

            /// <inheritdoc />
            protected override VarOperation TryGetValue_Impl()
            {
                return new VarOperation(m_AssignedValue, VariableState.Success, null);
            }

            /// <inheritdoc />
            protected override VarOperation TryAssignValue_Impl(SolValue value)
            {
                m_AssignedValue = value;
                return new VarOperation(m_AssignedValue, VariableState.Success, null);
            }

            #endregion
        }

        #endregion

        #region Nested type: VarOperation

        /// <summary>
        ///     Base class for value operation classes.
        /// </summary>
        private class VarOperation
        {
            /// <inheritdoc />
            public VarOperation([CanBeNull] SolValue value, VariableState state, [CanBeNull] Exception exception)
            {
#if DEBUG
                if ((state == VariableState.FailedNativeException || state == VariableState.FailedRuntimeError) && exception == null) {
                    throw new InvalidOperationException("(state == VariableState.FailedNativeException || state == VariableState.FailedRuntimeError) && exception == null");
                }
#endif
                Value = value;
                State = state;
                Exception = exception;
            }

            /// <summary>
            ///     The exception that triggered a possible failure.
            /// </summary>
            [CanBeNull] public readonly Exception Exception;

            /// <summary>
            ///     The state of the operation.
            /// </summary>
            public readonly VariableState State;

            /// <summary>
            ///     The <see cref="SolValue" /> produced by this operation.
            /// </summary>
            [CanBeNull] public readonly SolValue Value;
        }

        #endregion
    }
}