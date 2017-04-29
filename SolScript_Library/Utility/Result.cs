using System;
using JetBrains.Annotations;

namespace SolScript.Utility
{
    internal struct Result<T>
    {
        private readonly bool m_Success;
        private readonly T m_Value;
        private readonly Exception m_Exception;

        private Result(bool success, T value, Exception exception)
        {
            m_Success = success;
            m_Value = value;
            m_Exception = exception;
        }

        public bool DidSucceed => m_Success;

        public T GetValueOrDefault()
        {
            if (DidSucceed) {
                return m_Value;
            }
            return default(T);
        }

        /// <exception cref="InvalidOperationException" accessor="get">The operation did not succeed.</exception>
        public T Value {
            get {
                if (!DidSucceed) {
                    throw new InvalidOperationException("The operation did not succeed.");
                }
                return m_Value;
            }
        }

        public Exception Exception => m_Exception;

        public static implicit operator bool(Result<T> result)
        {
            return result.DidSucceed;
        }

        public static explicit operator T(Result<T> result)
        {
            return result.Value;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null);
        }

        public static Result<T> Failure(Exception exception = null)
        {
            return new Result<T>(false, default(T), exception);
        }
    }
}