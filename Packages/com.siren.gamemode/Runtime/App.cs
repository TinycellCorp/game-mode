//#define DOMAIN_RELOAD_HANDLING

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;

namespace GameMode
{
    public static partial class App
    {
        private static IGameMode _startMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            _startMode = null;
        }

        // AfterAssembliesLoaded is called before BeforeSceneLoad
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void InitUniTaskLoop()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref loop);
        }

        // ReSharper disable once Unity.IncorrectMethodSignature
        // BeforeSceneLoad에서는 UniTask의 awaiter가 처리 되지않아 Delay, WaitUntil등 오퍼레이터가 동작하지 않음.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Bootstrap()
        {
            var settings = AppSettings.Instance;
            if (settings.SkipInitialize)
            {
                Debug.LogWarning("skip game mode initialize");
                return;
            }

            var startScene = SceneManager.GetActiveScene();
            var startFromMain = startScene.buildIndex == AppSettings.MainSceneIndex;

            foreach (IGameMode gameMode in settings.GameModes)
            {
                gameMode.State = GameModeState.Ended;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(StartingGameMode))
            {
                _startMode = settings.GameModes.FirstOrDefault(_ => _.name == StartingGameMode);
                StartingGameMode = null;
            }
#endif
            if (_startMode == null && startFromMain)
            {
                _startMode = settings.GameModes.FirstOrDefault();
            }

            Debug.Log($"App Initialized: {SceneManager.GetActiveScene().name}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void SceneLoaded()
        {
            if (_startMode != null)
            {
                GameModeManager.SwitchMode(_startMode);
            }
        }

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        private static void ClearEditorPrefs()
        {
            StartingGameMode = null;
            RestoreScene = null;
        }

        public static string StartingGameMode
        {
            get => EditorPrefs.GetString(StartingGameModeID);
            private set => EditorPrefs.SetString(StartingGameModeID, value);
        }

        public static string RestoreScene
        {
            get => EditorPrefs.GetString(RestoreSceneID);
            set => EditorPrefs.SetString(RestoreSceneID, value);
        }

        private const string RestoreSceneID = "RestoreScene";
        private const string StartingGameModeID = "StartingGameMode";

        public static void StartGameMode(string name)
        {
            if (Application.isPlaying) return;
            var settings = AppSettings.Instance;
            var containsGameMode = settings.GameModes.Select(_ => _.name).Contains(name);
            // throw new Exception($"not found game mode: {name}");
            RestoreScene = containsGameMode ? null : SceneManager.GetActiveScene().name;
            StartingGameMode = name;

#if DOMAIN_RELOAD_HANDLING
            // Warn: 도메인 리로드 비활성화 상태에서 EnterPlaymode로 진입 시 
            // BeforeSceneLoad 시점에서 UniTask 오퍼레이터가 동작하지 않음.
            // 임의로 도메인리로드를 플레이모드 진입 전에 활성화하던가 AfterSceneLoad에서부터
            // 오퍼레이터를 사용하면 된다.
            if (EditorSettings.enterPlayModeOptionsEnabled)
            {
                EditorSettings.enterPlayModeOptionsEnabled = false;
                EditorPrefs.SetBool(nameof(EditorSettings.enterPlayModeOptions), true);
            }
#endif
            EditorSceneManager.playModeStartScene = settings.MainScene;
            EditorApplication.EnterPlaymode();
        }

        [RuntimeInitializeOnLoadMethod]
        static void ResetPlayModeStartScene()
        {
#if DOMAIN_RELOAD_HANDLING
            if (EditorPrefs.GetBool(nameof(EditorSettings.enterPlayModeOptions)))
            {
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorPrefs.DeleteKey(nameof(EditorSettings.enterPlayModeOptions));
            }
#endif
            EditorSceneManager.playModeStartScene = null;
        }

        public static void StartGameModeFirst()
        {
            var modes = AppSettings.Instance.GameModes;
            StartGameMode(modes.FirstOrDefault()?.name);
        }
#endif
    }
}