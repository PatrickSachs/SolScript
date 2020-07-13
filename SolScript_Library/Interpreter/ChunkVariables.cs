using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public class ChunkVariables : IVariables {
        public ChunkVariables([NotNull] SolAssembly assembly) {
            Assembly = assembly;
        }

        private readonly Dictionary<string, ValueInfo> m_Variables = new Dictionary<string, ValueInfo>();

        [CanBeNull] private IVariables m_ParentContext;

        [CanBeNull]
        public IVariables Parent {
            get { return m_ParentContext; }
            set {
                if (m_ParentContext == this) {
                    throw new ArgumentException(
                        "Tried to set the parent context of a var context to itself. Var context does not support cyclic references!",
                        nameof(value));
                }
                if (value != null && m_ParentContext != null && (value.Assembly != m_ParentContext.Assembly)) {
                    throw new ArgumentException("Cannot parent variable context from different assemblies!", nameof(value));
                }
                m_ParentContext = value;
            }
        }

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly { get; }

        public SolValue Get([NotNull] string name) {
            // issue: throw exceptions
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
                return Parent.Get(name);
            }
            throw new SolVariableException($"Cannot get the value of variable '{name}' - The variable has not been declared.");
        }

        /// <summary> Tries to get the value assigned to the given name. The result is only
        ///     valid if the method returned <see cref="VariableGet.Success"/>. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="value"> A pointer to where the variable should be saved. </param>
        public VariableGet TryGet(string name, out SolValue value) {
            ValueInfo valueInfo;
            if (m_Variables.TryGetValue(name, out valueInfo))
            {
                ValueInfo.ValueGetInfo info;
                value = valueInfo.TryGetValue(Assembly, false, out info);
                switch (info)
                {
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

        public void Declare(string name, SolType type) {
            if (m_Variables.ContainsKey(name)) {
                throw new SolScriptInterpreterException(null, "Tried to declare variable \"" + name + "\", but it already existed.");
            }
            m_Variables[name] = new ValueInfo(name, null, type);
        }

        public void DeclareNative(string name, SolType type, [NotNull] InspectorField field, [NotNull] DynamicReference fieldReference) {
            if (m_Variables.ContainsKey(name)) {
                throw new SolScriptInterpreterException(null, "Tried to declare variable \"" + name + "\", but it already existed.");
            }
            m_Variables[name] = new ValueInfo(name, field, fieldReference, type);
        }

        /// <summary> Assigns annotations to a given variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="annotations"> The annotations to assign to the variable. </param>
        public void AssignAnnotations(string name, params SolClass[] annotations) {
            m_Variables[name].Annotations = annotations;
        }
        
        /// <summary> Safely assigns a value to this var context. This method will fail if
        ///     no variable of with this name is present of the type of the passed value is
        ///     not compatible with the assigned type of the variable. </summary>
        /// <param name="name"> The variable name </param>
        /// <param name="value"> The value </param>
        public void Assign([NotNull] string name, [NotNull] SolValue value) {
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
        public bool IsDeclared(string name) {
            if (m_Variables.ContainsKey(name)) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsDeclared(name);
            }
            return false;
        }

        /// <summary> Is a variable with this name assigned(Also returns false if the
        ///     variable is not declared)? </summary>
        public bool IsAssigned(string name) {
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

        /// <summary> This method forcibly sets a value in this VarContext, overriding the
        ///     previously existing value(if any) regardless of its type and possible type
        ///     compatibility. It is discouraged the use this method in unexpected
        ///     cirumstances since a variable seemingly randomly changing its type will
        ///     catch a lot of users offguard and may lead to bugs. This method can only
        ///     fail in the inital types are not compatible. </summary>
        /// <param name="name"> The variable name </param>
        /// <param name="value"> The variable value </param>
        /// <param name="type"> The variable type </param>
        /// <param name="local"> Should be value be set in the parent? (= local or global?) </param>
        public void SetValue([NotNull] string name, [NotNull] SolValue value, SolType type) {
            if (!type.IsCompatible(Assembly, value.Type)) {
                throw new SolScriptInterpreterException(null,
                    "Cannot set variable '" + name + "' of type '" + type + "' to a value of type '" + value.Type + "'. - The types are not compatible.");
            }
            m_Variables[name] = new ValueInfo(name, value, type);
        }

        #region Nested type: ValueInfo

        private class ValueInfo {
            #region ValueGetInfo enum

            public enum ValueGetInfo {
                Success,
                FailedCouldNotResolveClrRef,
                FailedNotAssigned
            }

            #endregion

            #region ValueSetInfo enum

            public enum ValueSetInfo {
                Success,
                FailedCouldNotResolveNativeRef,
                FailedTypeMismatch
            }

            #endregion

            public ValueInfo(string varName, [CanBeNull] SolValue value, SolType assignedType) {
                //VarName = varName;
                m_RawValue = value;
                AssignedType = assignedType;
            }

            public ValueInfo(string varName, [NotNull] InspectorField field, [NotNull] DynamicReference fieldReference, SolType assignedType) {
                //VarName = varName;
                Field = field;
                FieldReference = fieldReference;
                AssignedType = assignedType;
            }

            [CanBeNull] private readonly InspectorField Field;
            private readonly DynamicReference FieldReference;

            //public readonly string VarName;
            public SolClass[] Annotations;
            public SolType AssignedType;
            [CanBeNull] private SolValue m_RawValue;

            public bool IsAssigned() {
                if (Field != null) return false;
                return m_RawValue != null;
            }

            [CanBeNull]
            public SolValue TryGetValue(SolAssembly assembly, bool enforce, out ValueGetInfo info) {
                SolValue retVal;
                if (Field != null) {
                    DynamicReference.GetState referenceState;
                    object reference = FieldReference.GetReference(out referenceState);
                    if (referenceState != DynamicReference.GetState.Retrieved) {
                        //throw new SolScriptInterpreterException("error in get -> Tried to get the value of field " + name +
                        //                                       ". However the clr reference could not be retrieved. Did you fully initialize the holding class?");
                        info = ValueGetInfo.FailedCouldNotResolveClrRef;
                        return null;
                    }
                    //PropertyExecutionContext = context;
                    object clrValue = Field.GetValue(reference);
                    //PropertyExecutionContext = null;
                    retVal = SolMarshal.MarshalFromCSharp(assembly, Field.DataType, clrValue);
                } else {
                    retVal = m_RawValue;
                }
                if (!enforce && Annotations != null) {
                    foreach (SolClass annotation in Annotations) {
                        SolFunction metaFunc = annotation.AnnotationMeta.MetaGetVar;
                        // issue: rooted annotation context (is it even an issue or will this simply mean nested callstacks?)
                        SolTable metaRet = metaFunc?.Call(new SolExecutionContext(assembly), annotation, m_RawValue) as SolTable;
                        if (metaRet == null) continue;
                        SolValue metaOverride = metaRet.GetIfDefined("override");
                        if (metaOverride != null) {
                            retVal = metaOverride;
                        }
                    }
                }
                info = retVal != null ? ValueGetInfo.Success : ValueGetInfo.FailedNotAssigned;
                return retVal;
            }

            public void TryAssignValue(SolAssembly assembly, SolValue value, bool enforce, out ValueSetInfo info) {
                // todo: move type check in here, but that doesn't play nice as we don't know the name here.
                if (!enforce) {
                    if (!AssignedType.IsCompatible(assembly, value.Type)) {
                        info = ValueSetInfo.FailedTypeMismatch;
                        return;
                    }
                    if (Annotations != null) {
                        SolValue rawValue = value;
                        foreach (SolClass annotation in Annotations) {
                            SolFunction metaFunc = annotation.AnnotationMeta.MetaSetVar;
                            // issue: rooted annotation context (maby this isnt an issue though - nested callstacks through exception checking? damn i wish i had checked exceptions!)
                            SolTable metaRet = metaFunc?.Call(new SolExecutionContext(assembly), annotation, rawValue) as SolTable;
                            if (metaRet == null) continue;
                            SolValue metaOverride = metaRet.GetIfDefined("override");
                            if (metaOverride != null) {
                                value = metaOverride;
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
                    m_RawValue = value;
                    info = ValueSetInfo.Success;
                }
            }
        }

        #endregion
    }
}