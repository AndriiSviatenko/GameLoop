using Azen.GameLoop.Infrastructure;

namespace Azen.GameLoop.Demo
{
    public sealed class DemoFocusHandler : GameLoopFocusHandlerBase
    {
        private IGameLoop _loop;

        public override IGameLoop GameLoop => _loop;

        public void Construct(IGameLoop loop) => _loop = loop;
    }
}
