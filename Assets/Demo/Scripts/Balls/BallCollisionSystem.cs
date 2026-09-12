using UnityEngine;

namespace Azen.GameLoop.Demo
{
    public sealed class BallCollisionSystem : IGameElement, IGameLateTickable
    {
        private readonly BallRegistry _registry;

        public BallCollisionSystem(BallRegistry registry) => _registry = registry;

        public void LateTick(float deltaTime)
        {
            var balls = _registry.Balls;
            const float minDistance = DemoConfig.Ball.Diameter;

            for (int iteration = 0; iteration < DemoConfig.Ball.CollisionSolverIterations; iteration++)
            {
                for (int i = 0; i < balls.Count; i++)
                {
                    for (int j = i + 1; j < balls.Count; j++)
                        ResolvePair(balls[i], balls[j], minDistance);
                }
            }
        }

        private static void ResolvePair(FallingBall first, FallingBall second, float minDistance)
        {
            var offset = first.transform.position - second.transform.position;
            var distanceSquared = offset.sqrMagnitude;

            if (distanceSquared >= minDistance * minDistance) return;
            if (distanceSquared < DemoConfig.Ball.MinCollisionDistanceSquared) return;

            var distance = Mathf.Sqrt(distanceSquared);
            var normal = offset / distance;
            var correction = normal * ((minDistance - distance) * 0.5f);
            first.transform.position += correction;
            second.transform.position -= correction;

            var velocityAlongNormal = Vector3.Dot(first.Velocity - second.Velocity, normal);
            if (velocityAlongNormal >= 0f) return;

            var impulse = normal * (velocityAlongNormal * DemoConfig.Ball.CollisionRestitution);
            first.Velocity -= impulse;
            second.Velocity += impulse;
        }
    }
}
