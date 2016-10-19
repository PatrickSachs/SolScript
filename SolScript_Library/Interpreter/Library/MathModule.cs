using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Library {
    [SolLibraryClass("std", TypeDef.TypeMode.Singleton)]
    [SolLibraryName("Math")]
    public class MathModule {
        /* public override string ModuleName => "Math";

        public override SolTable GetModule() {
            TypeDef mathLib = new TypeDef {
                Name = "Math",
                Fields = new [] {
                    
                }
            };
            SolTable table = new SolTable();
            Variable(table, "pi", new SolNumber(Math.PI));
            Variable(table, "e", new SolNumber(Math.E));
            Function(table, "sin");
            Function(table, "sinh");
            Function(table, "asin");
            Function(table, "cos");
            Function(table, "cosh");
            Function(table, "acos");
            Function(table, "tan");
            Function(table, "tanh");
            Function(table, "atan");
            Function(table, "abs");
            Function(table, "ceil");
            Function(table, "floor");
            Function(table, "deg");
            Function(table, "rad");
            Function(table, "exp");
            Function(table, "log");
            return table;
        }*/

        // ReSharper disable InconsistentNaming
        [SolLibraryVisibility("std", true)]
        private double sin(double value) {
            return Math.Sin(value);
        }

        [SolLibraryVisibility("std", true)]
        private double sinh(double value) {
            return Math.Sinh(value);
        }

        [SolLibraryVisibility("std", true)]
        private double asin(double value) {
            return Math.Asin(value);
        }

        [SolLibraryVisibility("std", true)]
        private double cos(double value) {
            return Math.Cos(value);
        }

        [SolLibraryVisibility("std", true)]
        private double cosh(double value) {
            return Math.Cosh(value);
        }

        [SolLibraryVisibility("std", true)]
        private double acos(double value) {
            return Math.Acos(value);
        }

        [SolLibraryVisibility("std", true)]
        private double tan(double value) {
            return Math.Tan(value);
        }

        [SolLibraryVisibility("std", true)]
        private double tanh(double value) {
            return Math.Tanh(value);
        }

        [SolLibraryVisibility("std", true)]
        private double atan(double value) {
            return Math.Atan(value);
        }

        [SolLibraryVisibility("std", true)]
        private double abs(double value) {
            return Math.Abs(value);
        }

        [SolLibraryVisibility("std", true)]
        private double ceil(double value) {
            return Math.Ceiling(value);
        }

        [SolLibraryVisibility("std", true)]
        private double floor(double value) {
            return Math.Floor(value);
        }

        [SolLibraryVisibility("std", true)]
        public double deg(double rad) {
            return rad*(180.0/Math.PI);
        }

        [SolLibraryVisibility("std", true)]
        public double rad(double deg) {
            return Math.PI*deg/180.0;
        }

        [SolLibraryVisibility("std", true)]
        public double exp(double value) {
            return Math.Exp(value);
        }

        [SolLibraryVisibility("std", true)]
        public double log(double value) {
            return Math.Log(value);
        }

        [SolLibraryVisibility("std", true)]
        [UsedImplicitly]
        public double random()
        {
            return m_Random.NextDouble();
        }

        [SolLibraryVisibility("std", true)]
        [UsedImplicitly]
        public int random_int_range(int min, int max) {
            return m_Random.Next(min, max);
        }

        private static readonly Random m_Random = new Random();

        // ReSharper restore InconsistentNaming
    }
}