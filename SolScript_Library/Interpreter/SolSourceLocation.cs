using System.Text;
using Irony.Parsing;
using JetBrains.Annotations;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     A SolSourceLocation is used to represent a certain location in the source file of a specific file.
    /// </summary>
    public struct SolSourceLocation
    {
        /// <summary>
        ///     The file name used when the location refers to native code.
        /// </summary>
        public const string NATIVE_FILE = "<native>";

        /// <summary>
        ///     Generates an empty Source Location for instances where the actual location cannot be determined.
        /// </summary>
        /// <param name="file">The optional file the source is in.</param>
        /// <returns>
        ///     A source location with Position, Line and Column set to -1. The file name is either an empty string,
        ///     or the one passed as argument.
        /// </returns>
        public static SolSourceLocation Empty([CanBeNull] string file = null)
        {
            return new SolSourceLocation(file ?? string.Empty, -1, -1, -1);
        }

        /// <summary>
        ///     Generates an empty Source Location for instances where the relevant location is in native code..
        /// </summary>
        /// <returns>
        ///     A source location with Position, Line and Column set to -1. The file name is set to <see cref="NATIVE_FILE" />.
        /// </returns>
        public static SolSourceLocation Native()
        {
            return new SolSourceLocation(NATIVE_FILE, -1, -1, -1);
        }

        /// <summary>
        ///     Creates a new SolSourceLocation struct from a file string and an Irony SourceLocation struct.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <param name="location">The irony location.</param>
        internal SolSourceLocation(string file, SourceLocation location)
        {
            File = file;
            Position = location.Position;
            Line = location.Line;
            Column = location.Column;
        }

        /// <summary>
        ///     Creates a new SolSourceLocation from the given parameters.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <param name="position">The exact position in the file.</param>
        /// <param name="line">The line index.</param>
        /// <param name="column">The column index in the line.</param>
        public SolSourceLocation(string file, int position, int line, int column)
        {
            File = file;
            Position = position;
            Line = line;
            Column = column;
        }

        /// <summary>
        ///     The file name this source location is in.
        /// </summary>
        public readonly string File;

        /// <summary>
        ///     The column index in the given line this source location is at.
        /// </summary>
        public readonly int Column;

        /// <summary>
        ///     The line index in the given file this source location is at.
        /// </summary>
        public readonly int Line;

        /// <summary>
        ///     The exact character index in the given file this source location is at.
        /// </summary>
        public readonly int Position;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(File.Length + 5);
            builder.Append(File);
            if (Position >= 0) {
                builder.Append(":");
                builder.Append(Line + 1);
                builder.Append(":");
                builder.Append(Column + 1);
            }
            return builder.ToString();
        }

        [Pure]
        public override int GetHashCode()
        {
            unchecked {
                int hashCode = File?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ Column;
                hashCode = (hashCode * 397) ^ Line;
                hashCode = (hashCode * 397) ^ Position;
                return hashCode;
            }
        }

        [Pure]
        public bool Equals(SolSourceLocation other)
        {
            return string.Equals(File, other.File) && Column == other.Column && Line == other.Line && Position == other.Position;
        }

        public static bool operator ==(SolSourceLocation obj1, SolSourceLocation obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator ==(SolSourceLocation obj1, object obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(SolSourceLocation obj1, object obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(SolSourceLocation obj1, SolSourceLocation obj2)
        {
            return !obj1.Equals(obj2);
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is SolSourceLocation && Equals((SolSourceLocation) obj);
        }

        [Pure]
        public static int Compare(SolSourceLocation x, SolSourceLocation y)
        {
            if (x.Position < y.Position) {
                return -1;
            }
            return x.Position == y.Position ? 0 : 1;
        }
    }
}