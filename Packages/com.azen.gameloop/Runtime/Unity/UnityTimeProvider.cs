using UnityEngine;

namespace Azen.GameLoop.Infrastructure
{
    public sealed class UnityTimeProvider : ITimeProvider
    {
        public static readonly UnityTimeProvider Instance = new UnityTimeProvider();

        public float DeltaTime      => Time.deltaTime;
        public float FixedDeltaTime => Time.fixedDeltaTime;
    }
}
