using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Annotations {
    [SolLibraryClass("std", SolTypeMode.Annotation)]
    [SolLibraryName("read_only")]
    public class ReadOnlyAnnotation {
        private bool m_DidSet;

        [CanBeNull, UsedImplicitly]
        public SolTable __a_set_var(SolExecutionContext context, SolValue value, SolValue rawValue) {
            if (m_DidSet) {
                throw SolScriptInterpreterException.IllegalAccessName(context, "??",
                    "Tried to assign a value to a read_only variable.");
            }
            m_DidSet = true;
            return null;
        }

        public override string ToString() {
            return "read_only - Assignable? " + (!m_DidSet);
        }
    }
}