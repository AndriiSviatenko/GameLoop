using System;

namespace Azen.GameLoop
{
    public enum PauseReasonKind
    {
        Manual,
        System,
    }

    public readonly struct PauseReason : IEquatable<PauseReason>
    {
        public string Name { get; }
        public PauseReasonKind Kind { get; }

        public static readonly PauseReason Manual   = new PauseReason(nameof(Manual));
        public static readonly PauseReason Menu     = new PauseReason(nameof(Menu));
        public static readonly PauseReason Dialog   = new PauseReason(nameof(Dialog));
        public static readonly PauseReason Focus    = new PauseReason(nameof(Focus), PauseReasonKind.System);
        public static readonly PauseReason Cutscene = new PauseReason(nameof(Cutscene), PauseReasonKind.System);

        internal PauseReason(string name, PauseReasonKind kind = PauseReasonKind.Manual)
        {
            Name = name ?? string.Empty;
            Kind = kind;
        }

        public static PauseReason Custom(string name, PauseReasonKind kind = PauseReasonKind.Manual)
            => new PauseReason(name, kind);

        public bool Equals(PauseReason other) => string.Equals(Name, other.Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PauseReason other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);
        public override string ToString() => Name ?? string.Empty;

        public static bool operator ==(PauseReason a, PauseReason b) => a.Equals(b);
        public static bool operator !=(PauseReason a, PauseReason b) => !a.Equals(b);
    }
}
