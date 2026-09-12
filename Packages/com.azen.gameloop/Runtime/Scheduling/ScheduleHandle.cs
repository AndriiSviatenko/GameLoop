using System;

namespace Azen.GameLoop
{
    public readonly struct ScheduleHandle : IEquatable<ScheduleHandle>
    {
        public int Id { get; }
        public bool IsValid => Id != 0;

        internal ScheduleHandle(int id) { Id = id; }

        public static readonly ScheduleHandle Empty = default;

        public bool Equals(ScheduleHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is ScheduleHandle other && Equals(other);
        public override int GetHashCode() => Id;

        public static bool operator ==(ScheduleHandle a, ScheduleHandle b) => a.Id == b.Id;
        public static bool operator !=(ScheduleHandle a, ScheduleHandle b) => a.Id != b.Id;
    }
}
