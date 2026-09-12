using UnityEngine;

namespace Azen.GameLoop
{
    [DefaultExecutionOrder(-500)]
    public abstract class GameLoopElement : MonoBehaviour, IGameElement
    {
        [SerializeField] private string _channel = GameLoopChannels.Default;
        [SerializeField] private int _priority = GameLoopPriority.Normal;
        [Tooltip("When true, registration happens in OnEnable. When false, a subclass must call TryRegister() manually.")]
        [SerializeField] private bool _autoRegisterOnEnable = true;

        private GameLoopElementHandle _handle;
        private IGameLoop _registeredLoop;

        public GameLoopElementHandle Handle => _handle;
        public bool IsRegistered => _handle.IsValid;
        public string Channel => _channel;
        public int Priority => _priority;

        protected void Configure(string channel = null, int? priority = null)
        {
            if (channel != null) _channel = channel;
            if (priority.HasValue) _priority = priority.Value;
        }

        protected virtual void OnEnable()
        {
            if (!_autoRegisterOnEnable) return;
            GameLoop.ActiveChanged += HandleActiveLoopChanged;
            TryRegister();
        }

        protected virtual void OnDisable()
        {
            GameLoop.ActiveChanged -= HandleActiveLoopChanged;
            TryUnregister();
        }

        protected void TryRegister()
        {
            if (_handle.IsValid) return;
            var loop = ResolveLoop();
            if (loop == null) return;
            _handle = loop.Add(this, _channel, _priority);
            _registeredLoop = loop;
        }

        protected void TryUnregister()
        {
            if (!_handle.IsValid) return;
            _registeredLoop?.Remove(_handle);
            _handle = default;
            _registeredLoop = null;
        }

        protected virtual IGameLoop ResolveLoop() => GameLoop.Active;

        protected virtual void OnDestroy()
        {
            GameLoop.ActiveChanged -= HandleActiveLoopChanged;
            TryUnregister();
        }

        private void HandleActiveLoopChanged(GameLoop _)
        {
            if (!_autoRegisterOnEnable || !isActiveAndEnabled) return;

            var resolvedLoop = ResolveLoop();
            if (ReferenceEquals(_registeredLoop, resolvedLoop)) return;

            TryUnregister();
            TryRegister();
        }
    }
}
