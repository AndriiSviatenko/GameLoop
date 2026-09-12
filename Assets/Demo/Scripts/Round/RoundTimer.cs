using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop.Demo
{
    public sealed class RoundTimer : IGameElement, IGameInitListener, IGameTickable
    {
        private readonly IGameManager _gameManager;

        public RoundTimer(IGameManager gameManager) => _gameManager = gameManager;

        public float RemainingSeconds { get; private set; }
        public bool IsExpired => RemainingSeconds <= 0f;

        public UniTask OnInit(CancellationToken cancellationToken)
        {
            RemainingSeconds = DemoConfig.Round.DurationSeconds;
            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            if (IsExpired) return;

            RemainingSeconds -= deltaTime;
            if (!IsExpired) return;

            RemainingSeconds = 0f;
            _gameManager.StopAsync().Forget();
        }
    }
}
