using System;

namespace Azen.GameLoop
{
    public readonly struct GameState
    {
        public string Name { get; }

        public bool IsOff     => Name == Off.Name;
        public bool IsPlaying => Name == Play.Name;
        public bool IsPaused  => Name == Pause.Name;
        public bool IsStopped => Name == Stop.Name;

        public static readonly GameState Off   = new GameState(nameof(Off));
        public static readonly GameState Play  = new GameState(nameof(Play));
        public static readonly GameState Pause = new GameState(nameof(Pause));
        public static readonly GameState Stop  = new GameState(nameof(Stop));

        private GameState(string name) { Name = name; }

        public static GameState Custom(string name) => new GameState(name);

        public bool Equals(GameState other) => string.Equals(Name, other.Name, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameState other && Equals(other);
        public override int GetHashCode() => Name is null ? 0 : StringComparer.Ordinal.GetHashCode(Name);
        public override string ToString() => Name ?? string.Empty;

        public static bool operator ==(GameState a, GameState b) => a.Equals(b);
        public static bool operator !=(GameState a, GameState b) => !a.Equals(b);
    }
}
