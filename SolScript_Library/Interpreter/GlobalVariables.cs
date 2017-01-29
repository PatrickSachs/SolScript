using System;
using SevenBiT.Inspector;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter
{
    public class GlobalVariables : IVariables
    {
        protected readonly ChunkVariables Members;
        public GlobalVariables(SolAssembly assembly)
        {
            Members = new ChunkVariables(assembly);
        }

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly => Members.Assembly;

        public virtual IVariables GetParent()
        {
            return null;
        }

        public IVariables Parent {
            get { return GetParent(); }
            set { throw new NotSupportedException("Cannot change the parent of global variables."); }
        }

        /// <summary> Gets the value assigned to the given name. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <exception cref="SolVariableException"> The value has not been declared or assigned. </exception>
        public SolValue Get(string name)
        {
            // todo: merge with try get
            // todo: classes will only be used as a string-SolValue lookup - no need for the overhead of a SolValue-SolValue lookup.
            SolValue value;
            switch (TryGet(name, out value)) {
                case VariableGet.Success:
                    return value.NotNull();
                case VariableGet.FailedNotDeclared:
                    throw new SolVariableException("The variable \"" + name + "\" has not been declared.");
                case VariableGet.FailedNotAssigned:
                    throw new SolVariableException("The variable \"" + name + "\" is declared, but not assigned.");
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
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out functionDefinition) && ValidateFunctionDefinition(functionDefinition))
            {
                switch (functionDefinition.Chunk.ChunkType) {
                    case SolChunkWrapper.Type.ScriptChunk:
                        value = new SolScriptGlobalFunction(functionDefinition);
                        break;
                    case SolChunkWrapper.Type.NativeMethod:
                        // todo: native global ref get dyn ref
                        value = new SolNativeGlobalFunction(functionDefinition, DynamicReference.NullReference.Instance);
                        break;
                    case SolChunkWrapper.Type.NativeConstructor:
                        throw new InvalidOperationException("A native constructor cannot be a global function."); // todo: investigate if a native constructor could be a global function even though it seemingly makes no sense.
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                SolDebug.WriteLine("created global func " + name + " : " + value);
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
                return;
            }
            SolFunctionDefinition definition;
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out definition)) {
                throw new SolVariableException("Cannot assign values to class function \"" + name + "\", they are immutable.");
            }
            if (Parent == null) {
                throw new SolVariableException("Cannot assign value to variable \"" + name + "\", no variable with this name has been declared.");
            }
            Parent.Assign(name, value);
        }

        /// <summary> Is a variable with this name declared? </summary>
        public bool IsDeclared(string name)
        {
            if (Members.IsDeclared(name)) {
                return true;
            }
            SolFunctionDefinition definition;
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out definition) && ValidateFunctionDefinition(definition)) {
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
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out definition) && ValidateFunctionDefinition(definition)) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsAssigned(name);
            }
            return false;
        }

        #endregion

        protected virtual bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            return definition.AccessModifier == AccessModifier.None;
        }
    }
}