using System;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Azen.GameLoop.Samples.VContainer
{
    public sealed class GameLoopLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameLoopDriver _driver;

        protected override void Configure(IContainerBuilder builder)
        {
            var loop = _driver.Bootstrap(logger: DebugLogGameLoopLogger.Instance);

            builder.RegisterInstance(loop).As<IGameLoop, IGameManager>();
            builder.RegisterEntryPoint<EnemyAi>();
            builder.RegisterEntryPoint<GameLoopStarter>();
        }
    }

    public sealed class GameLoopStarter : IStartable
    {
        private readonly IGameLoop _loop;

        public GameLoopStarter(IGameLoop loop) => _loop = loop;

        public void Start() => _loop.StartAsync().Forget();
    }

    public sealed class EnemyAi : IGameTickable, IInitializable, IDisposable
    {
        private readonly IGameLoop _loop;
        private GameLoopElementHandle _handle;

        public EnemyAi(IGameLoop loop) => _loop = loop;

        public void Initialize() => _handle = _loop.For(this).Gameplay().High().Add();
        public void Dispose() => _loop.Remove(_handle);
        public void Tick(float deltaTime) { }
    }
}
