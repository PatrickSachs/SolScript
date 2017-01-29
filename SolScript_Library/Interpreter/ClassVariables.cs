using System;
using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter
{
    public abstract class ClassVariables : IVariables
    {
        protected ClassVariables(SolAssembly assembly)
        {
            Members = new ChunkVariables(assembly);
        }

        protected readonly ChunkVariables Members;

        public abstract SolClassDefinition Definition { get; }

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly => Definition.Assembly;

        public IVariables Parent {
            get { return GetParent(); }
            set { throw new NotSupportedException("Cannot change the parent of class variables."); }
        }

        /// <summary> Gets the value assigned to the given name. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <exception cref="SolVariableException"> The value has not been declared or assigned. </exception>
        public SolValue Get(string name)
        {
            SolValue value;
            VariableGet result = TryGet(name, out value);
            switch (result) {
                case VariableGet.Success:
                    return value.NotNull();
                case VariableGet.FailedNotDeclared:
                    throw new SolVariableException($"The variable \"{name}\" has not been declared.");
                case VariableGet.FailedNotAssigned:
                    throw new SolVariableException($"The variable \"{name}\" is declared, but not assigned.");
                case VariableGet.FailedNativeError:
                    throw new SolVariableException("A native error occured while trying to receive variable \"" + name + "\"!");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Tries to get the value assigned to the given name. The result is only
        ///     valid if the method returned <see cref="VariableGet.Success" />.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="value"> A pointer to where the variable should be saved. </param>
        public VariableGet TryGet(string name, out SolValue value)
        {
            switch (Members.TryGet(name, out value)) {
                case VariableGet.Success:
                    return VariableGet.Success;
                case VariableGet.FailedNotDeclared:
                    // Not declared variables can be functions or parent variables.
                    break;
                case VariableGet.FailedNotAssigned:
                    return VariableGet.FailedNotAssigned;
                case VariableGet.FailedNativeError:
                    return VariableGet.FailedNativeError;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            SolFunctionDefinition functionDefinition;
            if (Definition.TryGetFunction(name, false, out functionDefinition) && ValidateFunctionDefinition(functionDefinition)) {
                switch (functionDefinition.Chunk.ChunkType) {
                    case SolChunkWrapper.Type.ScriptChunk:
                        value = new SolScriptClassFunction(GetInstance(), functionDefinition);
                        break;
                    case SolChunkWrapper.Type.NativeMethod:
                        value = new SolNativeClassFunction(GetInstance(), functionDefinition);
                        break;
                    case SolChunkWrapper.Type.NativeConstructor:
                        value = new SolNativeClassConstructorFunction(GetInstance(), functionDefinition);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                SolDebug.WriteLine("Created function instance '" + name + " for class '" + GetInstance() + " :: " + value);
                Members.SetValue(name, value, new SolType(SolFunction.TYPE, false));
                return VariableGet.Success;
            }
            if (Parent != null) {
                return Parent.TryGet(name, out value);
            }
            return VariableGet.FailedNotDeclared;
        }

        /// <summary>
        ///     Declares the value with the given name and type and also provides
        ///     some (optional) annotations.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type">
        ///     The type of the variable. Only values assignable to this
        ///     type can be assigned.
        /// </param>
        /// <exception cref="SolVariableException">
        ///     A variable with this name has already
        ///     been declared.
        /// </exception>
        public void Declare(string name, SolType type)
        {
            Members.Declare(name, type);
        }

        /// <summary> Declares a native variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type">
        ///     The type of the variable. Only values assignable to this
        ///     type can be assigned.
        /// </param>
        /// <param name="field"> The native field handle.(todo: change this to sth else) </param>
        /// <param name="fieldReference"> The reference to the native object handle. </param>
        public void DeclareNative(string name, SolType type, InspectorField field, DynamicReference fieldReference)
        {
            Members.DeclareNative(name, type, field, fieldReference);
        }

        /// <summary> Assigns annotations to a given variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="annotations"> The annotations to assign to the variable. </param>
        public void AssignAnnotations(string name, params SolClass[] annotations)
        {
            // issue: annotations in class variables not handled
        }

        /// <summary> Assigns a value to the variable with the giv en name. </summary>
        /// <exception cref="SolVariableException">
        ///     Np variable with this name has been
        ///     decalred.
        /// </exception>
        /// <exception cref="SolVariableException"> The type does not match. </exception>
        public void Assign(string name, SolValue value)
        {
            if (Members.IsDeclared(name)) {
                Members.Assign(name, value);
            } else if (Definition.HasFunction(name, false)) {
                throw new SolVariableException("Cannot assign values to class function \"" + name + "\", they are immutable.");
            } else if (Parent != null) {
                Parent.Assign(name, value);
            } else {
                throw new SolVariableException("Cannot assign value to variable \"" + name + "\", not variable with this name has been declared.");
            }
        }

        /// <summary> Is a variable with this name declared? </summary>
        public bool IsDeclared(string name)
        {
            if (Members.IsDeclared(name)) {
                return true;
            }
            SolFunctionDefinition definition;
            if (Definition.TryGetFunction(name, false, out definition) && ValidateFunctionDefinition(definition)) {
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
            if (Members.IsAssigned(name)) {
                return true;
            }
            SolFunctionDefinition definition;
            if (Definition.TryGetFunction(name, false, out definition) && ValidateFunctionDefinition(definition)) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsAssigned(name);
            }
            return false;
        }

        #endregion

        protected abstract SolClass GetInstance();

        protected abstract bool ValidateFunctionDefinition(SolFunctionDefinition definition);

        [CanBeNull]
        protected abstract IVariables GetParent();
    }
}