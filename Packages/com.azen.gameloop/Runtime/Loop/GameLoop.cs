using System;
using System.Collections.Generic;
using System.Threading;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azen.GameLoop
{
    public sealed class GameLoop : IGameLoop
    {
        public static GameLoop Active { get; private set; }
        internal static event Action<GameLoop> ActiveChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Active = null;
            ActiveChanged = null;
        }

        public GameLoopComposite Composite { get; }
        public GameLoopScheduler Scheduler { get; }
        public ITimeProvider TimeProvider { get; }
        public IGameLoopLogger Logger { get; }

        private readonly GameStateMachine _state = new GameStateMachine();
        private readonly HashSet<PauseReason> _pauseReasons = new HashSet<PauseReason>();
        private readonly CallbackTracker _callbacks = new CallbackTracker();
        private readonly Queue<UniTaskCompletionSource> _transitionWaiters = new Queue<UniTaskCompletionSource>();
        private bool _transitionRunning;
        private bool _initialized;
        private bool _destroyed;
        private bool _throwOnReentrancy = true;
        private bool _throwOnErrors;
        private float _tickBudgetMs;

        public GameLoop(ITimeProvider timeProvider = null, IGameLoopLogger logger = null,
                        bool throwOnErrors = false, bool throwOnReentrancy = true,
                        float tickBudgetMs = 0f)
        {
            TimeProvider       = timeProvider ?? UnityTimeProvider.Instance;
            Logger             = logger ?? NullGameLoopLogger.Instance;
            Composite          = new GameLoopComposite(Logger, _callbacks);
            Scheduler          = new GameLoopScheduler(
                ex => Logger.Error("Scheduler error", ex),
                channel => Composite.CanTick(channel, _state.State),
                channel => Composite.GetChannelTimeScale(channel));
            _throwOnReentrancy = throwOnReentrancy;
            _throwOnErrors     = throwOnErrors;
            _tickBudgetMs      = tickBudgetMs;
            Composite.Configure(throwOnErrors, tickBudgetMs);
            SetActive(this);
        }

        public GameState State => _state.State;

        public event Action<GameState> StateChanged
        {
            add    => _state.StateChanged += value;
            remove => _state.StateChanged -= value;
        }

        public IEnumerable<PauseReason> PauseReasons => _pauseReasons;

        public bool IsPausedFor(PauseReason reason) => _pauseReasons.Contains(reason);

        public bool ThrowOnReentrancy
        {
            get => _throwOnReentrancy;
            set => _throwOnReentrancy = value;
        }

        public float TickBudgetMs
        {
            get => _tickBudgetMs;
            set
            {
                _tickBudgetMs = value;
                Composite.Configure(_throwOnErrors, value);
            }
        }

        public UniTask StartAsync(CancellationToken cancellationToken)   => StartInternalAsync(cancellationToken);
        public UniTask PauseAsync(CancellationToken cancellationToken)  => PauseFor(PauseReason.Manual, cancellationToken);
        public UniTask ResumeAsync(CancellationToken cancellationToken) => ResumeFor(PauseReason.Manual, cancellationToken);
        public UniTask StopAsync(CancellationToken cancellationToken)   => StopInternalAsync(cancellationToken);
        public UniTask RestartAsync(CancellationToken cancellationToken) => RestartInternalAsync(cancellationToken);

        public async UniTask PauseFor(PauseReason reason, CancellationToken cancellationToken)
        {
            if (!await EnterTransitionAsync(cancellationToken)) return;
            try
            {
                if (_pauseReasons.Contains(reason)) return;

                if (_pauseReasons.Count == 0 && !_state.CanPause) return;
                _pauseReasons.Add(reason);
                if (_pauseReasons.Count > 1) return;

                cancellationToken.ThrowIfCancellationRequested();
                await Composite.DispatchPauseAsync(cancellationToken);
                _state.TryTransitionTo(GameState.Pause);
            }
            finally { ExitTransition(); }
        }

        public async UniTask ResumeFor(PauseReason reason, CancellationToken cancellationToken)
        {
            if (!await EnterTransitionAsync(cancellationToken)) return;
            try
            {
                if (!_pauseReasons.Remove(reason)) return;
                if (_pauseReasons.Count > 0) return;
                if (!_state.CanResume) return;

                cancellationToken.ThrowIfCancellationRequested();
                await Composite.DispatchResumeAsync(cancellationToken);
                _state.TryTransitionTo(GameState.Play);
            }
            finally { ExitTransition(); }
        }

        public async UniTask ResumeAllAsync(CancellationToken cancellationToken)
        {
            if (!await EnterTransitionAsync(cancellationToken)) return;
            try
            {
                var snapshot = new List<PauseReason>(_pauseReasons);
                var removed = false;
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var r = snapshot[i];
                    if (r.Kind == PauseReasonKind.Manual)
                    {
                        _pauseReasons.Remove(r);
                        removed = true;
                    }
                }
                if (!removed) return;
                if (_pauseReasons.Count > 0) return;
                if (!_state.CanResume) return;

                cancellationToken.ThrowIfCancellationRequested();
                await Composite.DispatchResumeAsync(cancellationToken);
                _state.TryTransitionTo(GameState.Play);
            }
            finally { ExitTransition(); }
        }

        private async UniTask StartInternalAsync(CancellationToken ct)
        {
            if (!await EnterTransitionAsync(ct)) return;
            try
            {
                ct.ThrowIfCancellationRequested();
                if (!_initialized) await InitializeAsync(ct);
                if (!_state.CanStart) return;
                await Composite.DispatchStartAsync(ct);
                _state.TryTransitionTo(GameState.Play);
            }
            finally { ExitTransition(); }
        }

        private async UniTask StopInternalAsync(CancellationToken ct)
        {
            if (!await EnterTransitionAsync(ct)) return;
            try
            {
                ct.ThrowIfCancellationRequested();
                if (!_state.CanStop) return;
                await Composite.DispatchStopAsync(ct);
                _pauseReasons.Clear();
                Composite.ResetChannelPauses();
                _state.TryTransitionTo(GameState.Stop);
            }
            finally { ExitTransition(); }
        }

        private async UniTask RestartInternalAsync(CancellationToken ct)
        {
            if (!await EnterTransitionAsync(ct)) return;
            try
            {
                ct.ThrowIfCancellationRequested();

                if (_state.State.IsPlaying || _state.State.IsPaused)
                {
                    await Composite.DispatchStopAsync(ct);
                }

                _state.ForceReset(GameState.Off);
                _pauseReasons.Clear();
                Composite.ResetChannelPauses();

                if (_initialized)
                    await Composite.DispatchInitAsync(ct);
                else
                    await InitializeAsync(ct);

                await Composite.DispatchStartAsync(ct);
                _state.TryTransitionTo(GameState.Play);
            }
            finally { ExitTransition(); }
        }

        public bool IsInitialized => _initialized;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_initialized) return;
            cancellationToken.ThrowIfCancellationRequested();
            await Composite.DispatchInitAsync(cancellationToken);
            _initialized = true;
        }

        public void Destroy()
        {
            if (_destroyed) return;
            _destroyed = true;
            try { Composite.DispatchDestroy(); }
            catch (Exception ex) { Logger.Error("Destroy failed", ex); }
            Scheduler?.Clear();
            if (ReferenceEquals(Active, this)) SetActive(null);
        }

        public GameState SubState { get; private set; } = GameState.Off;

        public event Action<GameState> SubStateChanged;

        public void SetSubState(GameState state)
        {
            if (state.Equals(SubState)) return;
            SubState = state;
            SubStateChanged?.Invoke(state);
        }

        public void SetChannelTimeScale(string channel, float scale)
            => Composite.SetChannelTimeScale(channel, scale);

        public float GetChannelTimeScale(string channel)
            => Composite.GetChannelTimeScale(channel);

        public void SetChannelTickWhenPaused(string channel, bool value)
            => Composite.SetChannelTickWhenPaused(channel, value);

        public GameLoopElementHandle Add(IGameElement element, string channel = GameLoopChannels.Default, int priority = 0)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return Composite.Add(element, channel, priority);
        }

        public void Remove(GameLoopElementHandle handle) => Composite.Remove(handle);
        public void Remove(IGameElement element)         => Composite.Remove(element);
        public bool Contains(IGameElement element)       => Composite.Contains(element);

        public event Action<string, GameState> ChannelStateChanged;

        public async UniTask PauseChannelAsync(string channel, CancellationToken cancellationToken)
        {
            if (!await EnterTransitionAsync(cancellationToken)) return;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var group = Composite.GetGroup(channel);
                if (group.IsChannelPaused) return;

                group.IsChannelPaused = true;
                if (_state.State.IsPlaying)
                    await group.DispatchPauseAsync(cancellationToken);

                ChannelStateChanged?.Invoke(group.Name, GetChannelState(group.Name));
            }
            finally { ExitTransition(); }
        }

        public async UniTask ResumeChannelAsync(string channel, CancellationToken cancellationToken)
        {
            if (!await EnterTransitionAsync(cancellationToken)) return;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var group = Composite.GetGroup(channel);
                if (!group.IsChannelPaused) return;

                if (_state.State.IsPlaying)
                    await group.DispatchResumeAsync(cancellationToken);
                group.IsChannelPaused = false;

                ChannelStateChanged?.Invoke(group.Name, GetChannelState(group.Name));
            }
            finally { ExitTransition(); }
        }

        public GameState GetChannelState(string channel)
            => Composite.GetChannelState(channel, _state.State);

        internal void DriveUpdate()
        {
            var dt = TimeProvider.DeltaTime;
            Composite.Tick(dt, _state.State);
            Scheduler.Tick(dt, _state.State);
        }

        internal void DriveFixedUpdate() => Composite.FixedTick(TimeProvider.FixedDeltaTime, _state.State);
        internal void DriveLateUpdate()  => Composite.LateTick (TimeProvider.DeltaTime,      _state.State);

        private async UniTask<bool> EnterTransitionAsync(CancellationToken cancellationToken)
        {
            if (_destroyed) return false;

            if (_callbacks.Depth > 0)
            {
                if (_throwOnReentrancy)
                    throw new InvalidOperationException(
                        "GameLoop reentrancy: do not call Start/Pause/Resume/Stop/Restart synchronously from a listener callback. " +
                        "Call it after an await or from a tick.");
                Logger.Warning("GameLoop reentrancy detected — call ignored.");
                return false;
            }

            if (!_transitionRunning)
            {
                _transitionRunning = true;
                return true;
            }

            var waiter = new UniTaskCompletionSource();
            _transitionWaiters.Enqueue(waiter);
            using (cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken)))
                await waiter.Task;
            return true;
        }

        private void ExitTransition()
        {
            while (_transitionWaiters.Count > 0)
            {
                if (_transitionWaiters.Dequeue().TrySetResult()) return;
            }
            _transitionRunning = false;
        }

        private static void SetActive(GameLoop loop)
        {
            if (ReferenceEquals(Active, loop)) return;
            Active = loop;
            ActiveChanged?.Invoke(loop);
        }
    }
}
