using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Implement this interface in your custom classes if you wish to obtain a reference to the class itself.
    /// </summary>
    public interface INativeClassSelf
    {
        /// <summary>
        ///     The class reference.<br /> Only available after the SolScript and native constructor has been called.<br />
        ///     Guaranteed to only be assigned once. This means you can use the setter as a callback once the SolClass has been
        ///     initialized.
        /// </summary>
        SolClass Self { get; set; }
    }
}