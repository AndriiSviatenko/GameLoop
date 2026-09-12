using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGameStartedListener : IGameElement
    {
        UniTask OnStartGame(CancellationToken cancellationToken);
    }
}
