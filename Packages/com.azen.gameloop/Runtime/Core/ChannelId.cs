using System;

namespace Azen.GameLoop
{
    public readonly struct ChannelId : IEquatable<ChannelId>
    {
        public string Name { get; }

        public static readonly ChannelId Default  = new ChannelId(nameof(Default));
        public static readonly ChannelId Gameplay = new ChannelId(nameof(Gameplay));
        public static readonly ChannelId UI       = new ChannelId(nameof(UI));
        public static readonly ChannelId Cutscene = new ChannelId(nameof(Cutscene));

        internal ChannelId(string name) { Name = name; }

        public static ChannelId Custom(string name) => new ChannelId(name);

        internal ChannelId Normalized() => string.IsNullOrEmpty(Name) ? Default : this;

        public bool Equals(ChannelId other) => string.Equals(Normalized().Name, other.Normalized().Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ChannelId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Normalized().Name);
        public override string ToString() => Normalized().Name ?? string.Empty;

        public static bool operator ==(ChannelId a, ChannelId b) => a.Equals(b);
        public static bool operator !=(ChannelId a, ChannelId b) => !a.Equals(b);

        public static implicit operator ChannelId(string name) => new ChannelId(name);
        public static implicit operator string(ChannelId channel) => channel.Normalized().Name;
    }
}
