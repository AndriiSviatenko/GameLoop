using System;
using System.Collections.Generic;

namespace Azen.GameLoop
{
    public sealed class GameLoopScheduler
    {
        private sealed class Entry
        {
            public int Id;
            public float Interval;
            public bool Repeat;
            public Action Action;
            public float Accumulator;
            public bool Alive = true;
            public string Channel;
        }

        private readonly List<Entry> _entries = new();
        private readonly Action<Exception> _errorHandler;
        private readonly Func<string, bool> _channelActive;
        private readonly Func<string, float> _channelTimeScale;
        private int _nextId = 1;

        public GameLoopScheduler(
            Action<Exception> errorHandler = null,
            Func<string, bool> channelActive = null,
            Func<string, float> channelTimeScale = null)
        {
            _errorHandler = errorHandler;
            _channelActive = channelActive;
            _channelTimeScale = channelTimeScale;
        }

        public int Count => _entries.Count;

        public ScheduleHandle Every(float intervalSeconds, Action action, string channel = null)
        {
            if (intervalSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Interval must be > 0");
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var entry = new Entry
            {
                Id = _nextId++,
                Interval = intervalSeconds,
                Repeat = true,
                Action = action,
                Channel = channel
            };
            _entries.Add(entry);
            return new ScheduleHandle(entry.Id);
        }

        public ScheduleHandle Once(float delaySeconds, Action action, string channel = null)
        {
            if (delaySeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(delaySeconds), "Delay must be >= 0");
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var entry = new Entry
            {
                Id = _nextId++,
                Interval = delaySeconds,
                Repeat = false,
                Action = action,
                Channel = channel
            };
            _entries.Add(entry);
            return new ScheduleHandle(entry.Id);
        }

        public bool Cancel(ScheduleHandle handle)
        {
            if (!handle.IsValid) return false;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id == handle.Id)
                {
                    _entries[i].Alive = false;
                    return true;
                }
            }
            return false;
        }

        public void CancelAll()
        {
            for (int i = 0; i < _entries.Count; i++)
                _entries[i].Alive = false;
        }

        public void Tick(float dt, GameState globalState)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (i >= _entries.Count) continue;

                var e = _entries[i];
                if (!e.Alive)
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                if (!CanRun(e.Channel, globalState)) continue;

                var entryDeltaTime = dt;
                if (e.Channel != null && _channelTimeScale != null)
                    entryDeltaTime *= _channelTimeScale(e.Channel);

                e.Accumulator += entryDeltaTime;
                if (e.Accumulator < e.Interval) continue;

                e.Accumulator = e.Repeat ? e.Accumulator - e.Interval : 0f;
                if (!e.Repeat) e.Alive = false;

                try { e.Action(); }
                catch (Exception ex) { _errorHandler?.Invoke(ex); }
            }
        }

        private bool CanRun(string channel, GameState globalState)
            => channel == null || _channelActive == null ? globalState.IsPlaying : _channelActive(channel);

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
