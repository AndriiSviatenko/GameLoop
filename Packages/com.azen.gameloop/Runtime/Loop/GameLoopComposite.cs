using System;
using System.Collections.Generic;
using System.Threading;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public sealed class GameLoopComposite
    {
        private readonly Dictionary<string, GameLoopGroup> _groups = new();
        private readonly HashSet<IGameElement> _allElements = new();
        private readonly IGameLoopLogger _logger;
        private readonly CallbackTracker _callbacks;
        private int _nextId = 1;
        private bool _throwOnErrors;
        private float _tickBudgetMs;

        public GameLoopComposite(IGameLoopLogger logger = null) : this(logger, new CallbackTracker()) { }

        internal GameLoopComposite(IGameLoopLogger logger, CallbackTracker callbacks)
        {
            _logger = logger ?? NullGameLoopLogger.Instance;
            _callbacks = callbacks;
            GetOrCreateGroup(GameLoopChannels.Default);
        }

        public GameLoopElementHandle Add(IGameElement element, string channel, int priority)
        {
            var resolved = channel ?? GameLoopChannels.Default;
            var group = GetOrCreateGroup(resolved);
            group.Add(element, priority);
            _allElements.Add(element);
            return new GameLoopElementHandle(_nextId++, resolved, element);
        }

        public void Remove(GameLoopElementHandle handle)
        {
            if (!handle.IsValid) return;
            if (_groups.TryGetValue(handle.Channel, out var group))
                group.Remove(handle.Element);
            _allElements.Remove(handle.Element);
        }

        public void Remove(IGameElement element)
        {
            if (element == null) return;
            foreach (var kv in _groups)
                kv.Value.Remove(element);
            _allElements.Remove(element);
        }

        public bool Contains(IGameElement element)
            => element != null && _allElements.Contains(element);

        public IEnumerable<string> Channels => _groups.Keys;

        public bool HasChannel(string channel)
            => _groups.ContainsKey(channel ?? GameLoopChannels.Default);

        public GameLoopGroup GetGroup(string channel)
            => GetOrCreateGroup(channel ?? GameLoopChannels.Default);

        public GameState GetChannelState(string channel, GameState globalState)
            => _groups.TryGetValue(channel ?? GameLoopChannels.Default, out var group)
                ? group.GetState(globalState)
                : globalState;

        public bool CanTick(string channel, GameState globalState)
            => _groups.TryGetValue(channel ?? GameLoopChannels.Default, out var group)
                ? group.CanTick(globalState)
                : globalState.IsPlaying;

        public void SetChannelTimeScale(string channel, float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0f)
                throw new ArgumentOutOfRangeException(nameof(scale), "Channel time scale must be finite and >= 0");

            GetOrCreateGroup(channel ?? GameLoopChannels.Default).TimeScale = scale;
        }

        public void SetChannelTickWhenPaused(string channel, bool value)
        {
            GetOrCreateGroup(channel ?? GameLoopChannels.Default).TickWhenGlobalPaused = value;
        }

        public float GetChannelTimeScale(string channel)
            => _groups.TryGetValue(channel ?? GameLoopChannels.Default, out var group) ? group.TimeScale : 1f;

        public async UniTask DispatchInitAsync(CancellationToken ct)
        {
            foreach (var group in SnapshotGroups())
                await group.DispatchInitAsync(ct);
        }

        public async UniTask DispatchStartAsync(CancellationToken ct)
        {
            foreach (var group in SnapshotGroups())
                await group.DispatchStartAsync(ct);
        }

        public async UniTask DispatchPauseAsync(CancellationToken ct)
        {
            foreach (var group in SnapshotGroups())
            {
                if (group.IsChannelPaused) continue;
                await group.DispatchPauseAsync(ct);
            }
        }

        public async UniTask DispatchResumeAsync(CancellationToken ct)
        {
            foreach (var group in SnapshotGroups())
            {
                if (group.IsChannelPaused) continue;
                await group.DispatchResumeAsync(ct);
            }
        }

        public async UniTask DispatchStopAsync(CancellationToken ct)
        {
            foreach (var group in SnapshotGroups())
                await group.DispatchStopAsync(ct);
        }

        public void DispatchDestroy()
        {
            foreach (var kv in _groups)
                kv.Value.DispatchDestroy();
        }

        internal void ResetChannelPauses()
        {
            foreach (var kv in _groups)
                kv.Value.IsChannelPaused = false;
        }

        public void Tick(float dt, GameState globalState)
        {
            foreach (var kv in _groups)
                kv.Value.Tick(dt, globalState);
        }

        public void FixedTick(float fixedDt, GameState globalState)
        {
            foreach (var kv in _groups)
                kv.Value.FixedTick(fixedDt, globalState);
        }

        public void LateTick(float dt, GameState globalState)
        {
            foreach (var kv in _groups)
                kv.Value.LateTick(dt, globalState);
        }

        public void Clear()
        {
            foreach (var kv in _groups)
                kv.Value.Clear();
            _groups.Clear();
            _allElements.Clear();
            GetOrCreateGroup(GameLoopChannels.Default);
        }

        public void Configure(bool throwOnErrors, float tickBudgetMs)
        {
            _throwOnErrors = throwOnErrors;
            _tickBudgetMs = tickBudgetMs;
            foreach (var kv in _groups)
                kv.Value.Configure(throwOnErrors, tickBudgetMs);
        }

        private List<GameLoopGroup> SnapshotGroups() => new List<GameLoopGroup>(_groups.Values);

        private GameLoopGroup GetOrCreateGroup(string channel)
        {
            if (!_groups.TryGetValue(channel, out var group))
            {
                group = new GameLoopGroup(channel, _logger, _callbacks);
                group.Configure(_throwOnErrors, _tickBudgetMs);
                _groups[channel] = group;
            }
            return group;
        }
    }
}
