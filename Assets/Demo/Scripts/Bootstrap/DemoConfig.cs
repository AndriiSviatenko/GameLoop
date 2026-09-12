namespace Azen.GameLoop.Demo
{
    public static class DemoConfig
    {
        public static class Scene
        {
            public const float FloorWidth = 20f;
            public const float FloorDepth = 10f;
            public const float FloorHeight = 1f;
            public const float FloorTopY = FloorHeight * 0.5f;
            public const float FloorCenterY = FloorTopY - FloorHeight * 0.5f;

            public const float SpawnAreaMinX = -4f;
            public const float SpawnAreaMaxX = 4f;
            public const float SpawnAreaMinZ = -2f;
            public const float SpawnAreaMaxZ = 2f;
            public const float SpawnHeight = 8f;
        }

        public static class Ball
        {
            public const float Radius = 0.5f;
            public const float Diameter = Radius * 2f;
            public const float Gravity = -9.81f;
            public const float BounceRestitution = 0.75f;
            public const float MinBounceVelocity = 0.5f;
            public const float FloorRestOffset = Radius;
            public const float FloorY = Scene.FloorTopY + FloorRestOffset;
            public const float LifetimeSeconds = 12f;
            public const float CollisionRestitution = 0.6f;
            public const int CollisionSolverIterations = 3;
            public const float MinCollisionDistanceSquared = 0.0001f;
        }

        public static class Spawner
        {
            public const float IntervalSeconds = 1.2f;
            public const int MaxSpawnAttempts = 10;
            public const float MinSpawnSeparation = Ball.Diameter;
        }

        public static class Round
        {
            public const float DurationSeconds = 30f;
        }

        public static class Camera
        {
            public const float PositionX = 0f;
            public const float PositionY = 4f;
            public const float PositionZ = -12f;
        }

        public static class TimeScale
        {
            public const float Normal = 1f;
            public const float SlowMotion = 0.25f;
        }

        public static class Score
        {
            public const int PointsPerSecond = 10;
        }

        public static class Colors
        {
            public const float MinGreenChannel = 0.3f;
            public const float MaxGreenChannel = 1f;
        }
    }
}
