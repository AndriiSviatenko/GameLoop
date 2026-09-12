using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azen.GameLoop.Demo
{
    [DefaultExecutionOrder(-2000)]
    public sealed class GameLoopDemoBootstrap : MonoBehaviour
    {
        private IGameLoop _loop;

        private void Awake()
        {
            DemoSceneBuilder.Build();

            _loop = gameObject.AddComponent<GameLoopDriver>().Bootstrap(logger: DebugLogGameLoopLogger.Instance);

            var balls = new BallRegistry();
            var ballFactory = new BallFactory(_loop, balls);
            var score = new ScoreSystem();
            var timer = new RoundTimer(_loop);

            _loop.Add(score, GameLoopChannels.Gameplay, GameLoopPriority.High);
            _loop.Add(timer, GameLoopChannels.Gameplay, GameLoopPriority.Normal);
            _loop.Add(new BallSpawner(_loop, ballFactory, balls), GameLoopChannels.Gameplay, GameLoopPriority.Normal);
            _loop.Add(new BallCollisionSystem(balls), GameLoopChannels.Gameplay, GameLoopPriority.Low);

            _loop.Add(new GameInputSystem(_loop), GameLoopChannels.UI, GameLoopPriority.Critical);
            _loop.SetChannelTickWhenPaused(GameLoopChannels.UI, true);

            gameObject.AddComponent<DemoFocusHandler>().Construct(_loop);
            gameObject.AddComponent<GameHud>().Construct(_loop, score, timer);
        }

        private void Start() => _loop.StartAsync(destroyCancellationToken).Forget();
    }
}
