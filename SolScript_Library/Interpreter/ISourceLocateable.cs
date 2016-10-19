using Irony.Parsing;

namespace SolScript.Interpreter {
    public interface ISourceLocateable {
        SourceLocation Location { get; set; }
    }
}