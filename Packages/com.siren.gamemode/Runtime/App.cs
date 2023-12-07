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
                _startMode = settings.GameModes.FirstOrDefault(mode => mode.name == StartingGameMode);
                StartingGameMode = null;
            }
            else if (!string.IsNullOrWhiteSpace(StartingGameModeGuid))
            {
                var path = AssetDatabase.GUIDToAssetPath(StartingGameModeGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    _startMode = AssetDatabase.LoadAssetAtPath<ScriptableGameMode>(path);
                    _startMode.State = GameModeState.Ended;
                }

                StartingGameModeGuid = null;
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

        class BootstrapState : ScriptableSingleton<BootstrapState>
        {
            public string startingGameMode;
            public string restoreScene;
            public string startingGameModeGuid;
        }

        public static string StartingGameMode
        {
            get => BootstrapState.instance.startingGameMode;
            private set => BootstrapState.instance.startingGameMode = value;
        }

        public static string StartingGameModeGuid
        {
            get => BootstrapState.instance.startingGameModeGuid;
            private set => BootstrapState.instance.startingGameModeGuid = value;
        }

        public static string RestoreScene
        {
            get => BootstrapState.instance.restoreScene;
            set => BootstrapState.instance.restoreScene = value;
        }

        public static void StartGameMode(ScriptableGameMode mode)
        {
            if (Application.isPlaying) return;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mode.GetInstanceID(), out var guid, out long localId))
            {
                return;
            }

            StartingGameModeGuid = guid;
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AppSettings.Instance.MainScene);
            EditorSceneManager.playModeStartScene = sceneAsset;
            EditorApplication.EnterPlaymode();
        }

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
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(settings.MainScene);
            EditorSceneManager.playModeStartScene = sceneAsset;
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