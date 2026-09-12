using System.Collections.Generic;

namespace Azen.GameLoop.Demo
{
    public sealed class BallRegistry
    {
        private readonly List<FallingBall> _balls = new();

        public IReadOnlyList<FallingBall> Balls => _balls;

        public void Add(FallingBall ball) => _balls.Add(ball);
        public void Remove(FallingBall ball) => _balls.Remove(ball);
    }
}
