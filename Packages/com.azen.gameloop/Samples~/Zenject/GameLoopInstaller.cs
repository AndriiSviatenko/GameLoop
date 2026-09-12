using System;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Azen.GameLoop.Samples.Zenject
{
    public sealed class GameLoopInstaller : MonoInstaller
    {
        [SerializeField] private GameLoopDriver _driver;

        public override void InstallBindings()
        {
            var loop = _driver.Bootstrap(logger: DebugLogGameLoopLogger.Instance);

            Container.Bind(typeof(IGameLoop), typeof(IGameManager)).FromInstance(loop);
            Container.BindInterfacesTo<EnemyAi>().AsSingle();
            Container.BindInterfacesTo<GameLoopStarter>().AsSingle();
            Container.BindExecutionOrder<GameLoopStarter>(100);
        }
    }

    public sealed class GameLoopStarter : IInitializable
    {
        private readonly IGameLoop _loop;

        public GameLoopStarter(IGameLoop loop) => _loop = loop;

        public void Initialize() => _loop.StartAsync().Forget();
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
