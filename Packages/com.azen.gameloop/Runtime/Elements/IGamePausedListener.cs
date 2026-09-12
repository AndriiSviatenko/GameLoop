using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGamePausedListener : IGameElement
    {
        UniTask OnPauseGame(CancellationToken cancellationToken);
    }
}
