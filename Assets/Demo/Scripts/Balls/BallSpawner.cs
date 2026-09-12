using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azen.GameLoop.Demo
{
    public sealed class BallSpawner : IGameElement, IGameStartedListener, IGameStoppedListener
    {
        private readonly IGameLoop _loop;
        private readonly BallFactory _factory;
        private readonly BallRegistry _registry;
        private ScheduleHandle _spawnSchedule;

        public BallSpawner(IGameLoop loop, BallFactory factory, BallRegistry registry)
        {
            _loop = loop;
            _factory = factory;
            _registry = registry;
        }

        public UniTask OnStartGame(CancellationToken cancellationToken)
        {
            _spawnSchedule = _loop.Every(DemoConfig.Spawner.IntervalSeconds, Spawn, GameLoopChannels.Gameplay);
            return UniTask.CompletedTask;
        }

        public UniTask OnStopGame(CancellationToken cancellationToken)
        {
            _loop.Cancel(_spawnSchedule);
            _spawnSchedule = ScheduleHandle.Empty;

            foreach (var ball in _registry.Balls)
                Object.Destroy(ball.gameObject);

            return UniTask.CompletedTask;
        }

        private void Spawn() => _factory.Create(FindSpawnPosition());

        private Vector3 FindSpawnPosition()
        {
            for (int attempt = 0; attempt < DemoConfig.Spawner.MaxSpawnAttempts; attempt++)
            {
                var position = new Vector3(
                    Random.Range(DemoConfig.Scene.SpawnAreaMinX, DemoConfig.Scene.SpawnAreaMaxX),
                    DemoConfig.Scene.SpawnHeight,
                    Random.Range(DemoConfig.Scene.SpawnAreaMinZ, DemoConfig.Scene.SpawnAreaMaxZ));

                if (!IsTooCloseToExistingBall(position))
                    return position;
            }

            return new Vector3(0f, DemoConfig.Scene.SpawnHeight, 0f);
        }

        private bool IsTooCloseToExistingBall(Vector3 position)
        {
            const float minSeparationSquared = DemoConfig.Spawner.MinSpawnSeparation * DemoConfig.Spawner.MinSpawnSeparation;

            foreach (var ball in _registry.Balls)
            {
                if ((ball.transform.position - position).sqrMagnitude < minSeparationSquared)
                    return true;
            }

            return false;
        }
    }
}
