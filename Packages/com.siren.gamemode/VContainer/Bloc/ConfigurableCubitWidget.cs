using System;
using UniBloc;
using UniBloc.Widgets;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameMode.VContainer.Bloc
{
    [DefaultExecutionOrder(ConfigurableMonoBehaviour.ExecutionOrder)]
    public abstract class ConfigurableCubitWidget<T, TCubit, TState> : CubitWidget<TCubit, TState>, IConfigurable
        where T : class
        where TCubit : Cubit<TState>, new()
        where TState : IEquatable<TState>, new()
    {
        protected override void Awake()
        {
            base.Awake();
            SceneContext.Register(gameObject.scene, this);
        }

        public virtual void Configure(IContainerBuilder builder)
        {
            var instance = GetRegisterInstance();
            builder.RegisterInstance(instance);
        }

        protected T GetRegisterInstance()
        {
            if (this is not T instance)
            {
                throw new Exception($"{gameObject.name} is not {typeof(T).Name}");
            }

            return instance;
        }

        public abstract class Scoped : ConfigurableCubitWidget<T, TCubit, TState>
        {
            public override void Configure(IContainerBuilder builder)
            {
                var instance = GetRegisterInstance();
                builder.Register(_ => instance, Lifetime.Scoped);
            }
        }

        public abstract class Aspect<TDescriptor> : ConfigurableCubitWidget<T, TCubit, TState>, IInitializable
            where TDescriptor : struct
        {
            protected TDescriptor Deps { get; private set; }

            public override void Configure(IContainerBuilder builder)
            {
                var instance = GetRegisterInstance();
                builder.RegisterInstance(instance).AsImplementedInterfaces();
                builder.Register<TDescriptor>(Lifetime.Scoped).AsSelf();
                builder.RegisterBuildCallback(OnBuild);
            }

            private void OnBuild(IObjectResolver resolver)
            {
                Deps = resolver.Resolve<TDescriptor>();
            }

            public abstract void Initialize();
        }
    }
}