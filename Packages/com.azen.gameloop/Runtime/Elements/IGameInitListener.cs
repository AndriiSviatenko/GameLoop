using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGameInitListener : IGameElement
    {
        UniTask OnInit(CancellationToken cancellationToken);
    }
}
