using System;

namespace SolScript.Interpreter.Library
{
    /// <summary>
    ///     Hides or exposes a single member to SolScript.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public class SolVisibilityAttribute : Attribute
    {
        /// <summary>
        ///     Hides or exposes this member to SolScript. Keep in mind that some containers(such as classes) will require
        ///     additional attributes on itself in order to be exposed.
        /// </summary>
        /// <param name="visible">Should the member be visible?</param>
        public SolVisibilityAttribute(bool visible)
        {
            Visible = visible;
        }

        /// <summary>
        ///     Is the member this attribute belongs to exposed to SolScript or expicitly hidden?
        /// </summary>
        public bool Visible { get; }
    }
}