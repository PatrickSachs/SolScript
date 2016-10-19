﻿using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Library {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public class SolLibraryVisibilityAttribute : Attribute {
        public SolLibraryVisibilityAttribute([NotNull] string libName, bool visible) {
            LibraryName = libName;
            Visible = visible;
        }

        public readonly string LibraryName;
        public readonly bool Visible;
    }
}