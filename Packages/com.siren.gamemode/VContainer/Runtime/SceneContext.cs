using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace GameMode.VContainer
{
    public interface IConfigurable
    {
        void Configure(IContainerBuilder builder);
    }

    public partial class SceneContext : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (!Registers.TryGetValue(gameObject.scene, out var registers)) return;
            while (registers.Any())
            {
                registers.Dequeue().Configure(builder);
            }

            LoadedContexts[gameObject.scene] = this;
        }

        protected override void OnDestroy()
        {
            LoadedContexts.Remove(gameObject.scene);
            base.OnDestroy();
        }

        #region Statics

        private static readonly Dictionary<Scene, SceneContext> LoadedContexts = new();
        private static readonly Dictionary<Scene, Queue<IConfigurable>> Registers = new();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void InitDomainReload()
        {
            foreach (var pair in LoadedContexts)
            {
                pair.Value.Dispose();
            }

            LoadedContexts.Clear();
            Registers.Clear();
        }

        public static bool TryGetContext(Scene scene, out SceneContext context)
        {
            return LoadedContexts.TryGetValue(scene, out context);
        }

        public static void Register(Scene scene, IConfigurable configurable)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (!Registers.TryGetValue(scene, out var queue))
            {
                queue = new();
                Registers[scene] = queue;
            }

            queue.Enqueue(configurable);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            if (Registers.TryGetValue(scene, out var queue))
            {
                queue.Clear();
            }
        }

        #endregion
    }
}