using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using Unity.Profiling;

namespace Azen.GameLoop
{
    public sealed class GameLoopGroup
    {
        private static readonly ConcurrentDictionary<Type, ProfilerMarker> SystemMarkers = new();

        public string Name { get; }
        public bool IsChannelPaused { get; internal set; }
        public float TimeScale { get; internal set; } = 1f;

        public bool TickWhenGlobalPaused { get; internal set; }

        private readonly List<TickEntry<IGameTickable>>      _tick       = new();
        private readonly List<TickEntry<IGameFixedTickable>> _fixedTick  = new();
        private readonly List<TickEntry<IGameLateTickable>>  _lateTick   = new();
        private readonly List<TickEntry<IGameAsyncTickable>> _asyncTick  = new();

        private readonly Dictionary<IGameTickable, float>      _tickAccum      = new();
        private readonly Dictionary<IGameFixedTickable, float> _fixedTickAccum = new();
        private readonly Dictionary<IGameLateTickable, float>  _lateTickAccum  = new();

        private readonly List<ListenerEntry<IGameInitListener>>     _init    = new();
        private readonly List<ListenerEntry<IGameStartedListener>>  _started = new();
        private readonly List<ListenerEntry<IGamePausedListener>>   _paused  = new();
        private readonly List<ListenerEntry<IGameResumedListener>>  _resumed = new();
        private readonly List<ListenerEntry<IGameStoppedListener>>  _stopped = new();
        private readonly List<ListenerEntry<IGameDestroyListener>>  _destroy = new();

        private readonly List<TickEntry<IGameTickable>>      _pendingTickAdd      = new();
        private readonly List<IGameTickable>                 _pendingTickRemove   = new();
        private readonly List<TickEntry<IGameFixedTickable>> _pendingFixedAdd     = new();
        private readonly List<IGameFixedTickable>            _pendingFixedRemove  = new();
        private readonly List<TickEntry<IGameLateTickable>>  _pendingLateAdd      = new();
        private readonly List<IGameLateTickable>             _pendingLateRemove   = new();
        private readonly List<TickEntry<IGameAsyncTickable>> _pendingAsyncAdd     = new();
        private readonly List<IGameAsyncTickable>            _pendingAsyncRemove  = new();

        private readonly HashSet<IGameAsyncTickable> _asyncInFlight = new();
        private CancellationTokenSource _asyncCts = new();

        private readonly IGameLoopLogger _logger;
        private readonly CallbackTracker _callbacks;
        private readonly ProfilerMarker _tickMarker;
        private readonly ProfilerMarker _fixedMarker;
        private readonly ProfilerMarker _lateMarker;
        private readonly Stopwatch _budgetWatch = new();
        private bool _throwOnErrors;
        private float _tickBudgetMs;

        internal GameLoopGroup(string name, IGameLoopLogger logger, CallbackTracker callbacks)
        {
            Name = name;
            _logger = logger ?? NullGameLoopLogger.Instance;
            _callbacks = callbacks ?? new CallbackTracker();
            _tickMarker  = new ProfilerMarker("GameLoop." + name + ".Tick");
            _fixedMarker = new ProfilerMarker("GameLoop." + name + ".FixedTick");
            _lateMarker  = new ProfilerMarker("GameLoop." + name + ".LateTick");
        }

        public bool IsActive(GameState globalState) => !IsChannelPaused && globalState.IsPlaying;

        public bool CanTick(GameState globalState)
            => !IsChannelPaused && (globalState.IsPlaying || TickWhenGlobalPaused);

        public GameState GetState(GameState globalState)
            => IsChannelPaused && globalState.IsPlaying ? GameState.Pause : globalState;

        internal void Add(IGameElement element, int priority)
        {
            if (element is IGameTickable t)        InsertSorted(_pendingTickAdd,  new TickEntry<IGameTickable>(t, priority));
            if (element is IGameFixedTickable f)   InsertSorted(_pendingFixedAdd, new TickEntry<IGameFixedTickable>(f, priority));
            if (element is IGameLateTickable l)    InsertSorted(_pendingLateAdd,  new TickEntry<IGameLateTickable>(l, priority));
            if (element is IGameAsyncTickable a)   InsertSorted(_pendingAsyncAdd, new TickEntry<IGameAsyncTickable>(a, priority));
            if (element is IGameInitListener i)    InsertSorted(_init,    new ListenerEntry<IGameInitListener>(i, priority));
            if (element is IGameStartedListener s) InsertSorted(_started, new ListenerEntry<IGameStartedListener>(s, priority));
            if (element is IGamePausedListener p)  InsertSorted(_paused,  new ListenerEntry<IGamePausedListener>(p, priority));
            if (element is IGameResumedListener r) InsertSorted(_resumed, new ListenerEntry<IGameResumedListener>(r, priority));
            if (element is IGameStoppedListener x) InsertSorted(_stopped, new ListenerEntry<IGameStoppedListener>(x, priority));
            if (element is IGameDestroyListener d) InsertSorted(_destroy, new ListenerEntry<IGameDestroyListener>(d, priority));
        }

