using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameMode.VContainer
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public abstract class ConfigurableMonoBehaviour : MonoBehaviour, IConfigurable
    {
        public const int ExecutionOrder = -5001;
        public abstract void Configure(IContainerBuilder builder);

        protected virtual void Awake()
        {
            SceneContext.Register(gameObject.scene, this);
        }
    }


    public abstract class ConfigurableMonoBehaviour<T> : ConfigurableMonoBehaviour where T : ConfigurableMonoBehaviour
    {
        protected T GetRegisterInstance()
        {
            if (this is not T instance)
            {
                throw new Exception($"{gameObject.name} is not {typeof(T).Name}");
            }

            return instance;
        }


        public override void Configure(IContainerBuilder builder)
        {
            var instance = GetRegisterInstance();
            builder.RegisterInstance(instance);
        }

        public abstract class Scoped : ConfigurableMonoBehaviour<T>
        {
            public override void Configure(IContainerBuilder builder)
            {
                var instance = GetRegisterInstance();
                builder.Register(_ => instance, Lifetime.Scoped);
            }
        }

        public abstract class Aspect<TDependencies> : ConfigurableMonoBehaviour<T>, IInitializable
            where TDependencies : struct
        {
            protected TDependencies Deps { get; private set; }

            public override void Configure(IContainerBuilder builder)
            {
                var instance = GetRegisterInstance();
                builder.RegisterInstance(instance).AsImplementedInterfaces();
                builder.Register<TDependencies>(Lifetime.Scoped).AsSelf();
                builder.RegisterBuildCallback(OnBuild);
            }

            private void OnBuild(IObjectResolver resolver)
            {
                Deps = resolver.Resolve<TDependencies>();
            }

            public abstract void Initialize();
        }
    }
}