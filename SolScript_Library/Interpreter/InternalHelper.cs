using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;

namespace SolScript.Interpreter
{
    internal static class InternalHelper
    {
        internal static readonly SolParameterInfo.Native EmptyNativeParameterInfo = new SolParameterInfo.Native(Array.Empty<SolParameter>(), Array.Empty<Type>(), false, false);

        /// <summary>
        ///     This method uniformly creates an exception for the result of a variable get exception. You may wish to check for
        ///     <see cref="VariableState.Success" /> beforehand.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="state">The state of the variable get operation.</param>
        /// <param name="exception">A wrapped exception.</param>
        /// <returns>The excpetion, ready to be thrown.</returns>
        /// <remarks>This method does NOT THROW the exception, only create the exception object.</remarks>
        internal static SolVariableException CreateVariableGetException(string name, VariableState state, Exception exception)
        {
            switch (state) {
                case VariableState.Success:
                    return new SolVariableException($"Cannot get the value of variable \"{name}\" - The operation was not expected to succeed.", exception);
                case VariableState.FailedCouldNotResolveNativeReference:
                    return new SolVariableException($"Cannot get the value of variable \"{name}\" - The underlying native object could not be resolved.", exception);
                case VariableState.FailedNotAssigned:
                    return new SolVariableException($"Cannot get the value of variable \"{name}\" - The variable has not been assigned.", exception);
                case VariableState.FailedNotDeclared:
                    return new SolVariableException($"Cannot get the value of variable \"{name}\" - The variable has not been declared.", exception);
                case VariableState.FailedTypeMismatch:
                    return new SolVariableException($"Cannot get the value of variable \"{name}\" - A type mismatch occured.", exception);
                case VariableState.FailedNativeException:
                    return new SolVariableException($"Cannot get the value of variable \"{name}\" - A native error occured.", exception);
                case VariableState.FailedRuntimeError:
                    return new SolVariableException($"Cannot get the value of variable \"{name}\" - A runtime error occured.", exception);
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        /// <summary>
        ///     This method uniformly creates an exception for the result of a variable set exception. You may wish to check for
        ///     <see cref="VariableState.Success" /> beforehand.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="state">The state of the variable set operation.</param>
        /// <param name="exception">A wrapped exception.</param>
        /// <returns>The excpetion, ready to be thrown.</returns>
        /// <remarks>This method does NOT THROW the exception, only create the exception object.</remarks>
        internal static SolVariableException CreateVariableSetException(string name, VariableState state, Exception exception)
        {
            switch (state) {
                case VariableState.Success:
                    return new SolVariableException($"Cannot set the value of variable \"{name}\" - The operation was not expected to succeed.", exception);
                case VariableState.FailedCouldNotResolveNativeReference:
                    return new SolVariableException($"Cannot set the value of variable \"{name}\" - The underlying native object could not be resolved.", exception);
                case VariableState.FailedNotAssigned:
                    return new SolVariableException($"Cannot set the value of variable \"{name}\" - The variable has not been assigned.", exception);
                case VariableState.FailedNotDeclared:
                    return new SolVariableException($"Cannot set the value of variable \"{name}\" - The variable has not been declared.", exception);
                case VariableState.FailedTypeMismatch:
                    return new SolVariableException($"Cannot set the value of variable \"{name}\" - A type mismatch occured.", exception);
                case VariableState.FailedNativeException:
                    return new SolVariableException($"Cannot set the value of variable \"{name}\" - A native error occured.", exception);
                case VariableState.FailedRuntimeError:
                    return new SolVariableException($"Cannot set the value of variable \"{name}\" - A runtime error occured.", exception);
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string JoinToString<T>(string separator, IEnumerable<T> array)
        {
            return string.Join(separator, array);
        }

        internal static T[] ArrayFilledWith<T>(T value, int length)
        {
            var array = new T[length];
            for (int i = 0; i < length; i++) {
                array[i] = value;
            }
            return array;
        }

        /// <summary>
        ///     Unescapes a string, making a "human-readable" string usable inside SolScript or any other language for that matter.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <returns>The unescaped string.</returns>
        /// <exception cref="ArgumentException">A parsing error occured.</exception>
        [NotNull]
        internal static string UnEscape([NotNull] this string source)
        {
            if (source.Length == 0) {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(source.Length);
            int pos = 0;
            while (pos < source.Length) {
                char c = source[pos];
                if (c == '\\') {
                    // Handle escape sequences
                    pos++;
                    if (pos >= source.Length) {
                        throw new ArgumentException("Missing string escape sequence.", nameof(source));
                    }
                    switch (source[pos]) {
                        // Simple character escapes
                        case '\'':
                            c = '\'';
                            break;
                        case '\"':
                            c = '\"';
                            break;
                        case '\\':
                            c = '\\';
                            break;
                        case '0':
                            c = '\0';
                            break;
                        case 'a':
                            c = '\a';
                            break;
                        case 'b':
                            c = '\b';
                            break;
                        case 'f':
                            c = '\f';
                            break;
                        case 'n':
                            c = ' ';
                            break;
                        case 'r':
                            c = ' ';
                            break;
                        case 't':
                            c = '\t';
                            break;
                        case 'v':
                            c = '\v';
                            break;
                        case 'x':
                            // Hexa escape (1-4 digits)
                            StringBuilder hexa = new StringBuilder(10);
                            pos++;
                            if (pos >= source.Length) {
                                throw new ArgumentException("Missing string hexa escape sequence.", nameof(source));
                            }
                            c = source[pos];
                            if (char.IsDigit(c) || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F') {
                                hexa.Append(c);
                                pos++;
                                if (pos < source.Length) {
                                    c = source[pos];
                                    if (char.IsDigit(c) || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F') {
                                        hexa.Append(c);
                                        pos++;
                                        if (pos < source.Length) {
                                            c = source[pos];
                                            if (char.IsDigit(c) || c >= 'a' && c <= 'f' ||
                                                c >= 'A' && c <= 'F') {
                                                hexa.Append(c);
                                                pos++;
                                                if (pos < source.Length) {
                                                    c = source[pos];
                                                    if (char.IsDigit(c) || c >= 'a' && c <= 'f' ||
                                                        c >= 'A' && c <= 'F') {
                                                        hexa.Append(c);
                                                        pos++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            c = (char) int.Parse(hexa.ToString(), NumberStyles.HexNumber);
                            pos--;
                            break;
                        case 'u':
                            // Unicode hexa escape (exactly 4 digits)
                            pos++;
                            if (pos + 3 >= source.Length) {
                                throw new ArgumentException("Unrecognized string unicode hexa escape sequence.", nameof(source));
                            }
                            try {
                                uint charValue = uint.Parse(source.Substring(pos, 4),
                                    NumberStyles.HexNumber);
                                c = (char) charValue;
                                pos += 3;
                            } catch (SystemException) {
                                throw new ArgumentException("Unrecognized string unicode hexa escape sequence.", nameof(source));
                            }
                            break;
                        case 'U':
                            // Unicode hexa escape (exactly 8 digits, first four must be 0000)
                            pos++;
                            if (pos + 7 >= source.Length) {
                                throw new ArgumentException("Unrecognized string unicode hexa escape sequence.", nameof(source));
                            }
                            try {
                                uint charValue = uint.Parse(source.Substring(pos, 8),
                                    NumberStyles.HexNumber);
                                if (charValue > 0xffff) {
                                    throw new ArgumentException("Unrecognized string unicode hexa escape sequence.", nameof(source));
                                }
                                c = (char) charValue;
                                pos += 7;
                            } catch (SystemException) {
                                throw new ArgumentException("Unrecognized string unicode hexa escape sequence.", nameof(source));
                            }
                            break;
                        default:
                            throw new ArgumentException("Unrecognized string escape symbol: \"" + c + "\"", nameof(source));
                    }
                }
                pos++;
                sb.Append(c);
            }

            return sb.ToString();
        }

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ParseTreeNode FindChildByName(this ParseTreeNodeList @this, string name)
        {
            return @this.Find(p => p.Term.Name == name);
        }

        [NotNull]
        [DebuggerStepThrough]
        internal static T NotNull<T>([CanBeNull] this T @this, string message = "Unexpected null value!")
        {
            if (@this == null) {
                throw new NullReferenceException(message);
            }
            return @this;
        }

        [DebuggerStepThrough]
        internal static Terminators BuildTerminators(bool incReturn, bool incBreak, bool incContinue)
        {
            Terminators terminators = Terminators.None;
            if (incReturn) {
                terminators |= Terminators.Return;
            }
            if (incBreak) {
                terminators |= Terminators.Break;
            }
            if (incContinue) {
                terminators |= Terminators.Continue;
            }
            return terminators;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DidReturn(Terminators terminators)
        {
            return (terminators & Terminators.Return) == Terminators.Return;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DidBreak(Terminators terminators)
        {
            return (terminators & Terminators.Break) == Terminators.Break;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DidContinue(Terminators terminators)
        {
            return (terminators & Terminators.Continue) == Terminators.Continue;
        }

        /// <summary>
        ///     Invokes the given <see cref="MethodBase" /> in a sandbox. This means that all exceptions will be catched and neatly
        ///     wrapped in SolScript compatible exceptions.
        /// </summary>
        /// <param name="context">The exceution context. Required to generate the stack trace.</param>
        /// <param name="method">The method to call.</param>
        /// <param name="target">The object to call the method on.</param>
        /// <param name="arguments">The method arguments.</param>
        /// <returns>The return value of the method call.</returns>
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured. Excecution may have to be halted.</exception>
        [CanBeNull]
        internal static object SandboxInvokeMethod(SolExecutionContext context, MethodBase method, [CanBeNull] object target, object[] arguments)
        {
            object nativeObject;
            try {
                ConstructorInfo ctorInfo = method as ConstructorInfo;
                if (ctorInfo != null) {
                    if (target != null) {
                        throw new InvalidOperationException("Invalid native object to call native method \"" + method.Name + "\" on: Constrcutors cannot be called on an object.");
                    }
                    nativeObject = ctorInfo.Invoke(arguments);
                } else {
                    nativeObject = method.Invoke(target, arguments);
                }
            } catch (Exception ex) {
                if (ex is TargetInvocationException) {
                    if (ex.InnerException is SolRuntimeException) {
                        // If the method threw a proper runtime exception we'll just let it bubble through.
                        throw (SolRuntimeException) ex.InnerException;
                    }
                    // Throwing with inner exception since we want to have the error message that created the 
                    // TargetInvocationException and not the target invocation exception itself.
                    throw new SolRuntimeException(context, "A native exception occured while calling this instance function.", ex.InnerException);
                }
                if (ex is TargetException) {
                    throw new InvalidOperationException("Invalid native object to call native method \"" + method.Name + "\" on: " + ex.Message, ex);
                }
                if (ex is TargetParameterCountException) {
                    throw new InvalidOperationException("Invalid native parameter count to call native method \"" + method.Name + "\" with: " + ex.Message, ex);
                }
                if (ex is MethodAccessException) {
                    throw new InvalidOperationException("Cannot access native method \"" + method.Name + "\": " + ex.Message, ex);
                }
                throw new InvalidOperationException("An unspecified internal error occured while calling native method \"" + method.Name + "\": " + ex.Message, ex);
            }
            return nativeObject;
        }

        /// <summary>
        ///     Gets the type of a member info. This method respects the the contracts of said member info.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="member">The name to exctract the info out of.</param>
        /// <returns>The type of this member info. This takes the <see cref="SolContractAttribute" /> into consideration.</returns>
        /// <exception cref="SolMarshallingException">No matching SolType for the return type.</exception>
        internal static SolType GetMemberReturnType(SolAssembly assembly, MethodInfo member)
        {
            SolContractAttribute contract = member.GetCustomAttribute<SolContractAttribute>();
            if (contract != null) {
                return contract.GetSolType();
            }
            return SolMarshal.GetSolType(assembly, member.ReturnType);
        }

        /// <inheritdoc
        ///     cref="GetMemberReturnType(SolScript.Interpreter.SolAssembly,System.Reflection.MethodInfo)" />
        /// <exception cref="SolMarshallingException">No matching SolType for the return type.</exception>
        internal static SolType GetMemberReturnType(SolAssembly assembly, ConstructorInfo member)
        {
            SolContractAttribute contract = member.GetCustomAttribute<SolContractAttribute>();
            if (contract != null) {
                return contract.GetSolType();
            }
            return SolMarshal.GetSolType(assembly, member.DeclaringType);
        }

        /// <inheritdoc
        ///     cref="GetMemberReturnType(SolScript.Interpreter.SolAssembly,System.Reflection.MethodInfo)" />
        /// <exception cref="SolMarshallingException">No matching SolType for the return type.</exception>
        internal static SolType GetMemberReturnType(SolAssembly assembly, FieldOrPropertyInfo member)
        {
            SolContractAttribute contract = member.GetCustomAttribute<SolContractAttribute>();
            if (contract != null) {
                return contract.GetSolType();
            }
            return SolMarshal.GetSolType(assembly, member.DataType);
        }

        /// <exception cref="SolMarshallingException">No matching SolType for a parameter type.</exception>
        internal static SolParameterInfo.Native GetParameterInfo(SolAssembly assembly, ParameterInfo[] parameterInfo)
        {
            if (parameterInfo.Length == 0) {
                return EmptyNativeParameterInfo;
            }
            // If null     -> false 
            // if not null -> true (+ value = optional array element type)
            Type allowOptional = null;
            bool sendContext = false;
            int offsetStart = 0;
            int offsetEnd = 0;
            if (parameterInfo[0].ParameterType == typeof(SolExecutionContext)) {
                sendContext = true;
                offsetStart++;
            }
            if (parameterInfo[parameterInfo.Length - 1].GetCustomAttribute<ParamArrayAttribute>() != null) {
                ParameterInfo paramsParameter = parameterInfo[parameterInfo.Length - 1];
                allowOptional = paramsParameter.ParameterType.GetElementType();
                offsetEnd++;
            }
            var solArray = new SolParameter[parameterInfo.Length - offsetStart - offsetEnd];
            var typeArray = new Type[solArray.Length + (allowOptional != null ? 1 : 0)];
            for (int i = offsetStart; i < parameterInfo.Length - offsetEnd; i++) {
                // i is the index in the parameter info array.
                ParameterInfo activeParameter = parameterInfo[i];
                SolContractAttribute customContract = activeParameter.GetCustomAttribute<SolContractAttribute>();
                SolLibraryNameAttribute customName = activeParameter.GetCustomAttribute<SolLibraryNameAttribute>();
                solArray[i - offsetStart] = new SolParameter(
                    customName?.Name ?? activeParameter.Name,
                    customContract?.GetSolType() ?? SolMarshal.GetSolType(assembly, activeParameter.ParameterType)
                );
                typeArray[i - offsetStart] = activeParameter.ParameterType;
            }
            if (allowOptional != null) {
                typeArray[typeArray.Length - 1] = allowOptional;
            }
            SolParameterInfo.Native infoClass = new SolParameterInfo.Native(solArray, typeArray, allowOptional != null, sendContext);
            return infoClass;
        }
    }
}