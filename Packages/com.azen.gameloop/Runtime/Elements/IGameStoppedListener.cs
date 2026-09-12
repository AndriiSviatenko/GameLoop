using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGameStoppedListener : IGameElement
    {
        UniTask OnStopGame(CancellationToken cancellationToken);
    }
}
