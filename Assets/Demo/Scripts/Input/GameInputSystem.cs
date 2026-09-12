using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Azen.GameLoop.Demo
{
    public sealed class GameInputSystem : IGameElement, IGameTickable
    {
        private readonly IGameLoop _loop;

        public GameInputSystem(IGameLoop loop) => _loop = loop;

        public void Tick(float deltaTime)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame) ToggleGlobalPause();
            if (keyboard.digit1Key.wasPressedThisFrame) TogglePauseReason(PauseReason.Menu);
            if (keyboard.digit2Key.wasPressedThisFrame) TogglePauseReason(PauseReason.Dialog);
            if (keyboard.cKey.wasPressedThisFrame) ToggleChannel(GameLoopChannels.Gameplay);
            if (keyboard.sKey.wasPressedThisFrame) ToggleSlowMotion();
            if (keyboard.rKey.wasPressedThisFrame) _loop.RestartAsync().Forget();
        }

        private void ToggleGlobalPause()
        {
            if (_loop.State.IsPlaying)
                _loop.PauseAsync().Forget();
            else if (_loop.State.IsPaused)
                _loop.ResumeAllAsync(default).Forget();
        }

        private void TogglePauseReason(PauseReason reason)
        {
            var operation = _loop.IsPausedFor(reason)
                ? _loop.ResumeFor(reason, default)
                : _loop.PauseFor(reason, default);
            operation.Forget();
        }

        private void ToggleChannel(string channel)
        {
            var operation = _loop.GetChannelState(channel).IsPaused
                ? _loop.ResumeChannelAsync(channel, default)
                : _loop.PauseChannelAsync(channel, default);
            operation.Forget();
        }

        private void ToggleSlowMotion()
        {
            var isSlow = _loop.GetChannelTimeScale(GameLoopChannels.Gameplay) < DemoConfig.TimeScale.Normal;
            var targetScale = isSlow ? DemoConfig.TimeScale.Normal : DemoConfig.TimeScale.SlowMotion;
            _loop.SetChannelTimeScale(GameLoopChannels.Gameplay, targetScale);
        }
    }
}
