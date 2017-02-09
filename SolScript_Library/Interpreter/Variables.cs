using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public class Variables : IVariables
    {
        public Variables([NotNull] SolAssembly assembly)
        {
            Assembly = assembly;
        }

        private readonly Dictionary<string, ValueInfo> m_Variables = new Dictionary<string, ValueInfo>();

        [CanBeNull] private IVariables m_ParentContext;

        #region IVariables Members

        /// <inheritdoc />
        /// <exception cref="ArgumentException" accessor="set">
        ///     The <paramref name="value" /> assembly differs from the
        ///     <see cref="Assembly" /> of this context or is a cyclic referenece
        /// </exception>
        public IVariables Parent {
            get { return m_ParentContext; }
            set {
                if (IsCyclicReferenceTo(value)) {
                    throw new ArgumentException("Tried to set a cyclic reference to the Parent of a Variables class.", nameof(value));
                }
                if (value != null && value.Assembly != Assembly) {
                    // todo: really? why should we not be able to parent contexts from different assemblies?
                    throw new ArgumentException("Cannot parent variable context from different assemblies!", nameof(value));
                }
                m_ParentContext = value;
            }
        }

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly { get; }

        /// <inheritdoc />
        /// <exception cref="SolVariableException">
        ///     All possible exceptions are catched and wrapped inside
        ///     SolVariableExceptions. (Some cases may directly throw a SolVariableException without wrapping anything.)
        /// </exception>
        public SolValue Get(string name)
        {
            ValueInfo valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                ValueInfo.GetOperation operation = valueInfo.TryGetValue(Assembly, false);
                if (operation.State != VariableState.Success) {
                    throw InternalHelper.CreateVariableGetException(name, operation.State, operation.Exception);
                }
                return operation.Value.NotNull();
            }
            if (Parent != null) {
                return Parent.Get(name);
            }
            throw new SolVariableException($"Cannot get the value of variable '{name}' - The variable has not been declared.");
        }

        /// <summary>
        ///     Tries to get the value assigned to the given name. The result is only
        ///     valid if the method returned <see cref="VariableState.Success" />.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="value"> A pointer to where the variable should be saved. </param>
        public VariableState TryGet(string name, out SolValue value)
        {
            ValueInfo valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                ValueInfo.GetOperation operation = valueInfo.TryGetValue(Assembly, false);
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
                throw new SolVariableException("Tried to declare variable \"" + name + "\", but it already existed.");
            }
            m_Variables[name] = new ValueInfo(name, null, type);
        }

        /// <inheritdoc />
        /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
        public void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
        {
            if (m_Variables.ContainsKey(name)) {
                throw new SolVariableException("Tried to declare variable \"" + name + "\", but it already existed.");
            }
            m_Variables[name] = new ValueInfo(name, field, fieldReference, type);
        }

        /// <summary> Assigns annotations to a given variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="annotations"> The annotations to assign to the variable. </param>
        public void AssignAnnotations(string name, params SolClass[] annotations)
        {
            m_Variables[name].Annotations = annotations;
        }

        /// <summary>
        ///     Safely assigns a value to this var context. This method will fail if
        ///     no variable of with this name is present of the type of the passed value is
        ///     not compatible with the assigned type of the variable.
        /// </summary>
        /// <param name="name"> The variable name </param>
        /// <param name="value"> The value </param>
        /// <exception cref="SolVariableException">Could not assign the value.</exception>
        public void Assign(string name, SolValue value)
        {
            ValueInfo valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                ValueInfo.SetOperation operation = valueInfo.TryAssignValue(Assembly, value);
                if (operation.State != VariableState.Success) {
                    throw InternalHelper.CreateVariableSetException(name, operation.State, operation.Exception);
                }
            } else if (Parent != null) {
                Parent.Assign(name, value);
            } else {
                throw new SolVariableException($"Cannot assign variable '{name}' - No variable with the given name has been declared.");
            }
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
            ValueInfo info;
            if (m_Variables.TryGetValue(name, out info)) {
                return info.IsAssigned();
            }
            if (Parent != null) {
                return Parent.IsAssigned(name);
            }
            return false;
        }

        #endregion

        internal bool IsCyclicReferenceTo(IVariables variables)
        {
            IVariables active = this;
            while (active != null) {
                if (active == variables) {
                    return true;
                }
                active = active.Parent;
            }
            return false;
        }

        /// <summary>
        ///     Directly indexes the variables and returns the raw value of the value. If you aren't 100% sure of what this method
        ///     does and what the possible implications are - don't use it.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Falls through from the underlying dictionary.</exception>
        /// <remarks>Warning: Only works with script variables. Native accessors are IGNORED.</remarks>
        internal SolValue DirectRawGet(string key)
        {
            return m_Variables[key].RawValue;
        }

        /// <summary>
        ///     This method forcibly sets a value in this VarContext, overriding the
        ///     previously existing value(if any) regardless of its type and possible type
        ///     compatibility. It is discouraged the use this method in unexpected
        ///     cirumstances since a variable seemingly randomly changing its type will
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
                throw new SolVariableException("Cannot set variable '" + name + "' of type '" + type + "' to a value of type '" + value.Type + "'. - The types are not compatible.");
            }
            m_Variables[name] = new ValueInfo(name, value, type);
        }

        #region Nested type: ValueInfo

        private class ValueInfo
        {
            public ValueInfo(string name, [CanBeNull] SolValue value, SolType assignedType)
            {
                Name = name;
                RawValue = value;
                AssignedType = assignedType;
            }

            public ValueInfo(string name, [NotNull] FieldOrPropertyInfo field, [NotNull] DynamicReference fieldReference, SolType assignedType)
            {
                Name = name;
                m_Field = field;
                m_FieldReference = fieldReference;
                AssignedType = assignedType;
            }

            /// <summary>
            ///     The native backing field of this field.
            /// </summary>
            [CanBeNull] private readonly FieldOrPropertyInfo m_Field;

            /// <summary>
            ///     The native reference of this field. Only valid if <see cref="m_Field" /> is not null.
            /// </summary>
            [CanBeNull] private readonly DynamicReference m_FieldReference;

            /// <summary>
            ///     The name of this field.
            /// </summary>
            public readonly string Name;

            /// <summary>
            ///     All annotations of this field. Can be null or empty if no annotations are assigned.
            /// </summary>
            [CanBeNull] public SolClass[] Annotations;

            /// <summary>
            ///     The type of this field.
            /// </summary>
            public SolType AssignedType;

            /// <summary>
            ///     The raw <see cref="SolValue" />. This value may very well be null in case the field is actually a native field.
            /// </summary>
            [CanBeNull] internal SolValue RawValue;

            public bool IsAssigned()
            {
                if (m_Field != null) {
                    // Native Fields are always assigned.
                    return true;
                }
                return RawValue != null;
            }

            /// <summary>
            ///     Tried to get the current value from this value info.
            /// </summary>
            /// <param name="assembly">The assembly to use.</param>
            /// <param name="enforce">Should this be enfored? (what does that event mean?)</param>
            /// <returns>Information about the success of the get operation.</returns>
            public GetOperation TryGetValue(SolAssembly assembly, bool enforce)
            {
                SolValue value;
                if (m_Field != null) {
                    DynamicReference.GetState referenceState;
                    object reference = m_FieldReference.NotNull().GetReference(out referenceState);
                    if (referenceState != DynamicReference.GetState.Retrieved) {
                        return new GetOperation(null, VariableState.FailedCouldNotResolveNativeReference, null);
                    }
                    object clrValue;
                    try {
                        clrValue = m_Field.GetValue(reference);
                    } catch (Exception ex) {
                        if (ex is TargetException || ex is TargetParameterCountException || ex is MethodAccessException || ex is TargetInvocationException
                            || ex is FieldAccessException || ex is NotSupportedException || ex is ArgumentException) {
                            return new GetOperation(null, VariableState.FailedNativeException, ex);
                        }
                        // ReSharper disable once ExceptionNotDocumented
                        // It shoudn't ever throw anything else, but let's make sure, just in case.
                        throw;
                    }
                    try {
                        value = SolMarshal.MarshalFromNative(assembly, m_Field.DataType, clrValue);
                    } catch (SolMarshallingException ex) {
                        return new GetOperation(null, VariableState.FailedNativeException, ex);
                    }
                } else {
                    value = RawValue;
                }
                if (!enforce && Annotations != null) {
                    SolValue rawValue = value;
                    SolExecutionContext context = new SolExecutionContext(assembly, $"\"{Name}\" field-getter");
                    foreach (SolClass annotation in Annotations) {
                        // Get Variable Annotation Function
                        SolClassDefinition.MetaFunctionLink link;
                        if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationGetVariable, out link)) {
                            SolTable table;
                            try {
                                table = SolMetaKey.AnnotationGetVariable.Cast(link.GetFunction(annotation).Call(context, value, rawValue));
                            } catch (SolRuntimeException ex) {
                                return new GetOperation(null, VariableState.FailedRuntimeError, ex);
                            }
                            SolValue metaOverride;
                            if (table.TryGet("override", out metaOverride)) {
                                if (!AssignedType.IsCompatible(assembly, metaOverride.Type)) {
                                    return new GetOperation(value, VariableState.FailedTypeMismatch, null);
                                }
                                value = metaOverride;
                            }
                        }
                    }
                }
                return new GetOperation(value, value != null ? VariableState.Success : VariableState.FailedNotAssigned, null);
            }

            /// <summary>
            ///     Tries to assign the given value to this field. Keep in mind that even though this method is labeled as "Try" it can
            ///     still throw an exception if e.g. an annotation raised an error.
            /// </summary>
            /// <param name="assembly">The assembly to use for type lookups.</param>
            /// <param name="value">The value to aassign.</param>
            /// <param name="ignoreAnnotations">Should annotations be ignored?</param>
            /// <returns>Information about the success of the operation.</returns>
            public SetOperation TryAssignValue(SolAssembly assembly, SolValue value, bool ignoreAnnotations = false)
            {
                if (!AssignedType.IsCompatible(assembly, value.Type)) {
                    return new SetOperation(VariableState.FailedTypeMismatch,
                        new SolVariableException("The field type \"" + AssignedType + "\" of field \"" + Name + "\" is not compatible with the given value of type \"" + value.Type + "\"."));
                }
                if (!ignoreAnnotations && Annotations != null) {
                    SolExecutionContext context = new SolExecutionContext(assembly, $"\"{Name}\" field-setter");
                    SolValue rawValue = value;
                    foreach (SolClass annotation in Annotations) {
                        // Get Variable Annotation Function
                        SolClassDefinition.MetaFunctionLink link;
                        if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationSetVariable, out link)) {
                            SolTable table;
                            try {
                                table = SolMetaKey.AnnotationSetVariable.Cast(link.GetFunction(annotation).Call(context, value, rawValue));
                            } catch (SolRuntimeException ex) {
                                return new SetOperation(VariableState.FailedRuntimeError, ex);
                            }
                            SolValue metaOverride;
                            if (table.TryGet("override", out metaOverride)) {
                                if (!AssignedType.IsCompatible(assembly, metaOverride.Type)) {
                                    return new SetOperation(VariableState.FailedTypeMismatch,
                                        new SolVariableException("The annotation \"" + annotation.Type + "\" tried to override the value set to the field \"" + Name + "\" with a value of type \"" +
                                                                 metaOverride.Type +
                                                                 "\". Is type is not compatible with the field type \"" + AssignedType + "\"."));
                                }
                                value = metaOverride;
                            }
                        }
                    }
                }
                if (m_Field != null) {
                    DynamicReference.GetState referenceState;
                    object reference = m_FieldReference.NotNull().GetReference(out referenceState);
                    if (referenceState != DynamicReference.GetState.Retrieved) {
                        return new SetOperation(VariableState.FailedCouldNotResolveNativeReference, null);
                    }
                    object clrValue = SolMarshal.MarshalFromSol(value, m_Field.DataType);
                    try {
                        m_Field.SetValue(reference, clrValue);
                    } catch (Exception ex) {
                        if (ex is TargetException || ex is TargetParameterCountException || ex is MethodAccessException || ex is TargetInvocationException
                            || ex is FieldAccessException || ex is NotSupportedException || ex is ArgumentException) {
                            return new SetOperation(VariableState.FailedNativeException, ex);
                        }
                        // ReSharper disable once ExceptionNotDocumented
                        // It shoudn't ever throw anything else, but let's make sure, just in case.
                        throw;
                    }
                    return new SetOperation(VariableState.Success, null);
                }
                RawValue = value;
                return new SetOperation(VariableState.Success, null);
            }

            #region Nested type: BaseOperation

            /// <summary>
            ///     Base class for value operation classes.
            /// </summary>
            public abstract class BaseOperation
            {
                /// <inheritdoc />
                protected BaseOperation(VariableState state, [CanBeNull] Exception exception)
                {
#if DEBUG
                    if ((state == VariableState.FailedNativeException || state == VariableState.FailedRuntimeError) && exception == null) {
                        throw new InvalidOperationException("(state == VariableState.FailedNativeException || state == VariableState.FailedRuntimeError) && exception == null");
                    }
#endif
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
            }

            #endregion

            #region Nested type: GetOperation

            /// <summary>
            ///     Contains information about the success of a value get operation.
            /// </summary>
            public sealed class GetOperation : BaseOperation
            {
                /// <summary>
                ///     Creates a new value get info.
                /// </summary>
                /// <param name="state">The state the operation resulted in.</param>
                /// <param name="value">The value the operation created.</param>
                /// <param name="exception">The exception that lead to a possible failure.</param>
                public GetOperation([CanBeNull] SolValue value, VariableState state, [CanBeNull] Exception exception) : base(state, exception)
                {
                    Value = value;
                }

                /// <summary>
                ///     The <see cref="SolValue" /> produced by this operation.
                /// </summary>
                [CanBeNull] public readonly SolValue Value;
            }

            #endregion

            #region Nested type: SetOperation

            /// <summary>
            ///     Contains information about the success of a value set operation.
            /// </summary>
            public class SetOperation : BaseOperation
            {
                /// <inheritdoc />
                public SetOperation(VariableState state, [CanBeNull] Exception exception) : base(state, exception) {}
            }

            #endregion
        }

        #endregion
    }
}