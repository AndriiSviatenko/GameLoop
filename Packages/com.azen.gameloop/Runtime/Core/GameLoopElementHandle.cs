using System;

namespace Azen.GameLoop
{
    public readonly struct GameLoopElementHandle : IEquatable<GameLoopElementHandle>
    {
        public int Id { get; }
        public string Channel { get; }
        public IGameElement Element { get; }

        public bool IsValid => Id != 0;

        internal GameLoopElementHandle(int id, string channel, IGameElement element)
        {
            Id = id;
            Channel = channel;
            Element = element;
        }

        public static readonly GameLoopElementHandle Empty = default;

        public bool Equals(GameLoopElementHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is GameLoopElementHandle other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => IsValid ? $"#{Id} on '{Channel}' ({Element?.GetType().Name})" : "Empty";

        public static bool operator ==(GameLoopElementHandle a, GameLoopElementHandle b) => a.Id == b.Id;
        public static bool operator !=(GameLoopElementHandle a, GameLoopElementHandle b) => a.Id != b.Id;
    }
}
