using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Exceptions;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Properties;

namespace SolScript.Interpreter.Types.Marshal
{
    /// <summary>
    ///     This class tries to take care of all delagtes. Do all the things!
    /// </summary>
    public class NativeDelegateMarshaller : ISolNativeMarshaller
    {
        private static readonly PSDictionary<int, Type> s_RetTypes = new PSDictionary<int, Type> {
            {0, typeof(GenRet<,>)},
            {1, typeof(GenRet<,,>)},
            {2, typeof(GenRet<,,,>)},
            {3, typeof(GenRet<,,,,>)},
            {4, typeof(GenRet<,,,,,>)},
            {5, typeof(GenRet<,,,,,,>)},
            {6, typeof(GenRet<,,,,,,,>)},
        };

        private static readonly PSDictionary<int, Type> s_VoidTypes = new PSDictionary<int, Type> {
            {0, typeof(GenVoid<>)},
            {1, typeof(GenVoid<,>)},
            {2, typeof(GenVoid<,,>)},
            {3, typeof(GenVoid<,,,>)},
            {4, typeof(GenVoid<,,,,>)},
            {5, typeof(GenVoid<,,,,,>)},
            {6, typeof(GenVoid<,,,,,,>)},
        };

        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolFunction.TYPE, true);
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">Failed to marshal a parameter type.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            if (value == null) {
                return SolNil.Instance;
            }
            return new SolNativeDelegateWrapperFunction(assembly, (Delegate) value);
        }

        #endregion

        /// <summary>
        ///     Creates a delegate of any type for the given function. While the creates delegate takes care of any marshalling to
        ///     the best of its ability, it is your responsility to make sure that the function can handle the parameters. e.g. a
        ///     <c>Func{string, bool}</c> will never be able to correctly call a <c>function(table?, Stream!) : number!</c>.
        /// </summary>
        /// <typeparam name="T">The delegate type.</typeparam>
        /// <param name="function">
        ///     The function. Keep in mind that the function will be directly referenced by the delegate and
        ///     thus extend the lifetime of the function and possibly the class for as long as the delegate exists.
        /// </param>
        /// <returns>The created delegate.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function" /> is <see langword="null" /> </exception>
        /// <exception cref="SolMarshallingException">
        ///     An error occured while building the delegate; Check inner exception for more
        ///     details.
        /// </exception>
        /// <exception cref="ArgumentException"><typeparamref name="T" /> is not a delegate type.</exception>
        public static T CreateDelegate<T>([NotNull] SolFunction function) where T : class
        {
            if (!typeof(Delegate).IsAssignableFrom(typeof(T))) {
                throw new ArgumentException("The type \"" + typeof(T) + "\" is not a delegate type.");
            }
            return CreateDelegate(typeof(T), function) as T;
        }

        /// <summary>
        ///     Creates a delegate of any type for the given function. While the creates delegate takes care of any marshalling to
        ///     the best of its ability, it is your responsility to make sure that the function can handle the parameters. e.g. a
        ///     <c>Func{string, bool}</c> will never be able to correctly call a <c>function(table?, Stream!) : number!</c>.
        /// </summary>
        /// <param name="delType">The delegate type.</param>
        /// <param name="function">
        ///     The function. Keep in mind that the function will be directly referenced by the delegate and
        ///     thus extend the lifetime of the function and possibly the class for as long as the delegate exists.
        /// </param>
        /// <returns>The created delegate.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="delType" /> is <see langword="null" /> -or-
        ///     <paramref name="function" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="SolMarshallingException">
        ///     An error occured while building the delegate; Check inner exception for more
        ///     details.
        /// </exception>
        public static Delegate CreateDelegate([NotNull] Type delType, [NotNull] SolFunction function)
        {
            if (delType == null) {
                throw new ArgumentNullException(nameof(delType));
            }
            if (function == null) {
                throw new ArgumentNullException(nameof(function));
            }
            try {
                MethodInfo delInvokeInfo = delType.GetMethod(nameof(Action.Invoke));
                List<ParameterInfo> invokeParams = delInvokeInfo.GetParameters().ToList();
                Type type;
                if (delInvokeInfo.ReturnType == typeof(void)) {
                    type = s_VoidTypes[invokeParams.Count].MakeGenericType(
                        invokeParams
                            .Select(p => p.ParameterType)
                            .Concat(EnumerableConcat.Prepend, delType)
                            .ToArray()
                    );
                } else {
                    type = s_RetTypes[invokeParams.Count].MakeGenericType(
                        invokeParams
                            .Select(p => p.ParameterType)
                            .Concat(EnumerableConcat.Prepend, delType, delInvokeInfo.ReturnType)
                            .ToArray()
                    );
                }
                object genInstance = Activator.CreateInstance(type, function);
                MethodInfo genInvokeMethod = type.GetMethod(nameof(GenVoid<object>.Invoke), BindingFlags.Public | BindingFlags.Instance);
                return Delegate.CreateDelegate(delType, genInstance, genInvokeMethod, true);
            } catch (Exception ex) {
                throw new SolMarshallingException(Resources.Err_FailedToBuildDelegate.FormatWith(delType, function), ex);
            }
        }

        #region Nested type: GenBase

        private abstract class GenBase<TDelegate, TReturn> where TDelegate : class
        {
            protected GenBase(SolFunction function)
            {
                Function = function;
            }

            public readonly SolFunction Function;

            protected TReturn CallFunction(params object[] objs)
            {
                SolValue[] solArgs = SolMarshal.MarshalFromNative(Function.Assembly, objs);
                return Function.Call(new SolExecutionContext(Function.Assembly, "bla"), solArgs).ConvertTo<TReturn>();
            }

            protected void CallFunctionVoid(params object[] objs)
            {
                SolValue[] solArgs = SolMarshal.MarshalFromNative(Function.Assembly, objs);
                Function.Call(new SolExecutionContext(Function.Assembly, "bla"), solArgs);
            }
        }

        #endregion

        #region Nested type: GenRet

        private class GenRet<TDelegate, TReturn> : GenBase<TDelegate, TReturn> where TDelegate : class
        {
            /// <inheritdoc />
            public GenRet(SolFunction function) : base(function) {}

            public TReturn Invoke()
            {
                return CallFunction();
            }
        }

        private class GenRet<TDelegate, TReturn, T1> : GenBase<TDelegate, TReturn> where TDelegate : class
        {
            /// <inheritdoc />
            public GenRet(SolFunction function) : base(function) {}

            public TReturn Invoke(T1 a1)
            {
                return CallFunction(a1);
            }
        }

        private class GenRet<TDelegate, TReturn, T1, T2> : GenBase<TDelegate, TReturn> where TDelegate : class
        {
            /// <inheritdoc />
            public GenRet(SolFunction function) : base(function) {}

            public TReturn Invoke(T1 a1, T2 a2)
            {
                return CallFunction(a1, a2);
            }
        }

        private class GenRet<TDelegate, TReturn, T1, T2, T3> : GenBase<TDelegate, TReturn> where TDelegate : class
        {
            /// <inheritdoc />
            public GenRet(SolFunction function) : base(function) { }

            public TReturn Invoke(T1 a1, T2 a2, T3 a3)
            {
                return CallFunction(a1, a2, a3);
            }
        }
        private class GenRet<TDelegate, TReturn, T1, T2, T3, T4> : GenBase<TDelegate, TReturn> where TDelegate : class
        {
            /// <inheritdoc />
            public GenRet(SolFunction function) : base(function) { }

            public TReturn Invoke(T1 a1, T2 a2, T3 a3, T4 a4)
            {
                return CallFunction(a1, a2, a3, a4);
            }
        }
        private class GenRet<TDelegate, TReturn, T1, T2, T3, T4, T5> : GenBase<TDelegate, TReturn> where TDelegate : class
        {
            /// <inheritdoc />
            public GenRet(SolFunction function) : base(function) { }

            public TReturn Invoke(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5)
            {
                return CallFunction(a1, a2, a3, a4, a5);
            }
        }
        private class GenRet<TDelegate, TReturn, T1, T2, T3, T4, T5, T6> : GenBase<TDelegate, TReturn> where TDelegate : class
        {
            /// <inheritdoc />
            public GenRet(SolFunction function) : base(function) { }

            public TReturn Invoke(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
            {
                return CallFunction(a1, a2, a3, a4, a5, a6);
            }
        }

        #endregion

        #region Nested type: GenVoid

        private class GenVoid<TDelegate> : GenBase<TDelegate, object> where TDelegate : class
        {
            /// <inheritdoc />
            public GenVoid(SolFunction function) : base(function) {}

            public void Invoke()
            {
                CallFunctionVoid();
            }
        }

        private class GenVoid<TDelegate, T1> : GenBase<TDelegate, object> where TDelegate : class
        {
            /// <inheritdoc />
            public GenVoid(SolFunction function) : base(function) {}

            public void Invoke(T1 a1)
            {
                CallFunctionVoid(a1);
            }
        }

        private class GenVoid<TDelegate, T1, T2> : GenBase<TDelegate, object> where TDelegate : class
        {
            /// <inheritdoc />
            public GenVoid(SolFunction function) : base(function) {}

            public void Invoke(T1 a1, T2 a2)
            {
                CallFunctionVoid(a1, a2);
            }
        }

        private class GenVoid<TDelegate, T1, T2, T3> : GenBase<TDelegate, object> where TDelegate : class
        {
            /// <inheritdoc />
            public GenVoid(SolFunction function) : base(function) { }

            public void Invoke(T1 a1, T2 a2, T3 a3)
            {
                CallFunctionVoid(a1, a2, a3);
            }
        }

        private class GenVoid<TDelegate, T1, T2, T3, T4> : GenBase<TDelegate, object> where TDelegate : class
        {
            /// <inheritdoc />
            public GenVoid(SolFunction function) : base(function) { }

            public void Invoke(T1 a1, T2 a2, T3 a3, T4 a4)
            {
                CallFunctionVoid(a1, a2, a3, a4);
            }
        }

        private class GenVoid<TDelegate, T1, T2, T3, T4, T5> : GenBase<TDelegate, object> where TDelegate : class
        {
            /// <inheritdoc />
            public GenVoid(SolFunction function) : base(function) { }

            public void Invoke(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5)
            {
                CallFunctionVoid(a1, a2, a3, a4, a5);
            }
        }

        private class GenVoid<TDelegate, T1, T2, T3, T4, T5, T6> : GenBase<TDelegate, object> where TDelegate : class
        {
            /// <inheritdoc />
            public GenVoid(SolFunction function) : base(function) { }

            public void Invoke(T1 a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
            {
                CallFunctionVoid(a1, a2, a3, a4, a5, a6);
            }
        }

        #endregion
    }
}