        internal void Remove(IGameElement element)
        {
            RemoveListener(_init, element);
            RemoveListener(_started, element);
            RemoveListener(_paused,  element);
            RemoveListener(_resumed, element);
            RemoveListener(_stopped, element);
            RemoveListener(_destroy, element);

            if (element is IGameTickable t)
            {
                _pendingTickRemove.Add(t);
                _tickAccum.Remove(t);
            }
            if (element is IGameFixedTickable f)
            {
                _pendingFixedRemove.Add(f);
                _fixedTickAccum.Remove(f);
            }
            if (element is IGameLateTickable l)
            {
                _pendingLateRemove.Add(l);
                _lateTickAccum.Remove(l);
            }
            if (element is IGameAsyncTickable a)   _pendingAsyncRemove.Add(a);
        }

        internal void Tick(float dt, GameState globalState)
        {
            RunTicks(_tick, _pendingTickAdd, _pendingTickRemove, _tickAccum, _tickMarker, dt, globalState,
                     static (target, d) => target.Tick(d), useBudget: true);

            FlushTickables(_asyncTick, _pendingAsyncAdd, _pendingAsyncRemove);
            if (_asyncTick.Count > 0 && CanTick(globalState)) FireAsyncTicks(dt * TimeScale);
        }

        internal void FixedTick(float fixedDt, GameState globalState)
            => RunTicks(_fixedTick, _pendingFixedAdd, _pendingFixedRemove, _fixedTickAccum, _fixedMarker, fixedDt, globalState,
                        static (target, d) => target.FixedTick(d), useBudget: false);

        internal void LateTick(float dt, GameState globalState)
            => RunTicks(_lateTick, _pendingLateAdd, _pendingLateRemove, _lateTickAccum, _lateMarker, dt, globalState,
                        static (target, d) => target.LateTick(d), useBudget: false);

        private void RunTicks<T>(
            List<TickEntry<T>> active,
            List<TickEntry<T>> pendingAdd,
            List<T> pendingRemove,
            Dictionary<T, float> accumulators,
            ProfilerMarker loopMarker,
            float dt,
            GameState globalState,
            Action<T, float> invoke,
            bool useBudget) where T : class, IGameElement
        {
            FlushTickables(active, pendingAdd, pendingRemove);
            if (!CanTick(globalState)) return;

            if (TimeScale != 1f) dt *= TimeScale;
            var budgeted = useBudget && _tickBudgetMs > 0f;
            if (budgeted) _budgetWatch.Restart();

            loopMarker.Begin();
            try
            {
                int count = active.Count;
                for (int i = 0; i < count; i++)
                {
                    if (budgeted && _budgetWatch.ElapsedMilliseconds >= (long)_tickBudgetMs) break;
                    var target = active[i].Target;
                    if (target == null) continue;

                    if (target is IGameTickRate rate && rate.TickInterval > 0f)
                    {
                        if (!ShouldFireThrottled(accumulators, target, rate.TickInterval, dt)) continue;
                    }

                    var marker = GetMarker(target.GetType());
                    marker.Begin();
                    try { invoke(target, dt); }
                    catch (Exception ex) { HandleError("Tick failed: " + target.GetType().Name, ex); }
                    finally { marker.End(); }
                }
            }
            finally { loopMarker.End(); }
        }

        private static bool ShouldFireThrottled<T>(Dictionary<T, float> accumulators, T target, float interval, float dt) where T : class, IGameElement
        {
            if (!accumulators.TryGetValue(target, out var acc)) acc = 0f;
            acc += dt;
            if (acc < interval)
            {
                accumulators[target] = acc;
                return false;
            }
            acc -= interval;
            accumulators[target] = acc;
            return true;
        }

        private static ProfilerMarker GetMarker(Type type)
            => SystemMarkers.GetOrAdd(type, t => new ProfilerMarker("GameLoop." + t.Name));

        private void FireAsyncTicks(float dt)
        {
            var token = _asyncCts.Token;
            for (int i = 0; i < _asyncTick.Count; i++)
            {
                var target = _asyncTick[i].Target;
                if (target == null || !_asyncInFlight.Add(target)) continue;
                RunAsyncTick(target, dt, token).Forget();
            }
        }

