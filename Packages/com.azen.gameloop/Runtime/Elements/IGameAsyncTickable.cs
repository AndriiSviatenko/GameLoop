using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGameAsyncTickable : IGameElement
    {
        UniTask TickAsync(float deltaTime, CancellationToken cancellationToken);
    }
}
