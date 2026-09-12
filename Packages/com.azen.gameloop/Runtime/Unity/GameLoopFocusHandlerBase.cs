using System.Threading;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azen.GameLoop.Infrastructure
{
    public abstract class GameLoopFocusHandlerBase : MonoBehaviour
    {
        public abstract IGameLoop GameLoop { get; }

        [SerializeField] private bool _pauseOnFocusLost = true;
        [SerializeField] private bool _resumeOnFocusGained = true;

        private static readonly PauseReason FocusReason = PauseReason.Focus;

        private void OnEnable()  => Application.focusChanged += OnFocusChanged;
        private void OnDisable() => Application.focusChanged -= OnFocusChanged;

        private void OnFocusChanged(bool focused)
        {
            var loop = GameLoop;
            if (loop == null) return;

            if (!focused && _pauseOnFocusLost)
                loop.PauseFor(FocusReason, default).Forget();
            else if (focused && _resumeOnFocusGained)
                loop.ResumeFor(FocusReason, default).Forget();
        }
    }
}
