using System;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The <see cref="std_Math" /> class provides several standard mathematical operation.
    /// </summary>
    [SolLibraryClass(std.NAME, SolTypeMode.Singleton)]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class std_Math
    {
        public std_Math()
        {
            int seed = Environment.TickCount;
            m_RandomSeed = new SolNumber(seed);
            m_Random = new Random(seed);
        }

        // todo: create a way to register the assembly in these modules. (maybe per interface?)
        [SolLibraryVisibility(std.NAME, false)] public const string TYPE = "Math";
        [SolLibraryVisibility(std.NAME, false)] private Random m_Random;
        [SolLibraryVisibility(std.NAME, false)] private SolNumber m_RandomSeed;

        /// <summary>
        ///     (Must be integer) The <see cref="randomseed" /> is used for generating random numbers. The same
        ///     <see cref="randomseed" /> will always
        ///     generate the same sequence of random numbers. Update this manually if you wish to provide consistent random
        ///     numbers.
        /// </summary>
        /// <exception cref="SolRuntimeNativeException" accessor="set"><paramref name="value" /> was not an integer.</exception>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber randomseed {
            get { return m_RandomSeed; }
            set {
                int intValue;
                if (!InternalHelper.NumberToInteger(value, out intValue)) {
                    throw new SolRuntimeNativeException("The randomseed can only be set to integers - Got " + value + ", which has a decimal part.");
                }
                m_RandomSeed = new SolNumber(intValue);
                m_Random = new Random(intValue);
            }
        }

        #region Overrides

        public override string ToString()
        {
            return "Math Module";
        }

        #endregion

        /// <summary>
        ///     Gets the smallest value out of several given <paramref name="values" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="values">The values to check for the smallest value.</param>
        /// <returns>The smallest value.</returns>
        /// <exception cref="SolRuntimeException">No values were provided.</exception>
        public SolNumber min(SolExecutionContext context, params SolNumber[] values)
        {
            if (values.Length == 0) {
                throw new SolRuntimeException(context, "Cannot get the min value of 0 values.");
            }
            SolNumber current = values[0];
            for (int i = 1; i < values.Length; i++) {
                if (values[i].Value < current.Value) {
                    current = values[i];
                }
            }
            return current;
        }

        /// <summary>
        ///     Gets the largest value out of several given <paramref name="values" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="values">The values to check for the largest value.</param>
        /// <returns>The largest value.</returns>
        /// <exception cref="SolRuntimeException">No values were provided.</exception>
        public SolNumber max(SolExecutionContext context, params SolNumber[] values)
        {
            if (values.Length == 0) {
                throw new SolRuntimeException(context, "Cannot get the min value of 0 values.");
            }
            SolNumber current = values[0];
            for (int i = 1; i < values.Length; i++) {
                if (values[i].Value > current.Value) {
                    current = values[i];
                }
            }
            return current;
        }

        /// <inheritdoc cref="double.IsNaN" />
        [SolContract(SolBool.TYPE, false)]
        public SolBool is_nan([SolContract(SolNumber.TYPE, false)] SolNumber value)
        {
            return SolBool.ValueOf(double.IsNaN(value.Value));
        }

        /// <inheritdoc cref="double.IsInfinity" />
        [SolContract(SolBool.TYPE, false)]
        public SolBool is_infinity([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return SolBool.ValueOf(double.IsInfinity(value.Value));
        }

        /// <inheritdoc cref="Math.Sin" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber sin([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Sin(value.Value));
        }


        /// <inheritdoc cref="Math.Sinh" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber sinh([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Sinh(value.Value));
        }


        /// <inheritdoc cref="Math.Asin" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber asin([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Asin(value.Value));
        }


        /// <inheritdoc cref="Math.Cos" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber cos([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Cos(value.Value));
        }


        /// <inheritdoc cref="Math.Cosh" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber cosh([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Cosh(value.Value));
        }


        /// <inheritdoc cref="Math.Acos" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber acos([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Acos(value.Value));
        }


        /// <inheritdoc cref="Math.Tan" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber tan([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Tan(value.Value));
        }


        /// <inheritdoc cref="Math.Tanh" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber tanh([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Tanh(value.Value));
        }


        /// <inheritdoc cref="Math.Atan" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber atan([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Atan(value.Value));
        }


        /// <inheritdoc cref="Math.Abs(double)" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber abs([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Abs(value.Value));
        }


        /// <inheritdoc cref="Math.Ceiling(double)" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber ceil([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Ceiling(value.Value));
        }


        /// <inheritdoc cref="Math.Floor(double)" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber floor([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Floor(value.Value));
        }

        /// <summary>
        ///     Converts the given radians into degrees.
        /// </summary>
        /// <param name="rad">The radians.</param>
        /// <returns>The degress the given radians represented.</returns>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber deg([SolContract(SolNumber.TYPE, false)] SolNumber rad)
        {
            return new SolNumber(rad.Value * (180.0 / Math.PI));
        }


        /// <summary>
        ///     Converts the given degress into radians.
        /// </summary>
        /// <param name="deg">The degrees.</param>
        /// <returns>The radians the given degrees represented.</returns>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber rad([SolContract(SolNumber.TYPE, false)] SolNumber deg)
        {
            return new SolNumber(Math.PI * deg.Value / 180.0);
        }


        /// <inheritdoc cref="Math.Exp(double)" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber exp([SolContract(SolBool.TYPE, false)] SolNumber value)
        {
            return new SolNumber(Math.Exp(value.Value));
        }


        /// <inheritdoc cref="Math.Log(double)" />
        /// <param name="d">The <see cref="SolNumber" /> whose logarithm is to be found. </param>
        /// <param name="newBase">(Optional) The base of the logarithm. </param>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber log([SolContract(SolBool.TYPE, false)] SolNumber d, [SolContract(SolNumber.TYPE, true)] [CanBeNull] SolNumber newBase)
        {
            if (!newBase.IsNil()) {
                return new SolNumber(Math.Log(d.Value, newBase.NotNull().Value));
            }
            return new SolNumber(Math.Log(d.Value));
        }


        /// <inheritdoc cref="Random.NextDouble()" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber random()
        {
            return new SolNumber(m_Random.NextDouble());
        }

        /// <summary>
        ///     Returns a random floating-point number that is greater than or equal to <paramref name="min" />, and less than
        ///     <paramref name="max" />.
        /// </summary>
        /// <param name="min">The min value(inclusive).</param>
        /// <param name="max">The max value(exlusive).</param>
        /// <returns>The generated random number.</returns>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber random_range([SolContract(SolNumber.TYPE, false)] SolNumber min, [SolContract(SolNumber.TYPE, false)] SolNumber max)
        {
            return new SolNumber(m_Random.NextDouble() * (max.Value - min.Value) + min.Value);
        }

        /// <inheritdoc cref="Random.Next()" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber random_int()
        {
            return new SolNumber(m_Random.Next());
        }

        /// <summary>
        ///     Generates a random number within the given interval.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="min">(Must be integer) The min value(inclusive).</param>
        /// <param name="max">(Must be integer) The max value(exclusive).</param>
        /// <returns></returns>
        /// <exception cref="SolRuntimeException"><paramref name="min" /> or <paramref name="max" /> are not an integer.</exception>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber random_int_range(SolExecutionContext context, [SolContract(SolNumber.TYPE, false)] SolNumber min, [SolContract(SolNumber.TYPE, false)] SolNumber max)
        {
            int intMin;
            if (!InternalHelper.NumberToInteger(min, out intMin)) {
                throw new SolRuntimeException(context, "The min argument has a decimal part. - " + min);
            }
            int intMax;
            if (!InternalHelper.NumberToInteger(max, out intMax)) {
                throw new SolRuntimeException(context, "The max argument has a decimal part. - " + max);
            }
            return new SolNumber(m_Random.Next(intMin, intMax));
        }

        /// <summary>
        ///     Converts the given <see cref="SolNumber" /> to an integer <see cref="SolNumber" />, stripping the decimal part.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        /// <returns>The integer representation of <paramref name="number" />.</returns>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber to_int([SolContract(SolNumber.TYPE, false)] SolNumber number)
        {
            return new SolNumber((int) number.Value);
        }

        /// <summary>
        ///     Checks if the given <see cref="SolNumber" /> is an integer, meaning does not have a decimal part.
        /// </summary>
        /// <param name="number">The number to check.</param>
        /// <returns>true if it is an integer, false if not.</returns>
        public SolBool is_int([SolContract(SolNumber.TYPE, false)] SolNumber number)
        {
            return SolBool.ValueOf(number.Value % 1 == 0);
        }
    }
}