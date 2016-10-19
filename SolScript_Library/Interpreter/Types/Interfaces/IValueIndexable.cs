namespace SolScript.Interpreter.Types.Interfaces {
    public interface IValueIndexable {
        SolValue this[SolValue key] { get; set; }
    }
}