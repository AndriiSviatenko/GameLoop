using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGameManager
    {
        GameState State { get; }
        event Action<GameState> StateChanged;

        UniTask StartAsync(CancellationToken cancellationToken);
        UniTask PauseAsync(CancellationToken cancellationToken);
        UniTask PauseFor(PauseReason reason, CancellationToken cancellationToken);
        UniTask ResumeAsync(CancellationToken cancellationToken);
        UniTask ResumeAllAsync(CancellationToken cancellationToken);
        UniTask ResumeFor(PauseReason reason, CancellationToken cancellationToken);
        UniTask StopAsync(CancellationToken cancellationToken);
        UniTask RestartAsync(CancellationToken cancellationToken);

        bool IsPausedFor(PauseReason reason);
        IEnumerable<PauseReason> PauseReasons { get; }
    }
}
