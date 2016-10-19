using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public class VarContext {
        private readonly Dictionary<string, ValueInfo> m_Variables = new Dictionary<string, ValueInfo>();

        public VarContext() {
        }

        [CanBeNull]
        public VarContext ParentContext {
            get {
                return m_ParentContext;
            }
            set {
                if (m_ParentContext == this) {
                    throw new ArgumentException("Tried to set the parent context of a var context to itself. Var context does not support cyclic references!", nameof(value));
                }
                m_ParentContext = value;
            }
        }

        [CanBeNull] private VarContext m_ParentContext;

        [CanBeNull]
        private ValueInfo GetValueInfo([NotNull] string name) {
            ValueInfo value;
            if (m_Variables.TryGetValue(name, out value))
            {
                return value;
            }
            if (ParentContext != null)
            {
                value = ParentContext.GetValueInfo(name);
                return value;
            }
            return null;
        }
        
        [CanBeNull]
        public SolValue GetValue_X([NotNull] string name) {
            ValueInfo valueInfo = GetValueInfo(name);
            if (valueInfo == null) {
                return null;
            }
            if (valueInfo.Field != null)
            {
                DynamicReference.ReferenceState referenceState;
                object reference = valueInfo.FieldReference.NotNull().GetReference(out referenceState);
                if (referenceState != DynamicReference.ReferenceState.Retrieved)
                {
                    throw new SolScriptInterpreterException("error in get -> Tried to get the value of field " + name + ". However the clr reference could not be retrieved. Did you fully initialize the holding class?");
                }
                return SolMarshal.MarshalFrom(valueInfo.Field.DataType, valueInfo.Field.GetValue(reference));
            }
            return valueInfo.Value;
        }

        /// <summary> This method forcibly sets a value in this VarContext, overriding the
        ///     previously existing value(if any) regardless of its type and possible type
        ///     compatibility. It is discouraged the use this method in unexpected
        ///     cirumstances since a variable seemingly randomly changing its type will
        ///     catch a lot of users offguard and may lead to bugs. This method can only
        ///     fail in the inital types are not compatible. </summary>
        /// <param name="name"> The variable name </param>
        /// <param name="value"> The variable value </param>
        /// <param name="type"> The variable type </param>
        /// <param name="local"> Should be value be set in the parent? (= local or
        ///     global?) </param>
        public void SetValue([NotNull] string name, [NotNull] SolValue value, SolType type, bool local) {
            if (!type.IsCompatible(value.Type))
            {
                throw new SolScriptInterpreterException("error in set -> setting " + (local ? "local " : "") +
                                                        name + ". " + type + " is not compatible with " +
                                                        value.Type);
            }
            m_Variables[name] = new ValueInfo(value, type, local);
        }

        public void DeclareVariable(string name, [CanBeNull] SolValue value, SolType type, bool local)
        {
            if (m_Variables.ContainsKey(name))
            {
                throw new SolScriptInterpreterException();
            }
            m_Variables[name] = new ValueInfo(value, type, local);
        }
        public void DeclareVariable(string name, [NotNull] InspectorField field, [NotNull] DynamicReference fieldReference, SolType type, bool local)
        {
            if (m_Variables.ContainsKey(name))
            {
                throw new SolScriptInterpreterException();
            }
            m_Variables[name] = new ValueInfo(field, fieldReference, type, local);
        }

        /// <summary> Safely assigns a value to this var context. This method will fail if
        ///     no variable of with this name is present of the type of the passed value is
        ///     not compatible with the assigned type of the variable. </summary>
        /// <param name="name"> The variable name </param>
        /// <param name="value"> The value </param>
        public void AssignValue([NotNull] string name, [NotNull] SolValue value) {
            ValueInfo valueInfo = GetValueInfo(name);
            if (valueInfo == null) {
                throw new SolScriptInterpreterException("error in assign -> no variable with name " + name +
                                                        " to assign " + value + " to.");
            }
            if (!valueInfo.AssignedType.IsCompatible(value.Type)) {
                throw new SolScriptInterpreterException("error in assign -> type " + value.Type + " of " + name +
                                                        " is not compatible with assigned type " +
                                                        valueInfo.AssignedType);
            }
            if (valueInfo.Field != null) {
                DynamicReference.ReferenceState referenceState;
                object reference = valueInfo.FieldReference.NotNull().GetReference(out referenceState);
                if (referenceState != DynamicReference.ReferenceState.Retrieved) {
                    throw new SolScriptInterpreterException("error in assign -> Tried to set value " + value + " to field " + name + ". However the clr reference could not be retrieved. Did you fully initialize the holding class?");
                }
                valueInfo.Field.SetValue(reference, SolMarshal.Marshal(value, valueInfo.Field.DataType));
            } else {
                valueInfo.Value = value;           
            }
        }

        /// <summary> This method combines assignment and declaration in one and is the
        ///     most commonly used way to declare variables in the interpreter
        ///     implementation. In order to use this method one must first understand the
        ///     ordering:
        ///     <list type="number">
        ///         <item>
        ///             <term> Try Assignment - </term>
        ///             <description> If a variable with the given name already exists an
        ///                 assignment is attempted. If this assignment fails an exception
        ///                 is raised. If no variable with the given name exists nothing
        ///                 happens. The <paramref name="inParent"/> and the
        ///                 <paramref name="type"/> parameters are ignored in this step. </description>
        ///         </item>
        ///         <item>
        ///             <term> Set - </term>
        ///             <description> Otherwise the value will simply be set </description>
        ///         </item>
        ///     </list>
        ///     This means that this method is not failsafe and may very well throw an
        ///     exception. The only differenc eot calling Assign and then Set individually
        ///     is, that Assign will throw if no variable with the given name exists. </summary>
        /// <param name="name"> </param>
        /// <param name="value"> </param>
        /// <param name="type"> </param>
        /// <param name="local"> </param>
        public void AssignOrSetValue_X([NotNull] string name, [NotNull] SolValue value, SolType type, bool local) {
            ValueInfo valueInfo = GetValueInfo(name);
            if (valueInfo != null) {
                if (!valueInfo.AssignedType.IsCompatible(value.Type)) {
                    throw new SolScriptInterpreterException("error in assign -> type " + value.Type + " of " + name +
                                                            " is not compatible with assigned type " +
                                                            valueInfo.AssignedType);
                }
                valueInfo.Value = value;
            }
            SetValue(name, value, type, local);
        }

        #region Nested type: ValueInfo

        private class ValueInfo
        {
            public ValueInfo([CanBeNull] SolValue value, SolType assignedType, bool local)
            {
                Value = value;
                AssignedType = assignedType;
                Local = local;
            }
            public ValueInfo([NotNull] InspectorField field,[NotNull]DynamicReference fieldReference, SolType assignedType, bool local)
            {
                Field = field;
                FieldReference = fieldReference;
                AssignedType = assignedType;
                Local = local;
            }

            public bool Local;
            public SolType AssignedType;

            [CanBeNull] public DynamicReference FieldReference;
            [CanBeNull] public InspectorField Field;
            [CanBeNull] public SolValue Value;
        }

        #endregion
    }
}