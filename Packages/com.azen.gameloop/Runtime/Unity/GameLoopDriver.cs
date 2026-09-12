using Azen.GameLoop.Infrastructure;
using UnityEngine;

namespace Azen.GameLoop
{

    [DefaultExecutionOrder(-1000)]
    public sealed class GameLoopDriver : MonoBehaviour
    {
        private GameLoop _loop;
        private bool _destroyed;

        public GameLoop Loop => _loop;

        public GameLoop Bootstrap(ITimeProvider timeProvider = null, IGameLoopLogger logger = null)
        {
            if (_loop != null) return _loop;
            _loop = new GameLoop(
                timeProvider: timeProvider ?? UnityTimeProvider.Instance,
                logger:       logger       ?? NullGameLoopLogger.Instance);
            return _loop;
        }

        private void Update()      { if (_loop != null) _loop.DriveUpdate(); }
        private void FixedUpdate() { if (_loop != null) _loop.DriveFixedUpdate(); }
        private void LateUpdate()  { if (_loop != null) _loop.DriveLateUpdate(); }

        private void OnDestroy()
        {
            if (_destroyed || _loop == null) return;
            _destroyed = true;
            _loop.Destroy();
        }
    }
}