        private async UniTaskVoid RunAsyncTick(IGameAsyncTickable target, float dt, CancellationToken token)
        {
            try { await target.TickAsync(dt, token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.Error("TickAsync failed: " + target.GetType().Name, ex); }
            finally { _asyncInFlight.Remove(target); }
        }

        internal UniTask DispatchInitAsync(CancellationToken ct)
            => DispatchAsync(_init, static (target, token) => target.OnInit(token), "OnInit", ct);

        internal UniTask DispatchStartAsync(CancellationToken ct)
            => DispatchAsync(_started, static (target, token) => target.OnStartGame(token), "OnStartGame", ct);

        internal UniTask DispatchPauseAsync(CancellationToken ct)
            => DispatchAsync(_paused, static (target, token) => target.OnPauseGame(token), "OnPauseGame", ct);

        internal UniTask DispatchResumeAsync(CancellationToken ct)
            => DispatchAsync(_resumed, static (target, token) => target.OnResumeGame(token), "OnResumeGame", ct);

        internal UniTask DispatchStopAsync(CancellationToken ct)
            => DispatchAsync(_stopped, static (target, token) => target.OnStopGame(token), "OnStopGame", ct);

        private async UniTask DispatchAsync<T>(
            List<ListenerEntry<T>> listeners,
            Func<T, CancellationToken, UniTask> invoke,
            string callbackName,
            CancellationToken ct) where T : class, IGameElement
        {
            var snapshot = Snapshot(listeners);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var target = snapshot[i];
                ct.ThrowIfCancellationRequested();
                try { await InvokeTracked(target, invoke, ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { HandleError(callbackName + " failed: " + target.GetType().Name, ex); }
            }
        }

        private UniTask InvokeTracked<T>(T target, Func<T, CancellationToken, UniTask> invoke, CancellationToken ct)
        {
            _callbacks.Depth++;
            try { return invoke(target, ct); }
            finally { _callbacks.Depth--; }
        }

        internal void DispatchDestroy()
        {
            _asyncCts.Cancel();

            for (int i = _destroy.Count - 1; i >= 0; i--)
            {
                var target = _destroy[i].Target;
                if (target == null) continue;
                try { target.OnDestroy(); }
                catch (Exception ex) { HandleError("OnDestroy failed: " + target.GetType().Name, ex); }
            }
        }

        internal void Clear()
        {
            _asyncCts.Cancel();
            _asyncCts.Dispose();
            _asyncCts = new CancellationTokenSource();
            _asyncInFlight.Clear();

            _tick.Clear(); _fixedTick.Clear(); _lateTick.Clear(); _asyncTick.Clear();
            _tickAccum.Clear(); _fixedTickAccum.Clear(); _lateTickAccum.Clear();
            _init.Clear(); _started.Clear(); _paused.Clear(); _resumed.Clear(); _stopped.Clear(); _destroy.Clear();
            _pendingTickAdd.Clear(); _pendingTickRemove.Clear();
            _pendingFixedAdd.Clear(); _pendingFixedRemove.Clear();
            _pendingLateAdd.Clear(); _pendingLateRemove.Clear();
            _pendingAsyncAdd.Clear(); _pendingAsyncRemove.Clear();
        }

        internal void Configure(bool throwOnErrors, float tickBudgetMs)
        {
            _throwOnErrors = throwOnErrors;
            _tickBudgetMs = tickBudgetMs;
        }

        private void HandleError(string message, Exception ex)
        {
            _logger.Error(message, ex);
            if (_throwOnErrors) ExceptionDispatchInfo.Capture(ex).Throw();
        }

        private static void InsertSorted<T>(List<T> list, T entry) where T : struct, IComparable<T>
        {
            int idx = list.BinarySearch(entry);
            if (idx < 0) idx = ~idx;
            list.Insert(idx, entry);
        }

        private static void RemoveListener<T>(List<ListenerEntry<T>> list, IGameElement element) where T : class, IGameElement
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if (ReferenceEquals(list[i].Target, element)) list.RemoveAt(i);
        }

        private static List<T> Snapshot<T>(List<ListenerEntry<T>> source) where T : class, IGameElement
        {
            var snapshot = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var target = source[i].Target;
                if (target != null) snapshot.Add(target);
            }
            return snapshot;
        }

        private static void FlushTickables<T>(List<TickEntry<T>> active, List<TickEntry<T>> toAdd, List<T> toRemove)
            where T : class, IGameElement
        {
            if (toAdd.Count > 0)
            {
                active.AddRange(toAdd);
                active.Sort();
                toAdd.Clear();
            }
            if (toRemove.Count > 0)
            {
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    var target = active[i].Target;
                    if (target == null) { active.RemoveAt(i); continue; }
                    for (int r = 0; r < toRemove.Count; r++)
                        if (ReferenceEquals(target, toRemove[r])) { active.RemoveAt(i); break; }
                }
                toRemove.Clear();
            }
        }

        private readonly struct TickEntry<T> : IComparable<TickEntry<T>> where T : class, IGameElement
        {
            private readonly T _target;
            public T Target => _target;
            public int Priority { get; }
            public TickEntry(T target, int priority) { _target = target; Priority = priority; }
            public int CompareTo(TickEntry<T> other) => other.Priority.CompareTo(Priority);
        }

        private readonly struct ListenerEntry<T> : IComparable<ListenerEntry<T>> where T : class, IGameElement
        {
            private readonly T _target;
            public T Target => _target;
            public int Priority { get; }
            public ListenerEntry(T target, int priority) { _target = target; Priority = priority; }
            public int CompareTo(ListenerEntry<T> other) => other.Priority.CompareTo(Priority);
        }
    }
}
