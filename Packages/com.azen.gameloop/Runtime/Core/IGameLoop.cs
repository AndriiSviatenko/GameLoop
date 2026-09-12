using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public interface IGameLoop : IGameManager
    {
        GameLoopElementHandle Add(IGameElement element, string channel = GameLoopChannels.Default, int priority = 0);
        void Remove(GameLoopElementHandle handle);
        void Remove(IGameElement element);
        bool Contains(IGameElement element);

        event Action<string, GameState> ChannelStateChanged;

        UniTask PauseChannelAsync(string channel, CancellationToken cancellationToken);
        UniTask ResumeChannelAsync(string channel, CancellationToken cancellationToken);
        GameState GetChannelState(string channel);

        UniTask InitializeAsync(CancellationToken cancellationToken);
        void Destroy();
        bool IsInitialized { get; }

        void SetChannelTimeScale(string channel, float scale);
        float GetChannelTimeScale(string channel);

        void SetChannelTickWhenPaused(string channel, bool value);

        GameState SubState { get; }
        event Action<GameState> SubStateChanged;
        void SetSubState(GameState state);

        bool ThrowOnReentrancy { get; set; }
        float TickBudgetMs { get; set; }

        GameLoopScheduler Scheduler { get; }
    }
}
