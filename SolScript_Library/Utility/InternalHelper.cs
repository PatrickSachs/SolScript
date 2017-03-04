using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

namespace SolScript.Utility
{
    /// <summary>
    ///     This class contains several helper methods for SolScript. These methods are not directly related to SolScript(or
    ///     not meant to be used by users) and thus not not exposed in the public API.
    /// </summary>
    internal static class InternalHelper
    {
        private static readonly HashSet<Type> FuncGenericTypes = new HashSet<Type> {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
            typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>),
            typeof(Func<,,,,,,,>),
            typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,,>)
        };

        private static readonly HashSet<Type> ActionGenericTypes = new HashSet<Type> {
            typeof(Action),
            typeof(Action<>),
            typeof(Action<,>),
            typeof(Action<,,>),
            typeof(Action<,,,>),
            typeof(Action<,,,,>),
            typeof(Action<,,,,,>),
            typeof(Action<,,,,,,>),
            typeof(Action<,,,,,,,>),
            typeof(Action<,,,,,,,,>),
            typeof(Action<,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,,>)
        };

        /// <summary>
        ///     Checks if a <see cref="SolValue" /> is <see cref="SolNil" /> or null.
        /// </summary>
        /// <param name="value">The <see cref="SolValue" /> to check.</param>
        /// <returns>true if the see <see cref="SolValue" /> is <see cref="SolNil" />, false if not.</returns>
        public static bool IsNil([CanBeNull] this SolValue value)
        {
            return value == null || value.Type == SolNil.TYPE;
        }

        /// <summary>
        ///     Checks if the given type is an <see cref="Action" />.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is an action, false if not.</returns>
        /// <exception cref="ArgumentException">The type is not an open geneic type.</exception>
        public static bool IsOpenGenericAction(Type type)
        {
            if (type.ContainsGenericParameters) {
                throw new ArgumentException("The type is not an open generic type.", nameof(type));
            }
            return ActionGenericTypes.Contains(type);
        }

        /// <summary>
        ///     Checks if <paramref name="mainArray" /> contains <paramref name="contentArray" /> starting at index
        ///     <paramref name="mainStartIndex" />.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="mainArray">The array to check for contents.</param>
        /// <param name="contentArray">The content array.</param>
        /// <param name="mainStartIndex">
        ///     The index in <paramref name="mainArray" /> <paramref name="contentArray" /> has to start
        ///     at.
        /// </param>
        /// <param name="referenceEquals">If this is true values will be compared by reference, if false by equality.</param>
        /// <returns>true if <paramref name="mainArray" /> contained <paramref name="contentArray" />, false if not.</returns>
        public static bool ArrayContainsAt<T>(T[] mainArray, T[] contentArray, int mainStartIndex = 0, bool referenceEquals = true)
        {
            if (mainArray.Length < contentArray.Length + mainStartIndex) {
                return false;
            }
            for (int i = 0; i < contentArray.Length; i++) {
                T main = mainArray[i + mainStartIndex];
                if (referenceEquals) {
                    if (!ReferenceEquals(main, contentArray[i])) {
                        return false;
                    }
                } else {
                    if (!main.Equals(contentArray[i])) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///     Checks if the given type is a <see cref="Func{TResult}" />.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is a func, false if not.</returns>
        /// <exception cref="ArgumentException">The type is not an open geneic type.</exception>
        public static bool IsOpenGenericFunc(Type type)
        {
            if (type.ContainsGenericParameters) {
                throw new ArgumentException("The type is not an open geneic type.", nameof(type));
            }
            return FuncGenericTypes.Contains(type);
        }

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

        /// <summary>
        ///     Converts an enumerable of objects to a string using the <see cref="object.ToString()" /> method.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="separator">The separator between values.</param>
        /// <param name="array">The enumerable to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(string,System.Collections.Generic.IEnumerable{T}, Func{T,string})" />
        [DebuggerStepThrough]
        internal static string JoinToString<T>(string separator, IEnumerable<T> array)
        {
            return string.Join(separator, array);
        }

        /// <summary>
        ///     Converts an enumerable of objects to a string using a conversion delegate.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="separator">The separator between values.</param>
        /// <param name="array">The enumerable to convert.</param>
        /// <param name="obtainer">The delegate used to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(string,System.Collections.Generic.IEnumerable{T})" />
        [DebuggerStepThrough]
        internal static string JoinToString<T>(string separator, IEnumerable<T> array, Func<T, string> obtainer)
        {
            StringBuilder builder = new StringBuilder();
            foreach (T element in array) {
                if (builder.Length != 0) {
                    builder.Append(separator);
                }
                builder.Append(obtainer(element));
            }
            return builder.ToString();
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
                string replStr = null;
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
                            replStr = Environment.NewLine;
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
                if (replStr == null) {
                    sb.Append(c);
                } else {
                    sb.Append(replStr);
                }
            }

            return sb.ToString();
        }

        [CanBeNull]
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
        internal static bool DidReturn(Terminators terminators)
        {
            return (terminators & Terminators.Return) == Terminators.Return;
        }

        [DebuggerStepThrough]
        internal static bool DidBreak(Terminators terminators)
        {
            return (terminators & Terminators.Break) == Terminators.Break;
        }

        [DebuggerStepThrough]
        internal static bool DidContinue(Terminators terminators)
        {
            return (terminators & Terminators.Continue) == Terminators.Continue;
        }

        /// <summary>
        ///     Converts a number to an integer.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        /// <param name="integer">The resulting integer.</param>
        /// <returns>True if the integer is the exact same as the number, false if not.</returns>
        internal static bool NumberToInteger(SolNumber number, out int integer)
        {
            integer = (int) number.Value;
            if (integer == number.Value) {
                return true;
            }
            return false;
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
                    if (ex.InnerException is SolRuntimeNativeException) {
                        SolRuntimeNativeException native = (SolRuntimeNativeException) ex.InnerException;
                        // ReSharper disable once ThrowFromCatchWithNoInnerException
                        // Throwing with inner exception since we want to "convert" the NativeRuntime-
                        // Exception and swallow the InvocationException.
                        throw new SolRuntimeException(context, native.Message, native.InnerException);
                    }
                    // ReSharper disable once ThrowFromCatchWithNoInnerException
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
        ///     Creates an instance of the given object using <see cref="Activator.CreateInstance(Type, object[])" />.
        /// </summary>
        /// <typeparam name="T">The exception type to throw.</typeparam>
        /// <param name="type">The type to create.</param>
        /// <param name="arguments">The constructor arguments.</param>
        /// <param name="exceptionFactory">A delegate to create the exception instances(1: Exception message, 2: Inner Exception).</param>
        /// <returns>The object.</returns>
        /// <exception cref="Exception">An error occured while creating the object(Actual type: <typeparamref name="T" />).</exception>
        internal static object SandboxCreateObject<T>(Type type, object[] arguments, Func<string, Exception, T> exceptionFactory) where T : Exception
        {
            try {
                return Activator.CreateInstance(type, arguments);
            } catch (TargetInvocationException ex) {
                throw exceptionFactory($"An exception occured while invoking the constructor of type \"{type}\".", ex);
            } catch (MethodAccessException ex) {
                throw exceptionFactory($"No access to the constructor of type \"{type}\".", ex);
            } catch (MemberAccessException ex) {
                throw exceptionFactory($"Cannot instantiate abstract class \"{type}\".", ex);
            } catch (InvalidComObjectException ex) {
                throw exceptionFactory($"Invalid Com Object of type \"{type}\".", ex);
            } catch (COMException ex) {
                throw exceptionFactory($"\"{type}\" is a COM object but the class identifier used to obtain the type is invalid, or the identified class is not registered.", ex);
            } catch (TypeLoadException ex) {
                throw exceptionFactory($"\"{type}\" is not a valid type.", ex);
            } catch (ArgumentException ex) {
                throw exceptionFactory($"\"{type}\" is an open generic type. ", ex);
            }
        }

        /// <summary>
        ///     Quick helper to create a number object of the given type with the given value.
        /// </summary>
        /// <param name="numberType">The type of the number you wish to create.</param>
        /// <param name="value">The value of the number. May loose accuracy(e.g. if using an integer).</param>
        /// <param name="number">The number. Only valid if the method returned true.</param>
        /// <param name="nullable">Should nullable number types be checked aswell?</param>
        /// <param name="isNull">Only valid if <paramref name="nullable" /> is true - Should the value of nullable types be null?</param>
        /// <returns>true if the number object could be created, false if not(e.g. type is not a numeric type).</returns>
        /// <exception cref="SolMarshallingException">An exception occured while creating the number value.</exception>
        /// <remarks>Supported are: double, float, int, uint, long, ulong, short, ushort, byte, sbyte, char, decimal</remarks>
        [ContractAnnotation("number:null => false")]
        internal static bool TryNumberObject(Type numberType, double value, out object number, bool nullable = true, bool isNull = false)
        {
            try {
                if (numberType == typeof(int)) {
                    number = (int) value;
                    return true;
                }
                if (numberType == typeof(double)) {
                    number = value;
                    return true;
                }
                if (numberType == typeof(float)) {
                    number = (float) value;
                    return true;
                }
                if (numberType == typeof(long)) {
                    number = (long) value;
                    return true;
                }
                if (numberType == typeof(uint)) {
                    number = (uint) value;
                    return true;
                }
                if (numberType == typeof(ulong)) {
                    number = (ulong) value;
                    return true;
                }
                if (numberType == typeof(short)) {
                    number = (short) value;
                    return true;
                }
                if (numberType == typeof(ushort)) {
                    number = (ushort) value;
                    return true;
                }
                if (numberType == typeof(byte)) {
                    number = (byte) value;
                    return true;
                }
                if (numberType == typeof(sbyte)) {
                    number = (sbyte) value;
                    return true;
                }
                if (numberType == typeof(char)) {
                    number = (char) (short) value;
                    return true;
                }
                if (numberType == typeof(decimal)) {
                    number = new decimal(value);
                    return true;
                }
                if (nullable) {
                    if (numberType == typeof(int?)) {
                        number = isNull ? new int?() : (int?) value;
                        return true;
                    }
                    if (numberType == typeof(double?)) {
                        number = isNull ? new double?() : (double?) value;
                        return true;
                    }
                    if (numberType == typeof(float?)) {
                        number = isNull ? new float?() : (float?) value;
                        return true;
                    }
                    if (numberType == typeof(long?)) {
                        number = isNull ? new long?() : (long?) value;
                        return true;
                    }
                    if (numberType == typeof(uint?)) {
                        number = isNull ? new uint?() : (uint?) value;
                        return true;
                    }
                    if (numberType == typeof(ulong?)) {
                        number = isNull ? new ulong?() : (ulong?) value;
                        return true;
                    }
                    if (numberType == typeof(short?)) {
                        number = isNull ? new short?() : (short?) value;
                        return true;
                    }
                    if (numberType == typeof(ushort?)) {
                        number = isNull ? new ushort?() : (ushort?) value;
                        return true;
                    }
                    if (numberType == typeof(byte?)) {
                        number = isNull ? new byte?() : (byte?) value;
                        return true;
                    }
                    if (numberType == typeof(sbyte?)) {
                        number = isNull ? new sbyte?() : (sbyte?) value;
                        return true;
                    }
                    if (numberType == typeof(char?)) {
                        number = isNull ? new char?() : (char?) (short) value;
                        return true;
                    }
                    if (numberType == typeof(decimal?)) {
                        number = isNull ? new decimal?() : (decimal?) new decimal(value);
                        return true;
                    }
                }
            } catch
                (OverflowException ex) {
                throw new SolMarshallingException($"Overflow while creating a \"{numberType.Name}\" number object.", ex);
            }
            number = null;
            return false;
        }

        /// <inheritdoc cref="TryNumberObject" />
        /// <param name="numberType">The type of the number you wish to create.</param>
        /// <param name="value">The value of the number. May loose accuracy(e.g. if using an integer).</param>
        /// <returns>The number object.</returns>
        /// <param name="nullable">Should nullable number types be checked aswell?</param>
        /// <param name="isNull">Only valid if <paramref name="nullable" /> is true - Should the value of nullable types be null?</param>
        /// <returns>true if the number object could be created, false if not(e.g. type is not a numeric type).</returns>
        /// <exception cref="SolMarshallingException">
        ///     The type is not a numeric type/An exception occured while creating the number
        ///     value.
        /// </exception>
        internal static object NumberObject(Type numberType, double value, bool nullable = true, bool isNull = false)
        {
            object number;
            if (!TryNumberObject(numberType, value, out number, nullable, isNull)) {
                throw new SolMarshallingException(numberType, "The type is not a numeric type.");
            }
            return number;
        }

        /// <inheritdoc cref="MemberInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="InvalidOperationException">
        ///     This member belongs to a type that is loaded into the reflection-only
        ///     context. See How to: Load Assemblies into the Reflection-Only Context.
        /// </exception>
        public static T[] GetCustomAttributes<T>(this MemberInfo member, bool inherit = true) where T : Attribute
        {
            object[] attr = member.GetCustomAttributes(typeof(T), inherit);
            if (attr.Length == 0) {
                return EmptyArray<T>.Value;
            }
            return (T[]) attr;
        }

        /// <inheritdoc cref="ParameterInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="ArgumentException">The type must be a type provided by the underlying runtime system.</exception>
        public static T[] GetCustomAttributes<T>(this ParameterInfo member, bool inherit = true) where T : Attribute
        {
            object[] attr = member.GetCustomAttributes(typeof(T), inherit);
            if (attr.Length == 0) {
                return EmptyArray<T>.Value;
            }
            return (T[]) attr;
        }

        /// <inheritdoc cref="MemberInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="InvalidOperationException">
        ///     This member belongs to a type that is loaded into the reflection-only
        ///     context. See How to: Load Assemblies into the Reflection-Only Context.
        /// </exception>
        [CanBeNull]
        public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit = true) where T : Attribute
        {
            T[] attr = GetCustomAttributes<T>(member, inherit);
            if (attr.Length == 0) {
                return null;
            }
            return attr[0];
        }

        /// <inheritdoc cref="ParameterInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="ArgumentException">The type must be a type provided by the underlying runtime system.</exception>
        [CanBeNull]
        public static T GetCustomAttribute<T>(this ParameterInfo member, bool inherit = true) where T : Attribute
        {
            T[] attr = GetCustomAttributes<T>(member, inherit);
            if (attr.Length == 0) {
                return null;
            }
            return attr[0];
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

        internal static void GetParameterBuilders(ParameterInfo[] parameterInfo, out SolParameterBuilder[] builders, out Type[] marshalTypes, out bool allowOptional, out bool sendContext)
        {
            if (parameterInfo.Length == 0) {
                builders = EmptyArray<SolParameterBuilder>.Value;
                marshalTypes = EmptyArray<Type>.Value;
                allowOptional = false;
                sendContext = false;
                return;
            }
            // If null     -> false 
            // if not null -> true (+ value = optional array element type)
            Type allowOptionalType = null;
            int offsetStart = 0;
            int offsetEnd = 0;
            sendContext = false;
            if (parameterInfo[0].ParameterType == typeof(SolExecutionContext)) {
                sendContext = true;
                offsetStart++;
            }
            if (parameterInfo[parameterInfo.Length - 1].GetCustomAttribute<ParamArrayAttribute>() != null) {
                ParameterInfo paramsParameter = parameterInfo[parameterInfo.Length - 1];
                allowOptionalType = paramsParameter.ParameterType.GetElementType();
                offsetEnd++;
            }
            builders = new SolParameterBuilder[parameterInfo.Length - offsetStart - offsetEnd];
            marshalTypes = new Type[builders.Length + (allowOptionalType != null ? 1 : 0)];
            for (int i = offsetStart; i < parameterInfo.Length - offsetEnd; i++) {
                // i is the index in the parameter info array.
                ParameterInfo activeParameter = parameterInfo[i];
                SolContractAttribute customContract = activeParameter.GetCustomAttribute<SolContractAttribute>();
                SolLibraryNameAttribute customName = activeParameter.GetCustomAttribute<SolLibraryNameAttribute>();
                builders[i - offsetStart] = new SolParameterBuilder(
                    customName?.Name ?? activeParameter.Name,
                    customContract != null ? SolTypeBuilder.Fixed(customContract.GetSolType()) : SolTypeBuilder.Native(activeParameter.ParameterType)
                );
                marshalTypes[i - offsetStart] = activeParameter.ParameterType;
            }
            if (allowOptionalType != null) {
                marshalTypes[marshalTypes.Length - 1] = allowOptionalType;
            }
            allowOptional = allowOptionalType != null;
        }


        /*/// <summary>
         /// Helper method to obtain the
         /// </summary>
         /// <param name="assembly"></param>
         /// <param name="builder"></param>
         /// <returns></returns>
         internal static SolParameterInfo GetParameterInfo(SolAssembly assembly, SolFunctionBuilder builder)
         {
             var parameters = new SolParameter[builder.Parameters.Count];
             for (int i = 0; i < parameters.Length; i++) {
                 parameters[i] = builder.Parameters[i].Get(assembly);
             }
             if (builder.IsNative) {
                 return new SolParameterInfo.Native(parameters, builder.NativeMarshalTypes.ToArray(), builder.AllowOptionalParameters, builder.NativeSendContext);
             }
             return new SolParameterInfo(parameters, builder.AllowOptionalParameters);
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
         }*/
    }
}