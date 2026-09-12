using System;

namespace Azen.GameLoop
{
    internal sealed class GameStateMachine
    {
        public GameState State { get; private set; } = GameState.Off;
        public event Action<GameState> StateChanged;

        public bool CanStart  => State.IsOff || State.IsStopped;
        public bool CanPause  => State.IsPlaying;
        public bool CanResume => State.IsPaused;
        public bool CanPlay   => CanStart || CanResume;
        public bool CanStop   => State.IsPlaying || State.IsPaused;

        public bool TryTransitionTo(GameState state)
        {
            var valid = state switch
            {
                _ when state.IsPlaying => CanPlay,
                _ when state.IsPaused  => CanPause,
                _ when state.IsStopped => CanStop,
                _ when state.IsOff     => true,
                _ => false
            };
            if (!valid) return false;

            State = state;
            StateChanged?.Invoke(state);
            return true;
        }

        public void ForceReset(GameState state)
        {
            State = state;
        }
    }
}
