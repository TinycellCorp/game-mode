using System;
using UniBloc;
using UniBloc.Widgets;
using UnityEngine;
using VContainer;

namespace GameMode.VContainer.Bloc
{
    [DefaultExecutionOrder(ConfigurableMonoBehaviour.ExecutionOrder)]
    public abstract class ConfigurableValueBlocWidget<T, TBloc, TID, TEvent, TState> :
        ValueBlocWidget<TBloc, TID, TEvent, TState>,
        IConfigurable
        where T : class
        where TBloc : ValueBloc<TID, TEvent, TState>, new()
        where TID : IEquatable<TID>
        where TEvent : struct, IEquatable<TEvent>, IEventEntity<TID>
        where TState : struct, IEquatable<TState>
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

        public abstract class Scoped : ConfigurableValueBlocWidget<T, TBloc, TID, TEvent, TState>
        {
            public override void Configure(IContainerBuilder builder)
            {
                var instance = GetRegisterInstance();
                builder.Register(_ => instance, Lifetime.Scoped);
            }
        }
    }
}