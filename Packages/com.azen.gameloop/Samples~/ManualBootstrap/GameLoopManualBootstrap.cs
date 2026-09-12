using System.Threading;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azen.GameLoop.Samples.Manual
{
    public sealed class GameLoopManualBootstrap : MonoBehaviour
    {
        [SerializeField] private GameLoopDriver _driver;

        private IGameLoop _loop;

        private void Awake()
        {
            if (_driver == null)
                _driver = gameObject.AddComponent<GameLoopDriver>();

            _loop = _driver.Bootstrap(logger: DebugLogGameLoopLogger.Instance);
            _loop.For(new EnemyAi()).Gameplay().High().Add();
        }

        private void Start() => _loop.StartAsync(destroyCancellationToken).Forget();
    }

    public sealed class EnemyAi : IGameTickable, IGameStartedListener
    {
        public UniTask OnStartGame(CancellationToken cancellationToken) => UniTask.CompletedTask;

        public void Tick(float deltaTime) { }
    }
}
