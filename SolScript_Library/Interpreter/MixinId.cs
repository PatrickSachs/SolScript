using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public struct MixinId {
        internal readonly int Index;
        internal readonly SolClass Instance;

        public MixinId(SolClass instance, int index) {
            Index = index;
            Instance = instance;
        }

        public bool Equals(MixinId other) {
            return Index == other.Index && Equals(Instance, other.Instance);
        }

        public override bool Equals([CanBeNull] object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MixinId && Equals((MixinId) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (Index*397) ^ (Instance != null ? Instance.GetHashCode() : 0);
            }
        }
    }
}