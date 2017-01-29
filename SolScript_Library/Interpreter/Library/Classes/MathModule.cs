using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Library.Classes {
    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Singleton)]
    [SolLibraryName("Math")]
    public class MathModule {
        // ReSharper disable InconsistentNaming
        [PublicAPI]
        public bool is_nan(double value)
        {
            return double.IsNaN(value);
        }
        [PublicAPI]
        public bool is_infinity(double value)
        {
            return double.IsInfinity(value);
        }

        [PublicAPI]
        public double sin(double value) {
            return Math.Sin(value);
        }

        [PublicAPI]
        public double sinh(double value) {
            return Math.Sinh(value);
        }

        [PublicAPI]
        public double asin(double value) {
            return Math.Asin(value);
        }

        [PublicAPI]
        public double cos(double value) {
            return Math.Cos(value);
        }

        [PublicAPI]
        public double cosh(double value) {
            return Math.Cosh(value);
        }

        [PublicAPI]
        public double acos(double value) {
            return Math.Acos(value);
        }

        [PublicAPI]
        public double tan(double value) {
            return Math.Tan(value);
        }

        [PublicAPI]
        public double tanh(double value) {
            return Math.Tanh(value);
        }

        [PublicAPI]
        public double atan(double value) {
            return Math.Atan(value);
        }

        [PublicAPI]
        public double abs(double value) {
            return Math.Abs(value);
        }

        [PublicAPI]
        public double ceil(double value) {
            return Math.Ceiling(value);
        }

        [PublicAPI]
        public double floor(double value) {
            return Math.Floor(value);
        }

        [PublicAPI]
        public double deg(double rad) {
            return rad*(180.0/Math.PI);
        }

        [PublicAPI]
        public double rad(double deg) {
            return Math.PI*deg/180.0;
        }

        [PublicAPI]
        public double exp(double value) {
            return Math.Exp(value);
        }

        [PublicAPI]
        public double log(double value) {
            return Math.Log(value);
        }

        [PublicAPI]
        public int randomseed {
            get { return m_RandomSeed; }
            set {
                if (value != m_RandomSeed) {
                    m_RandomSeed = value;
                    m_Random = new Random(value);
                }
            }
        }

        [PublicAPI]
        public double random() {
            return m_Random.NextDouble();
        }

        [PublicAPI]
        public double random_range(double min, double max) {
            return m_Random.NextDouble()*(max - min) + min;
        }

        [PublicAPI]
        public int random_int() {
            return m_Random.Next();
        }

        [PublicAPI]
        public int random_int_range(int min, int max) {
            return m_Random.Next(min, max);
        }

        [SolLibraryVisibility(SolLibrary.STD_NAME, false)] private int m_RandomSeed;
        [SolLibraryVisibility(SolLibrary.STD_NAME, false)] private Random m_Random;

        public MathModule() {
            m_RandomSeed = Environment.TickCount;
            m_Random = new Random(m_RandomSeed);
        }
        public override string ToString()
        {
            return "Math Module";
        }
        // ReSharper restore InconsistentNaming
    }
}