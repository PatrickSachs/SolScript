using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public class ChunkVariables : IVariables
    {
        public ChunkVariables([NotNull] SolAssembly assembly)
        {
            Assembly = assembly;
        }

        private readonly Dictionary<string, ValueInfo> m_Variables = new Dictionary<string, ValueInfo>();

        [CanBeNull] private IVariables m_ParentContext;

        #region IVariables Members

        /// <inheritdoc />
        /// <exception cref="ArgumentException" accessor="set">The <paramref name="value"/> assembly differs from the <see cref="Assembly"/> of this context or is a cycrlic referenece</exception>
        public IVariables Parent {
            get { return m_ParentContext; }
            set {
                if (value == this) {
                    throw new ArgumentException(
                        "Tried to set the parent context of a var context to itself. Var context does not support cyclic references!",
                        nameof(value));
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
        /// <exception cref="SolVariableException">Failed to get the variable.</exception>
        public SolValue Get(string name)
        {
            ValueInfo valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                ValueInfo.ValueGetInfo info;
                SolValue value = valueInfo.TryGetValue(Assembly, false, out info);
                switch (info) {
                    case ValueInfo.ValueGetInfo.Success:
                        break;
                    case ValueInfo.ValueGetInfo.FailedCouldNotResolveClrRef:
                        throw new SolVariableException($"Cannot get the value of variable '{name}' - The underlying native object could not be resolved.");
                    case ValueInfo.ValueGetInfo.FailedNotAssigned:
                        throw new SolVariableException($"Cannot get the value of variable '{name}' - The variable has not been assigned.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return value.NotNull();
            }
            if (Parent != null) {
                // <bubble>SolVariableException</bubble>
                return Parent.Get(name);
            }
            throw new SolVariableException($"Cannot get the value of variable '{name}' - The variable has not been declared.");
        }

        /// <summary>
        ///     Tries to get the value assigned to the given name. The result is only
        ///     valid if the method returned <see cref="VariableGet.Success" />.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="value"> A pointer to where the variable should be saved. </param>
        public VariableGet TryGet(string name, out SolValue value)
        {
            ValueInfo valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                ValueInfo.ValueGetInfo info;
                value = valueInfo.TryGetValue(Assembly, false, out info);
                switch (info) {
                    case ValueInfo.ValueGetInfo.Success:
                        return VariableGet.Success;
                    case ValueInfo.ValueGetInfo.FailedCouldNotResolveClrRef:
                        return VariableGet.FailedNativeError;
                    case ValueInfo.ValueGetInfo.FailedNotAssigned:
                        return VariableGet.FailedNotAssigned;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            if (Parent != null) {
                return Parent.TryGet(name, out value);
            }
            value = null;
            return VariableGet.FailedNotDeclared;
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
        public void Assign([NotNull] string name, [NotNull] SolValue value)
        {
            ValueInfo valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo)) {
                ValueInfo.ValueSetInfo info;
                valueInfo.TryAssignValue(Assembly, value, false, out info);
                switch (info) {
                    case ValueInfo.ValueSetInfo.Success:
                        break;
                    case ValueInfo.ValueSetInfo.FailedTypeMismatch:
                        throw new SolVariableException($"Cannot assign variable '{name}' of type '{valueInfo.AssignedType}' to a value of type '{value.Type}' - The types are not compatible.");
                    case ValueInfo.ValueSetInfo.FailedCouldNotResolveNativeRef:
                        throw new SolVariableException($"Cannot assign variable '{name}' - The underyling native object could not be resolved.");
                    default:
                        throw new ArgumentOutOfRangeException();
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
            #region ValueGetInfo enum

            public enum ValueGetInfo
            {
                Success,
                FailedCouldNotResolveClrRef,
                FailedNotAssigned
            }

            #endregion

            #region ValueSetInfo enum

            public enum ValueSetInfo
            {
                Success,
                FailedCouldNotResolveNativeRef,
                FailedTypeMismatch
            }

            #endregion

            public ValueInfo(string name, [CanBeNull] SolValue value, SolType assignedType)
            {
                Name = name;
                RawValue = value;
                AssignedType = assignedType;
            }

            public ValueInfo(string name, [NotNull] FieldOrPropertyInfo field, [NotNull] DynamicReference fieldReference, SolType assignedType)
            {
                Name = name;
                Field = field;
                FieldReference = fieldReference;
                AssignedType = assignedType;
            }

            [CanBeNull] private readonly FieldOrPropertyInfo Field;
            private readonly DynamicReference FieldReference;

            public readonly string Name;
            public SolClass[] Annotations;
            public SolType AssignedType;
            [CanBeNull] internal SolValue RawValue;

            public bool IsAssigned()
            {
                if (Field != null) {
                    return false;
                }
                return RawValue != null;
            }

            /// <summary>
            ///     Tried to get the current value from this value info.
            /// </summary>
            /// <param name="assembly">The assembly to use.</param>
            /// <param name="enforce">Should this be enfored? (what does that event mean?)</param>
            /// <param name="info">THis out value determines the success of the operation.</param>
            /// <returns>The assigned value. Note that this is only reliable if the method Succeeded.</returns>
            /// <exception cref="SolVariableException">
            ///     All other possible exceptions are catched and wrapped inside
            ///     SolVariableExceptions. (Some cases may directly throw a SolVariableException without wrapping anything.)
            /// </exception>
            [CanBeNull]
            public SolValue TryGetValue(SolAssembly assembly, bool enforce, out ValueGetInfo info)
            {
                // todo: what does enforced even mean and is it even required?
                // todo: shouldn't exceptions be catched and returned in an info object? this method is called TRY after all.
                SolValue value;
                if (Field != null) {
                    DynamicReference.GetState referenceState;
                    object reference = FieldReference.GetReference(out referenceState);
                    if (referenceState != DynamicReference.GetState.Retrieved) {
                        info = ValueGetInfo.FailedCouldNotResolveClrRef;
                        return null;
                    }
                    object clrValue = Field.GetValue(reference);
                    try {
                        value = SolMarshal.MarshalFromCSharp(assembly, Field.DataType, clrValue);
                    } catch (SolMarshallingException ex) {
                        throw new SolVariableException($"Failed to marshal native variable object of type \"{clrValue?.GetType().Name ?? "null"}\" to Sol!", ex);
                    }
                } else {
                    value = RawValue;
                }
                if (!enforce && Annotations != null)
                {
                    SolValue rawValue = value;
                    SolExecutionContext context = new SolExecutionContext(assembly, $"Get \"{Name}\" annotation resolver context");
                    foreach (SolClass annotation in Annotations) {
                        // Get Variable Annotation Function
                        SolClassDefinition.MetaFunctionLink link;
                        if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationGetVariable, out link)) {
                            SolTable table;
                            try {
                                table = SolMetaKey.AnnotationGetVariable.Cast(link.GetFunction(annotation).Call(context, value, rawValue));
                            } catch (SolRuntimeException ex) {
                                // todo: properly deal with sol script exception unwrapping.
                                throw new SolVariableException("An error occured while resolving an annotation of type \"" + annotation.Type + "\" on key \"" + Name + "\" - " + ex.Message, ex);
                            }
                            SolValue metaOverride;
                            if (table.TryGet("override", out metaOverride)) {
                                if (!AssignedType.IsCompatible(assembly, metaOverride.Type)) {
                                    throw new SolVariableException("An error occured while resolving an annotation of type \"" + annotation.Type + "\" on key \"" + Name +
                                                                   "\" - The annotation tried to override the return value of the field with a value of type \"" + metaOverride.Type +
                                                                   "\". This type is not compatible with the type \"" + AssignedType + "\" assigned to the field.");
                                }
                                value = metaOverride;
                            }
                        }
                    }
                }
                info = value != null ? ValueGetInfo.Success : ValueGetInfo.FailedNotAssigned;
                return value;
            }

            public void TryAssignValue(SolAssembly assembly, SolValue value, bool enforce, out ValueSetInfo info)
            {
                // todo: move type check in here, but that doesn't play nice as we don't know the name here.
                // todo: wrap exceptions etc
                if (!enforce) {
                    if (!AssignedType.IsCompatible(assembly, value.Type)) {
                        info = ValueSetInfo.FailedTypeMismatch;
                        return;
                    }
                    if (Annotations != null) {
                        SolExecutionContext context = new SolExecutionContext(assembly, $"Set \"{Name}\" annotation resolver context");
                        SolValue rawValue = value;
                        foreach (SolClass annotation in Annotations)
                        {
                            // Get Variable Annotation Function
                            SolClassDefinition.MetaFunctionLink link;
                            if (annotation.TryGetMetaFunction(SolMetaKey.AnnotationSetVariable, out link))
                            {
                                SolTable table;
                                try
                                {
                                    table = SolMetaKey.AnnotationSetVariable.Cast(link.GetFunction(annotation).Call(context, value, rawValue));
                                }
                                catch (SolRuntimeException ex)
                                {
                                    // todo: properly deal with sol script exception unwrapping.
                                    throw new SolVariableException("An error occured while resolving an annotation of type \"" + annotation.Type + "\" on key \"" + Name + "\" - " + ex.Message, ex);
                                }
                                SolValue metaOverride;
                                if (table.TryGet("override", out metaOverride))
                                {
                                    if (!AssignedType.IsCompatible(assembly, metaOverride.Type))
                                    {
                                        throw new SolVariableException("An error occured while resolving an annotation of type \"" + annotation.Type + "\" on key \"" + Name +
                                                                       "\" - The annotation tried to override the value set to the field with a value of type \"" + metaOverride.Type +
                                                                       "\". This type is not compatible with the type \"" + AssignedType + "\" assigned to the field.");
                                    }
                                    value = metaOverride;
                                }
                            }
                        }
                    }
                }
                if (Field != null) {
                    DynamicReference.GetState referenceState;
                    object reference = FieldReference.GetReference(out referenceState);
                    if (referenceState != DynamicReference.GetState.Retrieved) {
                        info = ValueSetInfo.FailedCouldNotResolveNativeRef;
                        return;
                    }
                    object clrValue = SolMarshal.MarshalFromSol(value, Field.DataType);
                    Field.SetValue(reference, clrValue);
                    info = ValueSetInfo.Success;
                } else {
                    RawValue = value;
                    info = ValueSetInfo.Success;
                }
            }
        }

        #endregion
    }
}