using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Library.Classes {
    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Singleton)]
    [SolLibraryName("Math")]
    public class MathModule {
        // ReSharper disable InconsistentNaming
        [UsedImplicitly]
        public double sin(double value) {
            return Math.Sin(value);
        }

        [UsedImplicitly]
        public double sinh(double value) {
            return Math.Sinh(value);
        }

        [UsedImplicitly]
        public double asin(double value) {
            return Math.Asin(value);
        }

        [UsedImplicitly]
        public double cos(double value) {
            return Math.Cos(value);
        }

        [UsedImplicitly]
        public double cosh(double value) {
            return Math.Cosh(value);
        }

        [UsedImplicitly]
        public double acos(double value) {
            return Math.Acos(value);
        }

        [UsedImplicitly]
        public double tan(double value) {
            return Math.Tan(value);
        }

        [UsedImplicitly]
        public double tanh(double value) {
            return Math.Tanh(value);
        }

        [UsedImplicitly]
        public double atan(double value) {
            return Math.Atan(value);
        }

        [UsedImplicitly]
        public double abs(double value) {
            return Math.Abs(value);
        }

        [UsedImplicitly]
        public double ceil(double value) {
            return Math.Ceiling(value);
        }

        [UsedImplicitly]
        public double floor(double value) {
            return Math.Floor(value);
        }

        [UsedImplicitly]
        public double deg(double rad) {
            return rad*(180.0/Math.PI);
        }

        [UsedImplicitly]
        public double rad(double deg) {
            return Math.PI*deg/180.0;
        }

        [UsedImplicitly]
        public double exp(double value) {
            return Math.Exp(value);
        }

        [UsedImplicitly]
        public double log(double value) {
            return Math.Log(value);
        }


        [UsedImplicitly]
        public int randomseed {
            get { return m_RandomSeed; }
            set {
                if (value != m_RandomSeed) {
                    m_RandomSeed = value;
                    m_Random = new Random(value);
                }
            }
        }

        [UsedImplicitly]
        public double random() {
            return m_Random.NextDouble();
        }

        [UsedImplicitly]
        public double random_range(double min, double max) {
            return m_Random.NextDouble()*(max - min) + min;
        }

        [UsedImplicitly]
        public int random_int() {
            return m_Random.Next();
        }

        [UsedImplicitly]
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