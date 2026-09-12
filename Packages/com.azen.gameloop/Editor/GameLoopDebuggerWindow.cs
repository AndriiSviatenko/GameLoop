using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Azen.GameLoop.Editor
{
    public class GameLoopDebuggerWindow : EditorWindow
    {
        private IGameLoop _loop;
        private bool _autoRefresh = true;
        private double _lastRepaint;
        private Vector2 _channelsScroll;

        [MenuItem("Window/Azen/GameLoop Debugger")]
        public static void Open()
        {
            var window = GetWindow<GameLoopDebuggerWindow>("GameLoop Debugger");
            window.minSize = new Vector2(320, 360);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            ResolveLoop();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh) return;
            if (EditorApplication.timeSinceStartup - _lastRepaint < 0.25) return;
            _lastRepaint = EditorApplication.timeSinceStartup;
            ResolveLoop();
            Repaint();
        }

        private void ResolveLoop()
        {
            _loop = GameLoop.Active;
        }

        private void OnGUI()
        {
            GUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                _autoRefresh = EditorGUILayout.ToggleLeft("Auto-refresh (4 Hz)", _autoRefresh, GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    ResolveLoop();
                    Repaint();
                }
            }

            EditorGUILayout.Space(2);
            Line();

            if (_loop == null)
            {
                EditorGUILayout.HelpBox(
                    "No active GameLoop found.\n" +
                    "Enter Play Mode and bootstrap a GameLoopDriver in your composition root.",
                    MessageType.Info);
                return;
            }

            DrawState();
            DrawSubState();
            DrawPauseReasons();
            DrawChannels();
            DrawActions();
        }

        private void DrawState()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("State", EditorStyles.boldLabel, GUILayout.Width(100));
                var color = GUI.color;
                GUI.color = StateColor(_loop.State);
                EditorGUILayout.LabelField(_loop.State.Name, EditorStyles.largeLabel);
                GUI.color = color;
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Initialized: {_loop.IsInitialized}", GUILayout.Width(140));
            }
        }

        private void DrawSubState()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Sub-state", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField(_loop.SubState.Name);
            }
        }

        private void DrawPauseReasons()
        {
            EditorGUILayout.LabelField("Pause reasons", EditorStyles.boldLabel);
            var any = false;
            foreach (var reason in _loop.PauseReasons)
            {
                any = true;
                EditorGUILayout.LabelField("•", reason.ToString(), EditorStyles.miniLabel);
            }
            if (!any) EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
        }

        private void DrawChannels()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Channels", EditorStyles.boldLabel);

            if (!(_loop is GameLoop concrete))
            {
                EditorGUILayout.LabelField("  (concrete GameLoop not available — install via GameLoop type for full view)", EditorStyles.miniLabel);
                return;
            }

            _channelsScroll = EditorGUILayout.BeginScrollView(_channelsScroll, GUILayout.MinHeight(120));
            foreach (var channel in concrete.Composite.Channels)
            {
                var state = _loop.GetChannelState(channel);
                var scale = _loop.GetChannelTimeScale(channel);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var color = GUI.color;
                    GUI.color = ChannelColor(state);
                    GUILayout.Box(string.Empty, GUILayout.Width(6), GUILayout.Height(14));
                    GUI.color = color;

                    EditorGUILayout.LabelField(channel, EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField(state.Name, GUILayout.Width(60));
                    EditorGUILayout.LabelField($"×{scale:0.00}", GUILayout.Width(50));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying == false);
                if (GUILayout.Button("Start")) _loop.StartAsync().Forget();
                if (GUILayout.Button("Pause")) _loop.PauseAsync().Forget();
                if (GUILayout.Button("Resume")) _loop.ResumeAsync().Forget();
                if (GUILayout.Button("Stop")) _loop.StopAsync().Forget();
                if (GUILayout.Button("Restart")) _loop.RestartAsync().Forget();
                EditorGUI.EndDisabledGroup();
            }
        }

        private static void Line()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        private static Color StateColor(GameState s)
        {
            if (s.IsPlaying)  return new Color(0.30f, 0.75f, 0.43f);
            if (s.IsPaused)   return new Color(0.96f, 0.65f, 0.14f);
            if (s.IsStopped)  return new Color(0.91f, 0.10f, 0.17f);
            return new Color(0.5f, 0.5f, 0.5f);
        }

        private static Color ChannelColor(GameState s)
        {
            if (s.IsPaused) return new Color(0.96f, 0.65f, 0.14f, 0.4f);
            if (s.IsPlaying) return new Color(0.30f, 0.75f, 0.43f, 0.4f);
            return new Color(0.5f, 0.5f, 0.5f, 0.2f);
        }
    }
}
