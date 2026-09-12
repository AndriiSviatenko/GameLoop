using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGameResumedListener : IGameElement
    {
        UniTask OnResumeGame(CancellationToken cancellationToken);
    }
}
