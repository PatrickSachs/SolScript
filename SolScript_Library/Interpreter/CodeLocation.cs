using Irony.Parsing;
using JetBrains.Annotations;

namespace SolScript.Interpreter {
    public struct CodeLocation {
        public readonly string File;
        public readonly int Position;
        public readonly int Line;
        public readonly int Column;

        public CodeLocation(string file, int position, int line, int column)
        {
            File = file;
            Position = position;
            Line = line;
            Column = column;
        }
        public CodeLocation(string file, SourceLocation location)
        {
            File = file;
            Position = location.Position;
            Line = location.Line;
            Column = location.Column;
        }

        public bool Equals(CodeLocation other) {
            return string.Equals(File, other.File) && Position == other.Position && Line == other.Line && Column == other.Column;
        }
        
        public override bool Equals([CanBeNull] object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CodeLocation && Equals((CodeLocation) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = File?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ Position;
                hashCode = (hashCode*397) ^ Line;
                hashCode = (hashCode*397) ^ Column;
                return hashCode;
            }
        }

        public override string ToString() {
            return $"({File}:{Line}:{Column})";
        }
    }
}