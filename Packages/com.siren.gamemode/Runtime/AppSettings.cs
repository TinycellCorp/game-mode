using System.Collections.Generic;
using System.Linq;
using GameMode.PropertyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameMode
{
    public partial class AppSettings : ScriptableObject
    {
        public const int MainSceneIndex = 0;

        void OnEnable()
        {
            Instance = this;
        }

        public static AppSettings Instance { get; private set; }

        [SerializeField] private bool skipInitialize = false;
        public bool SkipInitialize => skipInitialize;

        [ScenePath] [SerializeField] private string mainScene;
        public string MainScene => mainScene;

        [SerializeField] private List<ScriptableGameMode> gameModes;

        public List<ScriptableGameMode> GameModes => gameModes;
    }
#if UNITY_EDITOR

    public partial class AppSettings
    {
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
        [InitializeOnLoadMethod]
        static void RuntimeInitialize()
        {
            // For editor, we need to load the Preload asset manually.
            LoadInstanceFromPreloadAssets();
        }
    }
#endif
}