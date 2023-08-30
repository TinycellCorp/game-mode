using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameMode
{
    public class AppSettings : ScriptableObject
    {
        public const int MainSceneIndex = 0;
#if UNITY_EDITOR
        public const string AppSettingsPath = "Assets/Settings/" + nameof(AppSettings) + ".asset";

        public static AppSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AppSettings>(AppSettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<AppSettings>();
                AssetDatabase.CreateAsset(settings, AppSettingsPath);
                AssetDatabase.SaveAssets();


                var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
                preloadedAssets.RemoveAll(x => x is AppSettings);
                preloadedAssets.Add(settings);
                PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
            }

            Instance = settings;

            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        public static void LoadInstanceFromPreloadAssets()
        {
            var preloadAsset = UnityEditor.PlayerSettings.GetPreloadedAssets().FirstOrDefault(x => x is AppSettings);
            if (preloadAsset is AppSettings instance)
            {
                instance.OnEnable();
            }
        }

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        [InitializeOnLoadMethod()]
        static void RuntimeInitialize()
        {
            // For editor, we need to load the Preload asset manually.
            LoadInstanceFromPreloadAssets();
        }
#endif
        void OnEnable()
        {
            Instance = this;
        }

        public static AppSettings Instance { get; private set; }

        [SerializeField] private bool skipInitialize = false;
        public bool SkipInitialize => skipInitialize;


#if UNITY_EDITOR
        [SerializeField] private SceneAsset mainScene;
        public SceneAsset MainScene => mainScene;
#endif
        [SerializeField] private ScriptableGameMode globalGameMode;
        [SerializeField] private ScriptableGameMode mainGameMode;

        [SerializeField] private List<ScriptableGameMode> gameModes;

        public ScriptableGameMode GlobalGameMode => globalGameMode;
        public ScriptableGameMode MainGameMode => mainGameMode;
        public List<ScriptableGameMode> GameModes => gameModes;
    }
}