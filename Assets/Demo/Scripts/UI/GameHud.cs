using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azen.GameLoop.Demo
{
    public sealed class GameHud : MonoBehaviour
    {
        private IGameLoop _loop;
        private ScoreSystem _score;
        private RoundTimer _timer;
        private string _lastEvent = "-";

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;

        public void Construct(IGameLoop loop, ScoreSystem score, RoundTimer timer)
        {
            _loop = loop;
            _score = score;
            _timer = timer;

            _loop.StateChanged += HandleStateChanged;
            _loop.ChannelStateChanged += HandleChannelStateChanged;
        }

        private void OnDestroy()
        {
            if (_loop == null) return;

            _loop.StateChanged -= HandleStateChanged;
            _loop.ChannelStateChanged -= HandleChannelStateChanged;
        }

        private void HandleStateChanged(GameState state) => _lastEvent = $"State → {state}";

        private void HandleChannelStateChanged(string channel, GameState state) =>
            _lastEvent = $"Channel '{channel}' → {state}";

        private void OnGUI()
        {
            if (_loop == null) return;

            EnsureStyles();
            GUI.Box(new Rect(8, 8, 460, 280), GUIContent.none, _panelStyle);
            DrawContent();
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null) return;

            _panelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft };
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        }

        private void DrawContent()
        {
            GUI.Label(new Rect(20, 14, 440, 24), "Azen.GameLoop Demo", _titleStyle);
            GUI.Label(new Rect(20, 40, 440, 22), $"State: {_loop.State}  |  SubState: {_loop.SubState}", _labelStyle);
            GUI.Label(new Rect(20, 64, 440, 22), $"Last event: {_lastEvent}", _labelStyle);

            var gameplayState = _loop.GetChannelState(GameLoopChannels.Gameplay);
            var uiState = _loop.GetChannelState(GameLoopChannels.UI);
            var timeScale = _loop.GetChannelTimeScale(GameLoopChannels.Gameplay);
            GUI.Label(new Rect(20, 88, 440, 22),
                $"Gameplay: {gameplayState}  |  UI: {uiState}  |  TimeScale: {timeScale:0.##}", _labelStyle);

            var reasons = string.Join(", ", _loop.PauseReasons.Select(reason => $"{reason.Name}({reason.Kind})"));
            GUI.Label(new Rect(20, 112, 440, 22),
                $"Pause reasons: {(reasons.Length > 0 ? reasons : "none")}", _labelStyle);

            GUI.Label(new Rect(20, 136, 440, 22),
                $"Score: {_score.CurrentScore}  |  Time: {_timer.RemainingSeconds:0.0}s", _labelStyle);

            if (_loop.State.IsPaused && GUI.Button(new Rect(20, 164, 120, 28), "▶ Resume All"))
                _loop.ResumeAllAsync(default).Forget();

            if (_loop.State.IsStopped)
                GUI.Label(new Rect(20, 168, 440, 22), "Round over — press R to restart", _titleStyle);

            GUI.Label(new Rect(20, 200, 440, 60),
                "[Space] Pause / ResumeAll    [1] Menu pause    [2] Dialog pause\n" +
                "[C] Toggle Gameplay channel    [S] Slow-mo    [R] Restart", _labelStyle);
        }
    }
}
