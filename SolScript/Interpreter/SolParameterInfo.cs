using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolParameterInfo" /> is used to easliy access information about the parameters of a function. It
    ///     wraps the actual parameters aswell as information about parameters modifiers.
    /// </summary>
    public class SolParameterInfo : IReadOnlyList<SolParameter>
    {
        /// <summary>
        /// Used by the parser.
        /// </summary>
        public SolParameterInfo()
        {
            
        }

        /// <summary>
        ///     Creates a new <see cref="SolParameterInfo" /> object from the given parameters-.
        /// </summary>
        /// <param name="parameters">The parameter array.</param>
        /// <param name="allowOptional">Should optional arguments be allowed?</param>
        public SolParameterInfo(IEnumerable<SolParameter> parameters, bool allowOptional)
        {
            AllowOptional = allowOptional;
            ParametersList = new PSUtility.Enumerables.List<SolParameter>(parameters);
        }

        /// <summary>
        ///     Allows any value to be passed to this <see cref="SolParameterInfo" />.
        /// </summary>
        public static readonly SolParameterInfo Any = new SolParameterInfo(EmptyArray<SolParameter>.Value, true);

        /// <summary>
        ///     Allows no values to be passed to this <see cref="SolParameterInfo" />.
        /// </summary>
        public static readonly SolParameterInfo None = new SolParameterInfo(EmptyArray<SolParameter>.Value, false);

        // The parameter array.
        [UsedImplicitly]
        internal IList<SolParameter> ParametersList;

        /// <summary>
        ///     Are optional additional("args") arguments allowed?
        /// </summary>
        public bool AllowOptional { get; [UsedImplicitly] internal set; }

        #region IReadOnlyList<SolParameter> Members

        /// <summary>
        ///     How many parameters are registered in this parameter info(excluding the possibly infinite optional ones)?
        /// </summary>
        public int Count => ParametersList.Count;

        /// <summary>
        ///     Access the parameter at index <see cref="index" />.
        /// </summary>
        /// <param name="index">The parameter index.</param>
        /// <returns>The parameter.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is out of range.</exception>
        /// <seealso cref="Count" />
        public SolParameter this[int index] {
            get {
                if (index >= Count || index < 0) {
                    throw new ArgumentOutOfRangeException("Cannot acces parameter " + index + " in a parameter collection with " + Count + " parameters.");
                }
                return ParametersList[index];
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<SolParameter> GetEnumerator()
        {
            return ParametersList.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Contains(SolParameter item)
        {
            return ParametersList.Contains(item);
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals_Impl((SolParameterInfo) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return ((ParametersList?.GetHashCode() ?? 0) * 397) ^ AllowOptional.GetHashCode();
            }
        }

        #endregion

        /// <summary>
        ///     Verifies if the passed arguments are fitting for the parameters defined in this parameter info.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and checks.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>
        ///     The arguments more fitting for calling the function. Arguments length has bene adjusted to match the expected
        ///     parameter count.
        /// </returns>
        /// <exception cref="SolVariableException">The parameters do not fit.</exception>
        public SolValue[] VerifyArguments(SolAssembly assembly, SolValue[] arguments)
        {
            var newArguments = new PSUtility.Enumerables.List<SolValue>(Count > arguments.Length ? Count : arguments.Length);
            for (int i = 0; i < Count; i++) {
                // First go through every declared parameter and see if the types are compatible.
                if (i < arguments.Length) {
                    // The parameter has still been passed.
                    SolValue arg = arguments[i];
                    if (!ParametersList[i].Type.IsCompatible(assembly, arg.Type)) {
                        throw new SolVariableException(SolSourceLocation.Native(),
                            $"Parameter \"{ParametersList[i].Name}\" expected a value of type \"{ParametersList[i].Type}\", but recceived a value of the incompatible type \"{arg.Type}\".");
                    }
                    newArguments.Add(arg);
                } else {
                    // The parameter has no longer been passed and will thus be treated as nil.
                    if (!ParametersList[i].Type.CanBeNil) {
                        throw new SolVariableException(SolSourceLocation.Native(),
                            $"Parameter \"{ParametersList[i].Name}\" expected a value of type \"{ParametersList[i].Type}\", but did not recceive a value at all. No implicit nil value can be passed since the parameter does not accept nil values.");
                    }
                    newArguments.Add(SolNil.Instance);
                }
            }
            if (arguments.Length > Count) {
                // Once we are done check for argument overlength.
                if (!AllowOptional) {
                    // Additional arguments are not allowed.
                    throw new SolVariableException(SolSourceLocation.Native(), "Tried to pass " + (arguments.Length - Count) + " optional arguments although optional arguments are not allowed.");
                }
                // If the only arguments to the optional arguments is a table we use the 
                // array part of the table as args table.
                if (arguments.Length == Count + 1 && arguments[Count].Type == SolTable.TYPE) {
                    SolTable tableArg = (SolTable) arguments[Count];
                    newArguments.AddRange(tableArg.IterateArray());
                } else {
                    for (int i = Count; i < arguments.Length; i++) {
                        newArguments.Add(arguments[i]);
                    }
                }
            }
            return newArguments.ToArray();
        }

        /// <summary>
        ///     Checks if this parameter info class is equal to another one.
        /// </summary>
        /// <param name="other">The other parameter info class.</param>
        /// <returns>true if both are equal, false if not.</returns>
        /// <remarks>
        ///     Two parameter infos are considered equal if all parameters are the same, and they treat optional parameters in
        ///     the same way.
        /// </remarks>
        public bool Equals([CanBeNull] SolParameterInfo other)
        {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return Equals_Impl(other);
        }

        // Equals implementation. Does not check for null.
        private bool Equals_Impl(SolParameterInfo other)
        {
            return Equals(ParametersList, other.ParametersList) && AllowOptional == other.AllowOptional;
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(ParametersList, 0, array, index, ParametersList.Count);
        }

        /// <inheritdoc />
        public void CopyTo(Array<SolParameter> array, int index)
        {
            ArrayUtility.Copy(ParametersList, 0, array, index, ParametersList.Count);
        }

        #region Nested type: Native

        /// <summary>
        ///     The native parameter info is used to represent the native parameters aswell as the SolScript mapping of a function.
        /// </summary>
        public class Native : SolParameterInfo
        {
            /// <summary>
            ///     Creates a new instance of the native parameter info class.
            /// </summary>
            /// <param name="parameters">The parameters used for the type check on the SolScript side.</param>
            /// <param name="nativeTypes">
            ///     The native types the arguments passed to this function should be marshalled to. <br />If
            ///     <paramref name="allowOptional" /> is true the array needs to be one element longer than
            ///     <paramref name="parameters" />, with the last element being the element type of the "params array"(e.g. Int32 not
            ///     Int32[]).<br />If <paramref name="allowOptional" /> is false the array needs to have the same length as
            ///     <see cref="parameters" />.
            /// </param>
            /// <param name="allowOptional">Should optional arguments be allowed?</param>
            /// <param name="sendContext">Will the exceutiong context be sent?</param>
            /// <remarks>
            ///     If <paramref name="sendContext" /> is true the current execution conext will be passed as the first argument
            ///     to the native function.
            /// </remarks>
            /// <exception cref="ArgumentException">Array length mismatches.</exception>
            public Native(SolParameter[] parameters, Type[] nativeTypes, bool allowOptional, bool sendContext) : base(parameters, allowOptional)
            {
                SendContext = sendContext;
                if (AllowOptional) {
                    if (parameters.Length != nativeTypes.Length - 1) {
                        throw new ArgumentException("Got " + parameters.Length + " SolScript parameters and " + nativeTypes.Length +
                                                    " native types. Since optional arguments are supported one more native type than SolScript parameters is required.", nameof(nativeTypes));
                    }
                    OptionalType = nativeTypes[nativeTypes.Length - 1];
                    m_NativeTypes = new Type[parameters.Length];
                    Array.Copy(nativeTypes, 0, m_NativeTypes, 0, nativeTypes.Length - 1);
                } else {
                    if (parameters.Length != nativeTypes.Length) {
                        throw new ArgumentException("Got " + parameters.Length + " SolScript parameters and " + nativeTypes.Length +
                                                    " native types.", nameof(nativeTypes));
                    }
                    OptionalType = null;
                    m_NativeTypes = nativeTypes;
                }
            }

            private readonly Type[] m_NativeTypes;

            /// <summary>
            ///     A clone of the native type array.
            /// </summary>
            public Type[] NativeTypes => (Type[]) m_NativeTypes.Clone();

            /// <summary>
            ///     The element type of the optional native array(e.g Int32 and not Int32[]). Only valid if
            ///     <see cref="SolParameterInfo.AllowOptional" /> is true.
            /// </summary>
            public Type OptionalType { get; }

            /// <summary>
            ///     Should the current executiong conext be passed as first argument to the native method?
            /// </summary>
            public bool SendContext { get; }

            /// <summary>
            ///     Marshals the given arguments to their native counterparts.
            /// </summary>
            /// <param name="context">The context to marshal in and to possibly pass as a context argument.</param>
            /// <param name="arguments">The actual arguments.</param>
            /// <returns>The native objects.</returns>
            /// <exception cref="SolMarshallingException">Failed to marshal a value.</exception>
            public object[] Marshal(SolExecutionContext context, SolValue[] arguments)
            {
                int offsetStart = SendContext ? 1 : 0;
                int offsetEnd = AllowOptional ? 1 : 0;
                var array = new object[Count + offsetStart + offsetEnd];
                SolMarshal.MarshalFromSol(context.Assembly, 0, Count, arguments, m_NativeTypes, array, offsetStart);
                if (SendContext) {
                    array[0] = context;
                }
                if (AllowOptional) {
                    Array optionalArray = Array.CreateInstance(OptionalType, arguments.Length - Count);
                    array[array.Length - 1] = optionalArray;
                    SolMarshal.MarshalFromSol(context.Assembly, Count, optionalArray.Length, arguments, InternalHelper.ArrayFilledWith(OptionalType, optionalArray.Length), (object[]) optionalArray, 0);
                }
                return array;
            }

            /// <summary>
            ///     Gets the native type for the given index. Follows the same rules as indexing this class directy and the result of
            ///     this method call maps 1:1 with the result of the index operation.
            /// </summary>
            /// <param name="index">The index.</param>
            /// <returns>The native type this parameter of this index needs to be marshalled to.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            ///     The index was smaller than 0 or larger than
            ///     <see cref="SolParameterInfo.Count" />.
            /// </exception>
            public Type GetNativeType(int index)
            {
                if (index >= Count || index < 0) {
                    throw new ArgumentOutOfRangeException("Cannot acces parameter " + index + " in a parameter collection with " + Count + " parameters.");
                }
                return m_NativeTypes[index];
            }
        }

        #endregion
    }
}