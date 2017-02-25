using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Options for creating a SolAssembly.
    /// </summary>
    public sealed class SolAssemblyOptions : ICloneable
    {
        /// <summary>
        ///     Creates a new options instance.
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">The <paramref name="name" /> cannot be null.</exception>
        public SolAssemblyOptions([NotNull] string name)
        {
            m_SourceFilePattern = "*.sol";
            WarningsAreErrors = false;
            Name = name;
        }

        private string m_Name;
        private string m_SourceFilePattern;

        /// <summary>
        ///     The wildcard pattern for identifying source files. (Default: "*.sol")
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">
        ///     Cannot set source file pattern to
        ///     null. <paramref name="value" />
        /// </exception>
        [NotNull]
        public string SourceFilePattern {
            get { return m_SourceFilePattern; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value), "Cannot set source file pattern to null.");
                }
                m_SourceFilePattern = value;
            }
        }

        /// <summary>
        ///     Shoulds warnings be treated are errors? (Default: false)
        /// </summary>
        public bool WarningsAreErrors { get; set; }

        /// <summary>
        ///     The name of the assembly.
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">Cannot set name to null. <paramref name="value" /></exception>
        [NotNull]
        public string Name {
            get { return m_Name; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value), "Cannot set name to null.");
                }
                m_Name = value;
            }
        }

        #region ICloneable Members

        /// <inheritdoc />
        public object Clone()
        {
            SolAssemblyOptions options = new SolAssemblyOptions(m_Name) {
                m_SourceFilePattern = m_SourceFilePattern,
                WarningsAreErrors = WarningsAreErrors
            };
            return options;
        }

        #endregion
    }
}