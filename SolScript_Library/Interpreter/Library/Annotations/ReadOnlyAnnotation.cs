using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Annotations {
    [SolLibraryClass("std", SolTypeMode.Annotation)]
    [SolLibraryName("readonly")]
    public class ReadonlyAnnotation {
        private bool m_DidSet;

        [CanBeNull, UsedImplicitly]
        public SolTable __a_set_var(SolExecutionContext context, SolValue value, SolValue rawValue) {
            if (m_DidSet) {
                throw new SolRuntimeException(context, "Tried to assign a value to a @readonly variable.");
            }
            m_DidSet = true;
            return null;
        }

        public override string ToString() {
            return "read_only - Assignable? " + (!m_DidSet);
        }
    }
}